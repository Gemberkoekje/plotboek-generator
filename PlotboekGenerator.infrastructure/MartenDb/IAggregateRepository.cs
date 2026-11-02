#nullable enable

namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>
/// Defines a generic repository interface for saving and loading aggregate root entities.
/// </summary>
/// <typeparam name="TId">The type of the aggregate's unique identifier.</typeparam>
/// <typeparam name="TValue">The type of the aggregate root entity.</typeparam>
public interface IAggregateRepository<TId, TValue> where TValue : class
{
    /// <summary>
    /// Asynchronously saves the specified aggregate root entity.
    /// </summary>
    /// <param name="aggregate">The aggregate root entity to save.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A valid result on success, or an invalid result with error messages on concurrency conflict.</returns>
    Task<Result> SaveAsync(TValue aggregate, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously loads an aggregate root entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the aggregate to load.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Optional.</param>
    /// <returns>
    /// A task that represents the asynchronous load operation. The task result contains the loaded aggregate root entity.
    /// </returns>
    Task<Result<TValue>> LoadAsync(TId id, CancellationToken cancellationToken = default);
}
