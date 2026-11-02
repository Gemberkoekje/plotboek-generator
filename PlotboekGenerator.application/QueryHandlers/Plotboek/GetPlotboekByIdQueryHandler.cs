namespace PlotboekGenerator.Application.QueryHandlers.Plotboek;

/// <summary>
/// Handles queries for retrieving plotboeks by ID.
/// </summary>
public sealed class GetPlotboekByIdQueryHandler
{
    private readonly IPlotboekReadStore _readStore;
    private readonly ILogger<GetPlotboekByIdQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlotboekByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="readStore">The plotboek read store.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public GetPlotboekByIdQueryHandler(
        IPlotboekReadStore readStore,
        ILogger<GetPlotboekByIdQueryHandler> logger)
    {
        _readStore = readStore ?? throw new ArgumentNullException(nameof(readStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a plotboek by its ID.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the plotboek DTO when found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public async Task<Result<PlotboekDto>> HandleAsync(GetPlotboekByIdQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = await _readStore.GetByIdAsync(query.PlotboekId, cancellationToken).ConfigureAwait(false);

        if (!result.IsValid)
        {
            _logger.LogWarning("Plotboek {PlotboekId} not found", query.PlotboekId);
        }

        return result;
    }
}
