namespace PlotboekGenerator.infrastructure.MartenDb;

public sealed class PlotboekRepository : IAggregateRepository<PlotboekId, Plotboek>
{
    private readonly IEventStore<Uuid> _eventStore;

    public PlotboekRepository(IEventStore<Uuid> eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Result> SaveAsync(Plotboek aggregate, CancellationToken cancellationToken)
    {
        var result = await _eventStore.SaveEventsAsync((Uuid)aggregate.Id, aggregate.Buffer.CommittedVersion, aggregate.Buffer.Uncommitted, cancellationToken);
        if (result.IsValid)
        {
            aggregate.MarkAllAsCommitted();
        }
        return result;
    }

    public async Task<Result<Plotboek>> LoadAsync(PlotboekId id, CancellationToken cancellationToken = default)
    {
        if (id == PlotboekId.Empty)
            return Result.WithMessages<Plotboek>(ValidationMessage.Error($"Plotboek Id is empty."));

        var events = await _eventStore.LoadEventsAsync((Uuid)id, cancellationToken);
        if (events.Count == 0)
            return Result.WithMessages<Plotboek>(ValidationMessage.Error($"Plotboek {id} not found."));

        var eventbuffer = EventBuffer.FromStorage(id, events, e => e);
        var aggregate = Aggregate.FromStorage<Plotboek, PlotboekId>(eventbuffer);
        return aggregate;
    }
}
