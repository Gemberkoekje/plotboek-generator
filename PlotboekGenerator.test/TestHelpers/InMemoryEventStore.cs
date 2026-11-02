using Qowaiv.Validation.Abstractions;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// In-memory event store for integration tests.
/// Supports optimistic concurrency and event loading.
/// </summary>
internal sealed class InMemoryEventStore : IEventStore<Uuid>
{
    private readonly Dictionary<Uuid, List<object>> _streams = [];

    /// <summary>Removes all event streams. Used to isolate integration tests.</summary>
    public void Clear() => _streams.Clear();

    public Task<Result> SaveEventsAsync(Uuid aggregateId, long expectedVersion, IEnumerable<object> events, CancellationToken cancellationToken = default)
    {
        if (!_streams.TryGetValue(aggregateId, out var stream))
        {
            stream = [];
            _streams[aggregateId] = stream;
        }

        if (stream.Count != expectedVersion)
        {
            return Task.FromResult(Result.WithMessages(ValidationMessage.Error("Concurrency conflict: expected version " + expectedVersion + " but found " + stream.Count)));
        }

        stream.AddRange(events);
        return Task.FromResult(Result.OK);
    }

    public Task<IReadOnlyList<object>> LoadEventsAsync(Uuid aggregateId, CancellationToken cancellationToken = default)
    {
        if (_streams.TryGetValue(aggregateId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<object>>([.. stream]);
        }

        return Task.FromResult<IReadOnlyList<object>>([]);
    }

    public Task<IReadOnlyList<VersionedEvent>> LoadEventsWithVersionAsync(Uuid aggregateId, CancellationToken cancellationToken = default)
    {
        if (_streams.TryGetValue(aggregateId, out var stream))
        {
            return Task.FromResult<IReadOnlyList<VersionedEvent>>([.. stream.Select((e, i) => new VersionedEvent(i + 1, e))]);
        }

        return Task.FromResult<IReadOnlyList<VersionedEvent>>([]);
    }
}

