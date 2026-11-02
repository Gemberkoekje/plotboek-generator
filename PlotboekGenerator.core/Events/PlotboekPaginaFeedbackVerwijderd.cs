namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when an HSL withdraws a feedback point. The point disappears from the pagina's
/// feedback list; the audit trail keeps the history of it.
/// </summary>
public sealed record PlotboekPaginaFeedbackVerwijderd : AuditTrailEvent
{
    required public FeedbackId Id { get; init; }

    public override string What => "<strong>Feedback verwijderd</strong>";
}
