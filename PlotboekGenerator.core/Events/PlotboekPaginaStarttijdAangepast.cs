namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaStarttijdAangepast : AuditTrailEvent
{
    required public Starttijd Starttijd { get; init; }

    public override string What => $"<strong>Starttijd aangepast:</strong> {Starttijd}";
}
