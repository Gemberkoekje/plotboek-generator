namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when an HSL adds a feedback point to a pagina. The point starts out
/// <see cref="Enums.FeedbackStatus.Open"/> and blocks the pagina from being marked "volledig klaar".
/// </summary>
public sealed record PlotboekPaginaFeedbackToegevoegd : AuditTrailEvent
{
    required public FeedbackId Id { get; init; }

    required public string Tekst { get; init; }

    public override string What => "<strong>Feedback toegevoegd</strong>";

    public override string Details => PlainTextDetails(Tekst);
}
