using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekSamenvattingAlgemeenPlotAangepast : AuditTrailEvent
{
    required public string SamenvattingAlgemeenPlot { get; init; }

    public override string What => $"<strong>{CoreTekst.SamenvattingAlgemeenPlotAangepast}</strong>";

    public override string Details => string.IsNullOrWhiteSpace(SamenvattingAlgemeenPlot) ? null : SamenvattingAlgemeenPlot;
}
