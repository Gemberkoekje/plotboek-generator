using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when a plotboek pagina's plotlijn reference is changed.
/// </summary>
public sealed record PlotboekPaginaPlotlijnAangepast : AuditTrailEvent
{
    /// <summary>
    /// The ID of the plotlijn this pagina belongs to,
    /// or null if this plot is self-contained.
    /// </summary>
    public PlotlijnId PlotlijnId { get; init; }

    public override string What => PlotlijnId.HasValue
        ? $"<strong>{CoreTekst.PlotlijnAangepast}</strong> {CoreTekst.PlotlijnToegevoegd}"
        : $"<strong>{CoreTekst.PlotlijnVerwijderd}</strong> {CoreTekst.PlotlijnZelfstandig}";
}
