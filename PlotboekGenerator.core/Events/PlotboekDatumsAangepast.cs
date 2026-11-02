namespace PlotboekGenerator.core.Events;

public sealed record PlotboekDatumsAangepast : AuditTrailEvent
{
    required public DateOnly? StartDatum { get; init; }

    required public DateOnly? EindDatum { get; init; }

    public override string What => $"<strong>Datums aangepast:</strong> {StartDatum} t/m {EindDatum}";
}
