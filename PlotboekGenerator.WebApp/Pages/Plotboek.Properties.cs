namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public PlotboekId PlotboekId => Jaar.HasValue && !string.IsNullOrWhiteSpace(EventId) && !string.Equals(EventId, "None", StringComparison.Ordinal)
        ? PlotboekId.Create(Jaar, EventId)
        : PlotboekId.Empty;

    [BindProperty(SupportsGet = true)]
    public Year Jaar { get; set; }

    [BindProperty(SupportsGet = true, Name = "Event")]
    public string EventId { get; set; } = "None";

    /// <summary>
    /// Whether the currently selected plotboek is the Evergreen plotboek.
    /// </summary>
    public bool IsEvergreen => string.Equals(EventId, "Evergreen", StringComparison.Ordinal);

    [BindProperty(SupportsGet = true)]
    public PlotboekPaginaId PaginaId { get; set; }

    /// <summary>
    /// When set (via the "herstel naar dit punt" link in the audit trail), the pagina detail form is
    /// pre-filled with the values as of this stream version. Nothing is persisted until the user saves.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public long? RestorePaginaVersion { get; set; }

    /// <summary>Whether the pagina detail form currently shows a historical (restore) preview.</summary>
    public bool IsRestorePreview { get; internal set; }

    /// <summary>Timestamp of the historical version being previewed, for the banner.</summary>
    public DateTime? RestoreFromWhen { get; internal set; }

    /// <summary>User who created the historical version being previewed, for the banner.</summary>
    public string? RestoreFromWho { get; internal set; }

    [BindProperty]
    public string? Action { get; set; }

    [BindProperty]
    public string? NewPaginaTitel { get; set; }

    public List<PlotboekInfo> AvailablePlotboeken { get; set; } = [];

    public List<PlotboekPaginaInfo> PlotboekPaginas { get; set; } = [];

    public PlotboekPaginaInfo? SelectedPlotboekPagina { get; set; }

    public IReadOnlyList<Dag> AvailableDays { get; set; } = [Dag.None];

    public List<string> AvailableStarttijden { get; set; } = ["None"];

    public string PlotboekAccessMessage { get; set; } = string.Empty;

    public bool HasPlotboekAccess => string.IsNullOrWhiteSpace(PlotboekAccessMessage);

    public bool NoNpcPlotboekenAvailable { get; set; }

    public bool IsAdmin => User?.IsInRole(UserRoles.Admin) == true;

    public bool IsNpc => User?.IsInRole(UserRoles.NPC) == true && !IsAdmin && User?.IsInRole(UserRoles.User) != true;

    public bool IsPlotboekReadOnly => IsNpc || (PlotboekSettings?.IsVolledigKlaar == true && !IsAdmin);

    /// <summary>
    /// A pagina is read-only when the whole plotboek is read-only, or when the pagina itself is
    /// marked "volledig klaar" and the current user is not an admin. Mirrors the isPaginaReadOnly
    /// logic in _PlotboekPaginaDetails.cshtml so rich-text (Quill) editors match the disabled fieldset.
    /// During a restore preview the pagina self-lock is ignored: the plotboek-wide lock and the NPC
    /// role still apply via <see cref="IsPlotboekReadOnly"/>, but a restore is an editing action, so a
    /// version that was "volledig klaar" at the time must not render the preview read-only.
    /// </summary>
    public bool IsSelectedPaginaReadOnly => IsPlotboekReadOnly || (!IsRestorePreview && SelectedPlotboekPagina?.IsVolledigKlaar == true && !IsAdmin);

    public List<SlUserInfo> AvailableSls { get; set; } = [];

    /// <summary>
    /// The <see cref="SlUserInfo.Id"/> of the currently logged-in user, used to default the
    /// SL-overzicht to the user's own encounters. Null when it can't be resolved.
    /// </summary>
    public string? CurrentUserSlId { get; private set; }

    public List<Domain> AvailableDomains { get; set; } = [];

    /// <summary>
    /// The plotboek's bijlagen, offered as reference options on each encounter. Sourced from the loaded settings.
    /// </summary>
    public IReadOnlyList<BijlageData> AvailableBijlagen => PlotboekSettings?.Bijlagen ?? [];

    /// <summary>
    /// Available plotlijnen for the current plotboek, for selection in the UI.
    /// </summary>
    public List<Plotlijn> AvailablePlotlijnen { get; set; } = [];

    /// <summary>
    /// Starttijd options loaded from the database.
    /// </summary>
    public List<StarttijdConfig> AvailableStarttijdConfigs { get; set; } = [];

    /// <summary>
    /// AantalBlokken options loaded from the database.
    /// </summary>
    public List<AantalBlokkenConfig> AvailableAantalBlokkenConfigs { get; set; } = [];

    /// <summary>
    /// SoortEncounter options loaded from the database.
    /// </summary>
    public List<SoortEncounterConfig> AvailableSoortEncounterConfigs { get; set; } = [];

    /// <summary>
    /// LegacyEvent options loaded from the database.
    /// </summary>
    public List<EventConfig> AvailableEventConfigs { get; set; } = [];

    /// <summary>
    /// Questlocatie categories (the optgroups of the questlocatie picker) loaded from the database.
    /// </summary>
    public List<QuestLocatieCategorieConfig> AvailableQuestLocatieCategorieConfigs { get; set; } = [];

    /// <summary>
    /// Questlocatie options loaded from the database, including deactivated ones so a historic choice
    /// still resolves to its display name.
    /// </summary>
    public List<QuestLocatieConfig> AvailableQuestLocatieConfigs { get; set; } = [];

    /// <summary>
    /// The standaard-NPC's an SL can add to a plot as a filled-in role, sorted by name. Empty for NPC
    /// users, who never edit a plot.
    /// </summary>
    public List<StandaardNpc> AvailableStandaardNpcs { get; set; } = [];

    /// <summary>
    /// NPC totals per day and time block for display in the left menu.
    /// </summary>
    public Dictionary<(Dag, string), BlokNpcTotaal> BlokNpcTotalen { get; set; } = [];

    /// <summary>
    /// Verwacht aantal NPCs per dag, ingevuld in de plotboek instellingen.
    /// </summary>
    public IReadOnlyDictionary<Dag, int> DagNpcVerwacht { get; set; } = new Dictionary<Dag, int>();

    /// <summary>
    /// Concurrency error message to display when someone else has modified the same data.
    /// </summary>
    public string? ConcurrencyError { get; set; }

    // Google Drive properties moved to Plotboek.GoogleDrive.cs
    public sealed record PlotboekInfo()
    {
        required public PlotboekId Id { get; init; }

        required public Year Jaar { get; init; }

        required public string EventId { get; init; }

        public DateOnly? EindDatum { get; init; }

        public bool Exists { get; init; }

        public bool IsVolledigKlaar { get; init; }

        /// <summary>
        /// Short abbreviation for the event, populated from DB config during loading.
        /// Falls back to event ID if config is not loaded.
        /// </summary>
        public string EventAbbreviation { get; init; } = string.Empty;

        public string DisplayName => string.Equals(EventId, "Evergreen", StringComparison.Ordinal)
            ? "Evergreen"
            : $"{Jaar} - {GetAbbreviation()}";

        private string GetAbbreviation()
        {
            return string.IsNullOrEmpty(EventAbbreviation) ? EventId : EventAbbreviation;
        }
    }

    public sealed record PlotboekPaginaInfo()
    {
        required public PlotboekPaginaId Id { get; init; }

        public long EventVersion { get; init; }

        required public string Title { get; init; }

        public Guid DomainId { get; init; }

        public string? DomainShorthand { get; init; }

        public string? DomainDescription { get; init; }

        /// <summary>
        /// The ID of the plotlijn this pagina belongs to, or null if self-contained.
        /// </summary>
        public PlotlijnId PlotlijnId { get; init; }

        required public Dag Dag { get; init; }

        required public string StarttijdId { get; init; }

        required public int Order { get; init; }

        public string? VerantwoordelijkeSlId { get; init; }

        public string? VerantwoordelijkeSlName { get; init; }

        public IReadOnlyList<string>? ExtraSlIds { get; init; }

        public IReadOnlyList<string>? ExtraSlNames { get; init; }

        public string AantalBlokkenId { get; init; }

        public EncounterTypeIds EncounterTypeIds { get; init; }

        public LocatieEncounterData LocatieEncounter { get; init; }

        public AantalNpcsData AantalNpcs { get; init; }

        public string? ElevatorPitch { get; init; }

        public string? SamenvattingPlot { get; init; }

        public string? MeerInformatie { get; init; }

        /// <summary>
        /// The IDs of the plotboek-level bijlagen this encounter references.
        /// </summary>
        public IReadOnlyList<BijlageId> BijlageRefs { get; init; } = [];

        /// <summary>
        /// The feedback points on this encounter, so both the detail panel and the plot list icon can
        /// render without reloading the aggregate.
        /// </summary>
        public IReadOnlyList<FeedbackData> Feedback { get; init; } = [];

        /// <summary>Whether this encounter still has feedback waiting for a response, and is therefore blocked from "volledig klaar".</summary>
        public bool HeeftOpenstaandeFeedback => Feedback.Any(f => f.Status == FeedbackStatus.Open);

        /// <summary>Whether every feedback point has been responded to but the HSL has not signed them all off yet.</summary>
        public bool WachtOpAfhandeling => !HeeftOpenstaandeFeedback && Feedback.Any(f => f.Status == FeedbackStatus.Beantwoord);

        /// <summary>
        /// The computed plotlijn number for display (e.g., "1", "1.1", "2.2").
        /// Set during loading based on plotlijn relationships.
        /// </summary>
        public string? PlotlijnNummer { get; init; }

        /// <summary>
        /// Other encounters competing for this encounter's questlocatie. Projected here during loading by
        /// <see cref="BerekenLocatieConflicten"/> so the icon partial can render without extra plumbing;
        /// <see cref="LocatieConflicten"/> on the page model holds the same data keyed by pagina.
        /// </summary>
        public IReadOnlyList<LocatieConflict> LocatieConflicten { get; init; } = [];

        /// <summary>
        /// Indicates whether this plot has been marked as fully complete.
        /// </summary>
        public bool IsVolledigKlaar { get; init; }

        /// <summary>
        /// Indicates whether this evergreen encounter has already been run at an event.
        /// </summary>
        public bool IsGedraaid { get; init; }

        /// <summary>
        /// Indicates whether this encounter is player driven.
        /// </summary>
        public bool IsSpelerGedreven { get; init; }

        /// <summary>
        /// Indicates whether this encounter has a flexible time slot.
        /// </summary>
        public bool IsFlexibel { get; init; }

        /// <summary>
        /// Indicates whether this encounter is a flexible night plot (meant for a 20:00-or-later slot).
        /// </summary>
        public bool IsFlexibelNachtPlot { get; init; }

        /// <summary>
        /// Explicit icon flags set by the user.
        /// </summary>
        public PlotboekPaginaIcons Iconen { get; init; }

        public string? InstructiePentagon { get; init; }

        public PropsEnLootData PropsEnLoot { get; init; }

        public string? Opmerkingen { get; init; }

        public DebriefData Debrief { get; init; }
    }

    public sealed record SlUserInfo()
    {
        required public string Id { get; init; }

        required public string DisplayName { get; init; }
    }
}
