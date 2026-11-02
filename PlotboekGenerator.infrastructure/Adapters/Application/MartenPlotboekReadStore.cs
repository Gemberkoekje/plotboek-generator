using PlotboekGenerator.Application.DTOs;
using PlotboekGenerator.Application.Ports;

namespace PlotboekGenerator.infrastructure.Adapters.Application;

/// <summary>
/// Read-store adapter backed by Marten document queries.
/// </summary>
public sealed class MartenPlotboekReadStore : IPlotboekReadStore
{
    private readonly IDocumentStore _documentStore;

    public MartenPlotboekReadStore(IDocumentStore documentStore)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<Result<PlotboekDto>> GetByIdAsync(PlotboekId id, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenSession();
        var exportData = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == id.ToString(), cancellationToken).ConfigureAwait(false);

        if (exportData == null)
        {
            return Result.WithMessages<PlotboekDto>(ValidationMessage.Error($"Plotboek {id} not found."));
        }

        return new PlotboekDto(exportData.PlotboekId, exportData.Jaar, exportData.EventId)
        {
            EindDatum = exportData.EindDatum,
            IsVolledigKlaar = exportData.IsVolledigKlaar,
        };
    }

    public async Task<IReadOnlyList<PlotboekDto>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 20 : pageSize;

        using var session = _documentStore.OpenSession();
        var all = await session.QueryAllAsync<Plotboek.ExportData>(cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderBy(p => p.Jaar)
            .ThenBy(p => p.EventId)
            .Skip((safePageNumber - 1) * safePageSize)
            .Take(safePageSize)
            .Select(p => new PlotboekDto(p.PlotboekId, p.Jaar, p.EventId)
            {
                EindDatum = p.EindDatum,
                IsVolledigKlaar = p.IsVolledigKlaar,
            })
            .ToList();

        return items;
    }
}
