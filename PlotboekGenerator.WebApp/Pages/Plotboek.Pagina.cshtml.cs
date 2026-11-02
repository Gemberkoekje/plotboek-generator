namespace PlotboekGenerator.WebApp.Pages;

using PlotboekGenerator.core;
using PlotboekGenerator.WebApp.Resources;

public sealed partial class PlotboekModel
{
    /// <summary>
    /// Value of the last option in the questlocatie picker: "Anders… (vul zelf in)". Deliberately not an
    /// empty string, so an unposted field (a form without the picker) is distinguishable from an explicit
    /// choice for free text.
    /// </summary>
    public const string QuestLocatieAndersSentinel = "Anders";

    [BindProperty]
    public PlotboekId MoveToPlotboekId { get; set; }

    [BindProperty]
    public string? EditableTitle { get; set; }

    [BindProperty]
    public long PaginaVersion { get; set; }

    [BindProperty]
    public Guid EditableDomainId { get; set; }

    [BindProperty]
    public Dag EditableDay { get; set; }

    [BindProperty]
    public string EditableStarttijd { get; set; } = "None";

    [BindProperty]
    public int EditableOrder { get; set; }

    [BindProperty]
    public string? EditableVerantwoordelijkeSlId { get; set; }

    [BindProperty]
    public List<string> EditableExtraSlIds { get; set; } = [];

    [BindProperty]
    public PlotboekPaginaId ReorderPaginaId { get; set; }

    [BindProperty]
    public Dag ReorderDag { get; set; }

    [BindProperty]
    public string ReorderStarttijd { get; set; } = "None";

    [BindProperty]
    public int ReorderNewOrder { get; set; }

    [BindProperty]
    public string EditableAantalBlokken { get; set; } = "None";

    [BindProperty]
    public List<string> EditableEncounterTypesMajor { get; set; } = [];

    [BindProperty]
    public List<string> EditableEncounterTypesMinor { get; set; } = [];

    [BindProperty]
    public LocatieEncounter EditableLocatieEncounter { get; set; }

    /// <summary>
    /// The picked questlocatie id, or <see cref="QuestLocatieAndersSentinel"/> when the user chose
    /// "Anders…" and fills in <see cref="EditableLocatieEncounterSpecificatie"/> by hand.
    /// </summary>
    [BindProperty]
    public string? EditableQuestLocatieId { get; set; }

    [BindProperty]
    public string? EditableLocatieEncounterSpecificatie { get; set; }

    [BindProperty]
    public bool EditableAlleNpcs { get; set; }

    [BindProperty]
    public bool EditableGeenNpcsNodig { get; set; }

    [BindProperty]
    public List<NpcRolInput> EditableNpcRollen { get; set; } = [];

    /// <summary>
    /// The plotlijn selection: empty for no plotlijn, "plotlijn:{id}" for an existing plotlijn, or "pagina:{id}" to link with another plot.
    /// </summary>
    [BindProperty]
    public string? EditablePlotlijnIdRaw { get; set; }

    [BindProperty]
    public string? EditableElevatorPitch { get; set; }

    [BindProperty]
    public string? EditableSamenvattingPlot { get; set; }

    [BindProperty]
    public string? GeneratedElevatorPitchPreview { get; set; }

    [BindProperty]
    public string? GeneratedElevatorPitchHidden { get; set; }

    [BindProperty]
    public string? ElevatorPitchError { get; set; }

    /// <summary>Whether this deployment can generate an elevator pitch (Gemini is configured).</summary>
    public bool KanElevatorPitchGenereren => geminiSummaryService is { IsConfigured: true };

    [BindProperty]
    public string? DomainValidationMessage { get; set; }

    [BindProperty]
    public string? ConfirmedElevatorPitch { get; set; }

    [BindProperty]
    public string? EditableMeerInformatie { get; set; }

    /// <summary>The IDs (as strings) of the plotboek-level bijlagen this encounter references.</summary>
    [BindProperty]
    public List<string> EditableReferencedBijlagen { get; set; } = [];

    [BindProperty]
    public bool EditableIsVolledigKlaar { get; set; }

    [BindProperty]
    public bool EditableIsGedraaid { get; set; }

    [BindProperty]
    public bool EditableIsSpelerGedreven { get; set; }

    [BindProperty]
    public bool EditableIsFlexibel { get; set; }

    [BindProperty]
    public bool EditableIsFlexibelNachtPlot { get; set; }

    [BindProperty]
    public bool EditableTimingCruciaal { get; set; }

    [BindProperty]
    public bool EditablePlotCruciaal { get; set; }

    [BindProperty]
    public bool EditableMagAlsEersteCancelled { get; set; }

    [BindProperty]
    public bool EditableUitgebreideBriefing { get; set; }

    [BindProperty]
    public bool EditableGrootPakOfKostuum { get; set; }

    [BindProperty]
    public bool EditableMuziekVanuitTechniek { get; set; }

    [BindProperty]
    public bool EditableLichtOfRookVanuitTechniek { get; set; }

    [BindProperty]
    public string? EditableInstructiePentagon { get; set; }

    [BindProperty]
    public bool EditableEventBreed { get; set; }

    [BindProperty]
    public List<PropLootItemInput> EditableProps { get; set; } = [];

    [BindProperty]
    public List<PropLootItemInput> EditableLoot { get; set; } = [];

    [BindProperty]
    public string? EditableOpmerkingen { get; set; }

    [BindProperty]
    public bool EditableDebriefGewenst { get; set; }

    [BindProperty]
    public List<string> EditableDebriefVragen { get; set; } = [];

    /// <summary>
    /// Text fields that the user attempted to save but were rejected due to a concurrency conflict.
    /// Keys are field labels, values are the user's attempted input.
    /// </summary>
    public Dictionary<string, string?> AttemptedChanges { get; set; } = [];

    /// <summary>
    /// Parsed PlotlijnId from <see cref="EditablePlotlijnIdRaw"/>, or empty if none/pagina selection.
    /// </summary>
    public PlotlijnId EditablePlotlijnId
    {
        get
        {
            if (!string.IsNullOrEmpty(EditablePlotlijnIdRaw)
                && EditablePlotlijnIdRaw.StartsWith("plotlijn:", StringComparison.OrdinalIgnoreCase))
            {
                return PlotlijnId.Parse(EditablePlotlijnIdRaw["plotlijn:".Length..]);
            }
            return PlotlijnId.Empty;
        }
    }

    internal Dictionary<string, string?> CaptureEditableTextFields() => new ()
    {
        [PaginaUi.VeldTitel] = EditableTitle,
        [PaginaUi.VeldElevatorPitch] = EditableElevatorPitch,
        [PaginaUi.VeldSamenvattingPlot] = EditableSamenvattingPlot,
        [PaginaUi.VeldMeerInformatie] = EditableMeerInformatie,
        [PaginaUi.VeldOpmerkingen] = EditableOpmerkingen,
        [Terminologie.Huidig.InstructieTechniek] = EditableInstructiePentagon,
        [PaginaUi.VeldLocatieSpecificatie] = EditableLocatieEncounterSpecificatie,
    };

    internal void InitializePaginaEditableFields()
    {
        PaginaVersion = SelectedPlotboekPagina?.EventVersion ?? 0;
        EditableTitle = SelectedPlotboekPagina?.Title;
        EditableDomainId = SelectedPlotboekPagina?.DomainId ?? Guid.Empty;
        EditableDay = SelectedPlotboekPagina?.Dag ?? Dag.None;
        EditableStarttijd = SelectedPlotboekPagina?.StarttijdId ?? "None";
        EditableOrder = SelectedPlotboekPagina?.Order ?? 0;
        EditableVerantwoordelijkeSlId = SelectedPlotboekPagina?.VerantwoordelijkeSlId;
        EditableExtraSlIds = SelectedPlotboekPagina?.ExtraSlIds?.ToList() ?? [];
        EditableAantalBlokken = SelectedPlotboekPagina?.AantalBlokkenId ?? "None";
        EditableEncounterTypesMajor = SelectedPlotboekPagina?.EncounterTypeIds.Major?.ToList() ?? [];
        EditableEncounterTypesMinor = SelectedPlotboekPagina?.EncounterTypeIds.Minor?.ToList() ?? [];
        EditableLocatieEncounter = SelectedPlotboekPagina?.LocatieEncounter.Locatie ?? LocatieEncounter.None;

        // Must stay in sync with BuildPaginaData: the concurrency-reload path runs Init again, and a
        // field that is only read but never initialized silently loses the user's choice on a conflict.
        EditableQuestLocatieId = SelectedPlotboekPagina?.LocatieEncounter.HasQuestLocatieKeuze == true
            ? SelectedPlotboekPagina.LocatieEncounter.LocatieId
            : QuestLocatieAndersSentinel;
        EditableLocatieEncounterSpecificatie = SelectedPlotboekPagina?.LocatieEncounter.Specificatie;
        EditableAlleNpcs = SelectedPlotboekPagina?.AantalNpcs.AlleNpcs ?? false;
        EditableGeenNpcsNodig = SelectedPlotboekPagina?.AantalNpcs.GeenNpcsNodig ?? false;
        EditableNpcRollen = SelectedPlotboekPagina?.AantalNpcs.Rollen?
            .Select(r => new NpcRolInput
            {
                Rol = r.Rol,
                MinimumAantal = r.MinimumAantal,
                MaximumAantal = r.MaximumAantal,
                IsTerukerend = r.IsTerukerend,
                IsEerderGespeeld = r.IsEerderGespeeld,
                GespeeldDoor = r.GespeeldDoor,
                IsAlGekozen = r.IsAlGekozen,
                GekozenPersoon = r.GekozenPersoon,
                KledingEnSchmink = r.KledingEnSchmink,
                CastingInstructie = r.CastingInstructie,
                VereistAfstemming = r.VereistAfstemming,
                AfstemmingToelichting = r.AfstemmingToelichting,
            }).ToList() ?? [];
        EditablePlotlijnIdRaw = SelectedPlotboekPagina?.PlotlijnId.HasValue == true
            ? $"plotlijn:{SelectedPlotboekPagina.PlotlijnId}"
            : null;
        EditableElevatorPitch = SelectedPlotboekPagina?.ElevatorPitch;
        EditableSamenvattingPlot = SelectedPlotboekPagina?.SamenvattingPlot is null
            ? null
            : TextNormalization.Normalize(SelectedPlotboekPagina.SamenvattingPlot);
        EditableMeerInformatie = SelectedPlotboekPagina?.MeerInformatie is null
            ? null
            : TextNormalization.Normalize(SelectedPlotboekPagina.MeerInformatie);
        EditableReferencedBijlagen = SelectedPlotboekPagina?.BijlageRefs?.Select(id => id.ToString()).ToList() ?? [];
        EditableIsVolledigKlaar = SelectedPlotboekPagina?.IsVolledigKlaar ?? false;
        EditableIsGedraaid = SelectedPlotboekPagina?.IsGedraaid ?? false;
        EditableIsSpelerGedreven = SelectedPlotboekPagina?.IsSpelerGedreven ?? false;
        EditableIsFlexibel = SelectedPlotboekPagina?.IsFlexibel ?? false;
        EditableIsFlexibelNachtPlot = SelectedPlotboekPagina?.IsFlexibelNachtPlot ?? false;
        var iconen = SelectedPlotboekPagina?.Iconen ?? PlotboekPaginaIcons.None;
        EditableTimingCruciaal = iconen.HasFlag(PlotboekPaginaIcons.TimingCruciaal);
        EditablePlotCruciaal = iconen.HasFlag(PlotboekPaginaIcons.PlotCruciaal);
        EditableMagAlsEersteCancelled = iconen.HasFlag(PlotboekPaginaIcons.MagAlsEersteCancelled);
        EditableUitgebreideBriefing = iconen.HasFlag(PlotboekPaginaIcons.UitgebreideBriefing);
        EditableGrootPakOfKostuum = iconen.HasFlag(PlotboekPaginaIcons.GrootPakOfKostuum);
        EditableMuziekVanuitTechniek = iconen.HasFlag(PlotboekPaginaIcons.MuziekVanuitTechniek);
        EditableLichtOfRookVanuitTechniek = iconen.HasFlag(PlotboekPaginaIcons.LichtOfRookVanuitTechniek);
        EditableInstructiePentagon = SelectedPlotboekPagina?.InstructiePentagon is null
            ? null
            : TextNormalization.Normalize(SelectedPlotboekPagina.InstructiePentagon);
        var isHoofdveld = EditableLocatieEncounter == LocatieEncounter.Hoofdveld;
        EditableEventBreed = isHoofdveld && (EditableAlleNpcs || iconen.HasFlag(PlotboekPaginaIcons.EventBreed));
        EditableProps = SelectedPlotboekPagina?.PropsEnLoot.Props?
            .Select(p => new PropLootItemInput
            {
                Omschrijving = p.Omschrijving,
                VerantwoordelijkeSlId = p.VerantwoordelijkeSlId,
                VerantwoordelijkeSource = p.VerantwoordelijkeSource ?? "SL",
                VerantwoordelijkeVrijeTekst = p.VerantwoordelijkeVrijeTekst,
            }).ToList() ?? [];
        EditableLoot = SelectedPlotboekPagina?.PropsEnLoot.Loot?
            .Select(l => new PropLootItemInput
            {
                Omschrijving = l.Omschrijving,
                VerantwoordelijkeSlId = l.VerantwoordelijkeSlId,
                VerantwoordelijkeSource = l.VerantwoordelijkeSource ?? "SL",
                VerantwoordelijkeVrijeTekst = l.VerantwoordelijkeVrijeTekst,
            }).ToList() ?? [];
        EditableOpmerkingen = SelectedPlotboekPagina?.Opmerkingen;
        EditableDebriefGewenst = SelectedPlotboekPagina?.Debrief.IsDebriefGewenst ?? false;
        EditableDebriefVragen = SelectedPlotboekPagina?.Debrief.Vragen?.ToList() ?? [];
    }

    /// <summary>
    /// Loads the pagina as it was at the given stream version and pre-fills the editable form fields
    /// with that historical state, for a restore preview. Nothing is persisted here: the user must press
    /// Save, which then runs the normal <see cref="SavePlotboekPagina"/> flow and appends forward events.
    /// </summary>
    private async Task SeedEditableFieldsFromHistoryAsync(long version)
    {
        // A restore only makes sense for a pagina that belongs to the current plotboek. OnGet already
        // ran LoadSelectedPlotboekPagina(), which leaves SelectedPlotboekPagina null when PaginaId is
        // not in this plotboek's list. Don't reconstruct a foreign pagina under this plotboek's URL.
        if (SelectedPlotboekPagina is null)
        {
            InitializePaginaEditableFields();
            return;
        }

        var result = await paginaRepository.LoadAtVersionAsync(PaginaId, version, HttpContext.RequestAborted);
        if (!result.IsValid)
        {
            logger.LogWarning("Could not load pagina {PaginaId} at version {Version} for restore preview", PaginaId, version);
            InitializePaginaEditableFields();
            return;
        }

        var data = result.Value.Snapshot.GetData();

        // Point the detail panel at the historical snapshot and reuse the standard field mapping so
        // every Editable* field (incl. dynamic lists and rich text) renders the historical values.
        SelectedPlotboekPagina = BuildPaginaInfo(
            data,
            ResolveStarttijdId(data.StarttijdId),
            ResolveAantalBlokkenId(data.AantalBlokkenId),
            ResolveEncounterTypeIds(data.EncounterTypeIds));
        IsRestorePreview = true;
        LoadAvailableStarttijden();
        InitializePaginaEditableFields();

        // The optimistic-concurrency check in SavePlotboekPagina compares against the CURRENT stream
        // version, not the historical one, so saving the preview appends onto the latest state.
        PaginaVersion = result.Value.CurrentVersion;

        // Resolve who/when for the banner via the audit service (it resolves usernames the same way the
        // audit modal does). Uses the clamped TargetVersion so a clamped request still finds its entry.
        var trail = await auditTrailService.GetAuditTrailAsync((Uuid)PaginaId, HttpContext.RequestAborted);
        var entry = trail.FirstOrDefault(e => e.Version == result.Value.TargetVersion);
        RestoreFromWhen = entry?.When;
        RestoreFromWho = entry?.Who;
    }

    private async Task<IActionResult> HandleSavePagina()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        // Clear previous validation message
        DomainValidationMessage = null;

        if (PaginaId.HasValue && !string.IsNullOrWhiteSpace(EditableTitle))
        {
            await SavePaginaAndReload();
        }

        return Page();
    }

    /// <summary>
    /// Saves the posted pagina form and reloads the page state from the stored data. On a conflict the
    /// form shows the stored values again, with the user's attempted text changes listed alongside.
    /// </summary>
    /// <returns><see langword="true"/> when the pagina was saved.</returns>
    private async Task<bool> SavePaginaAndReload()
    {
        var result = await SavePlotboekPagina(PaginaId);
        if (!result.IsValid)
        {
            logger.LogWarning("Concurrency conflict while saving plotboek pagina {PaginaId}", PaginaId);
            ConcurrencyError = string.Join(" ", result.Messages.Select(m => m.Message));

            // Capture the user's attempted text values before reinitializing
            var attemptedValues = CaptureEditableTextFields();

            // Reload the latest data and reinitialize form fields with current DB values
            await LoadPlotboekPaginas();
            await LoadAvailablePlotlijnen();
            await LoadPlotboekSettings();
            LoadSelectedPlotboekPagina();
            LoadAvailableStarttijden();
            InitializePaginaEditableFields();

            // Build the list of attempted changes
            var currentValues = CaptureEditableTextFields();
            foreach (var (label, attemptedValue) in attemptedValues)
            {
                if (!string.Equals(attemptedValue, currentValues[label], StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(attemptedValue))
                {
                    AttemptedChanges[label] = attemptedValue;
                }
            }

            return false;
        }

        // Refresh the data to show the updated values
        await LoadPlotboekPaginas();
        await LoadAvailablePlotlijnen();
        await LoadPlotboekSettings();
        LoadSelectedPlotboekPagina();
        LoadAvailableStarttijden();
        InitializePaginaEditableFields();
        return true;
    }

    private async Task<IActionResult> HandleReorderPagina()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        if (ReorderPaginaId.HasValue)
        {
            var result = await ReorderPlotboekPagina(ReorderPaginaId, ReorderDag, ReorderStarttijd, ReorderNewOrder);
            if (!result.IsValid)
            {
                logger.LogWarning("Concurrency conflict while reordering plotboek pagina {PaginaId}", ReorderPaginaId);
                ConcurrencyError = string.Join(" ", result.Messages.Select(m => m.Message));
            }

            // Refresh the data to show the updated values
            await LoadPlotboekPaginas();
            await LoadPlotboekSettings();
            LoadSelectedPlotboekPagina();
            LoadAvailableStarttijden();
            InitializePaginaEditableFields();
        }

        return Page();
    }

    private async Task<Result> ReorderPlotboekPagina(PlotboekPaginaId paginaId, Dag newDag, string newStarttijdId, int newOrder)
    {
        logger.LogInformation("Reordering plotboek pagina {PaginaId} to {Dag}/{Starttijd} at position {Order}", paginaId, newDag, newStarttijdId, newOrder);

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            logger.LogWarning("No user found in session while trying to reorder plotboek pagina {PaginaId}", paginaId);
            return Result.OK;
        }

        if (await paginaRepository.LoadAsync(paginaId) is not { } plotboekPagina || !plotboekPagina.IsValid)
        {
            logger.LogWarning("Plotboek pagina {PaginaId} not found", paginaId);
            return Result.OK;
        }

        plotboekPagina = plotboekPagina.Value.UpdateOrder(newOrder, newDag, newStarttijdId, user);
        if (!plotboekPagina.IsValid)
        {
            return Result.WithMessages(plotboekPagina.Messages);
        }

        var result = await paginaRepository.SaveAsync(plotboekPagina.Value, CancellationToken.None);
        if (result.IsValid)
        {
            session.Store(plotboekPagina.Value.GetData());
            await session.SaveChangesAsync(CancellationToken.None);
        }
        return result;
    }

    private async Task<Result> SavePlotboekPagina(PlotboekPaginaId paginaId)
    {
        logger.LogInformation("Saving plotboek pagina {PaginaId} with title: {Title}", paginaId, EditableTitle);

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            logger.LogWarning("No user found in session while trying to save plotboek pagina {PaginaId}", paginaId);
            return Result.OK;
        }

        if (await paginaRepository.LoadAsync(paginaId) is not { } plotboekPagina || !plotboekPagina.IsValid)
        {
            logger.LogWarning("Plotboek pagina {PaginaId} not found", paginaId);
            return Result.OK;
        }

        if (PaginaVersion > 0 && plotboekPagina.Value.Version != PaginaVersion)
        {
            return Result.WithMessages(ValidationMessage.Error(PaginaUi.ConcurrencyConflict));
        }

        // Handle "pagina:" prefix - link current pagina with another unlinked pagina via a new plotlijn
        // Evergreen plots should never be part of a plotlijn
        if (!IsEvergreen && EditablePlotlijnIdRaw?.StartsWith("pagina:", StringComparison.OrdinalIgnoreCase) == true)
        {
            await LinkPaginaViaNewPlotlijn(session, user);
        }

        // Capture the old plotlijn ID before applying changes
        var oldPlotlijnId = plotboekPagina.Value.PlotlijnId;

        plotboekPagina = plotboekPagina.Value.Aanpassen(BuildPaginaData(), user);
        if (!plotboekPagina.IsValid)
        {
            return Result.WithMessages(plotboekPagina.Messages);
        }

        var result = await paginaRepository.SaveAsync(plotboekPagina.Value, CancellationToken.None);
        if (!result.IsValid)
        {
            return result;
        }

        // NOTE: SaveAsync already committed the events in their own transaction. The line
        // below rewrites the ExportData read model in a separate transaction, so a failure
        // here leaves the projection briefly stale until the next save re-derives it. This
        // self-heals but is not atomic — see code-review-plan.md #5 for the inline-projection
        // follow-up that would make the event + projection commit together.
        session.Store(plotboekPagina.Value.GetData());

        // Commit all document changes first so UpdatePlotlijnTitel can query committed data
        await session.SaveChangesAsync(CancellationToken.None);

        // Update the new plotlijn title with all member pagina titles
        await UpdatePlotlijnTitel(session, plotboekPagina.Value.PlotlijnId);

        // Also update the old plotlijn title if the pagina moved to a different plotlijn
        if (!oldPlotlijnId.Equals(plotboekPagina.Value.PlotlijnId))
        {
            await UpdatePlotlijnTitel(session, oldPlotlijnId);
        }

        await session.SaveChangesAsync(CancellationToken.None);
        return result;
    }

    /// <summary>
    /// Creates a new plotlijn linking the current pagina with another unlinked pagina.
    /// </summary>
    private async Task LinkPaginaViaNewPlotlijn(IDocumentSession session, PlotboekUser user)
    {
        var otherPaginaIdStr = EditablePlotlijnIdRaw!["pagina:".Length..];
        var otherPaginaId = PlotboekPaginaId.Parse(otherPaginaIdStr);

        var selectedDomain = EditableDomainId != Guid.Empty
            ? AvailableDomains.FirstOrDefault(d => d.Id == EditableDomainId)
            : null;

        var newPlotlijnId = PlotlijnId.Next();
        var newPlotlijn = new Plotlijn
        {
            PlotlijnId = newPlotlijnId,
            PlotboekId = PlotboekId,
            DomainId = EditableDomainId,
            DomainShorthand = selectedDomain?.Shorthand,
            PaginaTitels = [EditableTitle ?? "Nieuwe Pagina"],
        };
        session.Store(newPlotlijn);
        EditablePlotlijnIdRaw = $"plotlijn:{newPlotlijnId}";
        logger.LogInformation("Created new plotlijn {PlotlijnId} linking pagina {PaginaId} with {OtherPaginaId}", newPlotlijnId, PaginaId, otherPaginaId);

        // Also assign the new plotlijn to the other pagina. Only touch its plotlijn association:
        // a full Aanpassen with a partially-populated Data would reset every field we don't repopulate
        // here, wiping the other pagina's entire content (elevator pitch, plot summary, NPCs, props, ...).
        if (await paginaRepository.LoadAsync(otherPaginaId) is { } otherPagina && otherPagina.IsValid)
        {
            var linked = otherPagina.Value.AanpassenPlotlijn(newPlotlijnId, user);
            if (linked.IsValid)
            {
                var otherResult = await paginaRepository.SaveAsync(linked.Value, CancellationToken.None);
                if (otherResult.IsValid)
                {
                    session.Store(linked.Value.GetData());
                }
            }
        }
    }

    /// <summary>
    /// Updates the plotlijn document title by recomputing it from all member paginas.
    /// If the plotlijn does not exist yet, it will be created.
    /// </summary>
    private async Task UpdatePlotlijnTitel(IDocumentSession session, PlotlijnId? plotlijnId)
    {
        // A self-contained/evergreen plot has no plotlijn: its PlotlijnId is the empty SVO
        // (see EditablePlotlijnId / BuildPaginaData). There is nothing to recompute, and storing
        // a Plotlijn with an empty externally-assigned string key throws under Marten 9
        // (Marten 8 silently accepted it). Nullable.HasValue only tells us the wrapper is set,
        // not that the id is non-empty, so check the SVO's own HasValue.
        if (plotlijnId is not { } id || !id.HasValue)
        {
            return;
        }

        var plotlijnIdString = id.ToString();
        var plotlijn = await session.FirstOrDefaultAsync<Plotlijn>(p => p.Id == plotlijnIdString);

        // Get all paginas that belong to this plotlijn
        var memberPaginas = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotlijnIdQueryable == plotlijnIdString);

        var paginaTitels = memberPaginas
            .OrderBy(p => p.Dag)
            .ThenBy(p => p.StarttijdId)
            .ThenBy(p => p.Order)
            .Select(p => p.Titel)
            .ToList();

        if (plotlijn == null)
        {
            // Look up the domain from the first member pagina
            var firstMember = memberPaginas.Count > 0 ? memberPaginas[0] : null;

            plotlijn = new Plotlijn
            {
                PlotlijnId = id,
                PlotboekId = PlotboekId,
                DomainId = firstMember?.DomainId ?? Guid.Empty,
                DomainShorthand = firstMember?.DomainShorthand,
                PaginaTitels = [.. paginaTitels],
            };
        }
        else
        {
            plotlijn = plotlijn with
            {
                PaginaTitels = [.. paginaTitels],
            };
        }

        session.Store(plotlijn);
    }

    internal PlotboekPagina.Data BuildPaginaData()
    {
        var slName = AvailableSls.FirstOrDefault(s => s.Id == EditableVerantwoordelijkeSlId)?.DisplayName;
        var filteredExtraSlIds = EditableExtraSlIds?.Where(id => !string.IsNullOrEmpty(id)).ToList() ?? [];
        var extraSlNames = filteredExtraSlIds
            .Select(id => AvailableSls.FirstOrDefault(s => s.Id == id)?.DisplayName ?? id)
            .ToList();
        var npcRollen = EditableNpcRollen?
            .Where(r => !string.IsNullOrWhiteSpace(r.Rol))
            .Select(r => NpcRolData.Create(
                r.Rol,
                r.MinimumAantal,
                r.MaximumAantal,
                r.IsTerukerend,
                r.IsEerderGespeeld,
                r.GespeeldDoor,
                r.IsAlGekozen,
                r.GekozenPersoon,
                r.KledingEnSchmink,
                r.CastingInstructie,
                r.VereistAfstemming,
                r.AfstemmingToelichting))
            .ToList() ?? [];

        // Look up domain info if a domain is selected
        var selectedDomain = EditableDomainId != Guid.Empty
            ? AvailableDomains.FirstOrDefault(d => d.Id == EditableDomainId)
            : null;

        var props = EditableProps?
            .Where(p => !string.IsNullOrWhiteSpace(p.Omschrijving))
            .Select(p => PropLootItemData.Create(
                p.Omschrijving,
                // If source is SL use the sl id, otherwise leave empty
                string.Equals(p.VerantwoordelijkeSource, "SL", StringComparison.OrdinalIgnoreCase) ? p.VerantwoordelijkeSlId : null,
                // SL display name when source is SL
                string.Equals(p.VerantwoordelijkeSource, "SL", StringComparison.OrdinalIgnoreCase) ? AvailableSls.FirstOrDefault(s => s.Id == p.VerantwoordelijkeSlId)?.DisplayName : null,
                // Store the source and free-text
                p.VerantwoordelijkeSource,
                p.VerantwoordelijkeVrijeTekst))
            .ToList() ?? [];
        var loot = EditableLoot?
            .Where(l => !string.IsNullOrWhiteSpace(l.Omschrijving))
            .Select(l => PropLootItemData.Create(
                l.Omschrijving,
                string.Equals(l.VerantwoordelijkeSource, "SL", StringComparison.OrdinalIgnoreCase) ? l.VerantwoordelijkeSlId : null,
                string.Equals(l.VerantwoordelijkeSource, "SL", StringComparison.OrdinalIgnoreCase) ? AvailableSls.FirstOrDefault(s => s.Id == l.VerantwoordelijkeSlId)?.DisplayName : null,
                l.VerantwoordelijkeSource,
                l.VerantwoordelijkeVrijeTekst))
            .ToList() ?? [];

        // If EventBreed is checked and no location is set, auto-assign Hoofdveld
        if (EditableEventBreed && EditableLocatieEncounter == LocatieEncounter.None)
        {
            EditableLocatieEncounter = LocatieEncounter.Hoofdveld;
        }

        return new PlotboekPagina.Data
        {
            Titel = EditableTitle,
            DomainId = EditableDomainId,
            DomainShorthand = selectedDomain?.Shorthand,
            DomainDescription = selectedDomain?.Description,
            Dag = EditableDay,
            StarttijdId = EditableStarttijd,
            Order = EditableOrder,
            VerantwoordelijkeSlId = EditableVerantwoordelijkeSlId,
            VerantwoordelijkeSlName = slName,
            ExtraSlIds = [.. filteredExtraSlIds],
            ExtraSlNames = [.. extraSlNames],
            AantalBlokkenId = EditableAantalBlokken,
            EncounterTypeIds = EncounterTypeIds.Create(
                EditableEncounterTypesMajor?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? [],
                EditableEncounterTypesMinor?.Where(id => !string.IsNullOrWhiteSpace(id)).ToList() ?? []),
            LocatieEncounter = LocatieEncounterData.Create(
                EditableLocatieEncounter,
                EditableLocatieEncounterSpecificatie,
                ResolveEditableQuestLocatieId()),
            AantalNpcs = AantalNpcsData.Create(npcRollen, EditableAlleNpcs, EditableGeenNpcsNodig),
            ElevatorPitch = EditableElevatorPitch ?? string.Empty,
            SamenvattingPlot = TextNormalization.Normalize(EditableSamenvattingPlot),
            MeerInformatie = TextNormalization.Normalize(EditableMeerInformatie),
            BijlageIds = [.. (EditableReferencedBijlagen ?? [])
                .Select(s => BijlageId.TryParse(s, out var id) ? id : BijlageId.Empty)
                .Where(id => id.HasValue)],
            PlotlijnId = IsEvergreen ? PlotlijnId.Empty : EditablePlotlijnId,
            IsVolledigKlaar = EditableIsVolledigKlaar,
            IsGedraaid = EditableIsGedraaid,
            IsSpelerGedreven = EditableIsSpelerGedreven,
            IsFlexibel = EditableIsFlexibel,
            IsFlexibelNachtPlot = EditableIsFlexibelNachtPlot,
            Iconen = BuildIconen(),
            InstructiePentagon = TextNormalization.Normalize(EditableInstructiePentagon),
            PropsEnLoot = PropsEnLootData.Create(props, loot),
            Opmerkingen = EditableOpmerkingen ?? string.Empty,
            Debrief = DebriefData.Create(EditableDebriefGewenst, EditableDebriefVragen),
        };
    }

    /// <summary>
    /// Translates the posted picker value into the id stored on the aggregate: the "Anders…" sentinel and
    /// an id that no longer resolves both fall back to free text, so a location that was removed from the
    /// reference data can never leave the plot pointing at a dangling id.
    ///
    /// A deactivated location is only accepted where it already was this pagina's choice — the same
    /// exception the picker renders via <c>GroupByCategorie(..., includeInactiveId)</c>. Re-checking it
    /// here keeps a hand-crafted POST from picking up a location the admin retired.
    /// </summary>
    internal string? ResolveEditableQuestLocatieId()
    {
        if (string.IsNullOrWhiteSpace(EditableQuestLocatieId)
            || string.Equals(EditableQuestLocatieId, QuestLocatieAndersSentinel, StringComparison.Ordinal))
        {
            return null;
        }

        if (AvailableQuestLocatieConfigs.FindById(EditableQuestLocatieId) is not { } config)
        {
            return null;
        }

        var isHuidigeKeuze = SelectedPlotboekPagina?.LocatieEncounter.HasQuestLocatieKeuze == true
            && string.Equals(SelectedPlotboekPagina.LocatieEncounter.LocatieId, EditableQuestLocatieId, StringComparison.Ordinal);

        return config.IsActief || isHuidigeKeuze ? EditableQuestLocatieId : null;
    }

    internal PlotboekPaginaIcons BuildIconen()
    {
        var iconen = PlotboekPaginaIcons.None;
        if (EditableTimingCruciaal)
        {
            iconen |= PlotboekPaginaIcons.TimingCruciaal;
        }

        if (EditablePlotCruciaal)
        {
            iconen |= PlotboekPaginaIcons.PlotCruciaal;
        }

        if (EditableMagAlsEersteCancelled)
        {
            iconen |= PlotboekPaginaIcons.MagAlsEersteCancelled;
        }

        if (EditableUitgebreideBriefing)
        {
            iconen |= PlotboekPaginaIcons.UitgebreideBriefing;
        }

        if (EditableGrootPakOfKostuum)
        {
            iconen |= PlotboekPaginaIcons.GrootPakOfKostuum;
        }

        if (EditableMuziekVanuitTechniek)
        {
            iconen |= PlotboekPaginaIcons.MuziekVanuitTechniek;
        }

        if (EditableLichtOfRookVanuitTechniek)
        {
            iconen |= PlotboekPaginaIcons.LichtOfRookVanuitTechniek;
        }

        var isHoofdveld = EditableLocatieEncounter == LocatieEncounter.Hoofdveld;
        if (isHoofdveld && (EditableAlleNpcs || EditableEventBreed))
        {
            iconen |= PlotboekPaginaIcons.EventBreed;
        }
        return iconen;
    }

    /// <summary>
    /// Input model for binding NPC role data from the form.
    /// </summary>
    public sealed record NpcRolInput
    {
        required public string Rol { get; init; }

        required public int MinimumAantal { get; init; }

        required public int? MaximumAantal { get; init; }

        required public bool IsTerukerend { get; init; }

        required public bool IsEerderGespeeld { get; init; }

        required public string? GespeeldDoor { get; init; }

        required public bool IsAlGekozen { get; init; }

        required public string? GekozenPersoon { get; init; }

        required public string? KledingEnSchmink { get; init; }

        // Deliberately not `required`, unlike the fields above: they were added later and every existing
        // construction site (and the model binder for a role rendered before this feature) leaves them out.
        public string? CastingInstructie { get; init; }

        public bool VereistAfstemming { get; init; }

        public string? AfstemmingToelichting { get; init; }
    }

    /// <summary>
    /// Input model for binding prop or loot item data from the form.
    /// </summary>
    public sealed record PropLootItemInput
    {
        public string? Omschrijving { get; init; }

        // Source of the responsible person: "SL", "SpelerOfNpc" or "Anders".
        public string VerantwoordelijkeSource { get; init; } = "SL";

        // Free-text responsible person name used when source is not "SL".
        public string? VerantwoordelijkeVrijeTekst { get; init; }

        public string? VerantwoordelijkeSlId { get; init; }
    }
}
