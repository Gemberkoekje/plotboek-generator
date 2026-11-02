namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when an HSL reopens a feedback point. The point returns to
/// <see cref="Enums.FeedbackStatus.Open"/> and blocks "volledig klaar" again; the earlier response
/// stays readable.
/// </summary>
public sealed record PlotboekPaginaFeedbackHeropend : AuditTrailEvent
{
    required public FeedbackId Id { get; init; }

    required public string Reden { get; init; }

    public override string What => "<strong>Feedback heropend</strong>";

    public override string Details => PlainTextDetails(Reden);
}
