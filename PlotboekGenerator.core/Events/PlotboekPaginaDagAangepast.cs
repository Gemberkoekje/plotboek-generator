namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaDagAangepast : AuditTrailEvent
{
    required public Dag Dag { get; init; }

    public override string What => $"<strong>Dag aangepast:</strong> {Dag}";
}
