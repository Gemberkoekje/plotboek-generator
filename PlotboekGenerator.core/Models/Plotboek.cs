using Qowaiv.DomainModel;
using PlotboekGenerator.core.Validators;

namespace PlotboekGenerator.core.Models;

public sealed partial class Plotboek(PlotboekId aggregateId) : Aggregate<Plotboek, PlotboekId>(aggregateId, new PlotboekValidator())
{
    public Year Jaar { get; private set; }

    /// <summary>The event ID as a string: the id of its <see cref="EventConfig"/>.</summary>
    public string EventId { get; private set; }

    public Plotboek() : this(PlotboekId.Next())
    {
    }

    /// <summary>Creates the plotboek of one event in one year.</summary>
    /// <param name="jaar">The year.</param>
    /// <param name="eventId">The event's id in the reference data.</param>
    /// <param name="startDag">The event's first day, from its <see cref="EventConfig"/>.</param>
    /// <param name="eindDag">The event's last day.</param>
    /// <param name="updatedBy">Who creates it.</param>
    public static Result<Plotboek> Create(Year jaar, string eventId, Dag startDag, Dag eindDag, PlotboekUser updatedBy)
    {
        var plotboek = new Plotboek(PlotboekId.Create(jaar, eventId));
        return plotboek.ApplyEvent(new PlotboekAangemaaktV2()
        {
            Jaar = jaar,
            EventId = eventId,
            StartDag = startDag,
            EindDag = eindDag,
            Who = updatedBy.Id,
        });
    }

    internal void When(PlotboekAangemaakt @event)
    {
        Jaar = @event.Jaar;
        EventId = @event.Event.ToString();
        StartDag = @event.StartDag;
        EindDag = @event.EindDag;
    }

    internal void When(PlotboekAangemaaktV2 @event)
    {
        Jaar = @event.Jaar;
        EventId = @event.EventId;
        StartDag = @event.StartDag;
        EindDag = @event.EindDag;
    }

    public void MarkAllAsCommitted()
    {
        Buffer = Buffer.MarkAllAsCommitted();
    }
}
