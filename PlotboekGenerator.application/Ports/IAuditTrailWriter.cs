namespace PlotboekGenerator.Application.Ports;

/// <summary>
/// Writes audit trail entries for tracking changes to entities.
/// </summary>
public interface IAuditTrailWriter
{
    /// <summary>
    /// Writes an audit trail entry.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate being modified.</param>
    /// <param name="description">A description of the change.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when aggregateId or description is null.</exception>
    Task WriteAsync(PlotboekId aggregateId, string description, CancellationToken cancellationToken = default);
}
