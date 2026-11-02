namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// Feedback on a plot: the HSL raises points, whoever writes the plot responds, the HSL signs them
/// off or reopens them. An open point blocks the pagina from being marked "volledig klaar" (enforced
/// in the domain, see <see cref="PlotboekPagina.FeedbackToevoegen"/>).
///
/// Every action here is a targeted domain command on its own form, deliberately separate from the
/// pagina detail form: responding has to keep working while the plot itself is content-locked.
/// </summary>
public sealed partial class PlotboekModel
{
    /// <summary>
    /// The response stored by the "Gezien, geen actie" button. A fixed sentence rather than an empty
    /// response, so the card and the audit trail stay readable and the domain rule "a response needs
    /// text" holds without exception.
    /// </summary>
    public const string FeedbackGezienReactie = "Gezien, geen actie nodig.";

    /// <summary>The feedback point a resolve/reopen/answer/delete action applies to.</summary>
    [BindProperty]
    public FeedbackId SelectedFeedbackId { get; set; }

    /// <summary>Text of a new feedback point, posted by the admin-only "Feedback toevoegen" form.</summary>
    [BindProperty]
    public string? NieuweFeedbackTekst { get; set; }

    /// <summary>The response typed into the card of the feedback point being answered.</summary>
    [BindProperty]
    public string? FeedbackReactie { get; set; }

    /// <summary>Optional note explaining why a point is being reopened.</summary>
    [BindProperty]
    public string? FeedbackHeropenReden { get; set; }

    /// <summary>The feedback on the currently selected pagina, empty when there is none.</summary>
    public IReadOnlyList<FeedbackData> SelectedPaginaFeedback => SelectedPlotboekPagina?.Feedback ?? [];

    /// <summary>Points still waiting for a response. These are the ones blocking "volledig klaar".</summary>
    public IReadOnlyList<FeedbackData> OpenFeedback
        => [.. SelectedPaginaFeedback.Where(f => f.Status == FeedbackStatus.Open)];

    /// <summary>Points that have been responded to but not yet signed off by the HSL.</summary>
    public IReadOnlyList<FeedbackData> BeantwoordeFeedback
        => [.. SelectedPaginaFeedback.Where(f => f.Status == FeedbackStatus.Beantwoord)];

    /// <summary>Points the HSL has signed off. Folded away in the UI.</summary>
    public IReadOnlyList<FeedbackData> AfgehandeldeFeedback
        => [.. SelectedPaginaFeedback.Where(f => f.Status == FeedbackStatus.Afgehandeld)];

    /// <summary>Whether the selected pagina still has feedback blocking completion.</summary>
    public bool HeeftOpenstaandeFeedback => OpenFeedback.Count > 0;

    /// <summary>
    /// Only admins (HSL) raise, sign off, reopen or withdraw feedback.
    /// </summary>
    public bool CanManageFeedback => IsAdmin;

    /// <summary>
    /// Who may respond to feedback: everyone with edit rights on the plot — co-SLs and admins
    /// included, because in practice someone else picks it up. Deliberately NOT
    /// <c>CanEditCurrentPagina()</c>: responding has to work while the plot is content-locked (a
    /// "volledig klaar" pagina, or a closed plotboek), otherwise an SL could never unblock their own
    /// plot. NPCs see no feedback at all.
    /// </summary>
    public bool CanRespondToFeedback => !IsNpc;

    /// <summary>Whether the feedback section is shown at all. NPCs never see internal traffic.</summary>
    public bool CanSeeFeedback => !IsNpc;

    /// <summary>
    /// Formats a feedback timestamp for the <c>data-utc</c> attribute that timestamps.js converts to
    /// the reader's local time. Feedback timestamps are always stored in UTC, but a DateTime that has
    /// round-tripped through the document store can come back as <see cref="DateTimeKind.Unspecified"/>
    /// — the "o" format would then omit the trailing Z and the browser would read it as local time.
    /// </summary>
    /// <param name="moment">The stored timestamp.</param>
    /// <returns>A round-trip formatted string that is explicitly marked as UTC.</returns>
    public static string ToUtcAttribute(DateTime moment)
        => DateTime.SpecifyKind(moment, DateTimeKind.Utc).ToString("o");

    /// <summary>
    /// Resolves a user id to a display name via the SL list, falling back to the raw id so a user
    /// that is no longer an SL still renders something recognisable.
    /// </summary>
    public string GetFeedbackAuteurNaam(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "onbekend";
        }

        return AvailableSls.FirstOrDefault(sl => sl.Id == userId)?.DisplayName ?? userId;
    }

    private async Task<IActionResult> HandleAddFeedback()
    {
        if (!CanManageFeedback)
        {
            return await FeedbackNotAllowedResult("Alleen admins kunnen feedback geven.");
        }

        return await MutateFeedbackAsync(
            "added feedback to",
            (pagina, user) => pagina.FeedbackToevoegen(NieuweFeedbackTekst, user));
    }

    private async Task<IActionResult> HandleAnswerFeedback()
    {
        if (!CanRespondToFeedback)
        {
            return await FeedbackNotAllowedResult("Je mag niet op feedback reageren.");
        }

        return await MutateFeedbackAsync(
            "answered feedback on",
            (pagina, user) => pagina.FeedbackBeantwoorden(SelectedFeedbackId, FeedbackReactie, user));
    }

    private async Task<IActionResult> HandleSeenFeedback()
    {
        if (!CanRespondToFeedback)
        {
            return await FeedbackNotAllowedResult("Je mag niet op feedback reageren.");
        }

        return await MutateFeedbackAsync(
            "acknowledged feedback on",
            (pagina, user) => pagina.FeedbackBeantwoorden(SelectedFeedbackId, FeedbackGezienReactie, user));
    }

    private async Task<IActionResult> HandleResolveFeedback()
    {
        if (!CanManageFeedback)
        {
            return await FeedbackNotAllowedResult("Alleen admins kunnen feedback afhandelen.");
        }

        return await MutateFeedbackAsync(
            "resolved feedback on",
            (pagina, user) => pagina.FeedbackAfhandelen(SelectedFeedbackId, user));
    }

    private async Task<IActionResult> HandleReopenFeedback()
    {
        if (!CanManageFeedback)
        {
            return await FeedbackNotAllowedResult("Alleen admins kunnen feedback heropenen.");
        }

        return await MutateFeedbackAsync(
            "reopened feedback on",
            (pagina, user) => pagina.FeedbackHeropenen(SelectedFeedbackId, FeedbackHeropenReden, user));
    }

    private async Task<IActionResult> HandleDeleteFeedback()
    {
        if (!CanManageFeedback)
        {
            return await FeedbackNotAllowedResult("Alleen admins kunnen feedback verwijderen.");
        }

        return await MutateFeedbackAsync(
            "withdrew feedback on",
            (pagina, user) => pagina.FeedbackVerwijderen(SelectedFeedbackId, user));
    }

    /// <summary>
    /// Shared body for every feedback action: load the aggregate, run one targeted command, persist,
    /// and redirect back to the plot (post-redirect-get, so a refresh cannot re-submit).
    ///
    /// No optimistic-concurrency check on purpose: these commands touch only their own feedback point
    /// and never overwrite pagina content, so they always merge. The version bump they cause is what
    /// makes a stale pagina save fail — that direction is the point.
    /// </summary>
    /// <param name="actie">Verb phrase for the audit log line.</param>
    /// <param name="mutate">The domain command to run.</param>
    private async Task<IActionResult> MutateFeedbackAsync(
        string actie,
        Func<PlotboekPagina, PlotboekUser, Result<PlotboekPagina>> mutate)
    {
        // SelectedPlotboekPagina comes from the current plotboek's list, so this also stops an action
        // from reaching a pagina in another plotboek.
        if (!PaginaId.HasValue || SelectedPlotboekPagina is null)
        {
            logger.LogWarning("Feedback action for pagina {PaginaId} ignored: not part of plotboek {PlotboekId}", PaginaId, PlotboekId);
            return await FeedbackFailureResult();
        }

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            logger.LogWarning("No user found in session while handling feedback on pagina {PaginaId}", PaginaId);
            return await FeedbackFailureResult();
        }

        if (await paginaRepository.LoadAsync(PaginaId, HttpContext.RequestAborted) is not { } loaded || !loaded.IsValid)
        {
            logger.LogWarning("Plotboek pagina {PaginaId} not found while handling feedback", PaginaId);
            return await FeedbackFailureResult();
        }

        var mutated = mutate(loaded.Value, user);
        if (!mutated.IsValid)
        {
            logger.LogWarning(
                "Feedback action on plotboek pagina {PaginaId} rejected: {Errors}",
                PaginaId,
                string.Join(", ", mutated.Messages.Select(m => m.Message)));

            return await FeedbackFailureResult(mutated.Messages.Select(m => m.Message).OfType<string>());
        }

        var saveResult = await paginaRepository.SaveAsync(mutated.Value, CancellationToken.None);
        if (!saveResult.IsValid)
        {
            logger.LogWarning("Concurrency conflict while handling feedback on plotboek pagina {PaginaId}", PaginaId);
            ConcurrencyError = string.Join(" ", saveResult.Messages.Select(m => m.Message));
            return await FeedbackFailureResult();
        }

        session.Store(mutated.Value.GetData());
        await session.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("User {User} {Actie} plotboek pagina {PaginaId}", user.Id, actie, PaginaId);

        return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId });
    }

    /// <summary>
    /// Renders the page again after a rejected feedback action. OnPost deliberately skips
    /// <see cref="InitializePaginaEditableFields"/> because the detail form normally posts its own
    /// values — the feedback forms do not, so without this the detail panel would come back blank.
    /// </summary>
    private async Task<IActionResult> FeedbackFailureResult(IEnumerable<string>? messages = null)
    {
        foreach (var message in messages ?? [])
        {
            ModelState.AddModelError(string.Empty, message);
        }

        await LoadPlotboekSettings();
        LoadSelectedPlotboekPagina();
        LoadAvailableStarttijden();
        InitializePaginaEditableFields();

        return Page();
    }

    private async Task<IActionResult> FeedbackNotAllowedResult(string message)
    {
        logger.LogWarning("User without the required role attempted a feedback action on pagina {PaginaId}", PaginaId);
        return await FeedbackFailureResult([message]);
    }

    /// <summary>
    /// View model for a single feedback card. Carries the page model alongside the point, because the
    /// card needs the rights flags and the SL name lookup that live on it.
    /// </summary>
    public sealed record FeedbackKaart
    {
        required public PlotboekModel Page { get; init; }

        required public FeedbackData Punt { get; init; }
    }
}
