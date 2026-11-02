using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaStarttijdAangepastV2 : AuditTrailEvent
{
    /// <summary>The starttijd ID, corresponding to the <see cref="Starttijd"/> enum member name (e.g. "Tijd1030", "Tijd2000").</summary>
    required public string StarttijdId { get; init; }

    public override string What => $"<strong>{CoreTekst.StarttijdAangepast}</strong> {StarttijdId}";
}
