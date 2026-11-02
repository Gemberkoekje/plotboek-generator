namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public Dag Dag { get; private set; }

    private Result<PlotboekPagina> AanpassenDag(Dag dag, string who)
    {
        if (Dag == dag)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaDagAangepast
        {
            Dag = dag,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaDagAangepast @event)
    {
        Dag = @event.Dag;
    }
}
