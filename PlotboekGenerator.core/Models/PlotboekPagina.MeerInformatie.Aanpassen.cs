using PlotboekGenerator.core;

namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public string MeerInformatie { get; private set; }

    private Result<PlotboekPagina> AanpassenMeerInformatie(string meerInformatie, string who)
    {
        var current = TextNormalization.Normalize(MeerInformatie);
        var incoming = TextNormalization.Normalize(meerInformatie);
        TextNormalization.DebugLogComparison("MeerInformatie", current, incoming);
        if (string.Equals(current, incoming, StringComparison.Ordinal))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaMeerInformatieAangepast
        {
            MeerInformatie = incoming,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaMeerInformatieAangepast @event)
    {
        MeerInformatie = @event.MeerInformatie;
    }
}
