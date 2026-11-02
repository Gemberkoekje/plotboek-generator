using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaDagAangepast : AuditTrailEvent
{
    required public Dag Dag { get; init; }

    public override string What => $"<strong>{CoreTekst.DagAangepast}</strong> {Dag.Naam()}";
}
