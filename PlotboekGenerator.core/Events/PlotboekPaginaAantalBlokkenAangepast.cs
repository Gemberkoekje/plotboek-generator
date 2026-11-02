using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaAantalBlokkenAangepast : AuditTrailEvent
{
    required public AantalBlokken AantalBlokken { get; init; }

    public override string What => $"<strong>{CoreTekst.AantalBlokkenAangepast}</strong> {AantalBlokken}";
}
