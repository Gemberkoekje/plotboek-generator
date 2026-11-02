namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Database-driven configuration for a SoortEncounter option.
/// Seeded from the <see cref="Enums.SoortEncounter"/> enum values; the <see cref="EnumValue"/>
/// corresponds to the underlying integer so existing events remain valid.
/// </summary>
public sealed record SoortEncounterConfig
{
    /// <summary>
    /// Stable string key derived from the enum integer value.
    /// </summary>
    required public string Id { get; init; }

    /// <summary>
    /// The underlying enum integer value, used for mapping back to <see cref="Enums.SoortEncounter"/>.
    /// </summary>
    required public int EnumValue { get; init; }

    /// <summary>
    /// Display name shown in the UI (e.g. "Vechten", "Roleplay").
    /// </summary>
    required public string DisplayName { get; init; }

    /// <summary>
    /// Sort order for display.
    /// </summary>
    required public int Order { get; init; }

    /// <summary>
    /// Whether this entry is the "None" sentinel (not selectable as a real value).
    /// </summary>
    required public bool IsNone { get; init; }
}
