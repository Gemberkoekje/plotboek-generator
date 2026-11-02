namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when the reusable bijlagen defined on a plotboek are changed (added, edited, reordered
/// or removed). Carries the full replacement list.
/// </summary>
public sealed record PlotboekBijlagenAangepast : AuditTrailEvent
{
    required public IReadOnlyList<BijlageData> Bijlagen { get; init; }

    public override string What => $"Bijlagen aangepast ({Bijlagen.Count})";
}
