using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.core.Events;
using PlotboekGenerator.infrastructure.MartenDb;

namespace PlotboekGenerator.infrastructure.Adapters.Application;

/// <summary>
/// Persists application-level audit entries to the aggregate event stream.
/// </summary>
public sealed class EventStoreAuditTrailWriter : IAuditTrailWriter
{
    private readonly IEventStore<Uuid> _eventStore;

    public EventStoreAuditTrailWriter(IEventStore<Uuid> eventStore)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
    }

    public async Task WriteAsync(PlotboekId aggregateId, string description, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var existingEvents = await _eventStore.LoadEventsAsync((Uuid)aggregateId, cancellationToken).ConfigureAwait(false);
        var expectedVersion = existingEvents.Count;

        var auditEvent = new ApplicationAuditTrailEvent
        {
            Who = "system",
            Description = description,
        };

        var saveResult = await _eventStore
            .SaveEventsAsync((Uuid)aggregateId, expectedVersion, [auditEvent], cancellationToken)
            .ConfigureAwait(false);

        if (!saveResult.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", saveResult.Messages.Select(message => message.Message)));
        }
    }

    private sealed record ApplicationAuditTrailEvent : AuditTrailEvent
    {
        required public string Description { get; init; }

        public override string What => Description;
    }
}
