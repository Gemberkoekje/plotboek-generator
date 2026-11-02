namespace PlotboekGenerator.Application.QueryHandlers.Plotboek;

/// <summary>
/// Handles queries for retrieving lists of plotboeks.
/// </summary>
public sealed class GetPlotboekListQueryHandler
{
    private readonly IPlotboekReadStore _readStore;
    private readonly ILogger<GetPlotboekListQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlotboekListQueryHandler"/> class.
    /// </summary>
    /// <param name="readStore">The plotboek read store.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public GetPlotboekListQueryHandler(
        IPlotboekReadStore readStore,
        ILogger<GetPlotboekListQueryHandler> logger)
    {
        _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a paginated list of plotboeks.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of plotboek DTOs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public async Task<IReadOnlyList<PlotboekDto>> HandleAsync(GetPlotboekListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = await _readStore.GetListAsync(query.PageNumber, query.PageSize, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Retrieved {Count} plotboeks for page {PageNumber}", result.Count, query.PageNumber);

        return result;
    }
}
