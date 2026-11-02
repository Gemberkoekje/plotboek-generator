namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents a scheduled meeting/event item within a day type (first day, middle day, or last day).
/// </summary>
public sealed record Meeting
{
    /// <summary>
    /// Unique identifier for the meeting.
    /// </summary>
    required public Guid Id { get; init; }

    /// <summary>
    /// Which day type this meeting belongs to.
    /// </summary>
    required public MeetingDagType DagType { get; init; }

    /// <summary>
    /// Time of the meeting (e.g., "14:00", "18:30").
    /// </summary>
    required public string Tijd { get; init; }

    /// <summary>
    /// Name/description of the meeting.
    /// </summary>
    required public string Naam { get; init; }

    /// <summary>
    /// Order for display within the day type.
    /// </summary>
    required public int Order { get; init; }

    /// <summary>
    /// Creates a new Meeting instance.
    /// </summary>
    public static Meeting Create(MeetingDagType dagType, string tijd, string naam, int order)
    {
        return new Meeting
        {
            Id = Guid.NewGuid(),
            DagType = dagType,
            Tijd = tijd,
            Naam = naam,
            Order = order,
        };
    }

    /// <summary>
    /// Returns display string combining time and name.
    /// </summary>
    public string DisplayName => $"{Tijd} {Naam}";
}
