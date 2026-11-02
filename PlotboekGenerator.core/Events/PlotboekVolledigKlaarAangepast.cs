namespace PlotboekGenerator.core.Events;

public sealed record PlotboekVolledigKlaarAangepast : AuditTrailEvent
{
    required public bool IsVolledigKlaar { get; init; }

    public override string What => IsVolledigKlaar
        ? "<strong>Plotboek gemarkeerd als volledig klaar</strong>"
        : "<strong>Plotboek gemarkeerd als nog niet klaar</strong>";
}
