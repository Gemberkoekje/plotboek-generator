namespace PlotboekGenerator.core.Enums;

/// <summary>
/// How two encounters clash over a quest location.
/// </summary>
public enum LocatieConflictSoort
{
    /// <summary>No clash.</summary>
    None = 0,

    /// <summary>Both encounters use the exact same location in overlapping time slots.</summary>
    ExacteMatch,

    /// <summary>The locations physically overlap (adjacent fields, a route ending on a field).</summary>
    Overlapgroep,

    /// <summary>
    /// Same location, but the other encounter has no day/time slot (player-driven or not yet planned),
    /// so there is no "at the same time" to compare against.
    /// </summary>
    ZonderTijdvak,
}
