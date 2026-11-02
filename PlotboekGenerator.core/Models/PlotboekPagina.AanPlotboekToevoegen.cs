namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public PlotboekId PlotboekId { get; private set; }

    public Result<PlotboekPagina> AanPlotboekToevoegen(PlotboekId plotboekId, string plotboekName, PlotboekUser updatedBy)
    {
        return ApplyEvent(new PlotboekPaginaAanPlotboekToegevoegd
        {
            PlotboekId = plotboekId,
            PlotboekName = plotboekName,
            Who = updatedBy.Id,
        });
    }

    internal void When(PlotboekPaginaAanPlotboekToegevoegd @event)
    {
        PlotboekId = @event.PlotboekId;
    }
}
