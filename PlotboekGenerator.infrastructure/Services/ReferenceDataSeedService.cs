using Microsoft.Extensions.Logging;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.MartenDb;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Writes the deployment's <see cref="IReferenceDataSeed"/> (StarttijdConfig, AantalBlokkenConfig,
/// SoortEncounterConfig, EventConfig, QuestLocatieCategorieConfig, QuestLocatieConfig) into the
/// Marten document store on startup.
/// Seeding is idempotent: each record uses a fixed string ID, so existing stored events and
/// aggregates remain fully valid. As a rule only rows whose ID does not exist yet are written, so
/// admin edits are never overwritten; <see cref="SeedStarttijdenAsync"/> and
/// <see cref="SeedEventsAsync"/> are the exceptions and document what they touch on existing rows.
/// </summary>
public sealed class ReferenceDataSeedService(
    IReferenceDataSeed seed,
    IDocumentStore documentStore,
    ILogger<ReferenceDataSeedService> logger)
{
    private readonly IReferenceDataSeed _seed = seed ?? throw new ArgumentNullException(nameof(seed));

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenSession();

        await SeedStarttijdenAsync(session, _seed.Starttijden, cancellationToken);
        await SeedMissingAsync(session, _seed.AantalBlokken, c => c.Id, cancellationToken);
        await SeedMissingAsync(session, _seed.SoortEncounters, c => c.Id, cancellationToken);
        await SeedEventsAsync(session, _seed.Events, cancellationToken);
        await SeedMissingAsync(session, _seed.QuestLocatieCategorieen, c => c.Id, cancellationToken);
        await SeedMissingAsync(session, _seed.QuestLocaties, c => c.Id, cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Reference data seeding completed.");
    }

    /// <summary>
    /// Seeds the starttijd slots. Unlike the other reference data this one also touches rows that already
    /// exist, in two strictly bounded ways: it backfills the block windows on rows that were seeded before
    /// those fields existed, and it rewrites <see cref="StarttijdConfig.Order"/> to follow the time in the
    /// display name. Admin-entered text is never overwritten: the backfill fires only on a row that has not
    /// been marked <see cref="StarttijdConfig.BlokVenstersIngesteld"/> yet, which every row is after this
    /// has run once.
    /// </summary>
    private static async Task SeedStarttijdenAsync(IDocumentSession session, IReadOnlyList<StarttijdConfig> seeds, CancellationToken cancellationToken)
    {
        var existing = await session.QueryAllAsync<StarttijdConfig>(cancellationToken);
        var existingById = existing.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var seedsById = seeds.ToDictionary(s => s.Id, StringComparer.Ordinal);

        var merged = existing
            .Select(row => WithBlokVensters(row, seedsById))
            .ToList();

        // A row the seed creates carries the windows it was handed, so it starts out marked.
        merged.AddRange(seeds
            .Where(s => !existingById.ContainsKey(s.Id))
            .Select(s => s with { BlokVenstersIngesteld = true }));

        // Write back only what a new row, a backfilled window or the derived order actually changed.
        foreach (var config in merged.ResequenceOrder())
        {
            if (!existingById.TryGetValue(config.Id, out var stored) || stored != config)
            {
                session.Store(config);
            }
        }
    }

    /// <summary>
    /// Marks a stored row's block windows as deliberate, backfilling them from the seed first when the row
    /// still predates those fields. Backfilling every row that carries no window at all would also restore
    /// the seed text on a row whose windows the admin cleared on purpose — the two look identical — so the
    /// flag records that a row has been past here and the backfill never fires on it a second time.
    /// </summary>
    private static StarttijdConfig WithBlokVensters(StarttijdConfig row, Dictionary<string, StarttijdConfig> seedsById)
    {
        if (row.BlokVenstersIngesteld)
        {
            return row;
        }

        return HasGeenBlokVenster(row) && seedsById.TryGetValue(row.Id, out var seed)
            ? row with
            {
                BlokVensterEersteDag = seed.BlokVensterEersteDag,
                BlokVensterMiddelDagen = seed.BlokVensterMiddelDagen,
                BlokVensterLaatsteDag = seed.BlokVensterLaatsteDag,
                BlokVenstersIngesteld = true,
            }
            : row with { BlokVenstersIngesteld = true };
    }

    private static bool HasGeenBlokVenster(StarttijdConfig config)
        => string.IsNullOrWhiteSpace(config.BlokVensterEersteDag)
        && string.IsNullOrWhiteSpace(config.BlokVensterMiddelDagen)
        && string.IsNullOrWhiteSpace(config.BlokVensterLaatsteDag);

    /// <summary>
    /// Seeds the events. Besides adding missing rows it fills in the days of a row stored before
    /// <see cref="EventConfig.StartDag"/> and <see cref="EventConfig.EindDag"/> existed, from the seed
    /// row with the same id, once: the row is then marked <see cref="EventConfig.DagenIngesteld"/>
    /// and never touched again.
    /// </summary>
    private static async Task SeedEventsAsync(IDocumentSession session, IReadOnlyList<EventConfig> seeds, CancellationToken cancellationToken)
    {
        var existing = await session.QueryAllAsync<EventConfig>(cancellationToken);
        var existingIds = existing.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var seedsById = seeds.ToDictionary(s => s.Id, StringComparer.Ordinal);

        foreach (var row in existing.Where(row => !row.DagenIngesteld))
        {
            session.Store(seedsById.TryGetValue(row.Id, out var seed)
                ? row with { StartDag = seed.StartDag, EindDag = seed.EindDag, DagenIngesteld = true }
                : row with { DagenIngesteld = true });
        }

        foreach (var seed in seeds.Where(s => !existingIds.Contains(s.Id)))
        {
            session.Store(seed with { DagenIngesteld = true });
        }
    }

    private static async Task SeedMissingAsync<T>(
        IDocumentSession session,
        IReadOnlyList<T> seeds,
        Func<T, string> id,
        CancellationToken cancellationToken)
        where T : class
    {
        var existing = await session.QueryAllAsync<T>(cancellationToken);
        var existingIds = existing.Select(id).ToHashSet(StringComparer.Ordinal);

        foreach (var seed in seeds.Where(s => !existingIds.Contains(id(s))))
        {
            session.Store(seed);
        }
    }
}
