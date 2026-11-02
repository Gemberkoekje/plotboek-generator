namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public ExportData GetData()
    {
        var starttijdId = StarttijdId ?? "None";
        var aantalBlokkenId = AantalBlokkenId ?? "None";
        var encounterTypeIds = EncounterTypeIds.HasValue ? EncounterTypeIds : EncounterTypeIds.Empty;

        return new ExportData
        {
            PlotboekId = PlotboekId,
            PaginaId = Id,
            EventVersion = Version,
            Titel = Titel,
            DomainId = DomainId,
            DomainShorthand = DomainShorthand,
            DomainDescription = DomainDescription,
            Dag = Dag,
            StarttijdId = starttijdId,
            Order = Order,
            VerantwoordelijkeSl = VerantwoordelijkeSl,
            ExtraSlIds = ExtraSlIds,
            AantalBlokkenId = aantalBlokkenId,
            EncounterTypeIds = encounterTypeIds,
            LocatieEncounter = LocatieEncounter,
            AantalNpcs = AantalNpcs,
            ElevatorPitch = ElevatorPitch,
            SamenvattingPlot = SamenvattingPlot,
            MeerInformatie = MeerInformatie,
            BijlageIds = BijlageRefs,
            Feedback = Feedback,
            PlotlijnId = PlotlijnId,
            IsVolledigKlaar = IsVolledigKlaar,
            IsGedraaid = IsGedraaid,
            IsSpelerGedreven = IsSpelerGedreven,
            IsFlexibel = IsFlexibel,
            IsFlexibelNachtPlot = IsFlexibelNachtPlot,
            IsVerwijderd = IsVerwijderd,
            Iconen = Iconen,
            InstructiePentagon = InstructiePentagon,
            PropsEnLoot = PropsEnLoot,
            Opmerkingen = Opmerkingen,
            Debrief = Debrief,
        };
    }

    public sealed record ExportData
    {
        public string Id { get => PaginaId.ToString(); }

        public string PlotboekIdQueryable { get => PlotboekId.ToString(); }

        public PlotboekId PlotboekId { get; init; }

        public PlotboekPaginaId PaginaId { get; init; }

        public long EventVersion { get; init; }

        public string Titel { get; init; }

        public Guid DomainId { get; init; }

        public string DomainShorthand { get; init; }

        public string DomainDescription { get; init; }

        /// <summary>
        /// The ID of the plotlijn this pagina belongs to,
        /// or null if this plot is self-contained.
        /// </summary>
        public PlotlijnId PlotlijnId { get; init; }

        /// <summary>
        /// The queryable string version of PlotlijnId for database queries.
        /// </summary>
        public string PlotlijnIdQueryable { get => PlotlijnId.ToString(); }

        /// <summary>
        /// Gets the display title combining domain shorthand and title for export.
        /// Format: "Ti - Hier komt de handelaar" if domain is set, otherwise just the title.
        /// Note: Use GetDisplayTitleWithPlotlijn for numbered plotlijn display.
        /// </summary>
        public string DisplayTitle => !string.IsNullOrEmpty(DomainShorthand)
            ? $"{DomainShorthand} - {Titel}"
            : Titel;

        public Dag Dag { get; init; }

        public string StarttijdId { get; init; }

        public int Order { get; init; }

        public string VerantwoordelijkeSl { get; init; }

        public IReadOnlyList<string> ExtraSlIds { get; init; } = [];

        public string AantalBlokkenId { get; init; }

        public EncounterTypeIds EncounterTypeIds { get; init; }

        public LocatieEncounterData LocatieEncounter { get; init; }

        public AantalNpcsData AantalNpcs { get; init; }

        public string ElevatorPitch { get; init; }

        public string SamenvattingPlot { get; init; }

        public string MeerInformatie { get; init; }

        /// <summary>
        /// The IDs of the plotboek-level bijlagen this encounter references.
        /// </summary>
        public IReadOnlyList<BijlageId> BijlageIds { get; init; } = [];

        /// <summary>
        /// The feedback points on this encounter. Part of the read model (unlike
        /// <see cref="PlotboekPagina.Data"/>) so the plot list can render the feedback icon without
        /// replaying every event stream. Not included in the DOCX export: feedback is internal traffic.
        /// </summary>
        public IReadOnlyList<FeedbackData> Feedback { get; init; } = [];

        /// <summary>Whether at least one feedback point is still open, and therefore blocking completion.</summary>
        public bool HeeftOpenstaandeFeedback => Feedback.Any(f => f.Status == FeedbackStatus.Open);

        public bool IsVolledigKlaar { get; init; }

        public bool IsGedraaid { get; init; }

        public bool IsSpelerGedreven { get; init; }

        public bool IsFlexibel { get; init; }

        public bool IsFlexibelNachtPlot { get; init; }

        public bool IsVerwijderd { get; init; }

        public PlotboekPaginaIcons Iconen { get; init; }

        public string InstructiePentagon { get; init; }

        public PropsEnLootData PropsEnLoot { get; init; }

        public string Opmerkingen { get; init; }

        public DebriefData Debrief { get; init; }

    }
}
