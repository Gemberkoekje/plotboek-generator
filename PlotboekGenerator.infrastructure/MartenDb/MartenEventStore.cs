using JasperFx.Events;
using Marten;

namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>
/// Implements the <see cref="IEventStore{Uuid}"/> interface using Marten to persist and retrieve domain events for aggregates.
/// </summary>
public sealed class MartenEventStore(Marten.IDocumentStore store) : IEventStore<Uuid>
{
    /// <summary>
    /// Asynchronously saves a collection of events for the specified aggregate using a lightweight Marten session,
    /// with optimistic concurrency checking via the expected stream version.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate to which the events belong.</param>
    /// <param name="expectedVersion">The expected version of the event stream before appending.</param>
    /// <param name="events">The collection of events to persist.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>A valid result on success, or an invalid result with error messages on concurrency conflict.</returns>
    public async Task<Result> SaveEventsAsync(Uuid aggregateId, long expectedVersion, IEnumerable<object> events, CancellationToken cancellationToken = default)
    {
        try
        {
            using var session = store.LightweightSession();
            var eventArray = events.ToArray();
            var streamAction = session.Events.Append(aggregateId, eventArray);
            streamAction.ExpectedVersionOnServer = expectedVersion;
            await session.SaveChangesAsync(cancellationToken);
            return Result.OK;
        }
        catch (EventStreamUnexpectedMaxEventIdException)
        {
            return Result.WithMessages(ValidationMessage.Error("Iemand anders heeft al een aanpassing gedaan. Je wijzigingen zijn niet opgeslagen."));
        }
    }

    /// <summary>
    /// Asynchronously loads all events associated with the specified aggregate using a Marten query session.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate whose events are to be loaded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>
    /// A task that represents the asynchronous load operation. The task result contains a read-only list of events for the aggregate.
    /// </returns>
    public async Task<IReadOnlyList<object>> LoadEventsAsync(Uuid aggregateId, CancellationToken cancellationToken = default)
    {
        using var session = store.QuerySession();
        var stream = await session.Events.FetchStreamAsync(aggregateId, token: cancellationToken);
        return [.. stream.Select(e => e.Data)];
    }

    /// <summary>
    /// Asynchronously loads all events associated with the specified aggregate, each paired with its
    /// 1-based stream version, using a Marten query session.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate whose events are to be loaded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>A read-only list of events paired with their stream version, ordered oldest to newest.</returns>
    public async Task<IReadOnlyList<VersionedEvent>> LoadEventsWithVersionAsync(Uuid aggregateId, CancellationToken cancellationToken = default)
    {
        using var session = store.QuerySession();
        var stream = await session.Events.FetchStreamAsync(aggregateId, token: cancellationToken);
        return [.. stream.Select(e => new VersionedEvent(e.Version, e.Data))];
    }
}
