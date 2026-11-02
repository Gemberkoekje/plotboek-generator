using PlotboekGenerator.Application.DTOs;
using PlotboekGenerator.Application.Queries.Plotboek;
using PlotboekGenerator.infrastructure.Generator;
using PlotboekGenerator.WebApp.Resources;
using System.Globalization;
using System.Text.Json;

namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel
{
    private async Task LoadPlotboeken()
    {
        IReadOnlyList<PlotboekDto> plotboeken;
        if (getPlotboekListQueryHandler != null)
        {
            var query = new GetPlotboekListQuery(1, 10_000);
            plotboeken = await getPlotboekListQueryHandler
                .HandleAsync(query, HttpContext.RequestAborted)
                .ConfigureAwait(false);
        }
        else
        {
            using var session = documentStore.OpenSession();
            var exportData = await session.QueryAllAsync<Plotboek.ExportData>(HttpContext.RequestAborted);
            plotboeken = [.. exportData
                .OrderBy(p => p.Jaar)
                .ThenBy(p => p.EventId)
                .Select(p => new PlotboekDto(
                    PlotboekId.Create(p.Jaar, ResolvePlotboekEventId(p.PlotboekId, p.Jaar, p.EventId)),
                    p.Jaar,
                    ResolvePlotboekEventId(p.PlotboekId, p.Jaar, p.EventId))
                {
                    EindDatum = p.EindDatum,
                    IsVolledigKlaar = p.IsVolledigKlaar,
                })];
        }

        var eventOrderById = AvailableEventConfigs
            .Where(c => !c.IsNone)
            .ToDictionary(c => c.Id, c => c.Order, StringComparer.Ordinal);

        AvailablePlotboeken = [];
        var existingPlotboeken = plotboeken
            .Select(p => p with { EventId = ResolvePlotboekEventId(p.Id, p.Jaar, p.EventId) })
            .Where(p => !string.Equals(p.EventId, "Evergreen", StringComparison.Ordinal))
            .Where(p => !IsNpc || p.IsVolledigKlaar)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .OrderBy(p => p.Jaar)
            .ThenBy(p => eventOrderById.TryGetValue(p.EventId, out var order) ? order : int.MaxValue)
            .ThenBy(p => p.EventId, StringComparer.Ordinal)
            .ToList();

        foreach (var plotboek in existingPlotboeken)
        {
            var eventCfg = AvailableEventConfigs.FirstOrDefault(c => string.Equals(c.Id, plotboek.EventId, StringComparison.Ordinal));
            AvailablePlotboeken.Add(new PlotboekInfo
            {
                Id = PlotboekId.Create(plotboek.Jaar, plotboek.EventId),
                Jaar = plotboek.Jaar,
                EventId = plotboek.EventId,
                EventAbbreviation = eventCfg?.Abbreviation ?? plotboek.EventId,
                EindDatum = plotboek.EindDatum == default ? null : plotboek.EindDatum,
                Exists = true,
                IsVolledigKlaar = plotboek.IsVolledigKlaar,
            });
        }

        // A reading role opens what exists; it gets no next plotboek to start.
        if (IsReadOnlyRole)
        {
            NoPlotboekenToRead = AvailablePlotboeken.Count == 0;
            return;
        }

        if (AvailablePlotboeken.Count > 0)
        {
            var lastPlotboek = AvailablePlotboeken.Last();
            var year = lastPlotboek.Jaar;
            var eventId = lastPlotboek.EventId;

            var currentConfig = AvailableEventConfigs.FirstOrDefault(c => string.Equals(c.Id, eventId, StringComparison.Ordinal));
            var newYear = year;
            var newEventId = eventId;

            if (currentConfig != null)
            {
                var configuredNextEventId = !string.IsNullOrWhiteSpace(currentConfig.NextEventId)
                    ? currentConfig.NextEventId
                    : AvailableEventConfigs.FirstOrDefault(c => c.EnumValue == currentConfig.NextEventEnumValue)?.Id;

                if (!string.IsNullOrWhiteSpace(configuredNextEventId))
                {
                    newEventId = configuredNextEventId;
                    if (currentConfig.NextEventIncrementsYear)
                    {
                        newYear = Year.Create((int)year + 1);
                    }
                }
            }

            var nextEventCfg = AvailableEventConfigs.FirstOrDefault(c => string.Equals(c.Id, newEventId, StringComparison.Ordinal));
            AvailablePlotboeken.Add(new PlotboekInfo
            {
                Id = PlotboekId.Create(newYear, newEventId),
                Jaar = newYear,
                EventId = newEventId,
                EventAbbreviation = nextEventCfg?.Abbreviation ?? newEventId,
            });
        }
        else
        {
            var firstEvent = AvailableEventConfigs
                .Where(c => !c.IsNone && !c.IsEvergreen)
                .OrderBy(c => c.Order)
                .FirstOrDefault();
            var defaultEventId = firstEvent?.Id ?? "Event";

            AvailablePlotboeken.Add(new PlotboekInfo
            {
                Id = PlotboekId.Create(Year.Create(2026), defaultEventId),
                Jaar = Year.Create(2026),
                EventId = defaultEventId,
                EventAbbreviation = firstEvent?.Abbreviation ?? defaultEventId,
            });
        }

        var evergreenCfg = AvailableEventConfigs.FirstOrDefault(c => c.IsEvergreen);
        AvailablePlotboeken.Add(new PlotboekInfo
        {
            Id = PlotboekId.CreateEvergreen(),
            Jaar = Year.Create(1),
            EventId = "Evergreen",
            EventAbbreviation = evergreenCfg?.Abbreviation ?? "EG",
        });
    }

    private string ResolvePlotboekEventId(PlotboekId plotboekId, Year jaar, string eventId)
    {
        if (!string.IsNullOrWhiteSpace(eventId) && !string.Equals(eventId, "None", StringComparison.Ordinal))
        {
            return eventId;
        }

        var inferred = AvailableEventConfigs
            .Where(c => !c.IsNone)
            .Select(c => c.Id)
            .FirstOrDefault(candidate => PlotboekId.Create(jaar, candidate) == plotboekId);

        return !string.IsNullOrWhiteSpace(inferred)
            ? inferred
            : eventId;
    }

    internal PlotboekInfo? DetermineDefaultPlotboek()
    {
        var candidates = AvailablePlotboeken
            .Where(p => !string.Equals(p.EventId, "Evergreen", StringComparison.Ordinal))
            .Where(p => !IsNpc || (p.Exists && p.IsVolledigKlaar))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var existing = candidates.Where(p => p.Exists).ToList();

        // No real plotboeken exist yet — auto-select the hardcoded/next entry
        if (existing.Count == 0)
        {
            return candidates[0];
        }

        var today = DateOnly.FromDateTime(Clock.UtcNow().Date);
        var withoutDate = existing.Where(p => p.EindDatum is null).ToList();
        if (withoutDate.Count > 1)
        {
            return null;
        }

        var active = existing
            .Where(p => p.EindDatum.HasValue && p.EindDatum.Value >= today)
            .OrderBy(p => p.EindDatum)
            .FirstOrDefault();

        if (active != null)
        {
            return active;
        }

        if (withoutDate.Count == 1)
        {
            return withoutDate[0];
        }

        return candidates.Last();
    }

    private bool CanAccessSelectedPlotboek()
    {
        if (!PlotboekId.HasValue)
        {
            return true;
        }

        var selectedPlotboek = AvailablePlotboeken.FirstOrDefault(p => p.Id == PlotboekId);
        if (selectedPlotboek == null)
        {
            PlotboekAccessMessage = IsNpc
                ? PlotboekUi.AccessGeenPlotboekVoorNpcs
                : PlotboekUi.AccessNietBeschikbaar;
            NoPlotboekenToRead = IsReadOnlyRole && AvailablePlotboeken.Count == 0;
            return false;
        }

        if (IsNpc && !selectedPlotboek.IsVolledigKlaar)
        {
            PlotboekAccessMessage = PlotboekUi.AccessNogNietKlaarVoorNpcs;
            return false;
        }

        return true;
    }

    private async Task LoadPlotboekPaginas()
    {
        if (!PlotboekId.HasValue)
        {
            PlotboekPaginas.Clear();
            return;
        }

        using var session = documentStore.OpenSession();

        // Load the plotboek to get StartDag and EindDag for the available days
        var plotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == PlotboekId.ToString());
        if (plotboek != null)
        {
            AvailableDays = GetDaysInRange(plotboek.StartDag, plotboek.EindDag);
        }

        var plotboekpaginas = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotboekIdQueryable == PlotboekId.ToString() && !p.IsVerwijderd);
        var paginaInfos = new List<PlotboekPaginaInfo>();

        foreach (var pagina in plotboekpaginas)
        {
            var starttijdId = ResolveStarttijdId(pagina.StarttijdId);
            var aantalBlokkenId = ResolveAantalBlokkenId(pagina.AantalBlokkenId);
            var encounterTypeIds = ResolveEncounterTypeIds(pagina.EncounterTypeIds);

            if (ShouldRefreshLegacyPaginaFields(starttijdId, aantalBlokkenId, encounterTypeIds))
            {
                var aggregateResult = await paginaRepository.LoadAsync(pagina.PaginaId, HttpContext.RequestAborted).ConfigureAwait(false);
                if (aggregateResult.IsValid)
                {
                    var aggregate = aggregateResult.Value;
                    starttijdId = ResolveStarttijdId(aggregate.StarttijdId);
                    aantalBlokkenId = ResolveAantalBlokkenId(aggregate.AantalBlokkenId);
                    encounterTypeIds = ResolveEncounterTypeIds(aggregate.EncounterTypeIds);
                }
            }

            paginaInfos.Add(BuildPaginaInfo(pagina, starttijdId, aantalBlokkenId, encounterTypeIds));
        }

        PlotboekPaginas = paginaInfos;

        // Compute plotlijn numbers for display
        ComputePlotlijnNummers();

        // Calculate NPC totals per block
        CalculateBlokNpcTotalen();

        // Work out which encounters compete for the same questlocatie
        BerekenLocatieConflicten();
    }

    /// <summary>
    /// Maps a pagina read model (<see cref="PlotboekPagina.ExportData"/>) to the UI info model. The
    /// starttijd/aantalBlokken/encounter-type ids are passed in already resolved, because the caller
    /// may have refreshed them from the aggregate (legacy data) or reconstructed them from history.
    /// Reused by <see cref="LoadPlotboekPaginas"/> and the restore-preview flow.
    /// </summary>
    private PlotboekPaginaInfo BuildPaginaInfo(PlotboekPagina.ExportData pagina, string starttijdId, string aantalBlokkenId, EncounterTypeIds encounterTypeIds)
    {
        return new PlotboekPaginaInfo()
        {
            Id = pagina.PaginaId,
            EventVersion = pagina.EventVersion,
            Title = pagina.Titel,
            DomainId = pagina.DomainId,
            DomainShorthand = pagina.DomainShorthand,
            DomainDescription = pagina.DomainDescription,
            PlotlijnId = pagina.PlotlijnId,
            Dag = pagina.Dag,
            StarttijdId = starttijdId,
            Order = pagina.Order,
            VerantwoordelijkeSlId = pagina.VerantwoordelijkeSl,
            VerantwoordelijkeSlName = AvailableSls.FirstOrDefault(sl => sl.Id == pagina.VerantwoordelijkeSl)?.DisplayName,
            ExtraSlIds = pagina.ExtraSlIds?.ToList(),
            ExtraSlNames = pagina.ExtraSlIds?.Select(id => AvailableSls.FirstOrDefault(sl => sl.Id == id)?.DisplayName ?? string.Empty).Where(n => n != null).ToList(),
            AantalBlokkenId = aantalBlokkenId,
            EncounterTypeIds = encounterTypeIds,
            LocatieEncounter = pagina.LocatieEncounter,
            AantalNpcs = pagina.AantalNpcs,
            ElevatorPitch = pagina.ElevatorPitch,
            SamenvattingPlot = pagina.SamenvattingPlot,
            MeerInformatie = pagina.MeerInformatie,
            BijlageRefs = pagina.BijlageIds,
            Feedback = pagina.Feedback,
            IsVolledigKlaar = pagina.IsVolledigKlaar,
            IsGedraaid = pagina.IsGedraaid,
            IsSpelerGedreven = pagina.IsSpelerGedreven,
            IsFlexibel = pagina.IsFlexibel,
            IsFlexibelNachtPlot = pagina.IsFlexibelNachtPlot,
            Iconen = pagina.Iconen,
            InstructiePentagon = pagina.InstructiePentagon,
            PropsEnLoot = pagina.PropsEnLoot,
            Opmerkingen = pagina.Opmerkingen,
            Debrief = pagina.Debrief,
        };
    }

    private static bool ShouldRefreshLegacyPaginaFields(string starttijdId, string aantalBlokkenId, EncounterTypeIds encounterTypeIds)
    {
        return string.Equals(starttijdId, "None", StringComparison.Ordinal)
            || string.Equals(aantalBlokkenId, "None", StringComparison.Ordinal)
            || !encounterTypeIds.HasValue;
    }

    private string ResolveStarttijdId(string starttijdId)
    {
        return ResolveReferenceDataId(starttijdId, AvailableStarttijdConfigs.Select(cfg => (cfg.Id, cfg.EnumValue)), "None");
    }

    private string ResolveAantalBlokkenId(string aantalBlokkenId)
    {
        return ResolveReferenceDataId(aantalBlokkenId, AvailableAantalBlokkenConfigs.Select(cfg => (cfg.Id, cfg.EnumValue)), "None");
    }

    private EncounterTypeIds ResolveEncounterTypeIds(EncounterTypeIds encounterTypeIds)
    {
        if (!encounterTypeIds.HasValue)
        {
            return EncounterTypeIds.Empty;
        }

        return EncounterTypeIds.Create(
            ResolveEncounterTypeList(encounterTypeIds.Major),
            ResolveEncounterTypeList(encounterTypeIds.Minor));
    }

    private IReadOnlyList<string> ResolveEncounterTypeList(IReadOnlyList<string> encounterTypeIds)
    {
        if (encounterTypeIds == null || encounterTypeIds.Count == 0)
        {
            return [];
        }

        return [.. encounterTypeIds
            .Select(id => ResolveReferenceDataId(id, AvailableSoortEncounterConfigs.Select(cfg => (cfg.Id, cfg.EnumValue)), "None"))
            .Where(id => !string.IsNullOrWhiteSpace(id) && !string.Equals(id, "None", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)];
    }

    private static string ResolveReferenceDataId(string value, IEnumerable<(string Id, int EnumValue)> configs, string fallbackId)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallbackId;
        }

        var configList = configs.ToList();
        var directMatch = configList.FirstOrDefault(cfg => string.Equals(cfg.Id, value, StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(directMatch.Id))
        {
            return directMatch.Id;
        }

        if (int.TryParse(value, out var enumValue))
        {
            var enumMatch = configList.FirstOrDefault(cfg => cfg.EnumValue == enumValue);
            if (!string.IsNullOrWhiteSpace(enumMatch.Id))
            {
                return enumMatch.Id;
            }
        }

        return fallbackId;
    }

    /// <summary>
    /// Starttijd id → sort order, falling back to the seeded defaults when the reference data has not been
    /// loaded (unit tests, and the brief window before the first seed). The fallback holds every slot of
    /// every day type at once, so overflow computed on it can run into a slot the day does not have; the
    /// <see cref="BuildStarttijdOrderById(MeetingDagType)"/> overload is the one to use per day.
    /// </summary>
    internal Dictionary<string, int> BuildStarttijdOrderById()
        => AvailableStarttijdConfigs.Count > 0
            ? AvailableStarttijdConfigs
                .Where(cfg => !cfg.IsNone)
                .OrderBy(cfg => cfg.Order)
                .ToDictionary(cfg => cfg.Id, cfg => cfg.Order)
            : new Dictionary<string, int>
            {
                ["Tijd1030"] = 1,
                ["Tijd1130"] = 2,
                ["Tijd1230"] = 3,
                ["Tijd1330"] = 4,
                ["Tijd1430"] = 5,
                ["Tijd1630"] = 6,
                ["Tijd1930Introquest"] = 7,
                ["Tijd2000"] = 8,
                ["Tijd2200"] = 9,
                ["Tijd0000"] = 10,
            };

    /// <summary>
    /// The same map restricted to the slots that are actually selectable on <paramref name="dagType"/>.
    /// Overflow from a multi-block encounter has to walk the slots of its own day: with the global map a
    /// two-block encounter on a middle-day 10:30 would run over into a last-day-only 11:30 — a block that
    /// does not exist on that day. Falls back to the global map when the reference data is not loaded.
    /// </summary>
    internal Dictionary<string, int> BuildStarttijdOrderById(MeetingDagType dagType)
        => AvailableStarttijdConfigs.Count > 0
            ? AvailableStarttijdConfigs
                .GetAvailable(dagType)
                .ToDictionary(cfg => cfg.Id, cfg => cfg.Order)
            : BuildStarttijdOrderById();

    /// <summary>
    /// The slot order map to use for encounters on <paramref name="dag"/>, memoised in
    /// <paramref name="cache"/> because a plotboek has only a handful of days but many pages. Without a day
    /// range at all (unit tests) this is the global map, the behaviour from before slots became day-type
    /// specific. A page left on a day the plotboek no longer covers gets an empty map, so its encounter
    /// spans its own slot and nothing else.
    /// </summary>
    private Dictionary<string, int> BuildStarttijdOrderByIdVoorDag(Dag dag, Dictionary<Dag, Dictionary<string, int>> cache)
    {
        if (!cache.TryGetValue(dag, out var orderById))
        {
            var days = AvailableDays.Where(d => d != Dag.None).ToList();

            if (days.Count == 0)
            {
                orderById = BuildStarttijdOrderById();
            }
            else if (days.Contains(dag))
            {
                orderById = BuildStarttijdOrderById(PlotboekExportBuilder.GetMeetingDagType(dag, days));
            }
            else
            {
                // Shortening the plotboek's day range leaves pages behind on a day that no longer has a day
                // type. The global map holds every day type's slots at once, so handing it to such a page
                // would invent overflow into blocks its day never ran.
                orderById = new Dictionary<string, int>(StringComparer.Ordinal);
            }

            cache[dag] = orderById;
        }

        return orderById;
    }

    /// <summary>
    /// AantalBlokken id → the number of FOLLOWING slots the encounter also occupies, with the same
    /// fallback to the seeded defaults as <see cref="BuildStarttijdOrderById()"/>.
    /// </summary>
    internal Dictionary<string, int> BuildAantalBlokkenOverflowById()
        => AvailableAantalBlokkenConfigs.Count > 0
            ? AvailableAantalBlokkenConfigs
                .Where(cfg => !cfg.IsNone)
                .ToDictionary(cfg => cfg.Id, cfg => cfg.OverflowBlokken)
            : new Dictionary<string, int>
            {
                ["EenBlok"] = 0,
                ["TweeBlokken"] = 1,
                ["DrieBlokken"] = 2,
            };

    /// <summary>
    /// The time slots an encounter occupies: its own starttijd plus the
    /// <see cref="AantalBlokkenConfig.OverflowBlokken"/> slots that follow it by <c>Order</c>. The span
    /// simply ends early when the day runs out of slots.
    /// Shared by <see cref="CalculateBlokNpcTotalen"/> (NPC totals) and
    /// <see cref="BerekenLocatieConflicten"/> (double-booked questlocaties) so both agree on what
    /// "at the same time" means.
    /// </summary>
    internal static IReadOnlyList<string> BerekenSlotSpan(
        string starttijdId,
        string aantalBlokkenId,
        IReadOnlyDictionary<string, int> starttijdOrderById,
        IReadOnlyDictionary<string, int> overflowById)
    {
        if (string.IsNullOrWhiteSpace(starttijdId) || string.Equals(starttijdId, "None", StringComparison.Ordinal))
        {
            return [];
        }

        if (!starttijdOrderById.TryGetValue(starttijdId, out var currentOrder))
        {
            return [starttijdId];
        }

        if (aantalBlokkenId is null
            || !overflowById.TryGetValue(aantalBlokkenId, out var overflowBlokken)
            || overflowBlokken <= 0)
        {
            return [starttijdId];
        }

        return
        [
            starttijdId,
            .. starttijdOrderById
                .Where(kvp => kvp.Value > currentOrder)
                .OrderBy(kvp => kvp.Value)
                .Take(overflowBlokken)
                .Select(kvp => kvp.Key),
        ];
    }

    internal void CalculateBlokNpcTotalen()
    {
        BlokNpcTotalen.Clear();

        var starttijdOrderByDag = new Dictionary<Dag, Dictionary<string, int>>();
        var aantalBlokkenOverflowById = BuildAantalBlokkenOverflowById();

        // Group pages by day and starttijd (exclude speler gedreven from NPC totals)
        var paginasByDayAndStarttijd = PlotboekPaginas
            .Where(p => p.Dag != Dag.None && p.StarttijdId != "None" && !p.IsSpelerGedreven)
            .GroupBy(p => (p.Dag, p.StarttijdId))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Track overflow from multi-block encounters
        var overflowByDayAndStarttijd = new Dictionary<(Dag, string), List<OverflowNpcs>>();

        // First pass: calculate direct NPCs and record overflows
        foreach (var ((dag, starttijdId), paginas) in paginasByDayAndStarttijd)
        {
            var heeftAlleNpcs = paginas.Any(p => p.AantalNpcs.AlleNpcs);
            var directMinimum = paginas.Sum(p => p.AantalNpcs.TotaalMinimum);
            int? directMaximum = paginas.All(p => !p.AantalNpcs.AlleNpcs)
                ? paginas.Sum(p => p.AantalNpcs.TotaalMaximum ?? p.AantalNpcs.TotaalMinimum)
                : null;

            // Record overflows for multi-block encounters. Skip(1) drops the encounter's own slot: that
            // one is already accounted for by the direct minimum/maximum above.
            foreach (var pagina in paginas)
            {
                var overflowIds = BerekenSlotSpan(
                        starttijdId,
                        pagina.AantalBlokkenId,
                        BuildStarttijdOrderByIdVoorDag(dag, starttijdOrderByDag),
                        aantalBlokkenOverflowById)
                    .Skip(1)
                    .ToList();

                foreach (var overflowId in overflowIds)
                {
                    var key = (dag, overflowId);
                    if (!overflowByDayAndStarttijd.ContainsKey(key))
                    {
                        overflowByDayAndStarttijd[key] = [];
                    }

                    overflowByDayAndStarttijd[key].Add(new OverflowNpcs
                    {
                        VanStarttijdId = starttijdId,
                        Minimum = pagina.AantalNpcs.TotaalMinimum,
                        Maximum = pagina.AantalNpcs.AlleNpcs ? null : pagina.AantalNpcs.TotaalMaximum ?? pagina.AantalNpcs.TotaalMinimum,
                        IsAlleNpcs = pagina.AantalNpcs.AlleNpcs,
                    });
                }
            }

            BlokNpcTotalen[(dag, starttijdId)] = new BlokNpcTotaal
            {
                Dag = dag,
                StarttijdId = starttijdId,
                HeeftAlleNpcs = heeftAlleNpcs,
                DirectMinimum = directMinimum,
                DirectMaximum = directMaximum,
                OverflowVanVorigeBlokken = [],
            };
        }

        // Second pass: add overflow information to blocks
        foreach (var ((dag, starttijdId), overflows) in overflowByDayAndStarttijd)
        {
            if (BlokNpcTotalen.TryGetValue((dag, starttijdId), out var existing))
            {
                // Update existing block with overflow
                BlokNpcTotalen[(dag, starttijdId)] = existing with
                {
                    HeeftAlleNpcs = existing.HeeftAlleNpcs || overflows.Any(o => o.IsAlleNpcs),
                    OverflowVanVorigeBlokken = overflows,
                };
            }
            else
            {
                // Create a new block entry for overflow-only blocks
                BlokNpcTotalen[(dag, starttijdId)] = new BlokNpcTotaal
                {
                    Dag = dag,
                    StarttijdId = starttijdId,
                    HeeftAlleNpcs = overflows.Any(o => o.IsAlleNpcs),
                    DirectMinimum = 0,
                    DirectMaximum = 0,
                    OverflowVanVorigeBlokken = overflows,
                };
            }
        }
    }

    private void ComputePlotlijnNummers()
    {
        // Group paginas by domain
        var paginasByDomain = PlotboekPaginas
            .GroupBy(p => p.DomainId)
            .ToList();

        var updatedPaginas = new List<PlotboekPaginaInfo>();

        foreach (var domainGroup in paginasByDomain)
        {
            var domainPaginas = domainGroup.ToList();
            var (plotlijnNumbers, individualNumbers) = PlotboekExportBuilder.BuildSlotNumbers(
                domainPaginas,
                p => p.PlotlijnId,
                p => p.Dag,
                p => p.StarttijdId,
                p => p.Order,
                p => p.Id);

            // Assign display numbers to each pagina
            foreach (var pagina in domainPaginas)
            {
                updatedPaginas.Add(pagina with
                {
                    PlotlijnNummer = PlotboekExportBuilder.GetDisplayNumber(
                        pagina.PlotlijnId,
                        pagina.Id,
                        plotlijnNumbers,
                        individualNumbers,
                        domainPaginas,
                        p => p.PlotlijnId,
                        p => p.Dag,
                        p => p.StarttijdId,
                        p => p.Order,
                        p => p.Id)
                });
            }
        }

        PlotboekPaginas = updatedPaginas;
    }

    private async Task LoadAvailablePlotlijnen()
    {
        if (!PlotboekId.HasValue || IsEvergreen)
        {
            AvailablePlotlijnen = [];
            return;
        }

        using var session = documentStore.OpenSession();
        var plotlijnen = await session.QueryAsync<Plotlijn>(p => p.PlotboekIdQueryable == PlotboekId.ToString());

        // Only show plotlijnen that have at least one active (non-deleted) member page
        var activePlotlijnIds = PlotboekPaginas
            .Where(p => p.PlotlijnId.HasValue)
            .Select(p => p.PlotlijnId)
            .ToHashSet();

        AvailablePlotlijnen = [.. plotlijnen
            .Where(p => activePlotlijnIds.Contains(p.PlotlijnId))
            .OrderBy(p => p.DomainShorthand)
            .ThenBy(p => p.Titel)];
    }

    private async Task LoadAvailableSls()
    {
        using var session = documentStore.OpenSession();

        // Load all users from the database
        var allUsers = await session.QueryAllAsync<PlotboekUser>();

        // Get all users that have the User or Admin role
        var userRoles = await session.QueryAsync<UserRole>(r => r.RoleName == UserRoles.User || r.RoleName == UserRoles.Admin);

        List<PlotboekUser> users;
        if (userRoles.Count > 0)
        {
            // Filter users by role
            var userIds = userRoles.Select(r => r.UserId).Distinct().ToHashSet();
            users = [.. allUsers.Where(u => userIds.Contains(u.Id))];
        }
        else
        {
            // Fallback: if no UserRole records exist, show all users
            // This handles cases where roles are managed differently (e.g., claims-based)
            logger.LogWarning("No UserRole records found for User or Admin roles. Showing all users as available SLs.");
            users = [.. allUsers];
        }

        AvailableSls = [.. users.OrderBy(u => u.UserName).Select(u => new SlUserInfo
        {
            Id = u.Id,
            DisplayName = u.UserName ?? u.Email ?? u.Id,
        })];
    }

    private async Task LoadCurrentUserSlId()
    {
        // Fast path: for ASP.NET Core Identity the NameIdentifier claim already is the PlotboekUser.Id.
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(idClaim, out _))
        {
            CurrentUserSlId = idClaim;
            return;
        }

        using var session = documentStore.OpenSession();
        CurrentUserSlId = (await User.GetUserAsync(session, HttpContext.RequestAborted))?.Id;
    }

    private async Task LoadAvailableDomains()
    {
        using var session = documentStore.OpenSession();

        // Load all domains from the database, ordered by their Order property
        var domains = await session.QueryAllAsync<Domain>();
        AvailableDomains = [.. domains.OrderBy(d => d.Order)];
    }

    /// <summary>
    /// The days a new plotboek for <paramref name="eventId"/> starts and ends on, from the event's
    /// <see cref="EventConfig"/>; none when the event is unknown.
    /// </summary>
    internal async Task<(Dag StartDag, Dag EindDag)> GetEventDagenAsync(string eventId)
    {
        if (AvailableEventConfigs.Count == 0)
        {
            using var session = documentStore.OpenSession();
            AvailableEventConfigs = [.. (await session.QueryAllAsync<EventConfig>()).OrderBy(v => v.Order)];
        }

        var config = AvailableEventConfigs.FindById(eventId);
        return config is null ? (Dag.None, Dag.None) : (config.StartDag, config.EindDag);
    }

    internal async Task LoadReferenceDataAsync()
    {
        using var session = documentStore.OpenSession();

        var starttijden = await session.QueryAllAsync<StarttijdConfig>();
        AvailableStarttijdConfigs = [.. starttijden.OrderBy(s => s.Order)];

        var aantalBlokken = await session.QueryAllAsync<AantalBlokkenConfig>();
        AvailableAantalBlokkenConfigs = [.. aantalBlokken.OrderBy(a => a.Order)];

        var soortEncounters = await session.QueryAllAsync<SoortEncounterConfig>();
        AvailableSoortEncounterConfigs = [.. soortEncounters.OrderBy(s => s.Order)];

        var events = await session.QueryAllAsync<EventConfig>();
        AvailableEventConfigs = [.. events.OrderBy(v => v.Order)];

        var questLocatieCategorieen = await session.QueryAllAsync<QuestLocatieCategorieConfig>();
        AvailableQuestLocatieCategorieConfigs = [.. questLocatieCategorieen.OrderBy(c => c.Order)];

        var questLocaties = await session.QueryAllAsync<QuestLocatieConfig>();
        AvailableQuestLocatieConfigs = [.. questLocaties.OrderBy(q => q.Order).ThenBy(q => q.DisplayName, StringComparer.Ordinal)];

        // Only people who can edit a plot pick from (or save to) the standaard-NPC's.
        if (!IsReadOnlyRole)
        {
            var standaardNpcs = await session.QueryAllAsync<StandaardNpc>();
            AvailableStandaardNpcs = [.. standaardNpcs.OrderBy(s => s.Rol, StringComparer.CurrentCultureIgnoreCase)];
        }
    }

    private static IReadOnlyList<Dag> GetDaysInRange(Dag startDag, Dag eindDag) =>
        PlotboekExportBuilder.GetDaysInRange(startDag, eindDag);

    /// <summary>
    /// Resolves which day type <paramref name="selectedDag"/> is within the plotboek's range.
    /// Returns <c>false</c> when there is no day to speak of: a player-driven page without a day, a
    /// plotboek without a range, or a day outside the range.
    /// </summary>
    internal static bool TryGetMeetingDagType(Dag selectedDag, Dag startDag, Dag eindDag, out MeetingDagType dagType)
    {
        dagType = MeetingDagType.Tussendag;

        if (selectedDag == Dag.None || startDag == Dag.None || eindDag == Dag.None)
        {
            return false;
        }

        // GetDaysInRange puts Dag.None at index 0; leaving it in would make no day the first day.
        var days = GetDaysInRange(startDag, eindDag).Where(d => d != Dag.None).ToList();
        if (!days.Contains(selectedDag))
        {
            return false;
        }

        dagType = PlotboekExportBuilder.GetMeetingDagType(selectedDag, days);
        return true;
    }

    /// <summary>
    /// The starttijd ids the dropdown offers for the given day, always led by "None".
    /// <paramref name="huidigeStarttijdId"/> is the slot the encounter is on right now: it stays in the
    /// list even when it is no longer selectable on that day, because an option the browser cannot
    /// select falls back to the first one and the next save would silently drop the starttijd.
    /// </summary>
    internal static List<string> GetAvailableStarttijden(
        Dag selectedDag,
        Dag startDag,
        Dag eindDag,
        IReadOnlyList<StarttijdConfig> starttijdConfigs,
        string huidigeStarttijdId = "")
    {
        var beschikbaar = TryGetMeetingDagType(selectedDag, startDag, eindDag, out var dagType)
            ? starttijdConfigs.GetAvailable(dagType).ToList()
            : [];

        var huidige = starttijdConfigs.FindById(huidigeStarttijdId);
        if (huidige is not null && !huidige.IsNone && !beschikbaar.Contains(huidige))
        {
            beschikbaar.Add(huidige);
        }

        return ["None", .. beschikbaar.OrderBy(cfg => cfg.Order).Select(cfg => cfg.Id)];
    }

    /// <summary>
    /// Returns whether the slot is in the dropdown only because an encounter still sits on it: it is not
    /// selectable on that day. The dropdown marks those so it is clear the plot has to move.
    /// </summary>
    internal static bool IsStarttijdNietBeschikbaarOpDag(
        Dag selectedDag,
        string starttijdId,
        Dag startDag,
        Dag eindDag,
        IReadOnlyList<StarttijdConfig> starttijdConfigs)
    {
        if (string.IsNullOrWhiteSpace(starttijdId) || string.Equals(starttijdId, "None", StringComparison.Ordinal))
        {
            return false;
        }

        var config = starttijdConfigs.FindById(starttijdId);

        return config is not null
            && TryGetMeetingDagType(selectedDag, startDag, eindDag, out var dagType)
            && !config.IsAvailableOn(dagType);
    }

    /// <summary>
    /// The block part of a slot label — "Blok &lt;dagnummer&gt;.&lt;bloknummer&gt;: &lt;venster&gt;" — or an
    /// empty string when the slot has no block on that day. Both the window and the number come from the
    /// reference data, so a slot added or rescheduled in Admin → Starttijden shows up here without a code
    /// change. Every label shape (plot list, dropdown, summary) is built from this one calculation.
    /// </summary>
    internal static string GetStarttijdBlokLabel(
        Dag dag,
        string starttijdId,
        Dag startDag,
        Dag eindDag,
        IReadOnlyList<StarttijdConfig> starttijdConfigs)
    {
        if (dag == Dag.None || string.Equals(starttijdId, "None", StringComparison.Ordinal) || startDag == Dag.None || eindDag == Dag.None)
        {
            return string.Empty;
        }

        var days = GetDaysInRange(startDag, eindDag).Where(d => d != Dag.None).ToList();
        var dayIndex = days.IndexOf(dag);
        if (dayIndex < 0)
        {
            return string.Empty;
        }

        var dagType = PlotboekExportBuilder.GetMeetingDagType(dag, days);
        var venster = starttijdConfigs.FindById(starttijdId).GetBlokVenster(dagType);
        var blokNummer = starttijdConfigs.GetBlokNummer(dagType, starttijdId);

        return blokNummer == 0 || string.IsNullOrWhiteSpace(venster)
            ? string.Empty
            : string.Format(CultureInfo.CurrentCulture, PlotboekUi.BlokLabel, dayIndex + 1, blokNummer, venster);
    }

    /// <summary>
    /// The slot label as the plot page and the plot list show it: the display name, followed by
    /// "[Blok &lt;dagnummer&gt;.&lt;bloknummer&gt;: &lt;venster&gt;]" when the slot has a block window on
    /// that day.
    /// </summary>
    internal static string GetStarttijdSummaryLabel(
        Dag dag,
        string starttijdId,
        Dag startDag,
        Dag eindDag,
        IReadOnlyList<StarttijdConfig> starttijdConfigs)
    {
        var displayName = starttijdConfigs.GetDisplayName(starttijdId);
        var blokLabel = GetStarttijdBlokLabel(dag, starttijdId, startDag, eindDag, starttijdConfigs);

        return blokLabel.Length == 0 ? displayName : $"{displayName} [{blokLabel}]";
    }

    /// <summary>
    /// The label one slot gets in the starttijd dropdown: the display name, the block separated by a
    /// middle dot, and a marker when the slot is only in the list because an encounter still sits on it.
    /// </summary>
    internal static string GetStarttijdDropdownLabel(
        Dag dag,
        string starttijdId,
        Dag startDag,
        Dag eindDag,
        IReadOnlyList<StarttijdConfig> starttijdConfigs)
    {
        var displayName = starttijdConfigs.GetDisplayName(starttijdId);
        var blokLabel = GetStarttijdBlokLabel(dag, starttijdId, startDag, eindDag, starttijdConfigs);
        var label = blokLabel.Length == 0 ? displayName : $"{displayName} · {blokLabel}";

        return IsStarttijdNietBeschikbaarOpDag(dag, starttijdId, startDag, eindDag, starttijdConfigs)
            ? string.Format(CultureInfo.CurrentCulture, PlotboekUi.StarttijdNietBeschikbaarOpDag, label)
            : label;
    }

    /// <summary>One option in the starttijd dropdown, as the client rebuilds it when the day changes.</summary>
    internal sealed record StarttijdOptie(string Id, string Label);

    private static readonly JsonSerializerOptions StarttijdOptiesJsonOptions =
        new () { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// The dropdown options for every day the day select offers, keyed by the day's enum name — the very
    /// value its option carries. Built per day and not per day type, because the label holds the day
    /// number ("Blok 2.3") and a four-day event has two middle days. The currently selected day also
    /// keeps the encounter's current slot, so the list matches the server-rendered select exactly.
    /// </summary>
    internal static Dictionary<string, List<StarttijdOptie>> BuildStarttijdOptiesPerDag(
        IReadOnlyList<Dag> dagen,
        Dag startDag,
        Dag eindDag,
        IReadOnlyList<StarttijdConfig> starttijdConfigs,
        Dag huidigeDag,
        string huidigeStarttijdId)
    {
        var optiesPerDag = new Dictionary<string, List<StarttijdOptie>>(StringComparer.Ordinal);

        foreach (var dag in dagen)
        {
            var teBehoudenId = dag == huidigeDag ? huidigeStarttijdId : string.Empty;

            optiesPerDag[dag.ToString()] =
            [
                .. GetAvailableStarttijden(dag, startDag, eindDag, starttijdConfigs, teBehoudenId)
                    .Select(id => new StarttijdOptie(
                        id,
                        GetStarttijdDropdownLabel(dag, id, startDag, eindDag, starttijdConfigs))),
            ];
        }

        return optiesPerDag;
    }

    /// <summary>
    /// <see cref="BuildStarttijdOptiesPerDag"/> serialized for the &lt;script type="application/json"&gt;
    /// block on the plot page. System.Text.Json escapes &lt;, &gt; and &amp; by default, so the payload
    /// cannot break out of the script element.
    /// </summary>
    internal string BuildStarttijdOptiesJson()
        => JsonSerializer.Serialize(
            BuildStarttijdOptiesPerDag(
                AvailableDays,
                PlotboekSettings?.StartDag ?? Dag.None,
                PlotboekSettings?.EindDag ?? Dag.None,
                AvailableStarttijdConfigs,
                EditableDay,
                EditableStarttijd),
            StarttijdOptiesJsonOptions);

    /// <summary>
    /// The starttijd slots that count as a night slot (20:00 or later). The 19:30 Introquest slot
    /// deliberately does not count. Used to warn when a "Flexibel nacht plot" is not in a night slot.
    /// </summary>
    private static readonly HashSet<string> NachtStarttijdIds =
        new (StringComparer.Ordinal) { "Tijd2000", "Tijd2200", "Tijd0000" };

    /// <summary>
    /// Returns true when the given starttijd is a night slot (20:00 or later).
    /// </summary>
    internal static bool IsNachtStarttijd(string starttijdId)
        => starttijdId is not null && NachtStarttijdIds.Contains(starttijdId);

    /// <summary>
    /// Returns true when a "Flexibel nacht plot" should warn: the option is set, the encounter uses a
    /// day/time slot (not player-driven), but that slot is missing or not a 20:00-or-later slot.
    /// </summary>
    internal static bool ShouldWarnFlexibelNachtPlot(bool isFlexibelNachtPlot, bool isSpelerGedreven, string starttijdId)
        => isFlexibelNachtPlot && !isSpelerGedreven && !IsNachtStarttijd(starttijdId);

    internal void LoadAvailableStarttijden()
    {
        if (SelectedPlotboekPagina == null || PlotboekSettings == null)
        {
            AvailableStarttijden = ["None"];
            return;
        }

        AvailableStarttijden = GetAvailableStarttijden(
            SelectedPlotboekPagina.Dag,
            PlotboekSettings.StartDag,
            PlotboekSettings.EindDag,
            AvailableStarttijdConfigs,
            SelectedPlotboekPagina.StarttijdId);
    }

    internal void LoadSelectedPlotboekPagina()
    {
        if (!PaginaId.HasValue)
        {
            SelectedPlotboekPagina = null;
            return;
        }

        SelectedPlotboekPagina = PlotboekPaginas.FirstOrDefault(p => p.Id == PaginaId);
    }
}
