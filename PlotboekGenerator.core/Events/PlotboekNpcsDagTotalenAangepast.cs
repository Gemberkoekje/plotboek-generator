using System.Collections.Generic;
using System.Linq;

namespace PlotboekGenerator.core.Events;

public sealed record PlotboekNpcsDagTotalenAangepast : AuditTrailEvent
{
    required public Dictionary<Dag, int> NpcsDagTotalen { get; init; }

    public override string What =>
        "NPC-aantallen per dag aangepast: " +
        string.Join(", ", NpcsDagTotalen.Select(kv => $"{kv.Key}: {kv.Value}"));
}
