using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.core;
using PlotboekGenerator.core.Generator.Templates;
using PlotboekGenerator.core.Resources;
using PlotboekGenerator.infrastructure.Resources;

namespace PlotboekGenerator.infrastructure.Services;

public sealed partial class PlotboekExportBuilder : IPlotboekExportDataBuilder
{
    private readonly IDocumentStore _documentStore;

    public PlotboekExportBuilder(IDocumentStore documentStore)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<Result<PlotboekTemplateData>> BuildAsync(PlotboekId plotboekId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var session = _documentStore.OpenSession();
            var exportData = await BuildRecordsAsync(plotboekId, session, cancellationToken).ConfigureAwait(false);
            return exportData;
        }
        catch (Exception ex)
        {
            return Result.WithMessages<PlotboekTemplateData>(ValidationMessage.Error(ex.Message));
        }
    }

    public async Task<PlotboekTemplateData> BuildRecordsAsync(PlotboekId plotboekId, IDocumentSession session, CancellationToken cancellationToken = default)
    {
        var plotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == plotboekId.ToString(), cancellationToken)
            ?? throw new InvalidOperationException($"Plotboek with ID {plotboekId} not found");
        var pages = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotboekIdQueryable == plotboekId.ToString() && !p.IsVerwijderd, cancellationToken);

        var starttijdConfigs = await session.QueryAllAsync<StarttijdConfig>(cancellationToken);
        var starttijdLookup = starttijdConfigs.Where(c => !string.IsNullOrEmpty(c.Id)).ToDictionary(c => c.Id, StringComparer.Ordinal);
        var aantalBlokkenConfigs = await session.QueryAllAsync<AantalBlokkenConfig>(cancellationToken);
        var aantalBlokkenLookup = aantalBlokkenConfigs.Where(c => !string.IsNullOrEmpty(c.Id)).ToDictionary(c => c.Id, StringComparer.Ordinal);
        var encounterConfigs = await session.QueryAllAsync<SoortEncounterConfig>(cancellationToken);
        var encounterLookup = encounterConfigs.Where(c => !string.IsNullOrEmpty(c.Id)).ToDictionary(c => c.Id, StringComparer.Ordinal);
        var questLocatieConfigs = await session.QueryAllAsync<QuestLocatieConfig>(cancellationToken);
        var questLocatieLookup = questLocatieConfigs.Where(c => !string.IsNullOrEmpty(c.Id)).ToDictionary(c => c.Id, StringComparer.Ordinal);
        var eventConfigs = await session.QueryAllAsync<EventConfig>(cancellationToken);
        var eventLookup = eventConfigs.Where(c => !string.IsNullOrEmpty(c.Id)).ToDictionary(c => c.Id, StringComparer.Ordinal);

        var userIds = pages.SelectMany(p => new[] { p.VerantwoordelijkeSl }.Concat(p.ExtraSlIds ?? [])).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var users = await session.QueryAsync<PlotboekUser>(u => userIds.Contains(u.Id), cancellationToken);
        var userLookup = users.ToDictionary(u => u.Id, u => u.UserName ?? u.Email ?? u.Id);

        var plotlijnNummers = ComputePlotlijnNummersForExport(pages, starttijdLookup);
        var daysInRange = GetDaysInRange(plotboek.StartDag, plotboek.EindDag);
        var bijlageLookup = BuildBijlageLookup(plotboek.Bijlagen);

        var lijst = BuildDayDataList(pages, daysInRange, plotlijnNummers, userLookup, starttijdLookup, aantalBlokkenLookup, encounterLookup, bijlageLookup, questLocatieLookup);
        var extrainfolijst = BuildExtraInfoList(pages, daysInRange, plotlijnNummers, starttijdLookup);
        var bijlagenLijst = BuildBijlagenList(plotboek.Bijlagen);
        var tijdschemasLijst = await BuildTijdschemasListAsync(session, pages, daysInRange, plotlijnNummers, userLookup, starttijdLookup, cancellationToken);
        var evergreenPagesList = await BuildEvergreenPagesAsync(session, userLookup, encounterLookup, aantalBlokkenLookup, questLocatieLookup, cancellationToken);

        var usedDomainIds = pages.Where(p => p.DomainId != Guid.Empty).Select(p => p.DomainId).Distinct().ToList();
        var domains = await session.QueryAsync<Domain>(d => usedDomainIds.Contains(d.Id), cancellationToken);
        var orderedDomains = domains.OrderBy(d => d.Order).ToList();

        var aslDomeinen = orderedDomains.Where(d => d.Shorthand.Length == 1).Select(d => new DomeinData { DomeinLetter = d.Shorthand, DomeinNaam = d.Description }).ToImmutableList();
        var gslDomeinen = orderedDomains.Where(d => d.Shorthand.Length >= 2).Select(d => new DomeinData { DomeinLetter = d.Shorthand, DomeinNaam = d.Description }).ToImmutableList();

        var characterNames = await session.QueryAllAsync<CharacterName>(cancellationToken);
        var namen = characterNames.OrderBy(n => n.Naam).Select(n => new NaamData { Naam = n.Naam }).ToImmutableList();

        var worldNames = await session.QueryAllAsync<WorldName>(cancellationToken);
        var werelden = worldNames.OrderBy(n => n.Naam).Select(n => new NaamData { Naam = n.Naam }).ToImmutableList();

        var voorblad = await session.LoadAsync<PlotboekVoorblad>(plotboekId.ToString(), cancellationToken);

        return new PlotboekTemplateData
        {
            Naam = BuildPlotboekNaam(plotboek, eventLookup),
            Voorblad = voorblad?.Afbeelding ?? [],
            SamenvattingAlgemeenPlot = plotboek.SamenvattingAlgemeenPlot ?? string.Empty,
            Tijdschemas = [.. tijdschemasLijst],
            Days = [.. lijst],
            ExtraInfo = [.. extrainfolijst],
            Bijlagen = [.. bijlagenLijst],
            EvergreenBijlageNummer = (bijlagenLijst.Count + 1).ToString(),
            NamenBijlageNummer = (bijlagenLijst.Count + 2).ToString(),
            AslDomeinen = aslDomeinen,
            GslDomeinen = gslDomeinen,
            EvergreenPages = evergreenPagesList,
            Namen = namen,
            Werelden = werelden,
        };
    }

    public static IReadOnlyList<Dag> GetDaysInRange(Dag startDag, Dag eindDag)
    {
        var days = new List<Dag> { Dag.None };
        if (startDag == Dag.None || eindDag == Dag.None)
        {
            return days;
        }

        var current = startDag;
        while (true)
        {
            days.Add(current);
            if (current == eindDag)
            {
                break;
            }
            current = current == Dag.Zondag ? Dag.Maandag : current + 1;
        }
        return days;
    }

    public static List<DayData> BuildDayDataList(
        IReadOnlyList<PlotboekPagina.ExportData> pages,
        IReadOnlyList<Dag> daysInRange,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers,
        Dictionary<string, string> userLookup)
        => BuildDayDataList(pages, daysInRange, plotlijnNummers, userLookup, DefaultStarttijdConfigs, DefaultAantalBlokkenConfigs, DefaultEncounterConfigs, EmptyBijlageLookup, EmptyQuestLocatieConfigs);

    private static List<DayData> BuildDayDataList(
        IReadOnlyList<PlotboekPagina.ExportData> pages,
        IReadOnlyList<Dag> daysInRange,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers,
        IReadOnlyDictionary<string, string> userLookup,
        IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs,
        IReadOnlyDictionary<string, AantalBlokkenConfig> aantalBlokkenConfigs,
        IReadOnlyDictionary<string, SoortEncounterConfig> encounterConfigs,
        IReadOnlyDictionary<BijlageId, BijlageData> bijlageLookup,
        IReadOnlyDictionary<string, QuestLocatieConfig> questLocatieConfigs)
    {
        var lijst = new List<DayData>();
        foreach (var dag in daysInRange.Where(d => d != Dag.None && pages.Any(p => p.Dag == d && !p.IsSpelerGedreven)))
        {
            var dagStarttijdIds = pages
                .Where(p => p.Dag == dag && !p.IsSpelerGedreven && !string.IsNullOrEmpty(p.StarttijdId) && p.StarttijdId != "None")
                .Select(p => p.StarttijdId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => StarttijdSortKey(id, starttijdConfigs))
                .ThenBy(id => id, StringComparer.Ordinal);

            foreach (var starttijdId in dagStarttijdIds)
            {
                var relevantPages = pages.Where(p => p.Dag == dag && p.StarttijdId == starttijdId && !p.IsSpelerGedreven);
                if (relevantPages.Any())
                {
                    lijst.Add(new DayData
                    {
                        Day = $"{dag.Naam()} {GetStarttijdDisplayName(starttijdId, starttijdConfigs)}",
                        Pages = [.. relevantPages.OrderBy(p => p.Order).Select(p => BuildPageData(p, plotlijnNummers, userLookup, starttijdConfigs, aantalBlokkenConfigs, encounterConfigs, bijlageLookup, questLocatieConfigs))],
                    });
                }
            }
        }

        if (pages.Any(p => p.Dag == Dag.None && !p.IsSpelerGedreven))
        {
            lijst.Add(new DayData
            {
                Day = InfraTekst.ExportNogNietIngepland,
                Pages = [.. pages.Where(p => p.Dag == Dag.None && !p.IsSpelerGedreven)
                    .OrderBy(p => StarttijdSortKey(p.StarttijdId, starttijdConfigs))
                    .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
                    .ThenBy(p => p.Order)
                    .Select(p => BuildPageData(p, plotlijnNummers, userLookup, starttijdConfigs, aantalBlokkenConfigs, encounterConfigs, bijlageLookup, questLocatieConfigs))],
            });
        }

        var spelerGedrevenPages = pages.Where(p => p.IsSpelerGedreven)
            .OrderBy(p => p.Dag)
            .ThenBy(p => StarttijdSortKey(p.StarttijdId, starttijdConfigs))
            .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
            .ThenBy(p => p.Order)
            .ToList();
        if (spelerGedrevenPages.Any())
        {
            lijst.Add(new DayData
            {
                Day = InfraTekst.ExportSpelerGedrevenKop,
                Pages = [.. spelerGedrevenPages.Select(p => BuildPageData(p, plotlijnNummers, userLookup, starttijdConfigs, aantalBlokkenConfigs, encounterConfigs, bijlageLookup, questLocatieConfigs))],
            });
        }

        return lijst;
    }

    public static List<ExtraInfoData> BuildExtraInfoList(
        IReadOnlyList<PlotboekPagina.ExportData> pages,
        IReadOnlyList<Dag> daysInRange,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers)
        => BuildExtraInfoList(pages, daysInRange, plotlijnNummers, DefaultStarttijdConfigs);

    private static List<ExtraInfoData> BuildExtraInfoList(
        IReadOnlyList<PlotboekPagina.ExportData> pages,
        IReadOnlyList<Dag> daysInRange,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers,
        IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs)
    {
        var extrainfolijst = new List<ExtraInfoData>();
        foreach (var dag in daysInRange.Where(d => d != Dag.None && pages.Any(p => p.Dag == d)))
        {
            var relevantPages = pages.Where(p => p.Dag == dag && !p.IsSpelerGedreven && HasExtraInfo(p.MeerInformatie));
            if (relevantPages.Any())
            {
                extrainfolijst.AddRange(relevantPages
                    .OrderBy(p => StarttijdSortKey(p.StarttijdId, starttijdConfigs))
                    .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
                    .ThenBy(p => p.Order)
                    .Select(p => BuildExtraInfoData(p, plotlijnNummers)));
            }
        }

        if (pages.Any(p => p.Dag == Dag.None))
        {
            var relevantPages = pages.Where(p => p.Dag == Dag.None && !p.IsSpelerGedreven && HasExtraInfo(p.MeerInformatie));
            if (relevantPages.Any())
            {
                extrainfolijst.AddRange(relevantPages
                    .OrderBy(p => StarttijdSortKey(p.StarttijdId, starttijdConfigs))
                    .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
                    .ThenBy(p => p.Order)
                    .Select(p => BuildExtraInfoData(p, plotlijnNummers)));
            }
        }

        var spelerGedrevenExtraInfo = pages.Where(p => p.IsSpelerGedreven && HasExtraInfo(p.MeerInformatie))
            .OrderBy(p => p.Dag)
            .ThenBy(p => StarttijdSortKey(p.StarttijdId, starttijdConfigs))
            .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
            .ThenBy(p => p.Order)
            .ToList();
        if (spelerGedrevenExtraInfo.Any())
        {
            extrainfolijst.AddRange(spelerGedrevenExtraInfo.Select(p => BuildExtraInfoData(p, plotlijnNummers)));
        }

        return extrainfolijst;
    }

    private static bool HasExtraInfo(string meerInformatie)
        => !string.IsNullOrWhiteSpace(TextNormalization.Normalize(meerInformatie));

    private static async Task<List<TijdschemasData>> BuildTijdschemasListAsync(
        IDocumentSession session,
        IReadOnlyList<PlotboekPagina.ExportData> pages,
        IReadOnlyList<Dag> daysInRange,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers,
        IReadOnlyDictionary<string, string> userLookup,
        IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs,
        CancellationToken cancellationToken)
    {
        var tijdschemasLijst = new List<TijdschemasData>();
        var allMeetings = await session.QueryAllAsync<Meeting>(cancellationToken);
        var meetingsByType = allMeetings.GroupBy(m => m.DagType).ToDictionary(g => g.Key, g => g.OrderBy(m => m.Order).ToList());

        var daysWithPages = daysInRange.Where(d => d != Dag.None && pages.Any(p => p.Dag == d)).ToList();
        foreach (var dag in daysWithPages)
        {
            var dagPages = pages.Where(p => p.Dag == dag && !p.IsSpelerGedreven)
                .OrderBy(p => StarttijdSortKey(p.StarttijdId, starttijdConfigs))
                .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
                .ThenBy(p => p.Order)
                .ToList();
            var dagType = GetMeetingDagType(dag, daysWithPages);
            var dayMeetings = meetingsByType.GetValueOrDefault(dagType, []);
            tijdschemasLijst.Add(BuildTijdschemasData(dag.Naam(), dagPages, plotlijnNummers, userLookup, dayMeetings, starttijdConfigs));
        }

        if (pages.Any(p => p.Dag == Dag.None && !p.IsSpelerGedreven))
        {
            var dagPages = pages.Where(p => p.Dag == Dag.None && !p.IsSpelerGedreven)
                .OrderBy(p => StarttijdSortKey(p.StarttijdId, starttijdConfigs))
                .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
                .ThenBy(p => p.Order)
                .ToList();
            tijdschemasLijst.Add(BuildTijdschemasData(InfraTekst.ExportNogNietIngepland, dagPages, plotlijnNummers, userLookup, [], starttijdConfigs));
        }

        return tijdschemasLijst;
    }

    private static async Task<ImmutableList<PageData>> BuildEvergreenPagesAsync(
        IDocumentSession session,
        Dictionary<string, string> userLookup,
        IReadOnlyDictionary<string, SoortEncounterConfig> encounterConfigs,
        IReadOnlyDictionary<string, AantalBlokkenConfig> aantalBlokkenConfigs,
        IReadOnlyDictionary<string, QuestLocatieConfig> questLocatieConfigs,
        CancellationToken cancellationToken)
    {
        var evergreenPlotboekId = PlotboekId.CreateEvergreen();
        var evergreenPages = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotboekIdQueryable == evergreenPlotboekId.ToString() && !p.IsVerwijderd, cancellationToken);

        var evergreenUserIds = evergreenPages.SelectMany(p => new[] { p.VerantwoordelijkeSl }.Concat(p.ExtraSlIds ?? [])).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var evergreenUsers = await session.QueryAsync<PlotboekUser>(u => evergreenUserIds.Contains(u.Id), cancellationToken);
        foreach (var u in evergreenUsers.Where(u => !userLookup.ContainsKey(u.Id)))
        {
            userLookup[u.Id] = u.UserName ?? u.Email ?? u.Id;
        }

        return [.. evergreenPages
            .Where(p => p.IsVolledigKlaar && !p.IsGedraaid)
            .OrderBy(p => p.Order)
            .Select(p => BuildEvergreenPageData(p, userLookup, encounterConfigs, aantalBlokkenConfigs, questLocatieConfigs))];
    }

    private static Dictionary<PlotboekPaginaId, string> ComputePlotlijnNummersForExport(IReadOnlyList<PlotboekPagina.ExportData> pages, IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs)
    {
        var result = new Dictionary<PlotboekPaginaId, string>();
        var pagesByDomain = pages.GroupBy(p => p.DomainId).ToList();

        foreach (var domainGroup in pagesByDomain)
        {
            var domainPages = domainGroup.ToList();
            var (plotlijnNumbers, individualNumbers) = BuildSlotNumbers(domainPages, p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId, id => StarttijdSortKey(id, starttijdConfigs));

            foreach (var page in domainPages)
            {
                var nummer = GetDisplayNumber(page.PlotlijnId, page.PaginaId, plotlijnNumbers, individualNumbers, domainPages, p => p.PlotlijnId, p => p.Dag, p => p.StarttijdId, p => p.Order, p => p.PaginaId, id => StarttijdSortKey(id, starttijdConfigs));
                if (nummer != null)
                {
                    result[page.PaginaId] = nummer;
                }
            }
        }
        return result;
    }

    public static (IReadOnlyDictionary<PlotlijnId, int> PlotlijnNumbers, IReadOnlyDictionary<PlotboekPaginaId, int> IndividualNumbers) BuildSlotNumbers<T>(
        IReadOnlyList<T> domainItems,
        Func<T, PlotlijnId> getPlotlijnId,
        Func<T, Dag> getDag,
        Func<T, string> getStarttijdId,
        Func<T, int> getOrder,
        Func<T, PlotboekPaginaId> getPaginaId)
        => BuildSlotNumbers(domainItems, getPlotlijnId, getDag, getStarttijdId, getOrder, getPaginaId, DefaultStarttijdSortKey);

    private static (IReadOnlyDictionary<PlotlijnId, int> PlotlijnNumbers, IReadOnlyDictionary<PlotboekPaginaId, int> IndividualNumbers) BuildSlotNumbers<T>(
        IReadOnlyList<T> domainItems,
        Func<T, PlotlijnId> getPlotlijnId,
        Func<T, Dag> getDag,
        Func<T, string> getStarttijdId,
        Func<T, int> getOrder,
        Func<T, PlotboekPaginaId> getPaginaId,
        Func<string, int> getStarttijdSortKey)
    {
        var entries = new List<(Dag Dag, string StarttijdId, int Order, bool IsPlotlijn, PlotlijnId PlotlijnId, PlotboekPaginaId PaginaId)>();

        var plotlijnGroups = domainItems.Where(p => getPlotlijnId(p).HasValue).GroupBy(getPlotlijnId);
        foreach (var group in plotlijnGroups)
        {
            var earliest = group
                .OrderBy(p => DagSortKey(getDag(p)))
                .ThenBy(p => getStarttijdSortKey(getStarttijdId(p)))
                .ThenBy(p => getStarttijdId(p), StringComparer.Ordinal)
                .ThenBy(getOrder)
                .First();
            entries.Add((getDag(earliest), getStarttijdId(earliest), getOrder(earliest), true, group.Key, default));
        }

        foreach (var item in domainItems.Where(p => !getPlotlijnId(p).HasValue))
        {
            entries.Add((getDag(item), getStarttijdId(item), getOrder(item), false, default, getPaginaId(item)));
        }

        var plotlijnNumbers = new Dictionary<PlotlijnId, int>();
        var individualNumbers = new Dictionary<PlotboekPaginaId, int>();
        var counter = 1;
        foreach (var entry in entries.OrderBy(e => DagSortKey(e.Dag)).ThenBy(e => getStarttijdSortKey(e.StarttijdId)).ThenBy(e => e.StarttijdId, StringComparer.Ordinal).ThenBy(e => e.Order))
        {
            if (entry.IsPlotlijn)
            {
                plotlijnNumbers[entry.PlotlijnId] = counter++;
            }
            else
            {
                individualNumbers[entry.PaginaId] = counter++;
            }
        }
        return (plotlijnNumbers, individualNumbers);
    }

    public static string GetDisplayNumber<T>(
        PlotlijnId plotlijnId,
        PlotboekPaginaId paginaId,
        IReadOnlyDictionary<PlotlijnId, int> plotlijnNumbers,
        IReadOnlyDictionary<PlotboekPaginaId, int> individualNumbers,
        IReadOnlyList<T> domainItems,
        Func<T, PlotlijnId> getPlotlijnId,
        Func<T, Dag> getDag,
        Func<T, string> getStarttijdId,
        Func<T, int> getOrder,
        Func<T, PlotboekPaginaId> getPaginaId)
        => GetDisplayNumber(plotlijnId, paginaId, plotlijnNumbers, individualNumbers, domainItems, getPlotlijnId, getDag, getStarttijdId, getOrder, getPaginaId, DefaultStarttijdSortKey);

    private static string GetDisplayNumber<T>(
        PlotlijnId plotlijnId,
        PlotboekPaginaId paginaId,
        IReadOnlyDictionary<PlotlijnId, int> plotlijnNumbers,
        IReadOnlyDictionary<PlotboekPaginaId, int> individualNumbers,
        IReadOnlyList<T> domainItems,
        Func<T, PlotlijnId> getPlotlijnId,
        Func<T, Dag> getDag,
        Func<T, string> getStarttijdId,
        Func<T, int> getOrder,
        Func<T, PlotboekPaginaId> getPaginaId,
        Func<string, int> getStarttijdSortKey)
    {
        if (plotlijnId.HasValue && plotlijnNumbers.TryGetValue(plotlijnId, out var plotlijnNum))
        {
            var plotlijnMembers = domainItems
                .Where(p => Nullable.Equals(getPlotlijnId(p), plotlijnId))
                .OrderBy(p => DagSortKey(getDag(p)))
                .ThenBy(p => getStarttijdSortKey(getStarttijdId(p)))
                .ThenBy(p => getStarttijdId(p), StringComparer.Ordinal)
                .ThenBy(getOrder)
                .ToList();
            var subNum = plotlijnMembers.FindIndex(p => getPaginaId(p) == paginaId) + 1;
            return $"{plotlijnNum}.{subNum}";
        }
        if (individualNumbers.TryGetValue(paginaId, out var individualNum))
        {
            return individualNum.ToString();
        }
        return null;
    }

    private static PageData BuildPageData(
        PlotboekPagina.ExportData p,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers,
        IReadOnlyDictionary<string, string> userLookup,
        IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs,
        IReadOnlyDictionary<string, AantalBlokkenConfig> aantalBlokkenConfigs,
        IReadOnlyDictionary<string, SoortEncounterConfig> encounterConfigs,
        IReadOnlyDictionary<BijlageId, BijlageData> bijlageLookup,
        IReadOnlyDictionary<string, QuestLocatieConfig> questLocatieConfigs)
    {
        var iconPrefix = BuildIconenPrefix(p);
        var name = OrPlaceholder(GetDisplayTitleWithPlotlijn(p, plotlijnNummers));
        var hasMeerInfo = !string.IsNullOrWhiteSpace(p.MeerInformatie);
        var dagdeel = BuildDagdeel(p, starttijdConfigs);
        return new PageData
        {
            NaamEncounter = string.IsNullOrEmpty(iconPrefix) ? name : $"{iconPrefix} {name}",
            IconenPrefix = iconPrefix,
            BookmarkName = SanitizePageBookmarkName(p.PaginaId),
            VerantwoordelijkeSl = BuildSlDisplayName(p, userLookup),
            Dagdeel = dagdeel,
            GeschatteTijdsduur = ResolveAantalBlokkenDisplayName(p.AantalBlokkenId, aantalBlokkenConfigs),
            SoortEncounter = OrPlaceholder(ResolveEncounterTypesDisplayName(p.EncounterTypeIds, encounterConfigs)),
            LocatieEncounter = OrPlaceholder(ResolveLocatieEncounterDisplayName(p.LocatieEncounter, questLocatieConfigs)),
            AantalNpcs = OrPlaceholder(p.AantalNpcs.ToString()),
            KledingEnSchmink = OrPlaceholder(p.AantalNpcs.ToKledingEnSchminkString()),
            ElevatorPitch = OrPlaceholder(p.ElevatorPitch),
            SamenvattingPlot = OrPlaceholder(p.SamenvattingPlot),
            BenodigdeProps = p.PropsEnLoot.HasValue ? p.PropsEnLoot.ToPropsString() : string.Empty,
            GewensteLoot = p.PropsEnLoot.HasValue ? p.PropsEnLoot.ToLootString() : string.Empty,
            InstructiePentagon = BuildInstructiePentagon(p),
            MeerInformatie = hasMeerInfo ? InfraTekst.ExportZieExtraInformatie : string.Empty,
            MeerInformatieBookmark = hasMeerInfo ? SanitizeBookmarkName(p.PaginaId) : string.Empty,
            BijlageVerwijzingen = BuildBijlageVerwijzingen(p, bijlageLookup),
            Opmerkingen = p.Opmerkingen ?? string.Empty,
            Debrief = p.Debrief.ToDebriefString(),
            IsVolledigKlaar = p.IsVolledigKlaar,
        };
    }

    public static string BuildDagdeel(PlotboekPagina.ExportData p)
        => BuildDagdeel(p, DefaultStarttijdConfigs);

    private static string BuildDagdeel(PlotboekPagina.ExportData p, IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs)
    {
        if (p.IsSpelerGedreven)
        {
            return InfraTekst.ExportSpelerGedreven;
        }

        if (p.StarttijdId is null or "None")
        {
            return PageData.NogNietIngevuld;
        }

        var starttijd = GetStarttijdDisplayName(p.StarttijdId, starttijdConfigs);
        return p.IsFlexibel ? string.Format(CultureInfo.CurrentCulture, InfraTekst.ExportFlexibel, starttijd) : starttijd;
    }

    public static PageData BuildEvergreenPageData(PlotboekPagina.ExportData p, IReadOnlyDictionary<string, string> userLookup)
        => BuildEvergreenPageData(p, userLookup, DefaultEncounterConfigs, DefaultAantalBlokkenConfigs, EmptyQuestLocatieConfigs);

    private static PageData BuildEvergreenPageData(
        PlotboekPagina.ExportData p,
        IReadOnlyDictionary<string, string> userLookup,
        IReadOnlyDictionary<string, SoortEncounterConfig> encounterConfigs,
        IReadOnlyDictionary<string, AantalBlokkenConfig> aantalBlokkenConfigs,
        IReadOnlyDictionary<string, QuestLocatieConfig> questLocatieConfigs)
    {
        var iconPrefix = BuildIconenPrefix(p);
        var name = OrPlaceholder(p.DisplayTitle);
        return new PageData
        {
            NaamEncounter = string.IsNullOrEmpty(iconPrefix) ? name : $"{iconPrefix} {name}",
            IconenPrefix = iconPrefix,
            BookmarkName = string.Empty,
            VerantwoordelijkeSl = BuildSlDisplayName(p, userLookup),
            Dagdeel = "Evergreen",
            GeschatteTijdsduur = ResolveAantalBlokkenDisplayName(p.AantalBlokkenId, aantalBlokkenConfigs),
            SoortEncounter = OrPlaceholder(ResolveEncounterTypesDisplayName(p.EncounterTypeIds, encounterConfigs)),
            LocatieEncounter = OrPlaceholder(ResolveLocatieEncounterDisplayName(p.LocatieEncounter, questLocatieConfigs)),
            AantalNpcs = OrPlaceholder(p.AantalNpcs.ToString()),
            KledingEnSchmink = OrPlaceholder(p.AantalNpcs.ToKledingEnSchminkString()),
            ElevatorPitch = OrPlaceholder(p.ElevatorPitch),
            SamenvattingPlot = OrPlaceholder(p.SamenvattingPlot),
            BenodigdeProps = p.PropsEnLoot.HasValue ? p.PropsEnLoot.ToPropsString() : string.Empty,
            GewensteLoot = p.PropsEnLoot.HasValue ? p.PropsEnLoot.ToLootString() : string.Empty,
            InstructiePentagon = BuildInstructiePentagon(p),
            MeerInformatie = p.MeerInformatie ?? string.Empty,
            Opmerkingen = p.Opmerkingen ?? string.Empty,
            Debrief = p.Debrief.ToDebriefString(),
            IsVolledigKlaar = p.IsVolledigKlaar,
        };
    }

    public static TijdschemasData BuildTijdschemasData(
        string dayLabel,
        IReadOnlyList<PlotboekPagina.ExportData> dagPages,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers,
        IReadOnlyDictionary<string, string> userLookup,
        IReadOnlyList<Meeting> dayMeetings)
        => BuildTijdschemasData(dayLabel, dagPages, plotlijnNummers, userLookup, dayMeetings, DefaultStarttijdConfigs);

    private static TijdschemasData BuildTijdschemasData(
        string dayLabel,
        IReadOnlyList<PlotboekPagina.ExportData> dagPages,
        IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers,
        IReadOnlyDictionary<string, string> userLookup,
        IReadOnlyList<Meeting> dayMeetings,
        IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs)
    {
        var entries = new List<TijdschemaEntry>();

        foreach (var meeting in dayMeetings)
        {
            var sortMinutes = ParseTimeToMinutes(meeting.Tijd);
            entries.Add(new TijdschemaEntry(sortMinutes, 0, meeting.Order, new TijdschemaData
            {
                Tijd = meeting.Tijd,
                Icons = " ",
                NaamEncounter = meeting.Naam,
                SL = " ",
                NPCs = " ",
                Pagina = " ",
                IsMeeting = true,
            }));
        }

        foreach (var p in dagPages)
        {
            var rawTime = p.StarttijdId is not null and not "None" ? GetStarttijdDisplayName(p.StarttijdId, starttijdConfigs) : " ";
            var sortMinutes = ParseTimeToMinutes(rawTime);
            var timeMatch = TimeRegex().Match(rawTime);
            var timeDisplay = timeMatch.Success ? timeMatch.Value : rawTime;
            var iconPrefix = BuildIconenPrefix(p);
            var name = GetDisplayTitleWithPlotlijn(p, plotlijnNummers);

            entries.Add(new TijdschemaEntry(sortMinutes, 1, p.Order, new TijdschemaData
            {
                Tijd = timeDisplay,
                Icons = string.IsNullOrEmpty(iconPrefix) ? " " : iconPrefix,
                NaamEncounter = string.IsNullOrWhiteSpace(name) ? PageData.NogNietIngevuld : name,
                SL = BuildSlDisplayName(p, userLookup),
                NPCs = BuildNpcsSummary(p.AantalNpcs),
                Pagina = string.Empty,
                PaginaBookmark = SanitizePageBookmarkName(p.PaginaId),
            }));
        }

        var tijdschemaItems = new List<TijdschemaData>();
        int? lastPageSortMinutes = null;
        var pageBreakPending = false;

        foreach (var (sortMinutes, _, _, data) in entries.OrderBy(e => e.SortMinutes).ThenBy(e => e.TypeOrder).ThenBy(e => e.SubOrder))
        {
            var item = pageBreakPending ? data with { PageBreakBefore = true } : data;
            pageBreakPending = false;

            if (item.IsMeeting)
            {
                lastPageSortMinutes = null;
                tijdschemaItems.Add(item);

                // Everything after "Avondeten" moves to the next page so the daytime
                // and evening plots each get a full page without overflow.
                if (IsAvondetenMeeting(item.NaamEncounter))
                {
                    pageBreakPending = true;
                }
            }
            else
            {
                var isFirstInBlock = sortMinutes != lastPageSortMinutes;
                lastPageSortMinutes = sortMinutes;
                tijdschemaItems.Add(item with
                {
                    Tijd = isFirstInBlock ? item.Tijd : " ",
                    HideTopBorder = !isFirstInBlock,
                });
            }
        }

        return new TijdschemasData
        {
            Day = dayLabel,
            Tijdschema = [.. tijdschemaItems],
        };
    }

    public static ExtraInfoData BuildExtraInfoData(PlotboekPagina.ExportData p, IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers)
    {
        var iconPrefix = BuildIconenPrefix(p);
        var name = OrPlaceholder(GetDisplayTitleWithPlotlijn(p, plotlijnNummers));
        return new ExtraInfoData
        {
            NaamEncounter = string.IsNullOrEmpty(iconPrefix) ? name : $"{iconPrefix} {name}",
            MeerInformatie = p.MeerInformatie ?? string.Empty,
            BookmarkName = SanitizeBookmarkName(p.PaginaId),
        };
    }

    private static readonly IReadOnlyDictionary<BijlageId, BijlageData> EmptyBijlageLookup =
        new Dictionary<BijlageId, BijlageData>();

    /// <summary>
    /// Builds an id -> definition lookup of a plotboek's bijlagen, tolerating (unexpected) duplicate ids.
    /// </summary>
    public static IReadOnlyDictionary<BijlageId, BijlageData> BuildBijlageLookup(IReadOnlyList<BijlageData> bijlagen)
        => (bijlagen ?? [])
            .Where(b => b.Id.HasValue)
            .GroupBy(b => b.Id)
            .ToDictionary(g => g.Key, g => g.First());

    /// <summary>
    /// Builds the "Bijlagen" appendix list. Every authored bijlage (one with a title) is emitted once,
    /// whether or not any encounter references it, ordered by Order then Titel.
    /// </summary>
    public static List<BijlageAppendixData> BuildBijlagenList(IReadOnlyList<BijlageData> bijlagen)
        => [.. (bijlagen ?? [])
            .Where(b => b.HasValue)
            .OrderBy(b => b.Order)
            .ThenBy(b => b.Titel, StringComparer.Ordinal)
            .Select(b => new BijlageAppendixData
            {
                Nummer = (b.Order + 1).ToString(),
                Titel = b.Titel,
                // A title-only bijlage has empty content; emit an empty paragraph rather than an empty
                // string, otherwise the body-level Inhoud SDT hits the engine's empty-value branch (which
                // only removes table rows) and leaks its placeholder text into the exported appendix.
                Inhoud = string.IsNullOrWhiteSpace(b.Inhoud) ? "<p></p>" : b.Inhoud,
                BookmarkName = SanitizeBijlageBookmarkName(b.Id),
            })];

    /// <summary>
    /// Builds the per-encounter cross-references to bijlagen, resolving each referenced id against the
    /// plotboek's bijlagen (dropping ids that no longer exist) and ordering by the bijlage's Order.
    /// </summary>
    public static ImmutableList<BijlageVerwijzingData> BuildBijlageVerwijzingen(
        PlotboekPagina.ExportData p,
        IReadOnlyDictionary<BijlageId, BijlageData> bijlageLookup)
        => [.. (p.BijlageIds ?? [])
            .Distinct()
            .Where(bijlageLookup.ContainsKey)
            .Select(id => bijlageLookup[id])
            .OrderBy(b => b.Order)
            .ThenBy(b => b.Titel, StringComparer.Ordinal)
            .Select(b => new BijlageVerwijzingData
            {
                Naam = b.Titel,
                Nummer = (b.Order + 1).ToString(),
                Pagina = string.Empty,
                PaginaBookmark = SanitizeBijlageBookmarkName(b.Id),
            })];

    public static string BuildNpcsSummary(AantalNpcsData npcData)
    {
        if (!npcData.HasValue)
        {
            return " ";
        }

        if (npcData.AlleNpcs)
        {
            return CoreTekst.NpcsAlleKort;
        }
        var min = npcData.TotaalMinimum;
        var max = npcData.TotaalMaximum ?? min;
        return min == max ? min.ToString() : $"{min} - {max}";
    }

    public static int ParseTimeToMinutes(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return int.MaxValue;
        }

        var match = TimeRegex().Match(time);
        if (!match.Success)
        {
            return int.MaxValue;
        }

        var hours = int.Parse(match.Groups[1].Value);
        var minutes = int.Parse(match.Groups[2].Value);
        var totalMinutes = (hours * 60) + minutes;
        if (hours < 6)
        {
            totalMinutes += 24 * 60;
        }

        return totalMinutes;
    }

    [GeneratedRegex(@"(\d{1,2}):(\d{2})")]
    private static partial Regex TimeRegex();

    /// <summary>
    /// Name of the dinner meeting that marks the split between the daytime and
    /// evening programme. Rows after it are forced onto a new page.
    /// </summary>
    public const string AvondetenMeetingNaam = "Avondeten";

    /// <summary>
    /// The same meeting in the reference host's English example data. Meetings are data in the
    /// deployment's own language, so both names count.
    /// </summary>
    public const string DinnerMeetingNaam = "Dinner";

    private static bool IsAvondetenMeeting(string naam)
        => string.Equals(naam?.Trim(), AvondetenMeetingNaam, StringComparison.OrdinalIgnoreCase)
        || string.Equals(naam?.Trim(), DinnerMeetingNaam, StringComparison.OrdinalIgnoreCase);

    public static MeetingDagType GetMeetingDagType(Dag dag, IReadOnlyList<Dag> daysInRange)
    {
        if (daysInRange.Count > 0 && daysInRange[0] == dag)
        {
            return MeetingDagType.EersteDag;
        }

        if (daysInRange.Count > 0 && daysInRange[^1] == dag)
        {
            return MeetingDagType.LaatseDag;
        }

        return MeetingDagType.Tussendag;
    }

    public static string OrPlaceholder(string value) => string.IsNullOrWhiteSpace(value) ? PageData.NogNietIngevuld : value;

    public static string SanitizeBookmarkName(PlotboekPaginaId paginaId) => "_ei_" + paginaId.ToString().Replace("-", "_");

    public static string SanitizePageBookmarkName(PlotboekPaginaId paginaId) => "_pg_" + paginaId.ToString().Replace("-", "_");

    public static string SanitizeBijlageBookmarkName(BijlageId bijlageId) => "_bl_" + bijlageId.ToString().Replace("-", "_");

    public static string BuildIconenPrefix(PlotboekPagina.ExportData p)
    {
        var icons = new List<string>();
        if (p.EncounterTypeIds.Major?.Contains("Vechten", StringComparer.Ordinal) == true)
        {
            icons.Add("\u2694\uFE0F");
        }

        if (p.IsFlexibelNachtPlot) icons.Add("\U0001F319");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.EventBreed)) icons.Add("\U0001F46F");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.TimingCruciaal)) icons.Add("\u2B50");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.PlotCruciaal)) icons.Add("\u2757");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.MagAlsEersteCancelled)) icons.Add("\u2753");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.UitgebreideBriefing)) icons.Add("\U0001F4D6");

        // Derived from the NPC roles rather than from an icon flag: at least one role needs something
        // agreed with the figurant beforehand.
        if (p.AantalNpcs.HeeftAfstemmingNodig) icons.Add("\U0001F91D");
        if (p.LocatieEncounter.Locatie == LocatieEncounter.Questveld) icons.Add("\U0001FAA6");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.GrootPakOfKostuum)) icons.Add("\u2764\uFE0F");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.MuziekVanuitTechniek)) icons.Add("\U0001F3B6");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.LichtOfRookVanuitTechniek)) icons.Add("\U0001F4A1");

        return string.Join(string.Empty, icons);
    }

    public static string BuildInstructiePentagon(PlotboekPagina.ExportData p)
    {
        var userText = p.InstructiePentagon?.Trim() ?? string.Empty;
        var isHtml = !string.IsNullOrEmpty(userText)
            && userText.TrimStart().StartsWith('<')
            && (userText.Contains("<p>") || userText.Contains("<p ") || userText.Contains("<ul>") || userText.Contains("<ol>"));

        var remarks = new List<string>();
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.MuziekVanuitTechniek)) remarks.Add($"\u2705 {Terminologie.Huidig.MuziekVanuitTechniek}");
        if (p.Iconen.HasFlag(PlotboekPaginaIcons.LichtOfRookVanuitTechniek)) remarks.Add($"\u2705 {Terminologie.Huidig.LichtOfRookVanuitTechniek}");

        if (remarks.Count == 0)
        {
            return userText;
        }

        if (isHtml || !string.IsNullOrEmpty(userText))
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(userText))
            {
                sb.Append(userText);
            }

            foreach (var remark in remarks)
            {
                sb.Append("<p>").Append(remark).Append("</p>");
            }
            return sb.ToString();
        }

        return string.Join("\n", remarks);
    }

    public static string BuildSlDisplayName(PlotboekPagina.ExportData p, IReadOnlyDictionary<string, string> userLookup)
    {
        var names = new List<string>();
        if (!string.IsNullOrEmpty(p.VerantwoordelijkeSl) && userLookup.TryGetValue(p.VerantwoordelijkeSl, out var primaryName)) names.Add(primaryName);
        foreach (var extraId in p.ExtraSlIds ?? [])
        {
            if (!string.IsNullOrEmpty(extraId) && userLookup.TryGetValue(extraId, out var extraName)) names.Add(extraName);
        }
        return names.Count > 0 ? string.Join(", ", names) : PageData.NogNietIngevuld;
    }

    public static string GetDisplayTitleWithPlotlijn(PlotboekPagina.ExportData page, IReadOnlyDictionary<PlotboekPaginaId, string> plotlijnNummers)
    {
        var plotlijnNummer = plotlijnNummers.GetValueOrDefault(page.PaginaId);
        if (!string.IsNullOrEmpty(page.DomainShorthand))
        {
            return !string.IsNullOrEmpty(plotlijnNummer)
                ? $"{page.DomainShorthand} {plotlijnNummer} {page.Titel}"
                : $"{page.DomainShorthand} {page.Titel}";
        }
        return page.Titel;
    }

    private static int DagSortKey(Dag dag) => dag == Dag.None ? int.MaxValue : (int)dag;

    private static int StarttijdSortKey(string starttijdId, IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs)
    {
        if (string.IsNullOrEmpty(starttijdId) || string.Equals(starttijdId, "None", StringComparison.Ordinal))
        {
            return int.MaxValue;
        }

        return starttijdConfigs.TryGetValue(starttijdId, out var config) ? config.Order : int.MaxValue - 1;
    }

    private static string GetStarttijdDisplayName(string starttijdId, IReadOnlyDictionary<string, StarttijdConfig> starttijdConfigs)
    {
        if (string.IsNullOrEmpty(starttijdId) || string.Equals(starttijdId, "None", StringComparison.Ordinal))
        {
            return "-";
        }

        return starttijdConfigs.TryGetValue(starttijdId, out var config) ? config.DisplayName : starttijdId;
    }

    private static string ResolveAantalBlokkenDisplayName(string aantalBlokkenId, IReadOnlyDictionary<string, AantalBlokkenConfig> aantalBlokkenConfigs)
    {
        if (string.IsNullOrEmpty(aantalBlokkenId) || string.Equals(aantalBlokkenId, "None", StringComparison.Ordinal))
        {
            return PageData.NogNietIngevuld;
        }

        return aantalBlokkenConfigs.TryGetValue(aantalBlokkenId, out var config) ? config.DisplayName : aantalBlokkenId;
    }

    private static string ResolveEncounterTypesDisplayName(EncounterTypeIds encounterTypeIds, IReadOnlyDictionary<string, SoortEncounterConfig> encounterConfigs)
    {
        if (!encounterTypeIds.HasValue)
        {
            return string.Empty;
        }

        var major = encounterTypeIds.Major?.Select(id => ResolveEncounterDisplayName(id, encounterConfigs)).Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];
        if (major.Count == 0)
        {
            return string.Empty;
        }

        var display = string.Join(" / ", major);
        var minor = encounterTypeIds.Minor?.Select(id => ResolveEncounterDisplayName(id, encounterConfigs)).Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];
        if (minor.Count > 0)
        {
            display = string.Format(CultureInfo.CurrentCulture, CoreTekst.EncounterTypesMetMogelijk, display, string.Join(", ", minor));
        }

        return display;
    }

    private static string ResolveEncounterDisplayName(string encounterId, IReadOnlyDictionary<string, SoortEncounterConfig> encounterConfigs)
    {
        if (string.IsNullOrWhiteSpace(encounterId) || string.Equals(encounterId, "None", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return encounterConfigs.TryGetValue(encounterId, out var config) ? config.DisplayName : encounterId;
    }

    private static string BuildPlotboekNaam(Plotboek.ExportData plotboek, IReadOnlyDictionary<string, EventConfig> eventConfigs)
    {
        var eventLabel = !string.IsNullOrWhiteSpace(plotboek.EventId) && eventConfigs.TryGetValue(plotboek.EventId, out var config)
            ? config.DisplayName
            : plotboek.EventId;

        return $"{(!string.IsNullOrWhiteSpace(plotboek.Titel) ? $"{plotboek.Titel} (" : string.Empty)}{eventLabel} {plotboek.Jaar}{(!string.IsNullOrWhiteSpace(plotboek.Titel) ? ")" : string.Empty)}";
    }

    private static int DefaultStarttijdSortKey(string starttijdId) => StarttijdSortKey(starttijdId, DefaultStarttijdConfigs);

    // Mirrors the seed table in ReferenceDataSeedService.SeedStarttijdenAsync; only used by the overloads
    // that run without loaded reference data (tests), where the export needs a display name and a sort key.
    private static readonly IReadOnlyDictionary<string, StarttijdConfig> DefaultStarttijdConfigs = new Dictionary<string, StarttijdConfig>(StringComparer.Ordinal)
    {
        ["None"] = new StarttijdConfig { Id = "None", EnumValue = 0, DisplayName = "-", IsNone = true, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 0 },
        ["Tijd1030"] = new StarttijdConfig { Id = "Tijd1030", EnumValue = 1, DisplayName = "10:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 1 },
        ["Tijd1130"] = new StarttijdConfig { Id = "Tijd1130", EnumValue = 9, DisplayName = "11:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = true, Order = 2 },
        ["Tijd1230"] = new StarttijdConfig { Id = "Tijd1230", EnumValue = 2, DisplayName = "12:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 3 },
        ["Tijd1330"] = new StarttijdConfig { Id = "Tijd1330", EnumValue = 10, DisplayName = "13:30: Eindscène", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = true, Order = 4 },
        ["Tijd1430"] = new StarttijdConfig { Id = "Tijd1430", EnumValue = 3, DisplayName = "14:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 5 },
        ["Tijd1630"] = new StarttijdConfig { Id = "Tijd1630", EnumValue = 4, DisplayName = "16:30", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 6 },
        ["Tijd1930Introquest"] = new StarttijdConfig { Id = "Tijd1930Introquest", EnumValue = 5, DisplayName = "19:30: Introquest", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 7 },
        ["Tijd2000"] = new StarttijdConfig { Id = "Tijd2000", EnumValue = 6, DisplayName = "20:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 8 },
        ["Tijd2200"] = new StarttijdConfig { Id = "Tijd2200", EnumValue = 7, DisplayName = "22:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 9 },
        ["Tijd0000"] = new StarttijdConfig { Id = "Tijd0000", EnumValue = 8, DisplayName = "00:00", IsNone = false, IsAvailableOnEersteDag = true, IsAvailableOnMiddelDagen = true, IsAvailableOnLaatsteDag = false, Order = 10 },
    };

    private static readonly IReadOnlyDictionary<string, AantalBlokkenConfig> DefaultAantalBlokkenConfigs = new Dictionary<string, AantalBlokkenConfig>(StringComparer.Ordinal)
    {
        ["None"] = new AantalBlokkenConfig { Id = "None", EnumValue = 0, DisplayName = "-", OverflowBlokken = 0, Order = 0, IsNone = true },
        ["EenBlok"] = new AantalBlokkenConfig { Id = "EenBlok", EnumValue = 1, DisplayName = "1 blok (max 1 uur)", OverflowBlokken = 0, Order = 1, IsNone = false },
        ["TweeBlokken"] = new AantalBlokkenConfig { Id = "TweeBlokken", EnumValue = 2, DisplayName = "2 blokken (+/- 2.5 uur)", OverflowBlokken = 1, Order = 2, IsNone = false },
        ["DrieBlokken"] = new AantalBlokkenConfig { Id = "DrieBlokken", EnumValue = 3, DisplayName = "3 blokken (+/- 4.5 uur)", OverflowBlokken = 2, Order = 3, IsNone = false },
    };

    private static readonly IReadOnlyDictionary<string, SoortEncounterConfig> DefaultEncounterConfigs = new Dictionary<string, SoortEncounterConfig>(StringComparer.Ordinal)
    {
        ["Vechten"] = new SoortEncounterConfig { Id = "Vechten", EnumValue = 1, DisplayName = "Vechten", IsNone = false, Order = 1 },
        ["Roleplay"] = new SoortEncounterConfig { Id = "Roleplay", EnumValue = 2, DisplayName = "Roleplay", IsNone = false, Order = 2 },
        ["Ritueel"] = new SoortEncounterConfig { Id = "Ritueel", EnumValue = 3, DisplayName = "Ritueel", IsNone = false, Order = 3 },
        ["Handel"] = new SoortEncounterConfig { Id = "Handel", EnumValue = 4, DisplayName = "Handel", IsNone = false, Order = 4 },
        ["Politiek"] = new SoortEncounterConfig { Id = "Politiek", EnumValue = 5, DisplayName = "Politiek", IsNone = false, Order = 5 },
        ["Magie"] = new SoortEncounterConfig { Id = "Magie", EnumValue = 6, DisplayName = "Magie", IsNone = false, Order = 6 },
        ["Praten"] = new SoortEncounterConfig { Id = "Praten", EnumValue = 7, DisplayName = "Praten", IsNone = false, Order = 7 },
        ["Puzzel"] = new SoortEncounterConfig { Id = "Puzzel", EnumValue = 8, DisplayName = "Puzzel", IsNone = false, Order = 8 },
    };

    /// <summary>
    /// Renders a location for the export, resolving a picked questlocatie to its current display name.
    /// <see cref="LocatieEncounterData"/> cannot do this itself: <c>core</c> does not know the reference
    /// data, so renaming a location has to flow through here.
    /// </summary>
    private static string ResolveLocatieEncounterDisplayName(
        LocatieEncounterData locatie,
        IReadOnlyDictionary<string, QuestLocatieConfig> questLocatieConfigs)
    {
        if (!locatie.HasQuestLocatieKeuze)
        {
            return locatie.ToString();
        }

        var naam = questLocatieConfigs.TryGetValue(locatie.LocatieId, out var config)
            ? config.DisplayName
            : locatie.LocatieId;

        return $"{locatie.Locatie.Naam()} ({naam})";
    }

    private static readonly IReadOnlyDictionary<string, QuestLocatieConfig> EmptyQuestLocatieConfigs
        = new Dictionary<string, QuestLocatieConfig>(StringComparer.Ordinal);

    private sealed record TijdschemaEntry(int SortMinutes, int TypeOrder, int SubOrder, TijdschemaData Data);
}
