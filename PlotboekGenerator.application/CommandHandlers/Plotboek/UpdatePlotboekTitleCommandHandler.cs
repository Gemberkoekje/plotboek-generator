using Plotboek = PlotboekGenerator.core.Models.Plotboek;

namespace PlotboekGenerator.Application.CommandHandlers.Plotboek;

/// <summary>
/// Handles commands for updating plotboek titles.
/// </summary>
public sealed class UpdatePlotboekTitleCommandHandler
{
    private readonly IPlotboekRepository _repository;
    private readonly IAuditTrailWriter _auditTrailWriter;
    private readonly ILogger<UpdatePlotboekTitleCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePlotboekTitleCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The plotboek repository.</param>
    /// <param name="auditTrailWriter">The audit trail writer.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public UpdatePlotboekTitleCommandHandler(
        IPlotboekRepository repository,
        IAuditTrailWriter auditTrailWriter,
        ILogger<UpdatePlotboekTitleCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditTrailWriter = auditTrailWriter ?? throw new ArgumentNullException(nameof(auditTrailWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Updates the title of a plotboek.
    /// </summary>
    /// <param name="command">The update command.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    public async Task<Result> HandleAsync(UpdatePlotboekTitleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var plotboek = await _repository.GetByIdAsync(command.PlotboekId, cancellationToken).ConfigureAwait(false);
        if (!plotboek.IsValid)
            return Result.WithMessages(plotboek.Messages.ToArray());

        var existingPlotboek = plotboek.Value;
        var updatedPlotboek = existingPlotboek.Aanpassen(
            new PlotboekGenerator.core.Models.Plotboek.BasisData
            {
                Titel = command.Title,
                StartDag = existingPlotboek.StartDag,
                EindDag = existingPlotboek.EindDag,
            },
            command.UpdatedBy);

        if (!updatedPlotboek.IsValid)
            return Result.WithMessages(updatedPlotboek.Messages);

        await _repository.SaveAsync(updatedPlotboek.Value, cancellationToken).ConfigureAwait(false);

        await _auditTrailWriter.WriteAsync(
            command.PlotboekId,
            $"Title updated to '{command.Title}'",
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Plotboek {PlotboekId} title updated by {User}", command.PlotboekId, command.UpdatedBy.Id);

        return Result.OK;
    }
}
