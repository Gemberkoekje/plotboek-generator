namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaAantalBlokkenAangepastV2 : AuditTrailEvent
{
    /// <summary>The aantal blokken ID, corresponding to the <see cref="AantalBlokken"/> enum member name (e.g. "EenBlok", "TweeBlokken").</summary>
    required public string AantalBlokkenId { get; init; }

    public override string What => $"<strong>Aantal blokken aangepast:</strong> {AantalBlokkenId}";
}
