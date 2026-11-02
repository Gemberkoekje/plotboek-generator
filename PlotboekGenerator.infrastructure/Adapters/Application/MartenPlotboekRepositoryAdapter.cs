using Microsoft.Extensions.Logging;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.MartenDb;

namespace PlotboekGenerator.infrastructure.Adapters.Application;

/// <summary>
/// Application-layer repository adapter backed by Marten aggregate and document persistence.
/// </summary>
public sealed class MartenPlotboekRepositoryAdapter : IPlotboekRepository
{
    private readonly PlotboekRepository _aggregateRepository;
    private readonly IDocumentStore _documentStore;
    private readonly ILogger<MartenPlotboekRepositoryAdapter> _logger;

    public MartenPlotboekRepositoryAdapter(
        PlotboekRepository aggregateRepository,
        IDocumentStore documentStore,
        ILogger<MartenPlotboekRepositoryAdapter> logger)
    {
        _aggregateRepository = aggregateRepository ?? throw new ArgumentNullException(nameof(aggregateRepository));
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<Plotboek>> GetByIdAsync(PlotboekId id, CancellationToken cancellationToken = default) =>
        _aggregateRepository.LoadAsync(id, cancellationToken);

    public async Task<IReadOnlyList<Plotboek>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenSession();
        var exportData = await session.QueryAllAsync<Plotboek.ExportData>(cancellationToken).ConfigureAwait(false);

        var aggregates = new List<Plotboek>();
        foreach (var item in exportData)
        {
            var id = PlotboekId.Create(item.Jaar, item.EventId);
            var loaded = await _aggregateRepository.LoadAsync(id, cancellationToken).ConfigureAwait(false);
            if (loaded.IsValid)
            {
                aggregates.Add(loaded.Value);
            }
        }

        return aggregates;
    }

    public async Task SaveAsync(Plotboek plotboek, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plotboek);

        var result = await _aggregateRepository.SaveAsync(plotboek, cancellationToken).ConfigureAwait(false);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(string.Join("; ", result.Messages.Select(message => message.Message)));
        }

        using var session = _documentStore.OpenSession();
        session.Store(plotboek.GetData());
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RefreshProjectionAsync(PlotboekId id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RefreshProjectionAsync called for plotboek {PlotboekId}", id);
        var reloaded = await _aggregateRepository.LoadAsync(id, cancellationToken).ConfigureAwait(false);
        if (!reloaded.IsValid)
        {
            _logger.LogWarning("RefreshProjectionAsync: could not reload plotboek {PlotboekId} from event store", id);
            return;
        }

        _logger.LogInformation("RefreshProjectionAsync: reloaded plotboek {PlotboekId} at version {Version}, storing ExportData", id, reloaded.Value.Version);
        using var session = _documentStore.OpenSession();
        session.Store(reloaded.Value.GetData());
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("RefreshProjectionAsync: ExportData stored for plotboek {PlotboekId} with EventVersion {Version}", id, reloaded.Value.Version);
    }

    public async Task DeleteAsync(PlotboekId id, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenSession();
        session.Delete<Plotboek.ExportData>(id.ToString());
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
