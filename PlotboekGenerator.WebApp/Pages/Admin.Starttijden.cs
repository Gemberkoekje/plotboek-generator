using System.Globalization;
using System.Text.RegularExpressions;
using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// Admin management of the starttijd reference data: which slots exist, on which day types they can be
/// picked, and the block window each one carries there. Slots are never deleted — a stored plot must keep
/// resolving its id — so retiring one means clearing all three availability boxes.
///
/// The order is not edited: it follows the time the display name starts with, so a new slot slides in
/// between the existing ones without anyone renumbering them.
/// </summary>
public sealed partial class AdminModel
{
    /// <summary>Empty for a new slot; set means editing an existing one. Ids are never changed.</summary>
    [BindProperty]
    public string? EditStarttijdId { get; set; }

    [BindProperty]
    public string? EditStarttijdNaam { get; set; }

    [BindProperty]
    public bool EditStarttijdEersteDag { get; set; }

    [BindProperty]
    public bool EditStarttijdMiddelDagen { get; set; }

    [BindProperty]
    public bool EditStarttijdLaatsteDag { get; set; }

    [BindProperty]
    public string? EditStarttijdBlokVensterEersteDag { get; set; }

    [BindProperty]
    public string? EditStarttijdBlokVensterMiddelDagen { get; set; }

    [BindProperty]
    public string? EditStarttijdBlokVensterLaatsteDag { get; set; }

    /// <summary>Every slot including the "None" sentinel, in the order the plot page shows them.</summary>
    public List<StarttijdConfig> Starttijden { get; set; } = [];

    private async Task LoadStarttijden()
    {
        using var session = documentStore.OpenSession();

        var starttijden = await session.QueryAllAsync<StarttijdConfig>();
        Starttijden = [.. starttijden.OrderBy(s => s.Order).ThenBy(s => s.DisplayName, StringComparer.Ordinal)];
    }

    internal async Task HandleSaveStarttijd()
    {
        if (string.IsNullOrWhiteSpace(EditStarttijdNaam))
        {
            ErrorMessage = AdminUi.NaamVerplicht;
            return;
        }

        var naam = EditStarttijdNaam.Trim();

        // The time is not decoration: the key, the order and the export's time schedule all read it back
        // out of the name, so a name without one would sort last and yield an unusable key.
        var afgeleideId = DeriveStarttijdId(naam);
        if (afgeleideId.Length == 0)
        {
            ErrorMessage = AdminUi.StarttijdZonderTijd;
            return;
        }

        using var session = documentStore.OpenSession();

        var opgeslagen = await session.QueryAllAsync<StarttijdConfig>();
        var opgeslagenById = opgeslagen.ToDictionary(s => s.Id, StringComparer.Ordinal);

        if (HasAnderSlotMetNaam(opgeslagen, naam))
        {
            ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.StarttijdNaamBestaat, naam);
            return;
        }

        var alle = opgeslagen.ToList();
        var nieuweId = string.Empty;

        if (!string.IsNullOrWhiteSpace(EditStarttijdId))
        {
            if (!opgeslagenById.TryGetValue(EditStarttijdId, out var existing))
            {
                ErrorMessage = AdminUi.StarttijdNietGevonden;
                return;
            }

            if (existing.IsNone)
            {
                ErrorMessage = AdminUi.StarttijdLegeWaarde;
                return;
            }

            var gewijzigd = WithStarttijdFormValues(existing, naam);
            alle = [.. alle.Select(s => string.Equals(s.Id, existing.Id, StringComparison.Ordinal) ? gewijzigd : s)];

            logger.LogInformation("Admin {AdminUser} updated starttijd {StarttijdId}: {Naam}", User.Identity?.Name, existing.Id, naam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.StarttijdBijgewerkt, naam);
        }
        else
        {
            if (opgeslagenById.ContainsKey(afgeleideId))
            {
                ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.StarttijdSleutelBestaat, afgeleideId);
                return;
            }

            nieuweId = afgeleideId;

            // EnumValue only exists to map the legacy Starttijd enum of old events onto a slot. A new slot
            // has no enum member, so any number nobody else uses will do.
            var maxEnumValue = alle.Select(s => s.EnumValue).DefaultIfEmpty(0).Max();

            alle.Add(WithStarttijdFormValues(
                new StarttijdConfig
                {
                    Id = afgeleideId,
                    EnumValue = maxEnumValue + 1,
                    DisplayName = naam,
                    IsAvailableOnEersteDag = false,
                    IsAvailableOnMiddelDagen = false,
                    IsAvailableOnLaatsteDag = false,
                    Order = 0,
                    IsNone = false,
                },
                naam));

            logger.LogInformation("Admin {AdminUser} created starttijd {StarttijdId}: {Naam}", User.Identity?.Name, afgeleideId, naam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.StarttijdAangemaakt, naam);
        }

        StoreStarttijden(session, alle.ResequenceOrder(), opgeslagenById, nieuweId);

        try
        {
            await session.SaveChangesAsync();
        }
        catch (DocumentAlreadyExistsException ex)
        {
            logger.LogWarning(ex, "Admin {AdminUser} lost a create race for starttijd {StarttijdId}", User.Identity?.Name, afgeleideId);
            SuccessMessage = null;
            ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.StarttijdSleutelBestaatVerversen, afgeleideId);
            return;
        }

        ClearStarttijdForm();
    }

    /// <summary>
    /// Whether a slot other than the one being edited already carries this name. Two slots under one name
    /// are indistinguishable in every dropdown, list and time schedule. On create the derived key already
    /// collides, but a rename can walk onto another slot's name.
    /// </summary>
    private bool HasAnderSlotMetNaam(IEnumerable<StarttijdConfig> alle, string naam)
        => alle.Any(s => !string.Equals(s.Id, EditStarttijdId, StringComparison.Ordinal)
            && string.Equals(s.DisplayName, naam, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Writes back only what really differs from what is stored: the new row (inserted, so a lost create
    /// race fails instead of overwriting), the edited one, and whatever slots a new time pushed down.
    /// </summary>
    private static void StoreStarttijden(
        IDocumentSession session,
        IReadOnlyList<StarttijdConfig> alle,
        Dictionary<string, StarttijdConfig> opgeslagenById,
        string nieuweId)
    {
        foreach (var config in alle)
        {
            if (string.Equals(config.Id, nieuweId, StringComparison.Ordinal))
            {
                // Insert, not Store: the check in the caller can only see what was committed when it ran,
                // so a second request creating the same slot would otherwise overwrite the first record.
                session.Insert(config);
            }
            else if (!opgeslagenById.TryGetValue(config.Id, out var stored) || stored != config)
            {
                session.Store(config);
            }
        }
    }

    private StarttijdConfig WithStarttijdFormValues(StarttijdConfig config, string naam)
        => config with
        {
            DisplayName = naam,
            IsAvailableOnEersteDag = EditStarttijdEersteDag,
            IsAvailableOnMiddelDagen = EditStarttijdMiddelDagen,
            IsAvailableOnLaatsteDag = EditStarttijdLaatsteDag,
            BlokVensterEersteDag = EditStarttijdBlokVensterEersteDag?.Trim() ?? string.Empty,
            BlokVensterMiddelDagen = EditStarttijdBlokVensterMiddelDagen?.Trim() ?? string.Empty,
            BlokVensterLaatsteDag = EditStarttijdBlokVensterLaatsteDag?.Trim() ?? string.Empty,

            // Whatever the admin left in the three window boxes is the truth from here on, empty included:
            // without this the seed would read three cleared boxes as a row it still has to backfill.
            BlokVenstersIngesteld = true,
        };

    private void ClearStarttijdForm()
    {
        EditStarttijdId = null;
        EditStarttijdNaam = null;
        EditStarttijdEersteDag = false;
        EditStarttijdMiddelDagen = false;
        EditStarttijdLaatsteDag = false;
        EditStarttijdBlokVensterEersteDag = null;
        EditStarttijdBlokVensterMiddelDagen = null;
        EditStarttijdBlokVensterLaatsteDag = null;
    }

    /// <summary>
    /// The slots of one day type as the plot page numbers them: "1. 11:30 (11:30 - 13:30) · 2. 13:30:
    /// Eindscène (13:30 - 15:30)". A slot without a window is listed without a number, the way
    /// "19:30: Introquest" never got one, so it is visible why the numbering skips it.
    /// </summary>
    internal string BuildStarttijdOverzicht(MeetingDagType dagType)
    {
        var beschikbaar = Starttijden.GetAvailable(dagType);
        if (beschikbaar.Count == 0)
        {
            return AdminUi.GeenSlotsBeschikbaar;
        }

        return string.Join(" · ", beschikbaar.Select(config =>
        {
            var nummer = Starttijden.GetBlokNummer(dagType, config.Id);

            return nummer == 0
                ? string.Format(CultureInfo.CurrentCulture, AdminUi.SlotZonderBlok, config.DisplayName)
                : $"{nummer}. {config.DisplayName} ({config.GetBlokVenster(dagType)})";
        }));
    }

    /// <summary>
    /// Derives the stable key for a new slot from its name: "Tijd" plus the four digits of the time the
    /// name starts with ("11:30" -&gt; "Tijd1130", "9:30" -&gt; "Tijd0930", "13:30: Eindscène" -&gt;
    /// "Tijd1330"). Returns an empty string when the name does not start with a valid time.
    ///
    /// Only ever called when CREATING a slot: renaming must not change the id, because stored plots
    /// reference it.
    /// </summary>
    internal static string DeriveStarttijdId(string naam)
    {
        if (string.IsNullOrWhiteSpace(naam))
        {
            return string.Empty;
        }

        var match = StarttijdTimeRegex().Match(naam);
        if (!match.Success)
        {
            return string.Empty;
        }

        var uren = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minuten = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

        return uren > 23 || minuten > 59
            ? string.Empty
            : $"Tijd{uren:00}{minuten:00}";
    }

    [GeneratedRegex(@"^\s*(\d{1,2}):(\d{2})")]
    private static partial Regex StarttijdTimeRegex();
}
