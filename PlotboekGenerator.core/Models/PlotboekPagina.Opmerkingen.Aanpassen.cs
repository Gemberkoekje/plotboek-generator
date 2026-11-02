namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// General remarks/notes for this encounter.
    /// </summary>
    public string Opmerkingen { get; private set; }

    private Result<PlotboekPagina> AanpassenOpmerkingen(string opmerkingen, string who)
    {
        if (string.Equals(Opmerkingen ?? string.Empty, opmerkingen ?? string.Empty, StringComparison.Ordinal))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaOpmerkingenAangepast
        {
            Opmerkingen = opmerkingen,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaOpmerkingenAangepast @event)
    {
        Opmerkingen = @event.Opmerkingen;
    }
}
