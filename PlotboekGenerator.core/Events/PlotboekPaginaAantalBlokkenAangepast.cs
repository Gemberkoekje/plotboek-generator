namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaAantalBlokkenAangepast : AuditTrailEvent
{
    required public AantalBlokken AantalBlokken { get; init; }

    public override string What => $"<strong>Aantal blokken aangepast:</strong> {AantalBlokken}";
}
