namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaAanPlotboekToegevoegd : AuditTrailEvent
{
    required public PlotboekId PlotboekId { get; init; }

    required public string PlotboekName { get; init; }

    public override string What => $"<strong>Pagina toegevoegd</strong> aan plotboek <em>{PlotboekName}</em>";
}
