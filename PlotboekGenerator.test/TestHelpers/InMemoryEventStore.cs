using Qowaiv.Validation.Abstractions;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// In-memory event store for integration tests.
/// Supports optimistic concurrency and event loading, and a demo's wipe.
/// </summary>
internal sealed class InMemoryEventStore : IEventStore<Uuid>, IEventStoreWiper
{
    private readonly Dictionary<Uuid, List<object>> _streams = [];

    /// <summary>The number of streams that hold events.</summary>
    public int StreamCount => _streams.Count;

    /// <summary>Removes all event streams. Used to isolate integration tests.</summary>
    public void Clear() => _streams.Clear();

    /// <inheritdoc />
    public Task WipeAllAsync(CancellationToken cancellationToken = default)
    {
        Clear();
        return Task.CompletedTask;
    }

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
