namespace PlotboekGenerator.infrastructure.MartenDb;

public sealed class PlotboekPaginaRepository : IAggregateRepository<PlotboekPaginaId, PlotboekPagina>
{
    private readonly IEventStore<Uuid> _eventStore;

    public PlotboekPaginaRepository(IEventStore<Uuid> eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Result> SaveAsync(PlotboekPagina aggregate, CancellationToken cancellationToken)
    {
        var result = await _eventStore.SaveEventsAsync((Uuid)aggregate.Id, aggregate.Buffer.CommittedVersion, aggregate.Buffer.Uncommitted, cancellationToken);
        if (result.IsValid)
        {
            aggregate.MarkAllAsCommitted();
        }
        return result;
    }

    public async Task<Result<PlotboekPagina>> LoadAsync(PlotboekPaginaId id, CancellationToken cancellationToken = default)
    {
        if (id == PlotboekPaginaId.Empty)
            return Result.WithMessages<PlotboekPagina>(ValidationMessage.Error($"PlotboekPagina Id is empty."));

        var events = await _eventStore.LoadEventsAsync((Uuid)id, cancellationToken);
        if (events.Count == 0)
            return Result.WithMessages<PlotboekPagina>(ValidationMessage.Error($"PlotboekPagina {id} not found."));

        var eventbuffer = EventBuffer.FromStorage(id, events, e => e);
        var aggregate = Aggregate.FromStorage<PlotboekPagina, PlotboekPaginaId>(eventbuffer);
        return aggregate;
    }

    /// <summary>
    /// Reconstructs the aggregate as it was right after the given stream <paramref name="version"/> was
    /// applied, by replaying every event up to and including that version. Also returns the current
    /// (latest) stream version — needed for the optimistic-concurrency check when saving a restored
    /// snapshot — and the version actually reconstructed to after clamping.
    /// </summary>
    /// <param name="id">The identifier of the pagina to reconstruct.</param>
    /// <param name="version">The stream version to reconstruct to. Clamped to <c>[1, currentVersion]</c>.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>The historical snapshot together with the current and reconstructed stream versions.</returns>
    public async Task<Result<PaginaHistorySnapshot>> LoadAtVersionAsync(PlotboekPaginaId id, long version, CancellationToken cancellationToken = default)
    {
        if (id == PlotboekPaginaId.Empty)
            return Result.WithMessages<PaginaHistorySnapshot>(ValidationMessage.Error($"PlotboekPagina Id is empty."));

        var events = await _eventStore.LoadEventsWithVersionAsync((Uuid)id, cancellationToken);
        if (events.Count == 0)
            return Result.WithMessages<PaginaHistorySnapshot>(ValidationMessage.Error($"PlotboekPagina {id} not found."));

        // Anchor on the actual stream versions (not the element count) so that reconstructing to
        // "version N" always matches the version the audit trail shows, even if the stream were ever
        // to become non-contiguous.
        var currentVersion = events[events.Count - 1].Version;
        var targetVersion = Math.Clamp(version, 1L, currentVersion);

        var eventbuffer = EventBuffer.FromStorage(
            id,
            events.Where(e => e.Version <= targetVersion).Select(e => e.Data).ToList(),
            e => e);
        var snapshot = Aggregate.FromStorage<PlotboekPagina, PlotboekPaginaId>(eventbuffer);
        return new PaginaHistorySnapshot(snapshot, currentVersion, targetVersion);
    }
}

/// <summary>
/// The reconstructed historical state of a pagina, together with the stream versions the caller needs:
/// <paramref name="CurrentVersion"/> for the optimistic-concurrency check on save, and
/// <paramref name="TargetVersion"/> (the request clamped to the valid range) to look up who/when.
/// </summary>
/// <param name="Snapshot">The aggregate as it was right after <paramref name="TargetVersion"/>.</param>
/// <param name="CurrentVersion">The latest stream version at load time.</param>
/// <param name="TargetVersion">The version actually reconstructed to, after clamping the request.</param>
public sealed record PaginaHistorySnapshot(PlotboekPagina Snapshot, long CurrentVersion, long TargetVersion);
