using System.Globalization;
using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// "Bewaar als standaard" on an NPC role: save the plot, then keep that role's name, clothing and casting
/// as a standaard-NPC an SL can add to the next plot (see <see cref="StandaardNpc"/>).
///
/// It rides on the regular pagina form rather than a form of its own: a separate post would leave the
/// page and lose unsaved edits, and an SL saves the plot anyway. The role's values arrive in three hidden
/// fields that npc.js fills from the clicked row, because a row's index is no reliable position in
/// <see cref="EditableNpcRollen"/> once roles have been added and removed.
/// </summary>
public sealed partial class PlotboekModel
{
    /// <summary>Role name of the role being saved as a standaard-NPC.</summary>
    [BindProperty]
    public string? StandaardNpcRol { get; set; }

    /// <summary>Clothing and makeup of the role being saved as a standaard-NPC.</summary>
    [BindProperty]
    public string? StandaardNpcKledingEnSchmink { get; set; }

    /// <summary>Casting wishes of the role being saved as a standaard-NPC.</summary>
    [BindProperty]
    public string? StandaardNpcCastingInstructie { get; set; }

    /// <summary>Confirmation shown under the NPC roles after a role was saved as a standaard-NPC.</summary>
    public string StandaardNpcMelding { get; set; } = string.Empty;

    /// <summary>Why a role could not be saved as a standaard-NPC; empty when it could.</summary>
    public string StandaardNpcFout { get; set; } = string.Empty;

    private async Task<IActionResult> HandleBewaarStandaardNpc()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        DomainValidationMessage = null;

        if (string.IsNullOrEmpty(StandaardNpc.IdVoor(StandaardNpcRol ?? string.Empty)))
        {
            logger.LogWarning("Standaard-NPC not saved from pagina {PaginaId}: the role has no usable name", PaginaId);
            StandaardNpcFout = PaginaUi.StandaardNpcRolZonderNaam;
            return Page();
        }

        if (!PaginaId.HasValue || string.IsNullOrWhiteSpace(EditableTitle))
        {
            logger.LogWarning("Standaard-NPC not saved from pagina {PaginaId}: the pagina has no title, so it was not saved", PaginaId);
            StandaardNpcFout = PaginaUi.StandaardNpcPlotZonderNaam;
            return Page();
        }

        // The plot first: if it doesn't save (someone else changed it), the conflict screen takes over
        // and the standaard-NPC waits until the SL has sorted that out.
        if (!await SavePaginaAndReload())
        {
            return Page();
        }

        using var session = documentStore.OpenSession();
        var gebruiker = await User.GetUserAsync(session);
        var standaardNpc = StandaardNpc.Create(
            StandaardNpcRol!,
            StandaardNpcKledingEnSchmink ?? string.Empty,
            StandaardNpcCastingInstructie ?? string.Empty,
            gebruiker?.UserName ?? gebruiker?.Email ?? User.Identity?.Name ?? string.Empty,
            DateTime.UtcNow);

        // Store, not Insert: overwriting an existing standaard-NPC is the point here. The SL confirmed it
        // in the browser, and plots keep their own copy, so no plot changes.
        session.Store(standaardNpc);
        await session.SaveChangesAsync();

        logger.LogInformation("{User} saved standaard-NPC {StandaardNpcId} from pagina {PaginaId}", User.Identity?.Name, standaardNpc.Id, PaginaId);

        AvailableStandaardNpcs = [.. AvailableStandaardNpcs
            .Where(s => s.Id != standaardNpc.Id)
            .Append(standaardNpc)
            .OrderBy(s => s.Rol, StringComparer.CurrentCultureIgnoreCase)];
        StandaardNpcMelding = string.Format(CultureInfo.CurrentCulture, PaginaUi.StandaardNpcBewaard, standaardNpc.Rol);
        return Page();
    }
}
