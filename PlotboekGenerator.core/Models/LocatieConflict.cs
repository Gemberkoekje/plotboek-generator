namespace PlotboekGenerator.core.Models;

/// <summary>
/// One other encounter that competes for the same (or a physically overlapping) quest location.
/// Purely a planning aid: a conflict never blocks marking an encounter "volledig klaar", because
/// sharing a location is sometimes deliberate.
/// </summary>
public sealed record LocatieConflict
{
    /// <summary>The clashing encounter.</summary>
    required public PlotboekPaginaId PaginaId { get; init; }

    /// <summary>Title of the clashing encounter, for the message.</summary>
    required public string Titel { get; init; }

    /// <summary>What kind of clash this is.</summary>
    required public LocatieConflictSoort Soort { get; init; }

    /// <summary>The location id the clashing encounter occupies.</summary>
    required public string LocatieId { get; init; }

    /// <summary>The day of the clashing encounter, <see cref="Dag.None"/> when it has no slot.</summary>
    public Dag Dag { get; init; }

    /// <summary>The starttijd id of the clashing encounter, empty when it has no slot.</summary>
    public string StarttijdId { get; init; } = string.Empty;
}
