namespace PlotboekGenerator.Application.Queries.Plotboek;

/// <summary>
/// Query to retrieve a plotboek by ID.
/// </summary>
/// <param name="PlotboekId">The ID of the plotboek to retrieve.</param>
public sealed record GetPlotboekByIdQuery(PlotboekId PlotboekId) : IQuery<PlotboekDto>;
