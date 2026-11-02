using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekPaginaOrderAangepastV2 : AuditTrailEvent
{
    required public int Order { get; init; }

    required public Dag Dag { get; init; }

    /// <summary>The starttijd ID, corresponding to the <see cref="Starttijd"/> enum member name (e.g. "Tijd1030", "Tijd2000").</summary>
    required public string StarttijdId { get; init; }

    public override string What => $"<strong>{CoreTekst.VolgordeAangepast}</strong> {string.Format(CultureInfo.CurrentCulture, CoreTekst.VolgordePositie, Order, Dag.Naam(), StarttijdId)}";
}
