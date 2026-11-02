namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Database-driven configuration for a category of quest locations. Every category becomes an
/// <c>&lt;optgroup&gt;</c> in the questlocatie picker and carries the contextual note shown under it.
/// Unlike the other reference data records there is no <c>EnumValue</c>: quest locations were never
/// an enum, so there is no legacy value to map back to.
/// </summary>
public sealed record QuestLocatieCategorieConfig
{
    /// <summary>
    /// Stable string key (e.g. "PlekkenGeenKampeerders"). Derived from the name when the category is
    /// created and never changed afterwards, because stored plots reference it.
    /// </summary>
    required public string Id { get; init; }

    /// <summary>
    /// Display name shown as the optgroup label (e.g. "Alleen als er geen andere kampeerders staan").
    /// </summary>
    required public string DisplayName { get; init; }

    /// <summary>
    /// The contextual warning shown under the picker when a location from this category is chosen.
    /// Empty means no note.
    /// </summary>
    public string Notitie { get; init; } = string.Empty;

    /// <summary>
    /// How serious the reservation is, which decides whether the note renders as info or warning.
    /// </summary>
    required public QuestLocatieErnst Ernst { get; init; }

    /// <summary>
    /// Sort order of the optgroups.
    /// </summary>
    required public int Order { get; init; }
}
