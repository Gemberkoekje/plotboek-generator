using System.Globalization;
using System.Text.RegularExpressions;

namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Extension methods for looking up reference data config records by string ID or enum value.
/// </summary>
public static partial class ReferenceDataExtensions
{
    // ── StarttijdConfig ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="StarttijdConfig"/> with the given <paramref name="id"/>,
    /// or <c>null</c> when not found.
    /// </summary>
    public static StarttijdConfig FindById(this IEnumerable<StarttijdConfig> configs, string id)
        => configs.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Returns the <see cref="StarttijdConfig"/> whose <see cref="StarttijdConfig.EnumValue"/>
    /// matches the given <paramref name="enumValue"/>, or <c>null</c> when not found.
    /// </summary>
    public static StarttijdConfig FindByEnumValue(this IEnumerable<StarttijdConfig> configs, int enumValue)
        => configs.FirstOrDefault(c => c.EnumValue == enumValue);

    /// <summary>
    /// Returns the display name for the given <paramref name="id"/>,
    /// falling back to the raw <paramref name="id"/> when no config is found.
    /// </summary>
    public static string GetDisplayName(this IEnumerable<StarttijdConfig> configs, string id)
        => configs.FindById(id)?.DisplayName ?? id;

    /// <summary>
    /// Returns whether this slot is selectable on the given day type.
    /// </summary>
    public static bool IsAvailableOn(this StarttijdConfig config, MeetingDagType dagType)
        => config is not null && dagType switch
        {
            MeetingDagType.EersteDag => config.IsAvailableOnEersteDag,
            MeetingDagType.LaatseDag => config.IsAvailableOnLaatsteDag,
            _ => config.IsAvailableOnMiddelDagen,
        };

    /// <summary>
    /// Returns all configs that are selectable on the given day type, ordered by
    /// <see cref="StarttijdConfig.Order"/>. The "None" sentinel is never included.
    /// </summary>
    public static IReadOnlyList<StarttijdConfig> GetAvailable(
        this IEnumerable<StarttijdConfig> configs,
        MeetingDagType dagType)
        => [.. (configs ?? [])
            .Where(c => !c.IsNone && c.IsAvailableOn(dagType))
            .OrderBy(c => c.Order)];

    /// <summary>
    /// Returns the block window for the given day type (e.g. "18:00 - 21:00"), or an empty string when
    /// this slot has no window on that day type. The same slot can carry different windows per day type,
    /// because 20:00 runs from 18:00 on the first day but from 19:00 on the days after it.
    /// </summary>
    public static string GetBlokVenster(this StarttijdConfig config, MeetingDagType dagType)
        => config is null
            ? string.Empty
            : dagType switch
            {
                MeetingDagType.EersteDag => config.BlokVensterEersteDag ?? string.Empty,
                MeetingDagType.LaatseDag => config.BlokVensterLaatsteDag ?? string.Empty,
                _ => config.BlokVensterMiddelDagen ?? string.Empty,
            };

    /// <summary>
    /// Returns the block number of the slot with the given <paramref name="id"/> on the given day type:
    /// its 1-based position among the slots that are both available on that day type and have a block
    /// window. Returns <c>0</c> when the slot has no number — unknown, not available that day, or
    /// window-less (the way "19:30: Introquest" is skipped so 20:00 stays block 1).
    /// </summary>
    public static int GetBlokNummer(
        this IEnumerable<StarttijdConfig> configs,
        MeetingDagType dagType,
        string id)
    {
        var genummerd = configs
            .GetAvailable(dagType)
            .Where(c => !string.IsNullOrWhiteSpace(c.GetBlokVenster(dagType)))
            .ToList();

        return genummerd.FindIndex(c => string.Equals(c.Id, id, StringComparison.Ordinal)) + 1;
    }

    /// <summary>
    /// Returns the sort key derived from the first "H:MM" in the display name, as minutes since midnight.
    /// Hours below 6 count as the next day, so "00:00" sorts after "22:00". A name without a time sorts
    /// last. Mirrors the rule the export uses for the time schedule, so both agree on slot order.
    /// </summary>
    public static int GetSortMinutes(this StarttijdConfig config)
        => ParseSortMinutes(config?.DisplayName);

    /// <summary>
    /// Returns the sort key for a display name; see <see cref="GetSortMinutes"/>.
    /// </summary>
    public static int ParseSortMinutes(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return int.MaxValue;
        }

        var match = TimeRegex().Match(displayName);
        if (!match.Success)
        {
            return int.MaxValue;
        }

        var hours = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minutes = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var totalMinutes = (hours * 60) + minutes;

        return hours < 6 ? totalMinutes + (24 * 60) : totalMinutes;
    }

    /// <summary>
    /// Returns every config with its <see cref="StarttijdConfig.Order"/> rewritten to the position implied
    /// by the time in its display name (1..n, the "None" sentinel keeps 0), so a new slot slides into
    /// place without anyone renumbering the others. Records that already carry the right order are
    /// returned untouched, which lets callers store only what actually changed. Every consumer treats
    /// <c>Order</c> as relative, so resequencing is safe.
    /// </summary>
    public static IReadOnlyList<StarttijdConfig> ResequenceOrder(this IEnumerable<StarttijdConfig> configs)
    {
        var all = configs?.ToList() ?? [];

        return
        [
            .. all
                .Where(c => c.IsNone)
                .Select(c => c.Order == 0 ? c : c with { Order = 0 }),
            .. all
                .Where(c => !c.IsNone)
                .OrderBy(c => c.GetSortMinutes())
                .ThenBy(c => c.DisplayName, StringComparer.Ordinal)
                .Select((c, index) => c.Order == index + 1 ? c : c with { Order = index + 1 }),
        ];
    }

    [GeneratedRegex(@"(\d{1,2}):(\d{2})")]
    private static partial Regex TimeRegex();

    // ── AantalBlokkenConfig ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="AantalBlokkenConfig"/> with the given <paramref name="id"/>,
    /// or <c>null</c> when not found.
    /// </summary>
    public static AantalBlokkenConfig FindById(this IEnumerable<AantalBlokkenConfig> configs, string id)
        => configs.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Returns the <see cref="AantalBlokkenConfig"/> whose <see cref="AantalBlokkenConfig.EnumValue"/>
    /// matches the given <paramref name="enumValue"/>, or <c>null</c> when not found.
    /// </summary>
    public static AantalBlokkenConfig FindByEnumValue(this IEnumerable<AantalBlokkenConfig> configs, int enumValue)
        => configs.FirstOrDefault(c => c.EnumValue == enumValue);

    /// <summary>
    /// Returns the display name for the given <paramref name="id"/>,
    /// falling back to the raw <paramref name="id"/> when no config is found.
    /// </summary>
    public static string GetDisplayName(this IEnumerable<AantalBlokkenConfig> configs, string id)
        => configs.FindById(id)?.DisplayName ?? id;

    // ── EventConfig ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="EventConfig"/> with the given <paramref name="id"/>,
    /// or <c>null</c> when not found.
    /// </summary>
    public static EventConfig FindById(this IEnumerable<EventConfig> configs, string id)
        => configs.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Returns the <see cref="EventConfig"/> whose <see cref="EventConfig.EnumValue"/>
    /// matches the given <paramref name="enumValue"/>, or <c>null</c> when not found.
    /// </summary>
    public static EventConfig FindByEnumValue(this IEnumerable<EventConfig> configs, int enumValue)
        => configs.FirstOrDefault(c => c.EnumValue == enumValue);

    /// <summary>
    /// Returns the display name for the given <paramref name="id"/>,
    /// falling back to the raw <paramref name="id"/> when no config is found.
    /// </summary>
    public static string GetDisplayName(this IEnumerable<EventConfig> configs, string id)
        => configs.FindById(id)?.DisplayName ?? id;

    // ── SoortEncounterConfig ──────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="SoortEncounterConfig"/> with the given <paramref name="id"/>,
    /// or <c>null</c> when not found.
    /// </summary>
    public static SoortEncounterConfig FindById(this IEnumerable<SoortEncounterConfig> configs, string id)
        => configs.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Returns the <see cref="SoortEncounterConfig"/> whose <see cref="SoortEncounterConfig.EnumValue"/>
    /// matches the given <paramref name="enumValue"/>, or <c>null</c> when not found.
    /// </summary>
    public static SoortEncounterConfig FindByEnumValue(this IEnumerable<SoortEncounterConfig> configs, int enumValue)
        => configs.FirstOrDefault(c => c.EnumValue == enumValue);

    /// <summary>
    /// Returns the display name for the given <paramref name="id"/>,
    /// falling back to the raw <paramref name="id"/> when no config is found.
    /// </summary>
    public static string GetDisplayName(this IEnumerable<SoortEncounterConfig> configs, string id)
        => configs.FindById(id)?.DisplayName ?? id;

    // ── QuestLocatieCategorieConfig ───────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="QuestLocatieCategorieConfig"/> with the given <paramref name="id"/>,
    /// or <c>null</c> when not found.
    /// </summary>
    public static QuestLocatieCategorieConfig FindById(this IEnumerable<QuestLocatieCategorieConfig> configs, string id)
        => configs.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// Returns the display name for the given <paramref name="id"/>,
    /// falling back to the raw <paramref name="id"/> when no config is found.
    /// </summary>
    public static string GetDisplayName(this IEnumerable<QuestLocatieCategorieConfig> configs, string id)
        => configs.FindById(id)?.DisplayName ?? id;

    // ── QuestLocatieConfig ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="QuestLocatieConfig"/> with the given <paramref name="id"/>,
    /// or <c>null</c> when not found.
    /// </summary>
    public static QuestLocatieConfig FindById(this IEnumerable<QuestLocatieConfig> configs, string id)
        => configs.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// Returns the display name for the given <paramref name="id"/>,
    /// falling back to the raw <paramref name="id"/> when no config is found.
    /// </summary>
    public static string GetDisplayName(this IEnumerable<QuestLocatieConfig> configs, string id)
        => configs.FindById(id)?.DisplayName ?? id;

    /// <summary>
    /// Returns the selectable locations, grouped per category and ordered the way the picker renders them:
    /// categories by <see cref="QuestLocatieCategorieConfig.Order"/>, locations by
    /// <see cref="QuestLocatieConfig.Order"/> and then display name. Inactive locations are excluded, except
    /// the one matching <paramref name="includeInactiveId"/> so an existing choice never silently disappears.
    /// Locations pointing at an unknown category are dropped.
    /// </summary>
    public static IReadOnlyList<(QuestLocatieCategorieConfig Categorie, IReadOnlyList<QuestLocatieConfig> Locaties)> GroupByCategorie(
        this IEnumerable<QuestLocatieConfig> configs,
        IEnumerable<QuestLocatieCategorieConfig> categorieen,
        string includeInactiveId = null)
    {
        var categorieList = categorieen?.ToList() ?? [];
        var locatieList = configs?
            .Where(c => c.IsActief || string.Equals(c.Id, includeInactiveId, StringComparison.Ordinal))
            .ToList() ?? [];

        return [.. categorieList
            .OrderBy(cat => cat.Order)
            .ThenBy(cat => cat.DisplayName, StringComparer.Ordinal)
            .Select(cat => (
                Categorie: cat,
                Locaties: (IReadOnlyList<QuestLocatieConfig>)[.. locatieList
                    .Where(loc => string.Equals(loc.CategorieId, cat.Id, StringComparison.Ordinal))
                    .OrderBy(loc => loc.Order)
                    .ThenBy(loc => loc.DisplayName, StringComparer.Ordinal)]))
            .Where(g => g.Locaties.Count > 0)];
    }

    /// <summary>
    /// Builds the symmetric overlap map: a location id maps to every location id it physically overlaps
    /// with. An A → B entry in <see cref="QuestLocatieConfig.ConflicteertMet"/> implies B → A, so the admin
    /// only has to fill in one side. Self-references and ids that do not resolve to a config are dropped.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildSymmetrischeOverlap(
        this IEnumerable<QuestLocatieConfig> configs)
    {
        var locatieList = configs?.ToList() ?? [];
        var knownIds = locatieList.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        var pairs = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var locatie in locatieList)
        {
            foreach (var otherId in locatie.ConflicteertMet ?? [])
            {
                if (string.IsNullOrWhiteSpace(otherId)
                    || string.Equals(otherId, locatie.Id, StringComparison.Ordinal)
                    || !knownIds.Contains(otherId))
                {
                    continue;
                }

                AddOverlap(pairs, locatie.Id, otherId);
                AddOverlap(pairs, otherId, locatie.Id);
            }
        }

        return pairs.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<string>)[.. kvp.Value.OrderBy(id => id, StringComparer.Ordinal)],
            StringComparer.Ordinal);
    }

    private static void AddOverlap(Dictionary<string, HashSet<string>> pairs, string from, string to)
    {
        if (!pairs.TryGetValue(from, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            pairs[from] = set;
        }

        set.Add(to);
    }
}
