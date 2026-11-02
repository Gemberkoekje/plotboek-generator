namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>
/// Defines a contract for an event store that persists and retrieves domain events for aggregates.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's unique identifier.</typeparam>
public interface IEventStore<TId>
{
    /// <summary>
    /// Asynchronously saves a collection of events for the specified aggregate, using optimistic concurrency.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate to which the events belong.</param>
    /// <param name="expectedVersion">The expected version of the event stream before appending.</param>
    /// <param name="events">The collection of events to persist.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>A valid result on success, or an invalid result with error messages on concurrency conflict.</returns>
    Task<Result> SaveEventsAsync(TId aggregateId, long expectedVersion, IEnumerable<object> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously loads all events associated with the specified aggregate.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate whose events are to be loaded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>
    /// A task that represents the asynchronous load operation. The task result contains a read-only list of events for the aggregate.
    /// </returns>
    Task<IReadOnlyList<object>> LoadEventsAsync(TId aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously loads all events for the aggregate together with their 1-based stream version,
    /// ordered from oldest to newest. Used by features (such as the audit trail) that need to anchor
    /// an event to a specific point in the stream.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate whose events are to be loaded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>A read-only list of events paired with their stream version.</returns>
    Task<IReadOnlyList<VersionedEvent>> LoadEventsWithVersionAsync(TId aggregateId, CancellationToken cancellationToken = default);
}

/// <summary>
/// A domain event together with its 1-based version within the aggregate's event stream.
/// </summary>
/// <param name="Version">The 1-based position of the event within the stream.</param>
/// <param name="Data">The deserialized event payload.</param>
public sealed record VersionedEvent(long Version, object Data);
