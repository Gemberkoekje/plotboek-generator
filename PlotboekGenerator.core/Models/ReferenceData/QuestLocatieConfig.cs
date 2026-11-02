namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Database-driven configuration for a single quest location, selectable on an encounter whose
/// <see cref="Enums.LocatieEncounter"/> is <c>Questveld</c>.
/// </summary>
public sealed record QuestLocatieConfig
{
    /// <summary>
    /// Stable string key (e.g. "RouteHenrysHoekjeLeeuwenkuil"). Derived from the name when the location
    /// is created and never changed afterwards, because stored plots reference it.
    /// </summary>
    required public string Id { get; init; }

    /// <summary>
    /// Display name shown in the picker, on the plot page and in the export (e.g. "Leeuwenkuil").
    /// Renaming is allowed: the display follows everywhere because the id stays put.
    /// </summary>
    required public string DisplayName { get; init; }

    /// <summary>
    /// The <see cref="QuestLocatieCategorieConfig.Id"/> this location belongs to.
    /// </summary>
    required public string CategorieId { get; init; }

    /// <summary>
    /// A reservation specific to this location, on top of the category note
    /// (e.g. "als er niemand op het ruineveld staat"). Empty means no note.
    /// </summary>
    public string Notitie { get; init; } = string.Empty;

    /// <summary>
    /// Ids of locations that physically overlap with this one. Two plots on overlapping locations in the
    /// same time slot produce a soft warning. The relation is made symmetric when read, so the admin only
    /// has to fill in one side.
    /// </summary>
    public IReadOnlyList<string> ConflicteertMet { get; init; } = [];

    /// <summary>
    /// Whether this location can still be picked. Deactivated locations stay resolvable so historic plots
    /// remain readable; they are never deleted.
    /// </summary>
    required public bool IsActief { get; init; }

    /// <summary>
    /// Sort order within the category.
    /// </summary>
    required public int Order { get; init; }
}
