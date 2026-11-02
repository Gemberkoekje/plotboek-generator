namespace PlotboekGenerator.Application.Ports;

/// <summary>
/// Persists and retrieves plotboek aggregates for command use cases.
/// </summary>
public interface IPlotboekRepository
{
    /// <summary>
    /// Gets a plotboek aggregate by its ID.
    /// </summary>
    /// <param name="id">The plotboek ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the plotboek when it exists.</returns>
    Task<Result<Plotboek>> GetByIdAsync(PlotboekId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all plotboek aggregates.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of all plotboek aggregates.</returns>
    Task<IReadOnlyList<Plotboek>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a plotboek aggregate.
    /// </summary>
    /// <param name="plotboek">The plotboek aggregate to save.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when plotboek is null.</exception>
    Task SaveAsync(Plotboek plotboek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the read-model projection for an existing plotboek without appending new events.
    /// Use this after out-of-band event writes (e.g. audit trail) that advance the stream version.
    /// </summary>
    /// <param name="id">The plotboek ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RefreshProjectionAsync(PlotboekId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a plotboek by its ID.
    /// </summary>
    /// <param name="id">The plotboek ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when id is null.</exception>
    Task DeleteAsync(PlotboekId id, CancellationToken cancellationToken = default);
}
