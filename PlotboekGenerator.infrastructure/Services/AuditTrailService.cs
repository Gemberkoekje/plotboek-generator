using PlotboekGenerator.core.Events;
using PlotboekGenerator.infrastructure.MartenDb;

namespace PlotboekGenerator.infrastructure.Services;

/// <summary>
/// Service to retrieve audit trail events from the event store.
/// </summary>
public sealed class AuditTrailService(
    IDocumentStore documentStore,
    IEventStore<Uuid> eventStore) : IAuditTrailService
{
    /// <summary>
    /// Retrieves audit trail events for a specific aggregate identified by its UUID.
    /// </summary>
    /// <param name="aggregateId">The UUID of the aggregate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit trail entries ordered by most recent first.</returns>
    public async Task<IReadOnlyList<AuditTrailEntry>> GetAuditTrailAsync(Uuid aggregateId, CancellationToken cancellationToken = default)
    {
        var events = await eventStore.LoadEventsWithVersionAsync(aggregateId, cancellationToken);

        var auditEvents = events
            .Where(e => e.Data is AuditTrailEvent)
            .Select(e => (e.Version, Event: (AuditTrailEvent)e.Data))
            .ToList();

        var userIds = auditEvents.Select(e => e.Event.Who).Distinct().ToList();
        using var session = documentStore.OpenSession();
        var users = await session.QueryAsync<PlotboekUser>(u => userIds.Contains(u.Id), cancellationToken);

        var userNameLookup = users.ToDictionary(u => u.Id, u => u.UserName ?? u.Id);

        return [.. auditEvents
            .Select(e => new AuditTrailEntry
            {
                Version = e.Version,
                Who = userNameLookup.TryGetValue(e.Event.Who, out var userName) ? userName : e.Event.Who,
                When = e.Event.When,
                What = e.Event.What,
                Details = e.Event.Details,
                DebugDetails = GetDebugDetails(e.Event),
            })
            .OrderByDescending(e => e.When)];
    }

    private static string GetDebugDetails(AuditTrailEvent auditEvent) => auditEvent switch
    {
        PlotboekPaginaSamenvattingPlotAangepast richTextEvent => GetRawRichText(richTextEvent.SamenvattingPlot),
        PlotboekPaginaMeerInformatieAangepast richTextEvent => GetRawRichText(richTextEvent.MeerInformatie),
        PlotboekPaginaInstructiePentagonAangepast richTextEvent => GetRawRichText(richTextEvent.InstructiePentagon),
        PlotboekSamenvattingAlgemeenPlotAangepast richTextEvent => GetRawRichText(richTextEvent.SamenvattingAlgemeenPlot),
        _ => string.Empty,
    };

    private static string GetRawRichText(string richText) => string.IsNullOrWhiteSpace(richText)
        ? string.Empty
        : richText;
}

/// <summary>
/// Represents a single audit trail entry for display purposes.
/// </summary>
public sealed record AuditTrailEntry
{
    /// <summary>
    /// The 1-based stream version of the underlying event. Used to restore the aggregate to the
    /// state right after this change (see the "herstel naar dit punt" feature).
    /// </summary>
    public long Version { get; init; }

    /// <summary>
    /// The user who performed the action.
    /// </summary>
    required public string Who { get; init; }

    /// <summary>
    /// When the action was performed.
    /// </summary>
    required public DateTime When { get; init; }

    /// <summary>
    /// Description of what was done.
    /// </summary>
    required public string What { get; init; }

    /// <summary>
    /// Optional expandable details for the action.
    /// </summary>
    public string Details { get; init; }

    /// <summary>
    /// Optional raw unrendered debug details for rich text audit entries.
    /// </summary>
    public string DebugDetails { get; init; }
}
