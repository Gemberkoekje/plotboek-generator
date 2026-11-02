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
using PlotboekGenerator.test.TestHelpers;
using PlotboekGenerator.infrastructure.Services;

namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_Integration_specs
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000001";
    private const string TestUserName = "TestSL";

    private TestDocumentStore _testStore = null!;
    private InMemoryEventStore _eventStore = null!;
    private PlotboekPaginaRepository _paginaRepository = null!;
    private IPlotboekRepository _plotboekRepository = null!;
    private IGoogleDriveService _googleDriveService = null!;
    private PlotboekGoogleDriveSyncService _googleDriveSyncService = null!;
    private IAuditTrailService _auditTrailService = null!;
    private IPlotboekExportDataBuilder _exportDataBuilder = null!;
    private IDocumentGenerator _documentGenerator = null!;
    private ExportPlotboekCommandHandler _exportPlotboekHandler = null!;
    private CreatePlotboekCommandHandler _createPlotboekHandler = null!;
    private ILogger<PlotboekModel> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _testStore = new TestDocumentStore();
        _eventStore = new InMemoryEventStore();
        _paginaRepository = new PlotboekPaginaRepository(_eventStore);
        var aggregateRepository = new PlotboekRepository(_eventStore);
        _plotboekRepository = new MartenPlotboekRepositoryAdapter(aggregateRepository, _testStore, Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenPlotboekRepositoryAdapter>.Instance);
        _googleDriveService = Substitute.For<IGoogleDriveService>();
        _auditTrailService = Substitute.For<IAuditTrailService>();
        _auditTrailService.GetAuditTrailAsync(Arg.Any<Uuid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AuditTrailEntry>>([]));
        _exportDataBuilder = new PlotboekExportBuilder(_testStore);
        _documentGenerator = new DocumentGenerationService(TestTemplate.Provider);
        _exportPlotboekHandler = new ExportPlotboekCommandHandler(
            _exportDataBuilder,
            _documentGenerator,
            Substitute.For<ILogger<ExportPlotboekCommandHandler>>());
        _createPlotboekHandler = new CreatePlotboekCommandHandler(
            _plotboekRepository,
            new EventStoreAuditTrailWriter(_eventStore),
            Substitute.For<ILogger<CreatePlotboekCommandHandler>>());
        _logger = Substitute.For<ILogger<PlotboekModel>>();
    }

    private PlotboekModel CreateModel(IGeminiSummaryService geminiSummaryService = null)
    {
        var model = new PlotboekModel(
            _paginaRepository,
            _plotboekRepository,
            _testStore,
            _googleDriveService,
            _googleDriveSyncService,
            _exportPlotboekHandler,
            _createPlotboekHandler,
            _auditTrailService,
            _logger,
            geminiSummaryService);

        var httpContext = new DefaultHttpContext
        {
            User = CreateAuthenticatedUser(),
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
        var modelState = new ModelStateDictionary();

        model.PageContext = new PageContext(actionContext)
        {
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState),
        };
        model.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());

        return model;
    }

    private static ClaimsPrincipal CreateAuthenticatedUser()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, TestUserName),
            new Claim(ClaimTypes.Role, UserRoles.User),
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private PlotboekUser CreateTestUser(string id = "") => new ()
    {
        Id = id.Length > 0 ? id : TestUserId,
        UserName = TestUserName,
        Email = "test@example.com",
        Lidnummer = "12345",
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    private void SeedTestUser()
    {
        _testStore.Seed(CreateTestUser());
        _testStore.Seed(new UserRole { UserId = TestUserId, RoleName = UserRoles.User });
    }

    private Plotboek.ExportData CreatePlotboekExportData(Year jaar, string eventId = EventIds.Voorjaar) => new ()
    {
        PlotboekId = PlotboekId.Create(jaar, eventId),
        Jaar = jaar,
        EventId = eventId,
        StartDag = Dag.Woensdag,
        EindDag = Dag.Zondag,
        Titel = $"Plotboek {jaar} {eventId}",
    };

    // =======================================================================
    // OnGet tests
    // =======================================================================
    [Test]
    public async Task OnGet_without_plotboekId_redirects_to_default_plotboek()
    {
        SeedTestUser();
        var plotboekData = CreatePlotboekExportData(Year.Create(2026));
        _testStore.Seed(plotboekData);

        var model = CreateModel();

        var result = await model.OnGet();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Test]
    public async Task OnGet_with_plotboekId_loads_paginas()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var pagina = new PlotboekPagina.ExportData
        {
            PlotboekId = plotboekId,
            PaginaId = PlotboekPaginaId.Next(),
            Titel = "Test Pagina",
            Dag = Dag.Donderdag,
            StarttijdId = "Tijd1030",
            Order = 1,
            VerantwoordelijkeSl = TestUserId,
        };
        _testStore.Seed(pagina);

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;

        var result = await model.OnGet();

        result.Should().BeOfType<PageResult>();
        model.PlotboekPaginas.Should().ContainSingle();
        model.PlotboekPaginas[0].Title.Should().Be("Test Pagina");
    }

    [Test]
    public async Task OnGet_with_plotboekId_and_paginaId_selects_pagina()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        var paginaId = PlotboekPaginaId.Next();
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));
        _testStore.Seed(new PlotboekPagina.ExportData
        {
            PlotboekId = plotboekId,
            PaginaId = paginaId,
            Titel = "Selected Pagina",
            Dag = Dag.Donderdag,
            StarttijdId = "Tijd1030",
            Order = 1,
            VerantwoordelijkeSl = TestUserId,
        });

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = paginaId;

        var result = await model.OnGet();

        result.Should().BeOfType<PageResult>();
        model.SelectedPlotboekPagina.Should().NotBeNull();
        model.SelectedPlotboekPagina!.Title.Should().Be("Selected Pagina");
    }

    [Test]
    public async Task OnGet_loads_audit_trail_when_show_flag_set()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var expectedEntries = new List<AuditTrailEntry>
        {
            new () { Who = "SomeUser", When = DateTime.UtcNow, What = "Test change" },
        };
        _auditTrailService.GetAuditTrailAsync(Arg.Any<Uuid>(), Arg.Any<CancellationToken>())
            .Returns(expectedEntries);

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.ShowPlotboekAuditTrail = true;

        await model.OnGet();

        model.PlotboekAuditTrail.Should().ContainSingle();
        model.PlotboekAuditTrail[0].What.Should().Be("Test change");
    }

    [Test]
    public async Task OnGet_with_RestorePaginaVersion_shows_historical_values_as_preview()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        var paginaId = PlotboekPaginaId.Next();
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        // Current read-model state for the pagina list / normal load.
        _testStore.Seed(new PlotboekPagina.ExportData
        {
            PlotboekId = plotboekId,
            PaginaId = paginaId,
            Titel = "Huidige titel",
            Dag = Dag.Donderdag,
            StarttijdId = "Tijd1030",
            Order = 1,
            VerantwoordelijkeSl = TestUserId,
        });

        // Event stream: three title changes (versions 1, 2, 3).
        await _eventStore.SaveEventsAsync((Uuid)paginaId, 0, new object[]
        {
            new PlotboekPaginaTitelAangepast { Titel = "V1", Who = TestUserId },
            new PlotboekPaginaTitelAangepast { Titel = "V2", Who = TestUserId },
            new PlotboekPaginaTitelAangepast { Titel = "Huidige titel", Who = TestUserId },
        });

        var when = new DateTime(2025, 3, 1, 9, 0, 0, DateTimeKind.Utc);
        _auditTrailService.GetAuditTrailAsync(Arg.Any<Uuid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AuditTrailEntry>>(
                [new AuditTrailEntry { Version = 1, Who = "Bob", When = when, What = "Titel aangepast" }]));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = paginaId;
        model.RestorePaginaVersion = 1;

        var result = await model.OnGet();

        result.Should().BeOfType<PageResult>();
        model.IsRestorePreview.Should().BeTrue();
        model.EditableTitle.Should().Be("V1", "the form is pre-filled with the historical value");
        model.PaginaVersion.Should().Be(3, "concurrency check must use the current version, not the historical one");
        model.RestoreFromWho.Should().Be("Bob");
        model.RestoreFromWhen.Should().Be(when);
    }

    [Test]
    public async Task OnGet_without_RestorePaginaVersion_shows_current_values()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        var paginaId = PlotboekPaginaId.Next();
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));
        _testStore.Seed(new PlotboekPagina.ExportData
        {
            PlotboekId = plotboekId,
            PaginaId = paginaId,
            Titel = "Huidige titel",
            Dag = Dag.Donderdag,
            StarttijdId = "Tijd1030",
            Order = 1,
            VerantwoordelijkeSl = TestUserId,
        });

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = paginaId;

        await model.OnGet();

        model.IsRestorePreview.Should().BeFalse();
        model.EditableTitle.Should().Be("Huidige titel");
    }

    [Test]
    public async Task OnGet_ignores_RestorePaginaVersion_when_pagina_not_in_current_plotboek()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var paginaId = PlotboekPaginaId.Next();
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        // The pagina has an event stream, but is NOT part of this plotboek's read model, so it must
        // not be reconstructed and shown as a preview under this plotboek's URL.
        await _eventStore.SaveEventsAsync((Uuid)paginaId, 0, new object[]
        {
            new PlotboekPaginaTitelAangepast { Titel = "Foreign", Who = TestUserId },
        });

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = paginaId;
        model.RestorePaginaVersion = 1;

        await model.OnGet();

        model.IsRestorePreview.Should().BeFalse();
        model.SelectedPlotboekPagina.Should().BeNull();
    }

    [Test]
    public async Task OnGet_does_not_load_audit_trail_when_show_flag_not_set()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.ShowPlotboekAuditTrail = false;

        await model.OnGet();

        model.PlotboekAuditTrail.Should().BeEmpty();
        await _auditTrailService.DidNotReceive().GetAuditTrailAsync(Arg.Any<Uuid>(), Arg.Any<CancellationToken>());
    }

    // =======================================================================
    // OnPost: deletePagina
    // =======================================================================
    [Test]
    public async Task OnPost_deletePagina_soft_deletes_and_redirects()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        // Create aggregate via the real repository so it has events
        var user = CreateTestUser();
        var plotboekPagina = new PlotboekPagina();
        plotboekPagina = plotboekPagina.AanPlotboekToevoegen(plotboekId, "Test Plotboek", user).Value;
        plotboekPagina = plotboekPagina.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "To Delete",
                Dag = Dag.Donderdag,
                StarttijdId = "Tijd1030",
                Order = 1,
                VerantwoordelijkeSlId = TestUserId,
                VerantwoordelijkeSlName = TestUserName,
            }, user).Value;
        await _paginaRepository.SaveAsync(plotboekPagina, CancellationToken.None);
        _testStore.Seed(plotboekPagina.GetData());

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = plotboekPagina.Id;
        model.Action = "deletePagina";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        // Verify the pagina is marked as deleted in the document store
        var stored = _testStore.Load<PlotboekPagina.ExportData>(plotboekPagina.Id.ToString());
        stored.Should().NotBeNull();
        stored!.IsVerwijderd.Should().BeTrue();
    }

    [Test]
    public async Task OnPost_deletePagina_without_paginaId_returns_page()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.Action = "deletePagina";
        // PaginaId not set
        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
    }

    // =======================================================================
    // OnPost: duplicatePagina
    // =======================================================================
    [Test]
    public async Task OnPost_duplicatePagina_creates_copy_and_redirects()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var user = CreateTestUser();
        var source = new PlotboekPagina();
        source = source.AanPlotboekToevoegen(plotboekId, "Test Plotboek", user).Value;
        source = source.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Origineel plot",
                Dag = Dag.Donderdag,
                StarttijdId = "Tijd1030",
                Order = 1,
                VerantwoordelijkeSlId = TestUserId,
                VerantwoordelijkeSlName = TestUserName,
                AantalNpcs = new AantalNpcsData { AlleNpcs = false, Rollen = [NpcRolData.Create("guard", 2, 4)] },
                IsVolledigKlaar = true,
            }, user).Value;
        await _paginaRepository.SaveAsync(source, CancellationToken.None);
        _testStore.Seed(source.GetData());

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = source.Id;
        model.Action = "duplicatePagina";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        var allPaginas = _testStore.GetAll<PlotboekPagina.ExportData>();
        allPaginas.Should().HaveCount(2);

        var copy = allPaginas.Single(p => p.PaginaId != source.Id);
        copy.Titel.Should().Be("Origineel plot (kopie)");
        copy.Dag.Should().Be(Dag.Donderdag);
        copy.StarttijdId.Should().Be("Tijd1030");
        copy.VerantwoordelijkeSl.Should().Be(TestUserId);
        copy.AantalNpcs.TotaalMinimum.Should().Be(2);
        // The copy resets completion and is not part of the source's plotlijn.
        copy.IsVolledigKlaar.Should().BeFalse();
        copy.PlotlijnId.HasValue.Should().BeFalse();
    }

    [Test]
    public async Task OnPost_duplicatePagina_without_paginaId_returns_page()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.Action = "duplicatePagina";
        // PaginaId not set

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
    }

    // =======================================================================
    // OnPost: newPagina
    // =======================================================================
    [Test]
    public async Task OnPost_newPagina_creates_pagina_and_redirects()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.Action = "newPagina";
        model.NewPaginaTitel = "My New Pagina";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        // Verify a new pagina was stored in the document store
        var allPaginas = _testStore.GetAll<PlotboekPagina.ExportData>();
        allPaginas.Should().ContainSingle();
        allPaginas[0].Titel.Should().Be("My New Pagina");
    }

    [Test]
    public async Task OnPost_newPagina_uses_default_title_when_empty()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.Action = "newPagina";
        model.NewPaginaTitel = "   ";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        var allPaginas = _testStore.GetAll<PlotboekPagina.ExportData>();
        allPaginas.Should().ContainSingle();
        allPaginas[0].Titel.Should().Be("Nieuwe Pagina");
    }

    [Test]
    public async Task OnPost_newPagina_creates_plotboek_if_not_exists()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        // The first event of the year is the one a deployment without plotboeken offers.
        var eventId = TestReferenceData.Seed.Events.Where(e => !e.IsNone && !e.IsEvergreen).OrderBy(e => e.Order).First().Id;
        // Seed plotboek data so LoadPlotboeken works, but the plotboek aggregate is NOT stored
        // This triggers the CreateNewPlotboekPagina code path that creates a new plotboek
        // The events come from the reference data, which every deployment seeds on start.
        foreach (var eventConfig in TestReferenceData.Seed.Events)
        {
            _testStore.Seed(eventConfig);
        }

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.Action = "newPagina";
        model.NewPaginaTitel = "First Pagina";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();

        // A new plotboek should have been created in the document store
        var allPlotboeken = _testStore.GetAll<Plotboek.ExportData>();
        allPlotboeken.Should().ContainSingle();
        allPlotboeken[0].Jaar.Should().Be(jaar);
        allPlotboeken[0].EventId.Should().Be(eventId);
    }

    // =======================================================================
    // OnPost: uploadToDrive
    // =======================================================================
    [Test]
    public async Task OnPost_uploadToDrive_without_folder_url_sets_error_message()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));
        _googleDriveService.IsConfigured.Returns(true);

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.Action = "uploadToDrive";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();
        model.GoogleDriveUploadSuccess.Should().BeFalse();
        model.GoogleDriveUploadMessage.Should().Contain("Google Drive folder URL");
    }

    [Test]
    public async Task OnPost_uploadToDrive_when_not_configured_sets_error_message()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekData = CreatePlotboekExportData(jaar, eventId);
        plotboekData = new Plotboek.ExportData
        {
            PlotboekId = plotboekData.PlotboekId,
            Jaar = jaar,
            EventId = eventId,
            StartDag = Dag.Woensdag,
            EindDag = Dag.Zondag,
            Titel = "Test",
            GoogleDriveFolderUrl = "https://drive.google.com/folder/test",
        };
        _testStore.Seed(plotboekData);
        _googleDriveService.IsConfigured.Returns(false);

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.Action = "uploadToDrive";

        var result = await model.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();
        model.GoogleDriveUploadSuccess.Should().BeFalse();
        model.GoogleDriveUploadMessage.Should().Contain("niet geconfigureerd");
    }

    // =======================================================================
    // GoogleDriveConfigured property
    // =======================================================================
    [Test]
    public void GoogleDriveConfigured_delegates_to_service()
    {
        _googleDriveService.IsConfigured.Returns(true);
        var model = CreateModel();
        model.GoogleDriveConfigured.Should().BeTrue();

        _googleDriveService.IsConfigured.Returns(false);
        model = CreateModel();
        model.GoogleDriveConfigured.Should().BeFalse();
    }

    // =======================================================================
    // OnPost: exportDocx without plotboekId
    // =======================================================================
    [Test]
    public async Task OnPost_exportDocx_without_plotboekId_returns_page()
    {
        SeedTestUser();

        var model = CreateModel();
        model.Action = "exportDocx";
        // No Jaar/Event set, so PlotboekId.HasValue == false
        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
    }

    // =======================================================================
    // OnGet: loads available SLs and domains
    // =======================================================================
    [Test]
    public async Task OnGet_loads_available_sls()
    {
        SeedTestUser();
        var secondUser = CreateTestUser("second-user-id");
        secondUser.UserName = "SecondSL";
        _testStore.Seed(secondUser);
        _testStore.Seed(new UserRole { UserId = "second-user-id", RoleName = UserRoles.User });

        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;

        await model.OnGet();

        model.AvailableSls.Should().HaveCountGreaterThanOrEqualTo(2);
        model.AvailableSls.Should().Contain(sl => sl.DisplayName == TestUserName);
        model.AvailableSls.Should().Contain(sl => sl.DisplayName == "SecondSL");
    }

    [Test]
    public async Task OnGet_loads_available_domains()
    {
        SeedTestUser();
        var domain = new Domain
        {
            Id = Guid.NewGuid(),
            Shorthand = "Ti",
            Description = "Tijdsdomein",
            Order = 1,
        };
        _testStore.Seed(domain);

        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;

        await model.OnGet();

        model.AvailableDomains.Should().ContainSingle();
        model.AvailableDomains[0].Shorthand.Should().Be("Ti");
    }

    // =======================================================================
    // OnPost: save pagina
    // =======================================================================
    [Test]
    public async Task OnPost_savePagina_saves_changes_to_document_store()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        // Create aggregate
        var user = CreateTestUser();
        var plotboekPagina = new PlotboekPagina();
        plotboekPagina = plotboekPagina.AanPlotboekToevoegen(plotboekId, "Test Plotboek", user).Value;
        plotboekPagina = plotboekPagina.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Original Title",
                Dag = Dag.Donderdag,
                StarttijdId = "Tijd1030",
                Order = 1,
                VerantwoordelijkeSlId = TestUserId,
                VerantwoordelijkeSlName = TestUserName,
            }, user).Value;
        await _paginaRepository.SaveAsync(plotboekPagina, CancellationToken.None);
        _testStore.Seed(plotboekPagina.GetData());

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = plotboekPagina.Id;
        model.Action = "save";
        model.EditableTitle = "Updated Title";
        model.EditableDay = Dag.Donderdag;
        model.EditableStarttijd = "Tijd1030";
        model.EditableOrder = 1;
        model.EditableVerantwoordelijkeSlId = TestUserId;
        model.PaginaVersion = plotboekPagina.Version;

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();

        // Verify the updated data is in the document store
        var stored = _testStore.Load<PlotboekPagina.ExportData>(plotboekPagina.Id.ToString());
        stored.Should().NotBeNull();
        stored!.Titel.Should().Be("Updated Title");
    }

    // =======================================================================
    // OnPost: save pagina — linking two paginas via a new plotlijn.
    // Regression guard: selecting "Koppelen aan plot (nieuwe plotlijn)" (value "pagina:{id}")
    // must NOT wipe the linked (other) pagina's content, and must actually link it.
    // =======================================================================
    [Test]
    public async Task OnPost_savePagina_linking_other_pagina_preserves_its_content_and_links_it()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var user = CreateTestUser();

        // Pagina A: the page being edited (the "linking" page). Starts unlinked.
        var paginaA = new PlotboekPagina();
        paginaA = paginaA.AanPlotboekToevoegen(plotboekId, "Test Plotboek", user).Value;
        paginaA = paginaA.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Plot A",
                Dag = Dag.Donderdag,
                StarttijdId = "Tijd1030",
                Order = 1,
                VerantwoordelijkeSlId = TestUserId,
                VerantwoordelijkeSlName = TestUserName,
            }, user).Value;
        await _paginaRepository.SaveAsync(paginaA, CancellationToken.None);
        _testStore.Seed(paginaA.GetData());

        // Pagina B: the "other" page that gets linked. Fully populated with content that must survive.
        var paginaB = new PlotboekPagina();
        paginaB = paginaB.AanPlotboekToevoegen(plotboekId, "Test Plotboek", user).Value;
        paginaB = paginaB.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Plot B",
                Dag = Dag.Vrijdag,
                StarttijdId = "Tijd1430",
                Order = 1,
                VerantwoordelijkeSlId = TestUserId,
                VerantwoordelijkeSlName = TestUserName,
                ElevatorPitch = "B pitch",
                SamenvattingPlot = "<p>B samenvatting</p>",
                MeerInformatie = "<p>B meer info</p>",
                AantalNpcs = new AantalNpcsData { AlleNpcs = false, Rollen = [NpcRolData.Create("guard", 3, 5)] },
                Opmerkingen = "B opmerkingen",
                IsVolledigKlaar = true,
            }, user).Value;
        await _paginaRepository.SaveAsync(paginaB, CancellationToken.None);
        _testStore.Seed(paginaB.GetData());

        // Edit pagina A and pick "link with plot B" from the plotlijn dropdown.
        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = paginaA.Id;
        model.Action = "save";
        model.EditableTitle = "Plot A";
        model.EditableDay = Dag.Donderdag;
        model.EditableStarttijd = "Tijd1030";
        model.EditableOrder = 1;
        model.EditableVerantwoordelijkeSlId = TestUserId;
        model.PaginaVersion = paginaA.Version;
        model.EditablePlotlijnIdRaw = $"pagina:{paginaB.Id}";

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();

        // The linked (other) page keeps ALL of its content — this is the bug that regressed.
        var storedB = _testStore.Load<PlotboekPagina.ExportData>(paginaB.Id.ToString());
        storedB.Should().NotBeNull();
        storedB!.Titel.Should().Be("Plot B");
        storedB.ElevatorPitch.Should().Be("B pitch");
        storedB.SamenvattingPlot.Should().Be("<p>B samenvatting</p>");
        storedB.MeerInformatie.Should().Be("<p>B meer info</p>");
        storedB.AantalNpcs.TotaalMinimum.Should().Be(3);
        storedB.Opmerkingen.Should().Be("B opmerkingen");
        storedB.IsVolledigKlaar.Should().BeTrue();

        // Both pages are actually linked to the same, non-empty plotlijn.
        storedB.PlotlijnId.HasValue.Should().BeTrue();
        var storedA = _testStore.Load<PlotboekPagina.ExportData>(paginaA.Id.ToString());
        storedA!.PlotlijnId.HasValue.Should().BeTrue();
        storedA.PlotlijnId.Should().Be(storedB.PlotlijnId);

        // And the plotlijn document lists both member titles.
        var plotlijn = _testStore.Load<Plotlijn>(storedA.PlotlijnId.ToString());
        plotlijn.Should().NotBeNull();
        plotlijn!.PaginaTitels.Should().Contain("Plot A").And.Contain("Plot B");
    }

    [Test]
    public async Task OnGet_loads_plotboek_settings()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekData = new Plotboek.ExportData
        {
            PlotboekId = PlotboekId.Create(jaar, eventId),
            Jaar = jaar,
            EventId = eventId,
            StartDag = Dag.Woensdag,
            EindDag = Dag.Zondag,
            Titel = "Voorjaar 2026",
            GoogleDriveFolderUrl = "https://drive.google.com/folder/xyz",
        };
        _testStore.Seed(plotboekData);

        var model = CreateModel();
        model.Jaar = jaar;
        model.EventId = eventId;

        await model.OnGet();

        model.PlotboekSettings.Should().NotBeNull();
        model.PlotboekSettings!.Titel.Should().Be("Voorjaar 2026");
        model.PlotboekSettings.StartDag.Should().Be(Dag.Woensdag);
        model.PlotboekSettings.EindDag.Should().Be(Dag.Zondag);
        model.PlotboekSettings.GoogleDriveFolderUrl.Should().Be("https://drive.google.com/folder/xyz");
    }

    // =======================================================================
    // Evergreen behavior
    // =======================================================================
    [Test]
    public async Task OnGet_evergreen_does_not_load_plotlijnen()
    {
        SeedTestUser();
        var evergreenId = PlotboekId.CreateEvergreen();
        _testStore.Seed(new Plotboek.ExportData
        {
            PlotboekId = evergreenId,
            Jaar = Year.Create(1),
            EventId = EventIds.Evergreen,
            Titel = "Evergreen",
        });

        var model = CreateModel();
        model.Jaar = Year.Create(1);
        model.EventId = EventIds.Evergreen;

        await model.OnGet();

        model.AvailablePlotlijnen.Should().BeEmpty();
        model.IsEvergreen.Should().BeTrue();
    }

    // =======================================================================
    // Elevator Pitch tests
    // =======================================================================
    [Test]
    public async Task OnPost_genereerElevatorPitch_keeps_unsaved_field_values()
    {
        SeedTestUser();
        var jaar = Year.Create(2026);
        var eventId = EventIds.Voorjaar;
        var plotboekId = PlotboekId.Create(jaar, eventId);
        _testStore.Seed(CreatePlotboekExportData(jaar, eventId));

        var user = CreateTestUser();
        var plotboekPagina = new PlotboekPagina();
        plotboekPagina = plotboekPagina.AanPlotboekToevoegen(plotboekId, "Test Plotboek", user).Value;
        plotboekPagina = plotboekPagina.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = "Original Title",
                Dag = Dag.Donderdag,
                StarttijdId = "Tijd1030",
                Order = 1,
                VerantwoordelijkeSlId = TestUserId,
                VerantwoordelijkeSlName = TestUserName,
                SamenvattingPlot = "Original summary",
            }, user).Value;
        await _paginaRepository.SaveAsync(plotboekPagina, CancellationToken.None);
        _testStore.Seed(plotboekPagina.GetData());

        var geminiSummaryService = Substitute.For<IGeminiSummaryService>();
        geminiSummaryService.IsConfigured.Returns(true);
        geminiSummaryService.GenereerElevatorPitchAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns("Generated pitch");

        var model = CreateModel(geminiSummaryService);
        model.Jaar = jaar;
        model.EventId = eventId;
        model.PaginaId = plotboekPagina.Id;
        model.Action = "genereerElevatorPitch";
        model.EditableTitle = "Unsaved changed title";
        model.EditableSamenvattingPlot = "Unsaved changed summary";
        model.EditableDay = Dag.Vrijdag;

        var result = await model.OnPost();

        result.Should().BeOfType<PageResult>();
        model.GeneratedElevatorPitchPreview.Should().Be("Generated pitch");
        model.EditableTitle.Should().Be("Unsaved changed title");
        model.EditableSamenvattingPlot.Should().Be("Unsaved changed summary");
        model.EditableDay.Should().Be(Dag.Vrijdag);

        var stored = _testStore.Load<PlotboekPagina.ExportData>(plotboekPagina.Id.ToString());
        stored.Should().NotBeNull();
        stored!.Titel.Should().Be("Original Title");
        stored.SamenvattingPlot.Should().Be("Original summary");
    }
}
