namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaIsFlexibelNachtPlotAangepast : AuditTrailEvent
{
    required public bool IsFlexibelNachtPlot { get; init; }

    public override string What => IsFlexibelNachtPlot
        ? "<strong>Encounter gemarkeerd als flexibel nacht plot</strong>"
        : "<strong>Encounter gemarkeerd als niet flexibel nacht plot</strong>";
}
