using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// The feedback points raised on this encounter, in the order they were raised. Empty when there
    /// is none.
    ///
    /// Deliberately absent from <see cref="Data"/> and from the <c>|</c>-chain in
    /// <see cref="Aanpassen(Data, PlotboekUser)"/>: an ordinary pagina save can never wipe feedback, and
    /// the HSL and SL can edit the same plot at the same time without carrying each other's changes.
    /// It IS part of <see cref="ExportData"/>, because the plot list has to show the icon without
    /// replaying every stream.
    /// </summary>
    public IReadOnlyList<FeedbackData> Feedback { get; private set; } = [];

    /// <summary>
    /// Whether at least one feedback point is still open. This is the invariant behind the block on
    /// "volledig klaar": open feedback ⇒ the plot is not fully done. Answered and signed-off points
    /// do not block.
    /// </summary>
    public bool HeeftOpenstaandeFeedback => Feedback.Any(f => f.Status == FeedbackStatus.Open);

    /// <summary>
    /// Raises a new feedback point on this encounter. The point starts out open, so it blocks
    /// "volledig klaar" until someone responds.
    ///
    /// When the plot was already marked "volledig klaar", the flag is switched back off in the same
    /// call: that keeps the invariant true at all times and puts the ⚠ back in the list right away.
    /// </summary>
    /// <param name="tekst">The feedback, as plain text. Required.</param>
    /// <param name="gegevenDoor">The HSL raising the point.</param>
    /// <returns>The updated pagina, or an invalid result when the text is empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="gegevenDoor"/> is <c>null</c>.</exception>
    public Result<PlotboekPagina> FeedbackToevoegen(string tekst, PlotboekUser gegevenDoor)
    {
        ArgumentNullException.ThrowIfNull(gegevenDoor);

        if (string.IsNullOrWhiteSpace(tekst))
        {
            return Result.WithMessages<PlotboekPagina>(
                ValidationMessage.Error(CoreTekst.FeedbackZonderTekst));
        }

        var id = FeedbackId.Next();

        // The aggregate is immutable: ApplyEvent hands back a NEW instance, so the untick has to run
        // on the result of the first step, not on `this`. AanpassenVolledigKlaar is a no-op when the
        // flag is already false, so it can be chained unconditionally.
        return Result.For(this)
            | (p => p.ToevoegenFeedback(id, tekst.Trim(), gegevenDoor.Id))
            | (p => p.AanpassenVolledigKlaar(false, gegevenDoor.Id));
    }

    /// <summary>
    /// Records a response to an open or reopened feedback point, which stops it blocking
    /// "volledig klaar". Anyone with edit rights on the plot may respond, not just the responsible SL.
    /// </summary>
    /// <param name="id">The feedback point to respond to.</param>
    /// <param name="reactie">The response, as plain text. Required.</param>
    /// <param name="beantwoordDoor">Whoever writes the response.</param>
    /// <returns>The updated pagina, or an invalid result when the id is unknown, the response is empty, or the point is already signed off.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="beantwoordDoor"/> is <c>null</c>.</exception>
    public Result<PlotboekPagina> FeedbackBeantwoorden(FeedbackId id, string reactie, PlotboekUser beantwoordDoor)
    {
        ArgumentNullException.ThrowIfNull(beantwoordDoor);

        if (Zoek(id) is not { } feedback)
        {
            return OnbekendeFeedback();
        }

        if (string.IsNullOrWhiteSpace(reactie))
        {
            return Result.WithMessages<PlotboekPagina>(
                ValidationMessage.Error(CoreTekst.FeedbackReactieZonderTekst));
        }

        if (feedback.Status == FeedbackStatus.Afgehandeld)
        {
            return Result.WithMessages<PlotboekPagina>(
                ValidationMessage.Error(CoreTekst.FeedbackAlAfgehandeld));
        }

        return ApplyEvent(new PlotboekPaginaFeedbackBeantwoord
        {
            Id = id,
            Reactie = reactie.Trim(),
            Who = beantwoordDoor.Id,
        });
    }

    /// <summary>
    /// Signs off a feedback point that has been responded to. Only the HSL does this; the point folds
    /// away out of sight afterwards.
    /// </summary>
    /// <param name="id">The feedback point to sign off.</param>
    /// <param name="afgehandeldDoor">The HSL signing it off.</param>
    /// <returns>The updated pagina, or an invalid result when the id is unknown or the point has not been responded to.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="afgehandeldDoor"/> is <c>null</c>.</exception>
    public Result<PlotboekPagina> FeedbackAfhandelen(FeedbackId id, PlotboekUser afgehandeldDoor)
    {
        ArgumentNullException.ThrowIfNull(afgehandeldDoor);

        if (Zoek(id) is not { } feedback)
        {
            return OnbekendeFeedback();
        }

        if (feedback.Status != FeedbackStatus.Beantwoord)
        {
            return Result.WithMessages<PlotboekPagina>(
                ValidationMessage.Error(CoreTekst.FeedbackAfhandelenAlleenBeantwoord));
        }

        return ApplyEvent(new PlotboekPaginaFeedbackAfgehandeld
        {
            Id = id,
            Who = afgehandeldDoor.Id,
        });
    }

    /// <summary>
    /// Puts a feedback point back on open, so it blocks "volledig klaar" again. The earlier response
    /// stays readable.
    /// </summary>
    /// <param name="id">The feedback point to reopen.</param>
    /// <param name="reden">Optional note explaining why it is being reopened.</param>
    /// <param name="heropendDoor">The HSL reopening it.</param>
    /// <returns>The updated pagina, or an invalid result when the id is unknown or the point is already open.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="heropendDoor"/> is <c>null</c>.</exception>
    public Result<PlotboekPagina> FeedbackHeropenen(FeedbackId id, string reden, PlotboekUser heropendDoor)
    {
        ArgumentNullException.ThrowIfNull(heropendDoor);

        if (Zoek(id) is not { } feedback)
        {
            return OnbekendeFeedback();
        }

        if (feedback.Status == FeedbackStatus.Open)
        {
            return Result.WithMessages<PlotboekPagina>(
                ValidationMessage.Error(CoreTekst.FeedbackAlOpen));
        }

        // Same reasoning as FeedbackToevoegen: the point blocks again, so the plot is no longer done.
        return Result.For(this)
            | (p => p.HeropenenFeedback(id, reden?.Trim() ?? string.Empty, heropendDoor.Id))
            | (p => p.AanpassenVolledigKlaar(false, heropendDoor.Id));
    }

    /// <summary>
    /// Withdraws a feedback point entirely. Only the HSL does this, for points raised in error; the
    /// audit trail keeps the history.
    /// </summary>
    /// <param name="id">The feedback point to withdraw.</param>
    /// <param name="verwijderdDoor">The HSL withdrawing it.</param>
    /// <returns>The updated pagina, or an invalid result when the id is unknown.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="verwijderdDoor"/> is <c>null</c>.</exception>
    public Result<PlotboekPagina> FeedbackVerwijderen(FeedbackId id, PlotboekUser verwijderdDoor)
    {
        ArgumentNullException.ThrowIfNull(verwijderdDoor);

        if (Zoek(id) is null)
        {
            return OnbekendeFeedback();
        }

        return ApplyEvent(new PlotboekPaginaFeedbackVerwijderd
        {
            Id = id,
            Who = verwijderdDoor.Id,
        });
    }

    internal void When(PlotboekPaginaFeedbackToegevoegd @event)
    {
        Feedback = [.. Feedback, FeedbackData.Create(@event.Id, @event.Tekst, @event.Who, @event.When)];
    }

    internal void When(PlotboekPaginaFeedbackBeantwoord @event)
    {
        Feedback = [.. Feedback.Select(f => f.Id == @event.Id
            ? f with
            {
                Reactie = @event.Reactie,
                ReactieDoor = @event.Who,
                ReactieOp = @event.When,
                Status = FeedbackStatus.Beantwoord,
            }
            : f)];
    }

    internal void When(PlotboekPaginaFeedbackAfgehandeld @event)
    {
        Feedback = [.. Feedback.Select(f => f.Id == @event.Id ? f with { Status = FeedbackStatus.Afgehandeld } : f)];
    }

    internal void When(PlotboekPaginaFeedbackHeropend @event)
    {
        // The earlier response deliberately stays on the record: reopening asks for a follow-up, it
        // does not erase what was said.
        Feedback = [.. Feedback.Select(f => f.Id == @event.Id ? f with { Status = FeedbackStatus.Open } : f)];
    }

    internal void When(PlotboekPaginaFeedbackVerwijderd @event)
    {
        Feedback = [.. Feedback.Where(f => f.Id != @event.Id)];
    }

    /// <summary>Appends the "raised" event. Split out so the <c>|</c>-chain can reach it on the successor instance.</summary>
    private Result<PlotboekPagina> ToevoegenFeedback(FeedbackId id, string tekst, string who)
        => ApplyEvent(new PlotboekPaginaFeedbackToegevoegd
        {
            Id = id,
            Tekst = tekst,
            Who = who,
        });

    /// <summary>Appends the "reopened" event. Split out for the same reason as <see cref="ToevoegenFeedback"/>.</summary>
    private Result<PlotboekPagina> HeropenenFeedback(FeedbackId id, string reden, string who)
        => ApplyEvent(new PlotboekPaginaFeedbackHeropend
        {
            Id = id,
            Reden = reden,
            Who = who,
        });

    private FeedbackData Zoek(FeedbackId id) => Feedback.FirstOrDefault(f => f.Id == id);

    private static Result<PlotboekPagina> OnbekendeFeedback()
        => Result.WithMessages<PlotboekPagina>(
            ValidationMessage.Error(CoreTekst.FeedbackBestaatNiet));
}
