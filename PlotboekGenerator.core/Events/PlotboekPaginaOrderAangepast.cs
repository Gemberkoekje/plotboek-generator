using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaOrderAangepast : AuditTrailEvent
{
    required public int Order { get; init; }

    required public Dag Dag { get; init; }

    required public Starttijd Starttijd { get; init; }

    public override string What => $"<strong>{CoreTekst.VolgordeAangepast}</strong> {string.Format(CultureInfo.CurrentCulture, CoreTekst.VolgordePositie, Order, Dag.Naam(), Starttijd)}";
}
