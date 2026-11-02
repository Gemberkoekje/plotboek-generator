using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PlotboekGenerator.Application.CommandHandlers.Export;
using PlotboekGenerator.Application.CommandHandlers.Plotboek;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.core.Events;
using PlotboekGenerator.infrastructure.Adapters.Application;
using PlotboekGenerator.infrastructure.Services;
using PlotboekGenerator.test.TestHelpers;

namespace PlotboekGenerator.test.Pages;

/// <summary>
/// Page-model integration specs for the feedback loop: the six handlers, the rights matrix from the
/// feature design (admin raises/signs off/reopens/withdraws, everyone with edit rights responds, NPCs
/// see nothing, Readers see but do nothing), and the read model that the plot list icons are driven from.
/// </summary>
public sealed class PlotboekModel_Feedback_specs
{
    private const string AdminUserId = "00000000-0000-0000-0000-0000000000a1";
    private const string SlUserId = "00000000-0000-0000-0000-0000000000b1";

    private TestDocumentStore _testStore = null!;
    private InMemoryEventStore _eventStore = null!;
    private PlotboekPaginaRepository _paginaRepository = null!;
    private IPlotboekRepository _plotboekRepository = null!;
    private ExportPlotboekCommandHandler _exportPlotboekHandler = null!;
    private CreatePlotboekCommandHandler _createPlotboekHandler = null!;

    [SetUp]
    public void SetUp()
    {
        _testStore = new TestDocumentStore();
        _eventStore = new InMemoryEventStore();
        _paginaRepository = new PlotboekPaginaRepository(_eventStore);
        _plotboekRepository = new MartenPlotboekRepositoryAdapter(
            new PlotboekRepository(_eventStore),
            _testStore,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenPlotboekRepositoryAdapter>.Instance);
        _exportPlotboekHandler = new ExportPlotboekCommandHandler(
            new PlotboekExportBuilder(_testStore),
            new DocumentGenerationService(TestTemplate.Provider),
            Substitute.For<ILogger<ExportPlotboekCommandHandler>>());
        _createPlotboekHandler = new CreatePlotboekCommandHandler(
            _plotboekRepository,
            new EventStoreAuditTrailWriter(_eventStore),
            Substitute.For<ILogger<CreatePlotboekCommandHandler>>());
    }

    // =======================================================================
    // Rights matrix
    // =======================================================================
    [Test]
    public void An_admin_may_do_everything_with_feedback()
    {
        var model = CreateModel(UserRoles.Admin);

        model.CanManageFeedback.Should().BeTrue();
        model.CanRespondToFeedback.Should().BeTrue();
        model.CanSeeFeedback.Should().BeTrue();
    }

    [Test]
    public void A_regular_sl_may_only_respond()
    {
        var model = CreateModel(UserRoles.User);

        model.CanManageFeedback.Should().BeFalse();
        model.CanRespondToFeedback.Should().BeTrue();
        model.CanSeeFeedback.Should().BeTrue();
    }

    [Test]
    public void An_npc_sees_no_feedback_at_all()
    {
        var model = CreateModel(UserRoles.NPC);

        model.CanManageFeedback.Should().BeFalse();
        model.CanRespondToFeedback.Should().BeFalse();
        model.CanSeeFeedback.Should().BeFalse();
    }

    [Test]
    public void A_reader_sees_feedback_but_does_nothing_with_it()
    {
        var model = CreateModel(UserRoles.Reader);

        model.CanManageFeedback.Should().BeFalse();
        model.CanRespondToFeedback.Should().BeFalse();
        model.CanSeeFeedback.Should().BeTrue("following how the plots come along is what a reader is for");
        model.IsPlotboekReadOnly.Should().BeTrue();
    }

    // =======================================================================
    // addFeedback
    // =======================================================================
    [Test]
    public async Task An_admin_can_add_feedback()
    {
        var pagina = await SeedPaginaAsync();
        var model = CreateModel(UserRoles.Admin, AdminUserId);
        BindTo(model, pagina.Id);
        model.Action = "addFeedback";
        model.NieuweFeedbackTekst = "De NPC-rollen kloppen niet.";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        var stored = Stored(pagina.Id);
        stored.Feedback.Should().ContainSingle();
        stored.Feedback[0].Tekst.Should().Be("De NPC-rollen kloppen niet.");
        stored.Feedback[0].GegevenDoor.Should().Be(AdminUserId);
        stored.Feedback[0].Status.Should().Be(FeedbackStatus.Open);
        stored.HeeftOpenstaandeFeedback.Should().BeTrue();
    }

    [Test]
    public async Task A_regular_sl_cannot_add_feedback()
    {
        var pagina = await SeedPaginaAsync();
        var model = CreateModel(UserRoles.User);
        BindTo(model, pagina.Id);
        model.Action = "addFeedback";
        model.NieuweFeedbackTekst = "Stiekem zelf feedback geven.";

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        model.ModelState.IsValid.Should().BeFalse();
        Stored(pagina.Id).Feedback.Should().BeEmpty();
    }

    [Test]
    public async Task Adding_feedback_without_text_is_rejected_with_a_message()
    {
        var pagina = await SeedPaginaAsync();
        var model = CreateModel(UserRoles.Admin, AdminUserId);
        BindTo(model, pagina.Id);
        model.Action = "addFeedback";
        model.NieuweFeedbackTekst = "   ";

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        model.ModelState.IsValid.Should().BeFalse();
        Stored(pagina.Id).Feedback.Should().BeEmpty();
    }

    [Test]
    public async Task Adding_feedback_to_a_finished_plot_unticks_volledig_klaar()
    {
        var pagina = await SeedPaginaAsync(isVolledigKlaar: true);
        Stored(pagina.Id).IsVolledigKlaar.Should().BeTrue();

        var model = CreateModel(UserRoles.Admin, AdminUserId);
        BindTo(model, pagina.Id);
        model.Action = "addFeedback";
        model.NieuweFeedbackTekst = "Toch nog iets.";

        await model.OnPost();

        var stored = Stored(pagina.Id);
        stored.IsVolledigKlaar.Should().BeFalse();
        stored.HeeftOpenstaandeFeedback.Should().BeTrue();
    }

    // =======================================================================
    // answerFeedback / seenFeedback
    // =======================================================================
    [Test]
    public async Task A_regular_sl_can_respond_to_feedback()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync();
        var model = CreateModel(UserRoles.User);
        BindTo(model, pagina.Id);
        model.Action = "answerFeedback";
        model.SelectedFeedbackId = feedbackId;
        model.FeedbackReactie = "Aangepast, zie de nieuwe rollen.";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        var stored = Stored(pagina.Id);
        stored.Feedback[0].Status.Should().Be(FeedbackStatus.Beantwoord);
        stored.Feedback[0].Reactie.Should().Be("Aangepast, zie de nieuwe rollen.");
        stored.Feedback[0].ReactieDoor.Should().Be(SlUserId);
        stored.HeeftOpenstaandeFeedback.Should().BeFalse();
    }

    [Test]
    public async Task Responding_with_an_empty_reaction_is_rejected_and_the_point_stays_open()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync();
        var model = CreateModel(UserRoles.User);
        BindTo(model, pagina.Id);
        model.Action = "answerFeedback";
        model.SelectedFeedbackId = feedbackId;
        model.FeedbackReactie = "   ";

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        model.ModelState.IsValid.Should().BeFalse();
        Stored(pagina.Id).Feedback[0].Status.Should().Be(FeedbackStatus.Open);
    }

    [Test]
    public async Task The_gezien_button_unblocks_without_typing_anything()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync();
        var model = CreateModel(UserRoles.User);
        BindTo(model, pagina.Id);
        model.Action = "seenFeedback";
        model.SelectedFeedbackId = feedbackId;

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        var stored = Stored(pagina.Id);
        stored.Feedback[0].Status.Should().Be(FeedbackStatus.Beantwoord);
        stored.Feedback[0].Reactie.Should().Be(PlotboekModel.FeedbackGezienReactie);
        stored.HeeftOpenstaandeFeedback.Should().BeFalse();
    }

    // =======================================================================
    // resolveFeedback / reopenFeedback / deleteFeedback
    // =======================================================================
    [Test]
    public async Task An_admin_can_sign_off_answered_feedback()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync(beantwoord: true);
        var model = CreateModel(UserRoles.Admin, AdminUserId);
        BindTo(model, pagina.Id);
        model.Action = "resolveFeedback";
        model.SelectedFeedbackId = feedbackId;

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();
        Stored(pagina.Id).Feedback[0].Status.Should().Be(FeedbackStatus.Afgehandeld);
    }

    [Test]
    public async Task A_regular_sl_cannot_sign_off_feedback()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync(beantwoord: true);
        var model = CreateModel(UserRoles.User);
        BindTo(model, pagina.Id);
        model.Action = "resolveFeedback";
        model.SelectedFeedbackId = feedbackId;

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        model.ModelState.IsValid.Should().BeFalse();
        Stored(pagina.Id).Feedback[0].Status.Should().Be(FeedbackStatus.Beantwoord);
    }

    [Test]
    public async Task An_admin_can_reopen_feedback_which_blocks_again()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync(beantwoord: true);
        var model = CreateModel(UserRoles.Admin, AdminUserId);
        BindTo(model, pagina.Id);
        model.Action = "reopenFeedback";
        model.SelectedFeedbackId = feedbackId;
        model.FeedbackHeropenReden = "Toch nog niet goed.";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        var stored = Stored(pagina.Id);
        stored.Feedback[0].Status.Should().Be(FeedbackStatus.Open);
        stored.Feedback[0].Reactie.Should().NotBeEmpty("de eerdere reactie blijft leesbaar staan");
        stored.HeeftOpenstaandeFeedback.Should().BeTrue();
    }

    [Test]
    public async Task An_admin_can_withdraw_feedback()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync();
        var model = CreateModel(UserRoles.Admin, AdminUserId);
        BindTo(model, pagina.Id);
        model.Action = "deleteFeedback";
        model.SelectedFeedbackId = feedbackId;

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        var stored = Stored(pagina.Id);
        stored.Feedback.Should().BeEmpty();
        stored.HeeftOpenstaandeFeedback.Should().BeFalse();
    }

    [Test]
    public async Task A_regular_sl_cannot_withdraw_feedback()
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync();
        var model = CreateModel(UserRoles.User);
        BindTo(model, pagina.Id);
        model.Action = "deleteFeedback";
        model.SelectedFeedbackId = feedbackId;

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        Stored(pagina.Id).Feedback.Should().ContainSingle();
    }

    [Test]
    public async Task A_feedback_action_on_an_unknown_point_is_rejected()
    {
        var (pagina, _) = await SeedPaginaMetFeedbackAsync();
        var model = CreateModel(UserRoles.Admin, AdminUserId);
        BindTo(model, pagina.Id);
        model.Action = "deleteFeedback";
        model.SelectedFeedbackId = FeedbackId.Next();

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        model.ModelState.IsValid.Should().BeFalse();
        Stored(pagina.Id).Feedback.Should().ContainSingle();
    }

    [Test]
    public async Task A_feedback_action_cannot_reach_a_pagina_in_another_plotboek()
    {
        var jaar = Year.Create(2026);
        var (vreemdePagina, feedbackId) = await SeedPaginaMetFeedbackAsync(eventId: EventIds.Zomer);

        // The action targets the Zomer pagina, but the page is pointed at the Voorjaar plotboek.
        _testStore.Seed(PlotboekExportData(jaar, EventIds.Voorjaar));
        var model = CreateModel(UserRoles.Admin, AdminUserId);
        model.Jaar = jaar;
        model.EventId = EventIds.Voorjaar;
        model.PaginaId = vreemdePagina.Id;
        model.Action = "deleteFeedback";
        model.SelectedFeedbackId = feedbackId;

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        Stored(vreemdePagina.Id).Feedback.Should().ContainSingle();
    }

    // =======================================================================
    // NPCs are locked out of every action, not just of the section
    // =======================================================================

    /// <summary>
    /// The rights properties are asserted directly above, but nothing proved that a POST actually
    /// bounces off them: OnPost dispatches on the Action string alone, so the only thing between an
    /// NPC and a feedback mutation is the guard inside each handler. Deliberately run against a
    /// released plotboek, because that is exactly when an NPC gets past
    /// <c>CanAccessSelectedPlotboek</c> and the handler guard becomes the last line of defence.
    /// One case per action.
    /// </summary>
    /// <param name="actie">The posted Action value.</param>
    [TestCase("addFeedback")]
    [TestCase("answerFeedback")]
    [TestCase("seenFeedback")]
    [TestCase("resolveFeedback")]
    [TestCase("reopenFeedback")]
    [TestCase("deleteFeedback")]
    public async Task An_npc_cannot_reach_any_feedback_action(string actie)
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync();
        _testStore.Seed(PlotboekExportData(Year.Create(2026), EventIds.Voorjaar, isVolledigKlaar: true));
        var before = Stored(pagina.Id).Feedback[0];

        var model = CreateModel(UserRoles.NPC);
        BindTo(model, pagina.Id);
        model.Action = actie;
        model.SelectedFeedbackId = feedbackId;
        model.NieuweFeedbackTekst = "Stiekem meepraten.";
        model.FeedbackReactie = "Stiekem reageren.";
        model.FeedbackHeropenReden = "Stiekem heropenen.";

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>("the NPC gets the page back, not a mutation");
        model.ModelState.IsValid.Should().BeFalse();

        var after = Stored(pagina.Id).Feedback;
        after.Should().ContainSingle("an NPC may not add or withdraw a point");
        after[0].Status.Should().Be(before.Status, "an NPC may not move a point through its lifecycle");
        after[0].Reactie.Should().Be(before.Reactie, "an NPC may not respond on someone else's behalf");
    }

    /// <summary>
    /// A Reader may open a plotboek that is not finished, unlike an NPC, so every handler is reached:
    /// each one has to refuse on its own.
    /// </summary>
    /// <param name="actie">The posted Action value.</param>
    [TestCase("addFeedback")]
    [TestCase("answerFeedback")]
    [TestCase("seenFeedback")]
    [TestCase("resolveFeedback")]
    [TestCase("reopenFeedback")]
    [TestCase("deleteFeedback")]
    [TestCase("save")]
    [TestCase("newPagina")]
    [TestCase("duplicatePagina")]
    [TestCase("deletePagina")]
    public async Task A_reader_changes_nothing(string actie)
    {
        var (pagina, feedbackId) = await SeedPaginaMetFeedbackAsync();
        var before = Stored(pagina.Id);

        var model = CreateModel(UserRoles.Reader);
        BindTo(model, pagina.Id);
        model.Action = actie;
        model.SelectedFeedbackId = feedbackId;
        model.NieuweFeedbackTekst = "Stiekem meepraten.";
        model.FeedbackReactie = "Stiekem reageren.";
        model.FeedbackHeropenReden = "Stiekem heropenen.";
        model.EditableTitle = "Stiekem hernoemd";

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>("the reader gets the page back, not a mutation");
        model.ModelState.IsValid.Should().BeFalse();
        _testStore.GetAll<PlotboekPagina.ExportData>().Should().ContainSingle("a reader adds and copies no pagina")
            .Which.Should().BeEquivalentTo(before, "a reader changes nothing about it either");
    }

    // =======================================================================
    // Helpers
    // =======================================================================
    private PlotboekPagina.ExportData Stored(PlotboekPaginaId paginaId)
        => _testStore.Load<PlotboekPagina.ExportData>(paginaId.ToString())!;

    private static void BindTo(PlotboekModel model, PlotboekPaginaId paginaId)
    {
        model.Jaar = Year.Create(2026);
        model.EventId = EventIds.Voorjaar;
        model.PaginaId = paginaId;
    }

    private async Task<PlotboekPagina> SeedPaginaAsync(
        bool isVolledigKlaar = false,
        string eventId = EventIds.Voorjaar)
    {
        var jaar = Year.Create(2026);
        var plotboekId = PlotboekId.Create(jaar, eventId);
        _testStore.Seed(PlotboekExportData(jaar, eventId));

        var user = TestUser(SlUserId, "TestSL");
        var pagina = new PlotboekPagina();
        pagina = pagina.AanPlotboekToevoegen(plotboekId, $"Plotboek {jaar} {eventId}", user).Value;
        pagina = pagina.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Handelaar",
                Dag = Dag.Donderdag,
                StarttijdId = "Tijd1030",
                Order = 1,
                VerantwoordelijkeSlId = SlUserId,
                VerantwoordelijkeSlName = "TestSL",
                IsVolledigKlaar = isVolledigKlaar,
            }, user).Value;

        await _paginaRepository.SaveAsync(pagina, CancellationToken.None);
        _testStore.Seed(pagina.GetData());
        return pagina;
    }

    private async Task<(PlotboekPagina Pagina, FeedbackId FeedbackId)> SeedPaginaMetFeedbackAsync(
        bool beantwoord = false,
        string eventId = EventIds.Voorjaar)
    {
        var pagina = await SeedPaginaAsync(eventId: eventId);

        pagina = pagina.FeedbackToevoegen("De NPC-rollen kloppen niet.", TestUser(AdminUserId, "TestHSL")).Value;
        var feedbackId = pagina.Feedback[0].Id;

        if (beantwoord)
        {
            pagina = pagina.FeedbackBeantwoorden(feedbackId, "Aangepast.", TestUser(SlUserId, "TestSL")).Value;
        }

        await _paginaRepository.SaveAsync(pagina, CancellationToken.None);
        _testStore.Seed(pagina.GetData());
        return (pagina, feedbackId);
    }

    private static Plotboek.ExportData PlotboekExportData(Year jaar, string eventId, bool isVolledigKlaar = false) => new ()
    {
        PlotboekId = PlotboekId.Create(jaar, eventId),
        Jaar = jaar,
        EventId = eventId,
        StartDag = Dag.Woensdag,
        EindDag = Dag.Zondag,
        Titel = $"Plotboek {jaar} {eventId}",
        IsVolledigKlaar = isVolledigKlaar,
    };

    private static PlotboekUser TestUser(string id, string userName) => new ()
    {
        Id = id,
        UserName = userName,
        Email = $"{userName}@example.com",
        Lidnummer = "12345",
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    private PlotboekModel CreateModel(string role, string userId = SlUserId)
    {
        var userName = role == UserRoles.Admin ? "TestHSL" : "TestSL";
        _testStore.Seed(TestUser(userId, userName));
        _testStore.Seed(new UserRole { UserId = userId, RoleName = role });

        var auditTrailService = Substitute.For<IAuditTrailService>();
        auditTrailService.GetAuditTrailAsync(Arg.Any<Uuid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AuditTrailEntry>>([]));

        var model = new PlotboekModel(
            _paginaRepository,
            _plotboekRepository,
            _testStore,
            Substitute.For<IGoogleDriveService>(),
            null!,
            _exportPlotboekHandler,
            _createPlotboekHandler,
            auditTrailService,
            Substitute.For<ILogger<PlotboekModel>>());

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Role, role),
                ],
                "TestAuth")),
        };

        model.PageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new PageActionDescriptor()))
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());

        return model;
    }
}
