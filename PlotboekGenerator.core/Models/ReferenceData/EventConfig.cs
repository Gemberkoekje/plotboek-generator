namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Database-driven configuration for a plotboek event option.
/// Seeded with stable string IDs; <see cref="EnumValue"/> remains for historical compatibility.
/// </summary>
public sealed record EventConfig
{
    /// <summary>
    /// Stable string key derived from the enum integer value.
    /// </summary>
    required public string Id { get; init; }

    /// <summary>
    /// The historical integer value, kept for compatibility with existing reference-data documents.
    /// </summary>
    required public int EnumValue { get; init; }

    /// <summary>
    /// Display name shown in the UI (e.g. "Moots 1", "Summoning").
    /// </summary>
    required public string DisplayName { get; init; }

    /// <summary>
    /// Short abbreviation used in display (e.g. "M1", "Sum", "M2").
    /// </summary>
    required public string Abbreviation { get; init; }

    /// <summary>
    /// The event ID that logically follows this one.
    /// Used to suggest the next plotboek when creating new ones.
    /// </summary>
    public string NextEventId { get; init; } = string.Empty;

    /// <summary>
    /// The historical next event integer value, kept for compatibility with existing reference-data documents.
    /// </summary>
    public int? NextEventEnumValue { get; init; }

    /// <summary>
    /// Whether incrementing from this event also increments the year.
    /// </summary>
    public bool NextEventIncrementsYear { get; init; }

    /// <summary>
    /// Sort order within a year for display.
    /// </summary>
    required public int Order { get; init; }

    /// <summary>
    /// Whether this is the Evergreen event (special-cased in several places).
    /// </summary>
    required public bool IsEvergreen { get; init; }

    /// <summary>
    /// The first day of the event; <see cref="Dag.None"/> for an event without days, like Evergreen.
    /// A new plotboek for this event starts on it.
    /// </summary>
    public Dag StartDag { get; init; }

    /// <summary>
    /// The last day of the event; <see cref="Dag.None"/> for an event without days.
    /// </summary>
    public Dag EindDag { get; init; }

    /// <summary>
    /// Whether <see cref="StartDag"/> and <see cref="EindDag"/> have been set. A row stored before
    /// those fields existed has them filled in from the seed once, after which they are the admin's.
    /// </summary>
    public bool DagenIngesteld { get; init; }

    /// <summary>
    /// Whether this entry is the "None" sentinel.
    /// </summary>
    required public bool IsNone { get; init; }
}
