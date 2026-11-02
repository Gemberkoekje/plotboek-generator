using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaSamenvattingPlotAangepast : AuditTrailEvent
{
    required public string SamenvattingPlot { get; init; }

    public override string What => $"<strong>{CoreTekst.SamenvattingPlotAangepast}</strong>";

    public override string Details => string.IsNullOrWhiteSpace(SamenvattingPlot) ? null : SamenvattingPlot;
}
