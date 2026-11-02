using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekNpcsDagTotalenAangepast : AuditTrailEvent
{
    required public Dictionary<Dag, int> NpcsDagTotalen { get; init; }

    public override string What => string.Format(
        CultureInfo.CurrentCulture,
        CoreTekst.NpcsDagTotalenAangepast,
        string.Join(", ", NpcsDagTotalen.Select(kv => $"{kv.Key.Naam()}: {kv.Value}")));
}
