using Microsoft.Extensions.Logging;
using PlotboekGenerator.infrastructure.MartenDb;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Puts a demo back to its example: every plot and every visitor's account goes, then the example
/// is written again.
/// </summary>
/// <remarks>
/// Kept: the reference data, the domains, the meetings and the name lists, which only admins change;
/// and the accounts of admins and of everyone an admin gave a role by hand, so whoever was granted
/// write access keeps it. Dummy users go, whatever their role: the example brings its own.
/// </remarks>
public sealed class DemoResetService(
    IDocumentStore documentStore,
    IEventStoreWiper eventStoreWiper,
    ReferenceDataSeedService referenceDataSeed,
    DemoContentSeedService demoContentSeed,
    ILogger<DemoResetService> logger)
{
    private readonly IDocumentStore _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    private readonly IEventStoreWiper _eventStoreWiper = eventStoreWiper ?? throw new ArgumentNullException(nameof(eventStoreWiper));
    private readonly ReferenceDataSeedService _referenceDataSeed = referenceDataSeed ?? throw new ArgumentNullException(nameof(referenceDataSeed));
    private readonly DemoContentSeedService _demoContentSeed = demoContentSeed ?? throw new ArgumentNullException(nameof(demoContentSeed));
    private readonly ILogger<DemoResetService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Wipes the demo's plots and visitors and writes its example again.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>What was removed, or the reason the example could not be written again.</returns>
    public async Task<Result<DemoResetSummary>> ResetAsync(CancellationToken cancellationToken = default)
    {
        await _eventStoreWiper.WipeAllAsync(cancellationToken);

        int removedAccounts;
        using (var session = _documentStore.OpenSession())
        {
            await DeleteAllAsync<Plotboek.ExportData>(session, cancellationToken);
            await DeleteAllAsync<PlotboekPagina.ExportData>(session, cancellationToken);
            await DeleteAllAsync<PlotboekVoorblad>(session, cancellationToken);
            await DeleteAllAsync<Plotlijn>(session, cancellationToken);
            await DeleteAllAsync<StandaardNpc>(session, cancellationToken);
            await DeleteAllAsync<GoogleDriveSyncState>(session, cancellationToken);
            removedAccounts = await DeleteVisitorsAsync(session, cancellationToken);
            await session.SaveChangesAsync(cancellationToken);
        }

        await _referenceDataSeed.SeedAsync(cancellationToken);

        var seeded = await _demoContentSeed.SeedAsync(cancellationToken);
        if (!seeded.IsValid)
        {
            var reasons = string.Join(", ", seeded.Messages.Select(m => m.Message));
            _logger.LogError("Demo reset wiped the data but could not write the example again: {Reasons}", reasons);
            return Result.WithMessages<DemoResetSummary>(seeded.Messages);
        }

        _logger.LogInformation("Demo reset: {RemovedAccounts} visitors' accounts removed and the example written again", removedAccounts);
        return new DemoResetSummary { RemovedAccounts = removedAccounts };
    }

    /// <summary>
    /// Whether an account survives a reset: an admin's, or one an admin gave a role by hand. Never a
    /// dummy, which cannot sign in.
    /// </summary>
    /// <param name="user">The account.</param>
    /// <returns><c>true</c> when the account is kept.</returns>
    internal static bool IsKept(PlotboekUser user)
        => !user.IsDummy
        && (user.OverrideRole is not null || string.Equals(user.EffectiveRole(), UserRoles.Admin, StringComparison.Ordinal));

    private static async Task<int> DeleteVisitorsAsync(IDocumentSession session, CancellationToken cancellationToken)
    {
        var visitors = (await session.QueryAllAsync<PlotboekUser>(cancellationToken))
            .Where(user => !IsKept(user))
            .ToList();
        var visitorIds = visitors.Select(user => user.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var role in (await session.QueryAllAsync<UserRole>(cancellationToken)).Where(r => visitorIds.Contains(r.UserId)))
        {
            session.Delete(role);
        }

        foreach (var login in (await session.QueryAllAsync<UserExternalLogin>(cancellationToken)).Where(l => visitorIds.Contains(l.UserId)))
        {
            session.Delete(login);
        }

        foreach (var claim in (await session.QueryAllAsync<UserClaim>(cancellationToken)).Where(c => visitorIds.Contains(c.UserId)))
        {
            session.Delete(claim);
        }

        foreach (var visitor in visitors)
        {
            session.Delete(visitor);
        }

        return visitors.Count(user => !user.IsDummy);
    }

    private static async Task DeleteAllAsync<T>(IDocumentSession session, CancellationToken cancellationToken)
        where T : class
    {
        foreach (var document in await session.QueryAllAsync<T>(cancellationToken))
        {
            session.Delete(document);
        }
    }
}
