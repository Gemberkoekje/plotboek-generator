using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaAanPlotboekToegevoegd : AuditTrailEvent
{
    required public PlotboekId PlotboekId { get; init; }

    required public string PlotboekName { get; init; }

    public override string What => $"<strong>{CoreTekst.PaginaToegevoegd}</strong> {string.Format(CultureInfo.CurrentCulture, CoreTekst.PaginaToegevoegdAanPlotboek, $"<em>{PlotboekName}</em>")}";
}
