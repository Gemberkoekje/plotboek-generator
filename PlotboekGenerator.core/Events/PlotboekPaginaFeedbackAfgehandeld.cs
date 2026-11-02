using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when an HSL signs off a feedback point that was responded to. The point moves to
/// <see cref="Enums.FeedbackStatus.Afgehandeld"/> and folds away out of sight.
/// </summary>
public sealed record PlotboekPaginaFeedbackAfgehandeld : AuditTrailEvent
{
    required public FeedbackId Id { get; init; }

    public override string What => $"<strong>{CoreTekst.FeedbackAfgehandeld}</strong>";
}
