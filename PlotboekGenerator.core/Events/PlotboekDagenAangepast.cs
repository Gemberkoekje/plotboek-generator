namespace PlotboekGenerator.core.Events;

public sealed record PlotboekDagenAangepast : AuditTrailEvent
{
    required public Dag StartDag { get; init; }

    required public Dag EindDag { get; init; }

    public override string What => $"<strong>Dagen aangepast:</strong> {StartDag} t/m {EindDag}";
}
