namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public int Order { get; private set; }

    public Result<PlotboekPagina> UpdateOrder(int newOrder, Dag dag, string starttijdId, PlotboekUser updatedBy)
    {
        return ApplyEvent(new PlotboekPaginaOrderAangepastV2
        {
            Order = newOrder,
            Dag = dag,
            StarttijdId = starttijdId,
            Who = updatedBy.Id,
        });
    }

    internal void When(PlotboekPaginaOrderAangepast @event)
    {
        Order = @event.Order;
        Dag = @event.Dag;
        StarttijdId = @event.Starttijd.ToString();
    }

    internal void When(PlotboekPaginaOrderAangepastV2 @event)
    {
        Order = @event.Order;
        Dag = @event.Dag;
        StarttijdId = @event.StarttijdId;
    }
}
