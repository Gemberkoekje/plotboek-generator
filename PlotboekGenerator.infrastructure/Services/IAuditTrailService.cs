namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Abstraction for retrieving audit trail events from the event store.
/// </summary>
public interface IAuditTrailService
{
    /// <summary>
    /// Retrieves audit trail events for a specific aggregate identified by its UUID.
    /// </summary>
    /// <param name="aggregateId">The UUID of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit trail entries ordered by most recent first.</returns>
    Task<IReadOnlyList<AuditTrailEntry>> GetAuditTrailAsync(Uuid aggregateId, CancellationToken cancellationToken = default);
}
