using System.Globalization;
using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// Admin management of the questlocatie reference data: the categories that become the optgroups of the
/// picker, and the locations inside them. Locations are never deleted — a stored plot must keep resolving
/// its id — so retiring one means clearing "actief".
/// </summary>
public sealed partial class AdminModel
{
    // ── Categorie form ────────────────────────────────────────────────────────

    /// <summary>Empty for a new category; set means renaming an existing one. Ids are never changed.</summary>
    [BindProperty]
    public string? EditQuestLocatieCategorieId { get; set; }

    [BindProperty]
    public string? EditQuestLocatieCategorieNaam { get; set; }

    [BindProperty]
    public string? EditQuestLocatieCategorieNotitie { get; set; }

    [BindProperty]
    public QuestLocatieErnst EditQuestLocatieCategorieErnst { get; set; } = QuestLocatieErnst.Geen;

    [BindProperty]
    public int EditQuestLocatieCategorieOrder { get; set; }

    // ── Locatie form ──────────────────────────────────────────────────────────

    /// <summary>Empty for a new location; set means editing an existing one. Ids are never changed.</summary>
    [BindProperty]
    public string? EditQuestLocatieId { get; set; }

    [BindProperty]
    public string? EditQuestLocatieNaam { get; set; }

    [BindProperty]
    public string? EditQuestLocatieCategorie { get; set; }

    [BindProperty]
    public string? EditQuestLocatieNotitie { get; set; }

    [BindProperty]
    public int EditQuestLocatieOrder { get; set; }

    [BindProperty]
    public bool EditQuestLocatieIsActief { get; set; } = true;

    [BindProperty]
    public List<string> EditQuestLocatieConflicteertMet { get; set; } = [];

    public List<QuestLocatieCategorieConfig> QuestLocatieCategorieen { get; set; } = [];

    public List<QuestLocatieConfig> QuestLocaties { get; set; } = [];

    /// <summary>
    /// The symmetric overlap map, so the list can show the implied reverse relations the admin never typed.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> QuestLocatieOverlap { get; set; }
        = new Dictionary<string, IReadOnlyList<string>>();

    private async Task LoadQuestLocaties()
    {
        using var session = documentStore.OpenSession();

        var categorieen = await session.QueryAllAsync<QuestLocatieCategorieConfig>();
        QuestLocatieCategorieen = [.. categorieen.OrderBy(c => c.Order).ThenBy(c => c.DisplayName, StringComparer.Ordinal)];

        var locaties = await session.QueryAllAsync<QuestLocatieConfig>();
        QuestLocaties = [.. locaties.OrderBy(l => l.Order).ThenBy(l => l.DisplayName, StringComparer.Ordinal)];
        QuestLocatieOverlap = QuestLocaties.BuildSymmetrischeOverlap();
    }

    private async Task HandleSaveQuestLocatieCategorie()
    {
        if (string.IsNullOrWhiteSpace(EditQuestLocatieCategorieNaam))
        {
            ErrorMessage = AdminUi.NaamVerplicht;
            return;
        }

        var naam = EditQuestLocatieCategorieNaam.Trim();
        var notitie = EditQuestLocatieCategorieNotitie?.Trim() ?? string.Empty;
        var ernst = EditQuestLocatieCategorieErnst == QuestLocatieErnst.None
            ? QuestLocatieErnst.Geen
            : EditQuestLocatieCategorieErnst;

        using var session = documentStore.OpenSession();

        if (!string.IsNullOrWhiteSpace(EditQuestLocatieCategorieId))
        {
            var existing = await session.LoadAsync<QuestLocatieCategorieConfig>(EditQuestLocatieCategorieId);
            if (existing == null)
            {
                ErrorMessage = AdminUi.CategorieNietGevonden;
                return;
            }

            session.Store(existing with
            {
                DisplayName = naam,
                Notitie = notitie,
                Ernst = ernst,
                Order = EditQuestLocatieCategorieOrder,
            });
            logger.LogInformation("Admin {AdminUser} updated questlocatie categorie {CategorieId}: {Naam}", User.Identity?.Name, EditQuestLocatieCategorieId, naam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.CategorieBijgewerkt, naam);
        }
        else
        {
            var id = ReferenceDataId.Derive(naam);
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = AdminUi.GeenGeldigeSleutel;
                return;
            }

            if (await session.LoadAsync<QuestLocatieCategorieConfig>(id) != null)
            {
                ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.CategorieSleutelBestaat, id);
                return;
            }

            var maxOrder = QuestLocatieCategorieen.Count > 0 ? QuestLocatieCategorieen.Max(c => c.Order) : 0;

            // Insert, not Store: the check above can only see what was committed when it ran, so a
            // second request creating the same name would otherwise overwrite the first one's record.
            session.Insert(new QuestLocatieCategorieConfig
            {
                Id = id,
                DisplayName = naam,
                Notitie = notitie,
                Ernst = ernst,
                Order = EditQuestLocatieCategorieOrder > 0 ? EditQuestLocatieCategorieOrder : maxOrder + 1,
            });
            logger.LogInformation("Admin {AdminUser} created questlocatie categorie {CategorieId}: {Naam}", User.Identity?.Name, id, naam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.CategorieAangemaakt, naam);
        }

        try
        {
            await session.SaveChangesAsync();
        }
        catch (DocumentAlreadyExistsException ex)
        {
            logger.LogWarning(ex, "Admin {AdminUser} lost a create race for questlocatie categorie {Naam}", User.Identity?.Name, naam);
            SuccessMessage = null;
            ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.CategorieNaamBestaatVerversen, naam);
            return;
        }

        ClearQuestLocatieCategorieForm();
    }

    private async Task HandleSaveQuestLocatie()
    {
        if (string.IsNullOrWhiteSpace(EditQuestLocatieNaam))
        {
            ErrorMessage = AdminUi.NaamVerplicht;
            return;
        }

        if (string.IsNullOrWhiteSpace(EditQuestLocatieCategorie))
        {
            ErrorMessage = AdminUi.CategorieVerplicht;
            return;
        }

        var naam = EditQuestLocatieNaam.Trim();
        var notitie = EditQuestLocatieNotitie?.Trim() ?? string.Empty;

        using var session = documentStore.OpenSession();

        if (await session.LoadAsync<QuestLocatieCategorieConfig>(EditQuestLocatieCategorie) == null)
        {
            ErrorMessage = AdminUi.CategorieNietGevonden;
            return;
        }

        // Only keep overlaps that resolve, and never let a location overlap with itself.
        var conflicteertMet = (EditQuestLocatieConflicteertMet ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id) && !string.Equals(id, EditQuestLocatieId, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Where(id => QuestLocaties.Any(l => string.Equals(l.Id, id, StringComparison.Ordinal)))
            .ToList();

        if (!string.IsNullOrWhiteSpace(EditQuestLocatieId))
        {
            var existing = await session.LoadAsync<QuestLocatieConfig>(EditQuestLocatieId);
            if (existing == null)
            {
                ErrorMessage = AdminUi.QuestLocatieNietGevonden;
                return;
            }

            session.Store(existing with
            {
                DisplayName = naam,
                CategorieId = EditQuestLocatieCategorie,
                Notitie = notitie,
                ConflicteertMet = conflicteertMet,
                IsActief = EditQuestLocatieIsActief,
                Order = EditQuestLocatieOrder,
            });
            logger.LogInformation("Admin {AdminUser} updated questlocatie {LocatieId}: {Naam}", User.Identity?.Name, EditQuestLocatieId, naam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.QuestLocatieBijgewerkt, naam);
        }
        else
        {
            var id = ReferenceDataId.Derive(naam);
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = AdminUi.GeenGeldigeSleutel;
                return;
            }

            if (await session.LoadAsync<QuestLocatieConfig>(id) != null)
            {
                ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.QuestLocatieSleutelBestaat, id);
                return;
            }

            var maxOrder = QuestLocaties
                .Where(l => string.Equals(l.CategorieId, EditQuestLocatieCategorie, StringComparison.Ordinal))
                .Select(l => l.Order)
                .DefaultIfEmpty(0)
                .Max();

            // Insert, not Store: the check above can only see what was committed when it ran, so a
            // second request creating the same name would otherwise overwrite the first one's record.
            session.Insert(new QuestLocatieConfig
            {
                Id = id,
                DisplayName = naam,
                CategorieId = EditQuestLocatieCategorie,
                Notitie = notitie,
                ConflicteertMet = conflicteertMet,
                IsActief = EditQuestLocatieIsActief,
                Order = EditQuestLocatieOrder > 0 ? EditQuestLocatieOrder : maxOrder + 1,
            });
            logger.LogInformation("Admin {AdminUser} created questlocatie {LocatieId}: {Naam}", User.Identity?.Name, id, naam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.QuestLocatieAangemaakt, naam);
        }

        try
        {
            await session.SaveChangesAsync();
        }
        catch (DocumentAlreadyExistsException ex)
        {
            logger.LogWarning(ex, "Admin {AdminUser} lost a create race for questlocatie {Naam}", User.Identity?.Name, naam);
            SuccessMessage = null;
            ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.QuestLocatieNaamBestaatVerversen, naam);
            return;
        }

        ClearQuestLocatieForm();
    }

    private void ClearQuestLocatieCategorieForm()
    {
        EditQuestLocatieCategorieId = null;
        EditQuestLocatieCategorieNaam = null;
        EditQuestLocatieCategorieNotitie = null;
        EditQuestLocatieCategorieErnst = QuestLocatieErnst.Geen;
        EditQuestLocatieCategorieOrder = 0;
    }

    private void ClearQuestLocatieForm()
    {
        EditQuestLocatieId = null;
        EditQuestLocatieNaam = null;
        EditQuestLocatieCategorie = null;
        EditQuestLocatieNotitie = null;
        EditQuestLocatieOrder = 0;
        EditQuestLocatieIsActief = true;
        EditQuestLocatieConflicteertMet = [];
    }
}
