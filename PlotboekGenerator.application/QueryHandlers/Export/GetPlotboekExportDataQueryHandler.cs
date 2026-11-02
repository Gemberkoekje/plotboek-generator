namespace PlotboekGenerator.Application.QueryHandlers.Export;

/// <summary>
/// Handles queries for retrieving plotboek export data.
/// </summary>
public sealed class GetPlotboekExportDataQueryHandler
{
    private readonly IPlotboekExportDataBuilder _exportDataBuilder;
    private readonly ILogger<GetPlotboekExportDataQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlotboekExportDataQueryHandler"/> class.
    /// </summary>
    /// <param name="exportDataBuilder">The export data builder.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public GetPlotboekExportDataQueryHandler(
        IPlotboekExportDataBuilder exportDataBuilder,
        ILogger<GetPlotboekExportDataQueryHandler> logger)
    {
        _exportDataBuilder = exportDataBuilder ?? throw new ArgumentNullException(nameof(exportDataBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves export data for a plotboek.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the export data when successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public async Task<Result<PlotboekTemplateData>> HandleAsync(GetPlotboekExportDataQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var result = await _exportDataBuilder.BuildAsync(query.PlotboekId, cancellationToken).ConfigureAwait(false);

        if (!result.IsValid)
        {
            _logger.LogWarning("Failed to build export data for plotboek {PlotboekId}", query.PlotboekId);
        }

        return result;
    }
}
