namespace PlotboekGenerator.Application.Queries.Export;

/// <summary>
/// Query to retrieve export data for a plotboek.
/// </summary>
/// <param name="PlotboekId">The ID of the plotboek to export.</param>
public sealed record GetPlotboekExportDataQuery(PlotboekId PlotboekId) : IQuery<PlotboekTemplateData>;
