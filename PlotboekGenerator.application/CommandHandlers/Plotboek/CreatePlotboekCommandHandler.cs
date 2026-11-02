using Plotboek = PlotboekGenerator.core.Models.Plotboek;

namespace PlotboekGenerator.Application.CommandHandlers.Plotboek;

/// <summary>
/// Handles commands for creating plotboeks.
/// </summary>
public sealed class CreatePlotboekCommandHandler
{
    private readonly IPlotboekRepository _repository;
    private readonly IAuditTrailWriter _auditTrailWriter;
    private readonly ILogger<CreatePlotboekCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePlotboekCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The plotboek repository.</param>
    /// <param name="auditTrailWriter">The audit trail writer.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public CreatePlotboekCommandHandler(
        IPlotboekRepository repository,
        IAuditTrailWriter auditTrailWriter,
        ILogger<CreatePlotboekCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditTrailWriter = auditTrailWriter ?? throw new ArgumentNullException(nameof(auditTrailWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new plotboek.
    /// </summary>
    /// <param name="command">The create command.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the created plotboek ID when successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    public async Task<Result<PlotboekId>> HandleAsync(CreatePlotboekCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plotboek = PlotboekGenerator.core.Models.Plotboek.Create(command.Year, command.EventId, command.StartDag, command.EindDag, command.UpdatedBy);
        if (!plotboek.IsValid)
            return Result.WithMessages<PlotboekId>(plotboek.Messages.ToArray());

        var createdPlotboek = plotboek.Value;
        await _repository.SaveAsync(createdPlotboek, cancellationToken).ConfigureAwait(false);

        await _auditTrailWriter.WriteAsync(
            createdPlotboek.Id,
            "Plotboek created",
            cancellationToken).ConfigureAwait(false);

        // Refresh the projection after the audit trail write so the ExportData document
        // reflects the actual stream version (audit event increments it beyond SaveAsync).
        _logger.LogInformation("Refreshing ExportData projection for plotboek {PlotboekId} after audit trail write", createdPlotboek.Id);
        await _repository.RefreshProjectionAsync(createdPlotboek.Id, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("ExportData projection refresh complete for plotboek {PlotboekId}", createdPlotboek.Id);

        _logger.LogInformation("Plotboek {PlotboekId} created by {User}", createdPlotboek.Id, command.UpdatedBy.Id);

        return createdPlotboek.Id;
    }
}
