namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// Admin management of the standaard-NPC's: the role templates SLs save from a plot and pick in the
/// next one. Plots keep their own copy, so editing, renaming or deleting here never changes a plot.
/// </summary>
public sealed partial class AdminModel
{
    /// <summary>Empty for a new standaard-NPC; set to the current key when editing one.</summary>
    [BindProperty]
    public string? EditStandaardNpcId { get; set; }

    [BindProperty]
    public string? EditStandaardNpcRol { get; set; }

    [BindProperty]
    public string? EditStandaardNpcKledingEnSchmink { get; set; }

    [BindProperty]
    public string? EditStandaardNpcCastingInstructie { get; set; }

    [BindProperty]
    public string? DeleteStandaardNpcId { get; set; }

    public List<StandaardNpc> StandaardNpcs { get; set; } = [];

    private async Task LoadStandaardNpcs()
    {
        using var session = documentStore.OpenSession();
        var standaardNpcs = await session.QueryAllAsync<StandaardNpc>();
        StandaardNpcs = [.. standaardNpcs.OrderBy(s => s.Rol, StringComparer.CurrentCultureIgnoreCase)];
    }

    internal async Task HandleSaveStandaardNpc()
    {
        if (string.IsNullOrWhiteSpace(EditStandaardNpcRol))
        {
            ErrorMessage = "Rol is verplicht.";
            return;
        }

        var nieuwId = StandaardNpc.IdVoor(EditStandaardNpcRol);
        if (string.IsNullOrEmpty(nieuwId))
        {
            ErrorMessage = "Uit deze naam kan geen geldige sleutel worden afgeleid. Gebruik letters of cijfers.";
            return;
        }

        using var session = documentStore.OpenSession();
        var gebruiker = await User.GetUserAsync(session);
        var standaardNpc = StandaardNpc.Create(
            EditStandaardNpcRol,
            EditStandaardNpcKledingEnSchmink ?? string.Empty,
            EditStandaardNpcCastingInstructie ?? string.Empty,
            gebruiker?.UserName ?? gebruiker?.Email ?? User.Identity?.Name ?? string.Empty,
            DateTime.UtcNow);

        var origineelId = EditStandaardNpcId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(origineelId))
        {
            if (await session.LoadAsync<StandaardNpc>(nieuwId) != null)
            {
                ErrorMessage = $"Er bestaat al een standaard-NPC '{standaardNpc.Rol}'.";
                return;
            }

            // Insert, not Store: a second admin creating the same name at the same time must not
            // silently overwrite the first one's record.
            session.Insert(standaardNpc);
            logger.LogInformation("Admin {AdminUser} created standaard-NPC {StandaardNpcId}", User.Identity?.Name, nieuwId);
            SuccessMessage = $"Standaard-NPC '{standaardNpc.Rol}' aangemaakt.";
        }
        else
        {
            if (await session.LoadAsync<StandaardNpc>(origineelId) == null)
            {
                ErrorMessage = "Standaard-NPC niet gevonden.";
                return;
            }

            if (string.Equals(origineelId, nieuwId, StringComparison.Ordinal))
            {
                session.Store(standaardNpc);
            }
            else
            {
                // Unlike questlocaties, the key follows the name: nothing references it, and a key that
                // no longer matches its name would let saving "Stadswacht" overwrite a record that is
                // now called "Wachter".
                if (await session.LoadAsync<StandaardNpc>(nieuwId) != null)
                {
                    ErrorMessage = $"Er bestaat al een standaard-NPC '{standaardNpc.Rol}'.";
                    return;
                }

                session.Delete<StandaardNpc>(origineelId);
                session.Insert(standaardNpc);
            }

            logger.LogInformation("Admin {AdminUser} updated standaard-NPC {StandaardNpcId} (was {OrigineelId})", User.Identity?.Name, nieuwId, origineelId);
            SuccessMessage = $"Standaard-NPC '{standaardNpc.Rol}' bijgewerkt.";
        }

        await session.SaveChangesAsync();

        EditStandaardNpcId = null;
        EditStandaardNpcRol = null;
        EditStandaardNpcKledingEnSchmink = null;
        EditStandaardNpcCastingInstructie = null;
    }

    internal async Task HandleDeleteStandaardNpc()
    {
        if (string.IsNullOrWhiteSpace(DeleteStandaardNpcId))
        {
            ErrorMessage = "Geen standaard-NPC geselecteerd om te verwijderen.";
            return;
        }

        using var session = documentStore.OpenSession();
        var standaardNpc = await session.LoadAsync<StandaardNpc>(DeleteStandaardNpcId);
        if (standaardNpc == null)
        {
            ErrorMessage = "Standaard-NPC niet gevonden.";
            return;
        }

        session.Delete(standaardNpc);
        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} deleted standaard-NPC {StandaardNpcId}", User.Identity?.Name, DeleteStandaardNpcId);
        SuccessMessage = $"Standaard-NPC '{standaardNpc.Rol}' verwijderd.";
    }
}
