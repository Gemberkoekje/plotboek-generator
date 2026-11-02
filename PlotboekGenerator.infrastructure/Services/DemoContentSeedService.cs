using Microsoft.Extensions.Logging;
using PlotboekGenerator.Application.DTOs;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.MartenDb;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Writes a demo's <see cref="IDemoContent"/>: its spelleiders as dummy users, then its plotboeken
/// and paginas as the events and read models the plotboek page would have written for them.
/// </summary>
/// <remarks>
/// It runs after the reference data is seeded, which the example refers to by id. The domains are
/// written here when there are none yet: the admin page writes them the first time it opens, and a
/// demo needs them before anyone has.
/// </remarks>
public sealed class DemoContentSeedService(
    IDemoContent content,
    IReferenceDataSeed referenceData,
    IDocumentStore documentStore,
    IAggregateRepository<PlotboekId, Plotboek> plotboekRepository,
    IAggregateRepository<PlotboekPaginaId, PlotboekPagina> paginaRepository,
    ILogger<DemoContentSeedService> logger)
{
    private readonly IDemoContent _content = content ?? throw new ArgumentNullException(nameof(content));
    private readonly IReferenceDataSeed _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
    private readonly IDocumentStore _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    private readonly IAggregateRepository<PlotboekId, Plotboek> _plotboekRepository = plotboekRepository ?? throw new ArgumentNullException(nameof(plotboekRepository));
    private readonly IAggregateRepository<PlotboekPaginaId, PlotboekPagina> _paginaRepository = paginaRepository ?? throw new ArgumentNullException(nameof(paginaRepository));
    private readonly ILogger<DemoContentSeedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Writes the example unless a plotboek exists already: on a demo's first start, not on every
    /// restart.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A valid result, or the reason the example could not be written.</returns>
    public async Task<Result> SeedIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        using (var session = _documentStore.OpenSession())
        {
            if ((await session.QueryAllAsync<Plotboek.ExportData>(cancellationToken)).Count > 0)
            {
                _logger.LogDebug("The demo has plotboeken already; its example is not written again.");
                return Result.OK;
            }
        }

        return await SeedAsync(cancellationToken);
    }

    /// <summary>Writes the example, whatever the database holds.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A valid result, or the reason the example could not be written.</returns>
    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenSession();

        var domains = await EnsureDomainsAsync(session, cancellationToken);
        var spelleiders = WriteSpelleiders(session, domains);
        var events = (await session.QueryAllAsync<EventConfig>(cancellationToken))
            .ToDictionary(e => e.Id, StringComparer.Ordinal);

        foreach (var plotboek in _content.Plotboeken)
        {
            var written = await WritePlotboekAsync(session, plotboek, domains, spelleiders, events, cancellationToken);
            if (!written.IsValid)
            {
                return written;
            }
        }

        await session.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Demo example written: {PlotboekCount} plotboeken by {SpelleiderCount} spelleiders",
            _content.Plotboeken.Count,
            _content.Spelleiders.Count);
        return Result.OK;
    }

    private async Task<Dictionary<string, Domain>> EnsureDomainsAsync(IDocumentSession session, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain> domains = await session.QueryAllAsync<Domain>(cancellationToken);
        if (domains.Count == 0)
        {
            domains = [.. _referenceData.Domeinen.Select((d, index) => Domain.Create(d.Shorthand, d.Description, index + 1))];
            foreach (var domain in domains)
            {
                session.Store(domain);
            }
        }

        return domains
            .GroupBy(d => d.Shorthand, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    private Dictionary<string, PlotboekUser> WriteSpelleiders(IDocumentSession session, Dictionary<string, Domain> domains)
    {
        var users = new Dictionary<string, PlotboekUser>(StringComparer.Ordinal);
        foreach (var spelleider in _content.Spelleiders)
        {
            var user = new PlotboekUser
            {
                Id = spelleider.Id,
                UserName = spelleider.DisplayName,
                NormalizedUserName = spelleider.DisplayName.ToUpperInvariant(),
                Lidnummer = string.Empty,
                FirstName = spelleider.Voornaam,
                LastName = spelleider.Achternaam,
                CreatedAt = Clock.UtcNow(),
                UpdatedAt = Clock.UtcNow(),
                IsDummy = true,
                PrimaryDomainId = domains.TryGetValue(spelleider.DomainShorthand, out var domain) ? domain.Id : Guid.Empty,

                // Pinned as User, like a dummy the admin page makes, so they are listed as spelleiders.
                OverrideRole = UserRoles.User,
            };

            session.Store(user);
            session.Store(new UserRole { Id = $"demo-{spelleider.Id}", UserId = user.Id, RoleName = user.EffectiveRole() });
            users[user.Id] = user;
        }

        return users;
    }

    private async Task<Result> WritePlotboekAsync(
        IDocumentSession session,
        DemoPlotboek demo,
        Dictionary<string, Domain> domains,
        Dictionary<string, PlotboekUser> spelleiders,
        Dictionary<string, EventConfig> events,
        CancellationToken cancellationToken)
    {
        if (!events.TryGetValue(demo.EventId, out var eventConfig))
        {
            return Result.WithMessages(ValidationMessage.Error($"The demo plotboek '{demo.Titel}' is for event '{demo.EventId}', which the reference data does not have."));
        }

        if (!spelleiders.TryGetValue(demo.AangemaaktDoorId, out var maker))
        {
            return Result.WithMessages(ValidationMessage.Error($"The demo plotboek '{demo.Titel}' is started by '{demo.AangemaaktDoorId}', who is not one of the demo's spelleiders."));
        }

        var created = Plotboek.Create(Year.Create(Clock.UtcNow().Year), demo.EventId, eventConfig.StartDag, eventConfig.EindDag, maker)
            | (p => p.Aanpassen(new Plotboek.BasisData { StartDag = eventConfig.StartDag, EindDag = eventConfig.EindDag, Titel = demo.Titel }, maker))
            | (p => p.Aanpassen(new Plotboek.SamenvattingAlgemeenPlotData { SamenvattingAlgemeenPlot = demo.SamenvattingAlgemeenPlot }, maker));
        if (!created.IsValid)
        {
            return Result.WithMessages(created.Messages);
        }

        var plotboek = created.Value;
        var saved = await _plotboekRepository.SaveAsync(plotboek, cancellationToken);
        if (!saved.IsValid)
        {
            return saved;
        }

        var plotlijnIds = demo.Paginas
            .Select(p => p.Plotlijn)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(key => key, _ => PlotlijnId.Next(), StringComparer.Ordinal);

        var context = new PaginaContext(plotboek.Id, plotboek.GetData().DisplayName, maker, domains, spelleiders, plotlijnIds);
        var paginas = new List<PlotboekPagina.ExportData>();
        foreach (var demoPagina in demo.Paginas)
        {
            var pagina = await WritePaginaAsync(demoPagina, context, cancellationToken);
            if (!pagina.IsValid)
            {
                return Result.WithMessages(pagina.Messages);
            }

            session.Store(pagina.Value);
            paginas.Add(pagina.Value);
        }

        foreach (var plotlijnId in plotlijnIds.Values)
        {
            session.Store(BuildPlotlijn(plotlijnId, plotboek.Id, paginas));
        }

        if (demo.IsVolledigKlaar)
        {
            var klaar = plotboek.Aanpassen(new Plotboek.VolledigKlaarData { IsVolledigKlaar = true }, maker);
            if (!klaar.IsValid)
            {
                return Result.WithMessages(klaar.Messages);
            }

            plotboek = klaar.Value;
            saved = await _plotboekRepository.SaveAsync(plotboek, cancellationToken);
            if (!saved.IsValid)
            {
                return saved;
            }
        }

        session.Store(plotboek.GetData());
        return Result.OK;
    }

    private async Task<Result<PlotboekPagina.ExportData>> WritePaginaAsync(DemoPagina demo, PaginaContext context, CancellationToken cancellationToken)
    {
        Domain domain = null;
        if (!string.IsNullOrWhiteSpace(demo.DomainShorthand) && !context.Domains.TryGetValue(demo.DomainShorthand, out domain))
        {
            return Result.WithMessages<PlotboekPagina.ExportData>(ValidationMessage.Error($"The demo pagina '{demo.Data.Titel}' is in domain '{demo.DomainShorthand}', which does not exist."));
        }

        var author = context.Spelleider(demo.Data.VerantwoordelijkeSlId) ?? context.Maker;
        var data = demo.Data with
        {
            DomainId = domain?.Id ?? Guid.Empty,
            DomainShorthand = domain?.Shorthand,
            DomainDescription = domain?.Description,
            VerantwoordelijkeSlName = context.Spelleider(demo.Data.VerantwoordelijkeSlId)?.UserName,
            ExtraSlNames = [.. demo.Data.ExtraSlIds.Select(id => context.Spelleider(id)?.UserName ?? id)],
            PlotlijnId = context.PlotlijnIds.TryGetValue(demo.Plotlijn ?? string.Empty, out var plotlijnId) ? plotlijnId : PlotlijnId.Empty,
        };

        // Feedback before "volledig klaar": open feedback blocks the flag, and raising feedback clears it.
        var pagina = new PlotboekPagina().AanPlotboekToevoegen(context.PlotboekId, context.PlotboekNaam, author)
            | (p => p.Aanpassen(data with { IsVolledigKlaar = false }, author));
        foreach (var feedback in demo.Feedback)
        {
            pagina = pagina | (p => WriteFeedback(p, feedback, author, context));
        }

        pagina = pagina | (p => p.Aanpassen(data, author));
        if (!pagina.IsValid)
        {
            return Result.WithMessages<PlotboekPagina.ExportData>(pagina.Messages);
        }

        var saved = await _paginaRepository.SaveAsync(pagina.Value, cancellationToken);
        if (!saved.IsValid)
        {
            return Result.WithMessages<PlotboekPagina.ExportData>(saved.Messages);
        }

        return pagina.Value.GetData();
    }

    private static Result<PlotboekPagina> WriteFeedback(PlotboekPagina pagina, DemoFeedback feedback, PlotboekUser verantwoordelijke, PaginaContext context)
    {
        if (context.Spelleider(feedback.GegevenDoorId) is not { } gever)
        {
            return Result.WithMessages<PlotboekPagina>(ValidationMessage.Error($"Feedback on '{pagina.Titel}' is given by '{feedback.GegevenDoorId}', who is not one of the demo's spelleiders."));
        }

        var gegeven = pagina.FeedbackToevoegen(feedback.Tekst, gever);
        if (!gegeven.IsValid || string.IsNullOrWhiteSpace(feedback.Reactie))
        {
            return gegeven;
        }

        var id = gegeven.Value.Feedback[^1].Id;
        var beantwoord = gegeven | (p => p.FeedbackBeantwoorden(id, feedback.Reactie, verantwoordelijke));
        return feedback.IsAfgehandeld
            ? beantwoord | (p => p.FeedbackAfhandelen(id, gever))
            : beantwoord;
    }

    private static Plotlijn BuildPlotlijn(PlotlijnId plotlijnId, PlotboekId plotboekId, List<PlotboekPagina.ExportData> paginas)
    {
        var leden = paginas
            .Where(p => p.PlotlijnIdQueryable == plotlijnId.ToString())
            .OrderBy(p => p.Dag)
            .ThenBy(p => p.StarttijdId, StringComparer.Ordinal)
            .ThenBy(p => p.Order)
            .ToList();

        return new Plotlijn
        {
            PlotlijnId = plotlijnId,
            PlotboekId = plotboekId,
            DomainId = leden[0].DomainId,
            DomainShorthand = leden[0].DomainShorthand,
            PaginaTitels = [.. leden.Select(p => p.Titel)],
        };
    }

    /// <summary>What every pagina of one plotboek is written against.</summary>
    private sealed record PaginaContext(
        PlotboekId PlotboekId,
        string PlotboekNaam,
        PlotboekUser Maker,
        Dictionary<string, Domain> Domains,
        Dictionary<string, PlotboekUser> Spelleiders,
        Dictionary<string, PlotlijnId> PlotlijnIds)
    {
        public PlotboekUser Spelleider(string id)
            => !string.IsNullOrWhiteSpace(id) && Spelleiders.TryGetValue(id, out var user) ? user : null;
    }
}
