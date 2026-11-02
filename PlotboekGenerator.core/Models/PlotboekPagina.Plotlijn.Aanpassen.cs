namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// The ID of the plotlijn this pagina belongs to,
    /// or null if this plot is self-contained.
    /// </summary>
    public PlotlijnId PlotlijnId { get; private set; }

    /// <summary>
    /// Adjusts only the plotlijn association of this pagina, leaving every other field untouched.
    /// Use this when (re)linking a pagina to a plotlijn: a full <see cref="Aanpassen(Data, PlotboekUser)"/>
    /// would reset every field the caller does not repopulate, wiping the pagina's content.
    /// </summary>
    public Result<PlotboekPagina> AanpassenPlotlijn(PlotlijnId plotlijnId, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(updatedBy);
        return AanpassenPlotlijn(plotlijnId, updatedBy.Id);
    }

    private Result<PlotboekPagina> AanpassenPlotlijn(PlotlijnId plotlijnId, string who)
    {
        if (Nullable.Equals(PlotlijnId, plotlijnId))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaPlotlijnAangepast
        {
            PlotlijnId = plotlijnId,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaPlotlijnAangepast @event)
    {
        PlotlijnId = @event.PlotlijnId;
    }
}
