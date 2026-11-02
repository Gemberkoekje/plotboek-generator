namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaVolledigKlaarAangepast : AuditTrailEvent
{
    required public bool IsVolledigKlaar { get; init; }

    public override string What => IsVolledigKlaar
        ? "<strong>Plot gemarkeerd als volledig klaar</strong>"
        : "<strong>Plot gemarkeerd als nog niet klaar</strong>";
}
