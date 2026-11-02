namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>
/// Throws away every event stream. Only a demo's reset uses it; it is kept apart from
/// <see cref="IEventStore{TId}"/> so nothing that merely reads or appends can reach it.
/// </summary>
public interface IEventStoreWiper
{
    /// <summary>Deletes every event of every stream.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the events are gone.</returns>
    Task WipeAllAsync(CancellationToken cancellationToken = default);
}
