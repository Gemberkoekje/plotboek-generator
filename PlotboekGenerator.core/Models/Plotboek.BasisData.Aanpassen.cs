namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    private Dag _startDag;

    private Dag _eindDag;

    public Dag StartDag { get => StartDatum != null ? ConvertFromDayOfWeek(StartDatum.Value.DayOfWeek) : _startDag; private set => _startDag = value; }

    public Dag EindDag { get => EindDatum != null ? ConvertFromDayOfWeek(EindDatum.Value.DayOfWeek) : _eindDag; private set => _eindDag = value; }

    public string Titel { get; private set; }

    public Result<Plotboek> Aanpassen(BasisData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Result.For(this)
            | (p => Titel != data.Titel
                ? p.ApplyEvent(new PlotboekTitelAangepast
                {
                    Titel = data.Titel,
                    Who = updatedBy.Id,
                })
                : Result.For(p))
            | (p => (StartDatum == null && StartDag != data.StartDag) || (EindDatum == null && EindDag != data.EindDag)
                ? p.ApplyEvent(new PlotboekDagenAangepast
                {
                    StartDag = data.StartDag,
                    EindDag = data.EindDag,
                    Who = updatedBy.Id,
                })
                : Result.For(p));
    }

    internal void When(PlotboekTitelAangepast @event)
    {
        Titel = @event.Titel;
    }

    internal void When(PlotboekDagenAangepast @event)
    {
        StartDag = @event.StartDag;
        EindDag = @event.EindDag;
    }

    public sealed class BasisData
    {
        public Dag StartDag { get; init; }

        public Dag EindDag { get; init; }

        public string Titel { get; init; }
    }
}
