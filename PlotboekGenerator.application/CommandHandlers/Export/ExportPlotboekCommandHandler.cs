namespace PlotboekGenerator.Application.CommandHandlers.Export;

/// <summary>
/// Handles plotboek export commands.
/// </summary>
public sealed class ExportPlotboekCommandHandler
{
    private readonly IPlotboekExportDataBuilder _exportDataBuilder;
    private readonly IDocumentGenerator _documentGenerator;
    private readonly ILogger<ExportPlotboekCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportPlotboekCommandHandler"/> class.
    /// </summary>
    /// <param name="exportDataBuilder">The export data builder.</param>
    /// <param name="documentGenerator">The document generator.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public ExportPlotboekCommandHandler(
        IPlotboekExportDataBuilder exportDataBuilder,
        IDocumentGenerator documentGenerator,
        ILogger<ExportPlotboekCommandHandler> logger)
    {
        _exportDataBuilder = exportDataBuilder ?? throw new ArgumentNullException(nameof(exportDataBuilder));
        _documentGenerator = documentGenerator ?? throw new ArgumentNullException(nameof(documentGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports a plotboek as a DOCX document.
    /// </summary>
    /// <param name="command">The export command.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the DOCX file bytes when successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    public async Task<Result<byte[]>> HandleAsync(ExportPlotboekCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exportData = await _exportDataBuilder.BuildAsync(command.PlotboekId, cancellationToken).ConfigureAwait(false);
        if (!exportData.IsValid)
            return Result.WithMessages<byte[]>(exportData.Messages.ToArray());

        var validExportData = exportData.Value;
        var document = await _documentGenerator.GeneratePlotboekDocumentAsync(validExportData, cancellationToken).ConfigureAwait(false);
        if (!document.IsValid)
            return document;

        _logger.LogInformation("Plotboek {PlotboekId} exported successfully", command.PlotboekId);
        var generatedDocument = document.Value;
        return generatedDocument;
    }
}
