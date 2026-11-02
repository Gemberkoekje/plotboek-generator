namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Database-driven configuration for a Starttijd option.
/// Seeded from the <see cref="Enums.Starttijd"/> enum values; the <see cref="EnumValue"/>
/// corresponds to the underlying integer so existing events remain valid.
/// </summary>
public sealed record StarttijdConfig
{
    /// <summary>
    /// Stable string key matching the enum member name (e.g. "Tijd1030", "None").
    /// </summary>
    required public string Id { get; init; }

    /// <summary>
    /// The underlying enum integer value, used for mapping back to <see cref="Enums.Starttijd"/>.
    /// </summary>
    required public int EnumValue { get; init; }

    /// <summary>
    /// Display name shown in the UI (e.g. "10:30", "20:00").
    /// </summary>
    required public string DisplayName { get; init; }

    /// <summary>
    /// Whether this slot is available when the selected day is the first (start) day.
    /// </summary>
    required public bool IsAvailableOnEersteDag { get; init; }

    /// <summary>
    /// Whether this slot is available when the selected day is a middle day (not first, not last).
    /// </summary>
    required public bool IsAvailableOnMiddelDagen { get; init; }

    /// <summary>
    /// Whether this slot is available when the selected day is the last (end) day.
    /// </summary>
    required public bool IsAvailableOnLaatsteDag { get; init; }

    /// <summary>
    /// The block window shown behind the slot on the first day (e.g. "18:00 - 21:00").
    /// Empty means the slot gets no block label and does not take part in the block numbering,
    /// which is how "19:30: Introquest" stays unnumbered.
    /// </summary>
    public string BlokVensterEersteDag { get; init; } = string.Empty;

    /// <summary>
    /// The block window shown behind the slot on a middle day (e.g. "19:00 - 21:00").
    /// Empty means no block label; see <see cref="BlokVensterEersteDag"/>.
    /// </summary>
    public string BlokVensterMiddelDagen { get; init; } = string.Empty;

    /// <summary>
    /// The block window shown behind the slot on the last day (e.g. "11:30 - 13:30").
    /// Empty means no block label; see <see cref="BlokVensterEersteDag"/>.
    /// </summary>
    public string BlokVensterLaatsteDag { get; init; } = string.Empty;

    /// <summary>
    /// Whether the three block windows above are deliberate: written by the seed, by the backfill that
    /// fills in a row stored before those fields existed, or by the admin. It is what tells a row whose
    /// windows the admin cleared on purpose apart from a row that never had any, so the backfill runs at
    /// most once per row and never restores text the admin removed.
    /// Defaults to <c>false</c>, because a row stored before this field existed has to read as un-backfilled.
    /// </summary>
    public bool BlokVenstersIngesteld { get; init; }

    /// <summary>
    /// Sort order for display. Derived from the time in <see cref="DisplayName"/> and rewritten to 1..n
    /// by <see cref="ReferenceDataExtensions.ResequenceOrder"/> whenever the slots are seeded or edited,
    /// so nobody has to shift numbers by hand to fit a new slot in between.
    /// </summary>
    required public int Order { get; init; }

    /// <summary>
    /// Whether this entry is the "None" sentinel (not selectable as a real value).
    /// </summary>
    required public bool IsNone { get; init; }

    /// <summary>
    /// Whether this slot is an Introquest slot (available only on the first day as a special early-evening option).
    /// Derived from availability flags: available on eerste dag only and not on middel or laatste dag.
    /// </summary>
    public bool IsIntroquest => IsAvailableOnEersteDag && !IsAvailableOnMiddelDagen && !IsAvailableOnLaatsteDag;
}
