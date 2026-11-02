namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Indicates whether this encounter is player driven.
    /// Player driven encounters are shown under their own section and excluded from the time table.
    /// </summary>
    public bool IsSpelerGedreven { get; private set; }

    private Result<PlotboekPagina> AanpassenIsSpelerGedreven(bool isSpelerGedreven, string who)
    {
        if (IsSpelerGedreven == isSpelerGedreven)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaIsSpelerGedrevenAangepast
        {
            IsSpelerGedreven = isSpelerGedreven,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaIsSpelerGedrevenAangepast @event)
    {
        IsSpelerGedreven = @event.IsSpelerGedreven;
    }
}
