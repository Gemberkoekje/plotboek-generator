using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

[Authorize(Roles = UserRoles.User + "," + UserRoles.Admin + "," + UserRoles.Reader + "," + UserRoles.NPC)]
public sealed partial class PlotboekModel(
    PlotboekPaginaRepository paginaRepository,
    IPlotboekRepository plotboekRepository,
    IDocumentStore documentStore,
    IGoogleDriveService googleDriveService,
    PlotboekGoogleDriveSyncService googleDriveSyncService,
    ExportPlotboekCommandHandler exportPlotboekHandler,
    CreatePlotboekCommandHandler createPlotboekHandler,
    IAuditTrailService auditTrailService,
    ILogger<PlotboekModel> logger,
    PlotboekGenerator.infrastructure.Services.IGeminiSummaryService? geminiSummaryService = null,
    GetPlotboekByIdQueryHandler getPlotboekByIdQueryHandler = null!,
    GetPlotboekListQueryHandler getPlotboekListQueryHandler = null!,
    LocalizationSettings? localization = null) : PageModel
{
    // Properties and nested record types have been moved to Plotboek.Properties.cs
    public async Task<IActionResult> OnGet()
    {
        await LoadReferenceDataAsync();
        await LoadPlotboeken();

        if (!PlotboekId.HasValue)
        {
            var autoSelected = DetermineDefaultPlotboek();
            if (autoSelected != null && !string.IsNullOrWhiteSpace(autoSelected.EventId))
            {
                return RedirectToPage(
                    "/Plotboek",
                    new
                    {
                        Jaar = autoSelected.Jaar.ToString(),
                        Event = autoSelected.EventId,
                    });
            }

            if (IsReadOnlyRole && NoPlotboekenToRead)
            {
                PlotboekAccessMessage = IsNpc
                    ? PlotboekUi.AccessGeenPlotboekVoorNpcs
                    : PlotboekUi.AccessGeenPlotboek;
                return Page();
            }
        }

        await LoadAvailableSls();
        await LoadCurrentUserSlId();
        await LoadAvailableDomains();

        if (PlotboekId.HasValue)
        {
            if (!CanAccessSelectedPlotboek())
            {
                return Page();
            }

            var selectedPlotboek = AvailablePlotboeken.FirstOrDefault(p => p.Id == PlotboekId);
            if (selectedPlotboek != null && selectedPlotboek.Exists && getPlotboekByIdQueryHandler != null)
            {
                var queryResult = await getPlotboekByIdQueryHandler
                    .HandleAsync(new GetPlotboekByIdQuery(PlotboekId), HttpContext.RequestAborted)
                    .ConfigureAwait(false);

                if (!queryResult.IsValid)
                {
                    foreach (var message in queryResult.Messages)
                    {
                        ModelState.AddModelError(string.Empty, message.Message);
                    }

                    logger.LogWarning("Unable to load selected plotboek {PlotboekId}. Staying on Plotboek page to avoid redirect loop.", PlotboekId);
                    return Page();
                }
            }

            await LoadPlotboekPaginas();
            await LoadAvailablePlotlijnen();
            await LoadPlotboekSettings();
            await LoadPlotboekAuditTrailAsync();

            if (PaginaId.HasValue)
            {
                LoadSelectedPlotboekPagina();
                LoadAvailableStarttijden();

                if (RestorePaginaVersion is { } restoreVersion && CanEditCurrentPagina())
                {
                    await SeedEditableFieldsFromHistoryAsync(restoreVersion);
                }
                else
                {
                    InitializePaginaEditableFields();
                }

                await LoadPaginaAuditTrailAsync();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        await LoadReferenceDataAsync();
        await LoadPlotboeken();
        if (!CanAccessSelectedPlotboek())
        {
            return Forbid();
        }

        await LoadAvailableSls();
        await LoadCurrentUserSlId();
        await LoadAvailableDomains();
        await LoadPlotboekPaginas();
        await LoadAvailablePlotlijnen();
        // Don't initialize editable fields during POST - they are already bound from form
        await LoadPlotboekSettings(initializeEditableFields: false);
        LoadSelectedPlotboekPagina();
        LoadAvailableStarttijden();

        return Action switch
        {
            "save" => await HandleSavePagina(),
            "bewaarStandaardNpc" => await HandleBewaarStandaardNpc(),
            "savePlotboek" => await HandleSavePlotboek(),
            "newPagina" => await HandleNewPagina(),
            "exportDocx" => await HandleExportDocx(),
            "uploadToDrive" => await HandleUploadToDrive(),
            "reorder" => await HandleReorderPagina(),
            "genereerElevatorPitch" => await HandleGenereerElevatorPitch(),
            "vervangElevatorPitch" => await HandleVervangElevatorPitch(),
            "movePagina" => await HandleMovePagina(),
            "duplicatePagina" => await HandleDuplicatePagina(),
            "deletePagina" => await HandleDeletePagina(),
            "addFeedback" => await HandleAddFeedback(),
            "answerFeedback" => await HandleAnswerFeedback(),
            "seenFeedback" => await HandleSeenFeedback(),
            "resolveFeedback" => await HandleResolveFeedback(),
            "reopenFeedback" => await HandleReopenFeedback(),
            "deleteFeedback" => await HandleDeleteFeedback(),
            _ => Page(),
        };
    }

    // Handle* methods have been moved to Plotboek.Handlers.cs

    // Google Drive TempData and helper moved to Plotboek.Properties.cs

    // Handle* methods have been moved to Plotboek.Handlers.cs
    private async Task<Result<PlotboekPaginaId>> CreateNewPlotboekPagina(Year plotboekJaar, string plotboekEventId, string titel)
    {
        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            return Result.WithMessages<PlotboekPaginaId>(ValidationMessage.Error($"No user found in session while trying to create new plotboek pagina for {plotboekJaar} {plotboekEventId}"));
        }
        var plotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == PlotboekId.ToString());
        if (plotboek == null)
        {
            var (startDag, eindDag) = await GetEventDagenAsync(plotboekEventId);
            var createResult = await createPlotboekHandler
                .HandleAsync(new CreatePlotboekCommand(plotboekJaar, plotboekEventId, startDag, eindDag, user), CancellationToken.None)
                .ConfigureAwait(false);

            if (!createResult.IsValid)
            {
                return Result.WithMessages<PlotboekPaginaId>(createResult.Messages);
            }

            plotboek = await session
                .FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == PlotboekId.ToString())
                .ConfigureAwait(false);

            if (plotboek == null)
            {
                return Result.WithMessages<PlotboekPaginaId>(ValidationMessage.Error($"Plotboek {PlotboekId} kon niet worden aangemaakt."));
            }
        }

        // Get the highest order number among existing pages to ensure deterministic ordering
        var existingPages = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotboekIdQueryable == PlotboekId.ToString());
        var nextOrder = existingPages.Count > 0 ? existingPages.Max(p => p.Order) + 1 : 1;

        var plotboekPagina = new PlotboekPagina();
        plotboekPagina = plotboekPagina.AanPlotboekToevoegen(PlotboekId, plotboek.DisplayName, user).Value;
        var userDisplayName = user.UserName ?? user.Email ?? user.Id;

        // Look up the user's primary domain to auto-set on the new pagina
        var domainId = Guid.Empty;
        var domainShorthand = string.Empty;
        var domainDescription = string.Empty;
        if (user.PrimaryDomainId != Guid.Empty)
        {
            var domain = await session.LoadAsync<Domain>(user.PrimaryDomainId);
            if (domain != null)
            {
                domainId = domain.Id;
                domainShorthand = domain.Shorthand;
                domainDescription = domain.Description;
            }
        }

        plotboekPagina = plotboekPagina.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = titel,
                Dag = Dag.None,
                StarttijdId = "None",
                Order = nextOrder,
                VerantwoordelijkeSlId = user.Id,
                VerantwoordelijkeSlName = userDisplayName,
                DomainId = domainId,
                DomainShorthand = domainShorthand,
                DomainDescription = domainDescription,
            }, user).Value;
        await paginaRepository.SaveAsync(plotboekPagina, CancellationToken.None);
        session.Store(plotboekPagina.GetData());
        await session.SaveChangesAsync(CancellationToken.None);
        var newPaginaId = plotboekPagina.Id;
        logger.LogInformation("Creating new plotboek pagina for plotboek {PlotboekJaar}-{PlotboekEventId} with ID: {PaginaId}", plotboekJaar, plotboekEventId, newPaginaId);

        return newPaginaId;
    }

    // Load and helper methods have been moved to Plotboek.Loading.cs

    // Properties and nested record types have been moved to Plotboek.Properties.cs
}
