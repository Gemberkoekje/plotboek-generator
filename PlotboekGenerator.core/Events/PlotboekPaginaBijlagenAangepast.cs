using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when the set of plotboek-level bijlagen an encounter references is changed.
/// Carries the full replacement list of referenced bijlage IDs.
/// </summary>
public sealed record PlotboekPaginaBijlagenAangepast : AuditTrailEvent
{
    required public IReadOnlyList<BijlageId> BijlageIds { get; init; }

    public override string What => $"<strong>{CoreTekst.GekoppeldeBijlagenAangepast}</strong>";

    public override string Details => BijlageIds.Count == 0
        ? null
        : string.Join(", ", BijlageIds.Select(id => id.ToString()));
}
