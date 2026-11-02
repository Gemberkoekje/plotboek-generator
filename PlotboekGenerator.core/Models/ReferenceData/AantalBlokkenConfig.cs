namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Database-driven configuration for an AantalBlokken option.
/// Seeded from the <see cref="Enums.AantalBlokken"/> enum values; the <see cref="EnumValue"/>
/// corresponds to the underlying integer so existing events remain valid.
/// </summary>
public sealed record AantalBlokkenConfig
{
    /// <summary>
    /// Stable string key derived from the enum integer value.
    /// </summary>
    required public string Id { get; init; }

    /// <summary>
    /// The underlying enum integer value, used for mapping back to <see cref="Enums.AantalBlokken"/>.
    /// </summary>
    required public int EnumValue { get; init; }

    /// <summary>
    /// Display name shown in the UI (e.g. "1 blok (max 1 uur)").
    /// </summary>
    required public string DisplayName { get; init; }

    /// <summary>
    /// How many additional time slots this duration occupies (0 = one slot, 1 = two slots, etc.).
    /// Used by overflow calculation logic.
    /// </summary>
    required public int OverflowBlokken { get; init; }

    /// <summary>
    /// Sort order for display.
    /// </summary>
    required public int Order { get; init; }

    /// <summary>
    /// Whether this entry is the "None" sentinel (not selectable as a real value).
    /// </summary>
    required public bool IsNone { get; init; }
}
