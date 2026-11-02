namespace PlotboekGenerator.Application.DTOs;

/// <summary>
/// Data transfer object for plotboek read operations.
/// </summary>
/// <param name="Id">The plotboek ID.</param>
/// <param name="Jaar">The year of the plotboek.</param>
/// <param name="EventId">The event ID (enum member-name string).</param>
public sealed record PlotboekDto(PlotboekId Id, Year Jaar, string EventId)
{
    /// <summary>
    /// Gets the end date of the plotboek.
    /// </summary>
    public DateOnly? EindDatum { get; init; }

    /// <summary>
    /// Gets a value indicating whether the full plotboek has been marked complete.
    /// </summary>
    public bool IsVolledigKlaar { get; init; }

    /// <summary>
    /// Gets an empty plotboek DTO instance.
    /// </summary>
    public static PlotboekDto Empty => new(PlotboekId.Empty, Year.Empty, "None");
}
