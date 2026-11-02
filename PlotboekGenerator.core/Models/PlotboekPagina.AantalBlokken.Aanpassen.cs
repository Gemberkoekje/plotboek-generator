namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>The aantal blokken ID as a string (e.g. "EenBlok", "TweeBlokken").</summary>
    public string AantalBlokkenId { get; private set; }

    private Result<PlotboekPagina> AanpassenAantalBlokken(string aantalBlokkenId, string who)
    {
        if (AantalBlokkenId == aantalBlokkenId)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaAantalBlokkenAangepastV2
        {
            AantalBlokkenId = aantalBlokkenId,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaAantalBlokkenAangepast @event)
    {
        AantalBlokkenId = @event.AantalBlokken.ToString();
    }

    internal void When(PlotboekPaginaAantalBlokkenAangepastV2 @event)
    {
        AantalBlokkenId = @event.AantalBlokkenId;
    }
}
