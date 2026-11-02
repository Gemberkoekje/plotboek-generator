using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Collections.Immutable;
using JasperFx.Events;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.core.Generator.Templates;
using PlotboekGenerator.infrastructure.Services;

namespace PlotboekGenerator.test.Services;

/// <summary>
/// <see cref="PlotboekGoogleDriveSyncService.UploadPlotboekAsync"/> is the manual "Upload naar
/// Google Drive" button's entry point (<c>Plotboek.GoogleDrive.cs</c>) - it must share the exact
/// same generation, page-number correction, and sync-state bookkeeping as the automatic
/// <see cref="PlotboekGoogleDriveSyncService.SyncAsync"/> cron path, not a second, divergent one
/// that (as it did before this feature existed) silently resets the correction baseline every time
/// someone clicks the button.
/// </summary>
public sealed class PlotboekGoogleDriveSyncService_UploadPlotboekAsync_specs
{
    private static readonly PlotboekId TestPlotboekId = PlotboekId.Create(Year.Create(2026), EventIds.Zomer);

    private TestDocumentStore _store = null!;
    private IGoogleDriveService _googleDriveService = null!;
    private IPlotboekExportDataBuilder _exportDataBuilder = null!;
    private IDocumentGenerator _documentGenerator = null!;
    private IPageNumberCorrectionService _pageNumberCorrectionService = null!;
    private IConfiguration _configuration = null!;
    private PlotboekGoogleDriveSyncService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new TestDocumentStore();
        _googleDriveService = Substitute.For<IGoogleDriveService>();
        _googleDriveService.IsConfigured.Returns(true);
        _googleDriveService.UploadOrUpdateFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("https://drive.google.com/file/d/generated/view"));

        _exportDataBuilder = Substitute.For<IPlotboekExportDataBuilder>();
        _exportDataBuilder.BuildAsync(Arg.Any<PlotboekId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Qowaiv.Validation.Abstractions.Result<PlotboekTemplateData>>(MinimalExportData()));

        _documentGenerator = Substitute.For<IDocumentGenerator>();
        _pageNumberCorrectionService = Substitute.For<IPageNumberCorrectionService>();
        _pageNumberCorrectionService.ComputeCorrectionsAsync(
            Arg.Any<GoogleDriveSyncState>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ImmutableDictionary<string, int>.Empty));

        var martenSession = Substitute.For<Marten.IDocumentSession>();
        var eventOps = Substitute.For<Marten.Events.IEventStoreOperations>();
        eventOps.FetchStreamStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<StreamState>(null));
        martenSession.Events.Returns(eventOps);
        var martenStore = Substitute.For<Marten.IDocumentStore>();
        martenStore.LightweightSession().Returns(martenSession);

        _configuration = new ConfigurationBuilder().Build();

        _sut = new PlotboekGoogleDriveSyncService(
            _store,
            martenStore,
            _googleDriveService,
            _exportDataBuilder,
            _documentGenerator,
            _pageNumberCorrectionService,
            _configuration,
            Substitute.For<ILogger<PlotboekGoogleDriveSyncService>>());
    }

    [Test]
    public async Task Returns_an_error_when_google_drive_is_not_configured()
    {
        _googleDriveService.IsConfigured.Returns(false);
        Seed(WithFolderUrl("https://drive.google.com/folder/x"));

        var result = await _sut.UploadPlotboekAsync(TestPlotboekId);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Returns_an_error_when_the_plotboek_does_not_exist()
    {
        var result = await _sut.UploadPlotboekAsync(TestPlotboekId);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Returns_an_error_when_no_folder_url_is_configured()
    {
        Seed(WithFolderUrl(string.Empty));

        var result = await _sut.UploadPlotboekAsync(TestPlotboekId);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Uploads_and_persists_the_full_correction_baseline_just_like_the_automatic_sync()
    {
        // This is the exact bug the manual button had: it used to upload via a separate path that
        // never computed corrections and overwrote GoogleDriveSyncState with EstimatedBookmarkPages
        // /BookmarkHeadingText defaulting to empty, silently discarding the automatic sync's
        // baseline. A real fix must persist the SAME rich state SyncAsync would.
        Seed(WithFolderUrl("https://drive.google.com/folder/x"));

        var bookmarkPages = ImmutableDictionary<string, int>.Empty.Add("_pg_1", 7);
        var bookmarkHeadingText = ImmutableDictionary<string, string>.Empty.Add("_pg_1", "Encounter Foo");
        _documentGenerator.GeneratePlotboekDocumentWithCorrectionsAsync(
            Arg.Any<PlotboekTemplateData>(), Arg.Any<IReadOnlyDictionary<string, int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Qowaiv.Validation.Abstractions.Result<GeneratedDocument>>(new GeneratedDocument
            {
                Bytes = [1, 2, 3],
                BookmarkPages = bookmarkPages,
                BookmarkHeadingText = bookmarkHeadingText,
            }));

        var result = await _sut.UploadPlotboekAsync(TestPlotboekId);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be("https://drive.google.com/file/d/generated/view");

        var persistedState = _store.Load<GoogleDriveSyncState>(TestPlotboekId.ToString());
        persistedState.Should().NotBeNull("the manual button must leave a correction baseline behind, exactly like the automatic sync");
        persistedState!.EstimatedBookmarkPages.Should().BeEquivalentTo(bookmarkPages);
        persistedState.BookmarkHeadingText.Should().BeEquivalentTo(bookmarkHeadingText);
        persistedState.LastSyncedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Computes_corrections_from_the_prior_baseline_before_generating_when_one_exists()
    {
        Seed(WithFolderUrl("https://drive.google.com/folder/x"));
        _store.Seed(new GoogleDriveSyncState
        {
            Id = TestPlotboekId.ToString(),
            EstimatedBookmarkPages = ImmutableDictionary<string, int>.Empty.Add("_pg_1", 3),
            BookmarkHeadingText = ImmutableDictionary<string, string>.Empty.Add("_pg_1", "Encounter Foo"),
        });

        _documentGenerator.GeneratePlotboekDocumentWithCorrectionsAsync(
            Arg.Any<PlotboekTemplateData>(), Arg.Any<IReadOnlyDictionary<string, int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Qowaiv.Validation.Abstractions.Result<GeneratedDocument>>(new GeneratedDocument
            {
                Bytes = [1],
                BookmarkPages = ImmutableDictionary<string, int>.Empty,
                BookmarkHeadingText = ImmutableDictionary<string, string>.Empty,
            }));

        await _sut.UploadPlotboekAsync(TestPlotboekId);

        await _pageNumberCorrectionService.Received(1).ComputeCorrectionsAsync(
            Arg.Is<GoogleDriveSyncState>(s => s.Id == TestPlotboekId.ToString()),
            "https://drive.google.com/folder/x",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private void Seed(Plotboek.ExportData plotboek) => _store.Seed(plotboek);

    private static Plotboek.ExportData WithFolderUrl(string folderUrl) => new()
    {
        PlotboekId = TestPlotboekId,
        Jaar = Year.Create(2026),
        EventId = EventIds.Zomer,
        StartDag = Dag.Donderdag,
        EindDag = Dag.Zondag,
        Titel = "Test",
        GoogleDriveFolderUrl = folderUrl,
    };

    private static PlotboekTemplateData MinimalExportData() => new()
    {
        Naam = "Test",
        SamenvattingAlgemeenPlot = string.Empty,
        Tijdschemas = ImmutableList<TijdschemasData>.Empty,
        Days = ImmutableList<DayData>.Empty,
        ExtraInfo = ImmutableList<ExtraInfoData>.Empty,
        Bijlagen = [],
        AslDomeinen = ImmutableList<DomeinData>.Empty,
        GslDomeinen = ImmutableList<DomeinData>.Empty,
        EvergreenPages = ImmutableList<PageData>.Empty,
        Namen = ImmutableList<NaamData>.Empty,
        Werelden = ImmutableList<NaamData>.Empty,
    };
}
