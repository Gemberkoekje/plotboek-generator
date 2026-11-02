namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public string Titel { get; private set; }

    private Result<PlotboekPagina> AanpassenTitel(string titel, string who)
    {
        if (Titel == titel)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaTitelAangepast
        {
            Titel = titel,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaTitelAangepast @event)
    {
        Titel = @event.Titel;
    }
}
