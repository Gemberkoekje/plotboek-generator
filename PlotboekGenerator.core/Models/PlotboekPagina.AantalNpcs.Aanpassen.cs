namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public AantalNpcsData AantalNpcs { get; private set; }

    private Result<PlotboekPagina> AanpassenAantalNpcs(AantalNpcsData aantalNpcs, string who)
    {
        if (AantalNpcs == aantalNpcs)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = aantalNpcs.AlleNpcs,
            GeenNpcsNodig = aantalNpcs.GeenNpcsNodig,
            Rollen = aantalNpcs.Rollen?.ToList() ?? [],
            Who = who,
        });
    }

    internal void When(PlotboekPaginaAantalNpcsAangepast @event)
    {
        AantalNpcs = AantalNpcsData.Create(@event.Rollen, @event.AlleNpcs);
    }

    internal void When(PlotboekPaginaAantalNpcsAangepastV2 @event)
    {
        AantalNpcs = AantalNpcsData.Create(@event.Rollen, @event.AlleNpcs, @event.GeenNpcsNodig);
    }
}
