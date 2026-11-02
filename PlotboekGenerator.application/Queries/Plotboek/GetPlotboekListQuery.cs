namespace PlotboekGenerator.Application.Queries.Plotboek;

/// <summary>
/// Query to retrieve a list of plotboeks with pagination.
/// </summary>
/// <param name="PageNumber">The page number (1-based).</param>
/// <param name="PageSize">The number of items per page.</param>
public sealed record GetPlotboekListQuery(int PageNumber = 1, int PageSize = 20) : IQuery<IReadOnlyList<PlotboekDto>>;
