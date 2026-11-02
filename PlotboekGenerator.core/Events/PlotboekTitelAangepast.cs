using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekTitelAangepast : AuditTrailEvent
{
    required public string Titel { get; init; }

    public override string What => $"<strong>{CoreTekst.TitelAangepast}</strong> {Titel}";
}
