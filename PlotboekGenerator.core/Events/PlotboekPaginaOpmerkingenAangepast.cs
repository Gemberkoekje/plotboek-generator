namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaOpmerkingenAangepast : AuditTrailEvent
{
    required public string Opmerkingen { get; init; }

    public override string What => "<strong>Opmerkingen aangepast</strong>";

    public override string Details => string.IsNullOrWhiteSpace(Opmerkingen) ? null : Opmerkingen;
}
