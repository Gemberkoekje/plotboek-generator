namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Debrief configuration for this encounter.
    /// </summary>
    public DebriefData Debrief { get; private set; }

    private Result<PlotboekPagina> AanpassenDebrief(DebriefData debrief, string who)
    {
        if (Debrief == debrief)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaDebriefAangepast
        {
            IsDebriefGewenst = debrief.IsDebriefGewenst,
            Vragen = debrief.Vragen?.ToList() ?? [],
            Who = who,
        });
    }

    internal void When(PlotboekPaginaDebriefAangepast @event)
    {
        Debrief = DebriefData.Create(@event.IsDebriefGewenst, @event.Vragen);
    }
}
