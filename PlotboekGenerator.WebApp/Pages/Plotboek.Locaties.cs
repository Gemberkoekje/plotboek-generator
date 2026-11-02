namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// Questlocatie support for the pagina detail panel: resolving a stored <c>LocatieId</c> to its current
/// display name, and building the contextual note shown under the picker.
/// </summary>
public sealed partial class PlotboekModel
{
    /// <summary>
    /// Renders a location for display, resolving a picked questlocatie to its current display name.
    /// Resolution deliberately happens here and not in <see cref="LocatieEncounterData"/>: <c>core</c> does
    /// not know the reference data, so renaming a location updates every screen at once.
    /// </summary>
    public string GetLocatieEncounterDisplay(LocatieEncounterData locatie)
    {
        if (!locatie.HasValue)
        {
            return string.Empty;
        }

        return locatie.HasQuestLocatieKeuze
            ? $"{locatie.Locatie} ({AvailableQuestLocatieConfigs.GetDisplayName(locatie.LocatieId)})"
            : locatie.ToString();
    }

    /// <summary>
    /// The contextual note for the questlocatie currently selected in the form, or an empty note when
    /// nothing is picked. Used to render the initial state server-side; the JS takes over from the
    /// <c>data-*</c> attributes on the options after that.
    /// </summary>
    public QuestLocatieNotitie CurrentQuestLocatieNotitie
    {
        get
        {
            if (EditableLocatieEncounter != LocatieEncounter.Questveld)
            {
                return QuestLocatieNotitie.Empty;
            }

            var locatie = AvailableQuestLocatieConfigs.FindById(EditableQuestLocatieId);
            if (locatie is null)
            {
                return QuestLocatieNotitie.Empty;
            }

            return BuildQuestLocatieNotitie(
                AvailableQuestLocatieCategorieConfigs.FindById(locatie.CategorieId),
                locatie);
        }
    }

    /// <summary>
    /// Combines the category note and the location's own note into a single alert. An empty result means
    /// nothing needs to be shown.
    /// </summary>
    internal static QuestLocatieNotitie BuildQuestLocatieNotitie(QuestLocatieCategorieConfig? categorie, QuestLocatieConfig? locatie)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(categorie?.Notitie))
        {
            parts.Add(categorie.Notitie.Trim());
        }

        if (!string.IsNullOrWhiteSpace(locatie?.Notitie))
        {
            parts.Add(locatie.Notitie.Trim());
        }

        if (parts.Count == 0)
        {
            return QuestLocatieNotitie.Empty;
        }

        return new QuestLocatieNotitie
        {
            Text = string.Join(" ", parts),
            CssClass = ErnstCssClass(categorie?.Ernst ?? QuestLocatieErnst.None),
        };
    }

    /// <summary>
    /// Maps the category severity onto its Bootstrap alert class. Anything short of "afgeraden" is an
    /// informational note, so a location-specific remark without a severe category still renders.
    /// </summary>
    internal static string ErnstCssClass(QuestLocatieErnst ernst)
        => ernst == QuestLocatieErnst.Afgeraden ? "alert-warning" : "alert-info";

    /// <summary>
    /// Per pagina, the other encounters competing for the same questlocatie. Filled once per load by
    /// <see cref="BerekenLocatieConflicten"/>; a pagina without conflicts is simply absent.
    /// </summary>
    public Dictionary<PlotboekPaginaId, List<LocatieConflict>> LocatieConflicten { get; private set; } = [];

    /// <summary>The conflicts for one pagina, empty when there are none.</summary>
    public IReadOnlyList<LocatieConflict> GetLocatieConflicten(PlotboekPaginaId paginaId)
        => LocatieConflicten.TryGetValue(paginaId, out var conflicten) ? conflicten : [];

    /// <summary>Whether a pagina double-books a questlocatie with another encounter in the same slot.</summary>
    public bool HeeftExacteLocatieBotsing(PlotboekPaginaId paginaId)
        => GetLocatieConflicten(paginaId).Any(c => c.Soort == LocatieConflictSoort.ExacteMatch);

    /// <summary>
    /// Works out which encounters compete for the same questlocatie.
    ///
    /// Only encounters with a curated questlocatie take part: free text from the "Anders…" branch is
    /// too typo-prone to compare. Two encounters clash when they are on the same day and their slot spans
    /// (see <see cref="BerekenSlotSpan"/>) overlap. An encounter without a day/time slot cannot overlap in
    /// time, but a shared location is still worth mentioning, so it produces a
    /// <see cref="LocatieConflictSoort.ZonderTijdvak"/> note on the exact match.
    /// </summary>
    internal void BerekenLocatieConflicten()
    {
        LocatieConflicten = [];

        var kandidaten = PlotboekPaginas
            .Where(p => p.LocatieEncounter.HasQuestLocatieKeuze)
            .ToList();

        if (kandidaten.Count < 2)
        {
            ProjecteerLocatieConflicten();
            return;
        }

        var starttijdOrderByDag = new Dictionary<Dag, Dictionary<string, int>>();
        var overflowById = BuildAantalBlokkenOverflowById();
        var overlapById = AvailableQuestLocatieConfigs.BuildSymmetrischeOverlap();

        var slotsByPagina = kandidaten.ToDictionary(
            p => p.Id,
            p => HeeftTijdvak(p)
                ? BerekenSlotSpan(
                        p.StarttijdId,
                        p.AantalBlokkenId,
                        BuildStarttijdOrderByIdVoorDag(p.Dag, starttijdOrderByDag),
                        overflowById)
                    .ToHashSet(StringComparer.Ordinal)
                : []);

        foreach (var pagina in kandidaten)
        {
            var conflicten = new List<LocatieConflict>();

            foreach (var ander in kandidaten.Where(a => a.Id != pagina.Id))
            {
                var soort = BepaalConflictSoort(pagina, ander, slotsByPagina, overlapById);
                if (soort == LocatieConflictSoort.None)
                {
                    continue;
                }

                conflicten.Add(new LocatieConflict
                {
                    PaginaId = ander.Id,
                    Titel = ander.Title,
                    Soort = soort,
                    LocatieId = ander.LocatieEncounter.LocatieId,
                    Dag = ander.Dag,
                    StarttijdId = ander.StarttijdId ?? string.Empty,
                });
            }

            if (conflicten.Count > 0)
            {
                // Hard clashes first, so the detail panel and the list icon lead with the worst news.
                LocatieConflicten[pagina.Id] = [.. conflicten
                    .OrderBy(c => c.Soort switch
                    {
                        LocatieConflictSoort.ExacteMatch => 0,
                        LocatieConflictSoort.Overlapgroep => 1,
                        _ => 2,
                    })
                    .ThenBy(c => c.Titel, StringComparer.OrdinalIgnoreCase)];
            }
        }

        ProjecteerLocatieConflicten();
    }

    /// <summary>
    /// Copies the computed conflicts onto the pagina infos, mirroring how <c>ComputePlotlijnNummers</c>
    /// attaches its display numbers. The list partial renders per pagina and has no access to the page
    /// model, so the icon needs the data on the record itself.
    /// </summary>
    private void ProjecteerLocatieConflicten()
    {
        PlotboekPaginas = [.. PlotboekPaginas.Select(p => p with { LocatieConflicten = GetLocatieConflicten(p.Id) })];
    }

    private static LocatieConflictSoort BepaalConflictSoort(
        PlotboekPaginaInfo pagina,
        PlotboekPaginaInfo ander,
        IReadOnlyDictionary<PlotboekPaginaId, HashSet<string>> slotsByPagina,
        IReadOnlyDictionary<string, IReadOnlyList<string>> overlapById)
    {
        var locatieId = pagina.LocatieEncounter.LocatieId;
        var anderLocatieId = ander.LocatieEncounter.LocatieId;
        var isExact = string.Equals(locatieId, anderLocatieId, StringComparison.Ordinal);
        var isOverlap = !isExact
            && overlapById.TryGetValue(locatieId, out var overlapt)
            && overlapt.Contains(anderLocatieId, StringComparer.Ordinal);

        if (!isExact && !isOverlap)
        {
            return LocatieConflictSoort.None;
        }

        // Without a time slot on either side there is no "at the same moment" to compare. Only the exact
        // match is worth a note then; a merely adjacent field is not interesting without a time.
        if (!HeeftTijdvak(pagina) || !HeeftTijdvak(ander))
        {
            return isExact ? LocatieConflictSoort.ZonderTijdvak : LocatieConflictSoort.None;
        }

        if (pagina.Dag != ander.Dag || !slotsByPagina[pagina.Id].Overlaps(slotsByPagina[ander.Id]))
        {
            return LocatieConflictSoort.None;
        }

        return isExact ? LocatieConflictSoort.ExacteMatch : LocatieConflictSoort.Overlapgroep;
    }

    /// <summary>
    /// Whether an encounter is planned at a moment in time at all. Player-driven encounters and encounters
    /// without a day or starttijd (the evergreen plotboek, or simply not planned yet) are not.
    /// </summary>
    private static bool HeeftTijdvak(PlotboekPaginaInfo pagina)
        => !pagina.IsSpelerGedreven
        && pagina.Dag != Dag.None
        && !string.IsNullOrWhiteSpace(pagina.StarttijdId)
        && !string.Equals(pagina.StarttijdId, "None", StringComparison.Ordinal);

    /// <summary>
    /// A contextual note under the questlocatie picker, plus the alert class it should render with.
    /// </summary>
    public sealed record QuestLocatieNotitie
    {
        public static QuestLocatieNotitie Empty { get; } = new ();

        public string Text { get; init; } = string.Empty;

        public string CssClass { get; init; } = "alert-info";

        public bool HasValue => !string.IsNullOrWhiteSpace(Text);
    }
}
