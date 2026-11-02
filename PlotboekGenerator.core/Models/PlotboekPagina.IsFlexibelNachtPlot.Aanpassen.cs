namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Indicates whether this encounter is a flexible night plot: a flexible encounter
    /// that is meant to be scheduled in an evening/night slot (20:00 or later).
    /// The UI warns when it is set but the encounter is not in a 20:00-or-later slot.
    /// </summary>
    public bool IsFlexibelNachtPlot { get; private set; }

    private Result<PlotboekPagina> AanpassenIsFlexibelNachtPlot(bool isFlexibelNachtPlot, string who)
    {
        if (IsFlexibelNachtPlot == isFlexibelNachtPlot)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaIsFlexibelNachtPlotAangepast
        {
            IsFlexibelNachtPlot = isFlexibelNachtPlot,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaIsFlexibelNachtPlotAangepast @event)
    {
        IsFlexibelNachtPlot = @event.IsFlexibelNachtPlot;
    }
}
