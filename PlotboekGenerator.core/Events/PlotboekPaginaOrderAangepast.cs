namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaOrderAangepast : AuditTrailEvent
{
    required public int Order { get; init; }

    required public Dag Dag { get; init; }

    required public Starttijd Starttijd { get; init; }

    public override string What => $"<strong>Volgorde aangepast:</strong> positie {Order} in {Dag} - {Starttijd}";
}
