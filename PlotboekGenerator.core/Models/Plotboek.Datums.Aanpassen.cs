namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    public DateOnly? StartDatum { get; private set; }

    public DateOnly? EindDatum { get; private set; }

    public Result<Plotboek> Aanpassen(KalenderData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (StartDatum != data.StartDatum || EindDatum != data.EindDatum)
        {
            return ApplyEvent(new PlotboekDatumsAangepast
            {
                StartDatum = data.StartDatum,
                EindDatum = data.EindDatum,
                Who = updatedBy.Id,
            });
        }

        return Result.For(this);
    }

    internal void When(PlotboekDatumsAangepast @event)
    {
        StartDatum = @event.StartDatum;
        EindDatum = @event.EindDatum;
    }

    private static Dag ConvertFromDayOfWeek(DayOfWeek? dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => Dag.Maandag,
            DayOfWeek.Tuesday => Dag.Dinsdag,
            DayOfWeek.Wednesday => Dag.Woensdag,
            DayOfWeek.Thursday => Dag.Donderdag,
            DayOfWeek.Friday => Dag.Vrijdag,
            DayOfWeek.Saturday => Dag.Zaterdag,
            DayOfWeek.Sunday => Dag.Zondag,
            _ => Dag.None,
        };
    }

    public sealed class KalenderData
    {
        public DateOnly? StartDatum { get; init; }

        public DateOnly? EindDatum { get; init; }
    }
}
