namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Explicit icon flags set by the user for this pagina.
    /// </summary>
    public PlotboekPaginaIcons Iconen { get; private set; }

    private Result<PlotboekPagina> AanpassenIconen(PlotboekPaginaIcons iconen, string who)
    {
        if (Iconen == iconen)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaIconenAangepast
        {
            Iconen = iconen,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaIconenAangepast @event)
    {
        Iconen = @event.Iconen;
    }
}
