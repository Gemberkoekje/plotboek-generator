namespace PlotboekGenerator.Application.Ports;

/// <summary>
/// Provides read-optimized plotboek data for query use cases.
/// </summary>
public interface IPlotboekReadStore
{
    /// <summary>
    /// Gets a plotboek DTO by ID.
    /// </summary>
    /// <param name="id">The plotboek ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result containing the DTO when found.</returns>
    Task<Result<PlotboekDto>> GetByIdAsync(PlotboekId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged list of plotboeken.
    /// </summary>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of plotboek DTOs.</returns>
    Task<IReadOnlyList<PlotboekDto>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
