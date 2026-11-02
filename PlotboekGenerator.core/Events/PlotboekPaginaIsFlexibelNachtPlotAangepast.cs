using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsFlexibelNachtPlotAangepast : AuditTrailEvent
{
    required public bool IsFlexibelNachtPlot { get; init; }

    public override string What => IsFlexibelNachtPlot
        ? $"<strong>{CoreTekst.EncounterFlexibelNachtPlot}</strong>"
        : $"<strong>{CoreTekst.EncounterNietFlexibelNachtPlot}</strong>";
}
