using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Events;

/// <summary>
/// Event raised when someone with edit rights responds to a feedback point. The point moves to
/// <see cref="Enums.FeedbackStatus.Beantwoord"/> and stops blocking "volledig klaar".
/// </summary>
public sealed record PlotboekPaginaFeedbackBeantwoord : AuditTrailEvent
{
    required public FeedbackId Id { get; init; }

    required public string Reactie { get; init; }

    public override string What => $"<strong>{CoreTekst.FeedbackBeantwoord}</strong>";

    public override string Details => PlainTextDetails(Reactie);
}
