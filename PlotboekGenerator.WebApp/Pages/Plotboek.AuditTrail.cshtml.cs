namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel
{
    [BindProperty(SupportsGet = true)]
    public bool ShowPlotboekAuditTrail { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ShowPaginaAuditTrail { get; set; }

    public List<AuditTrailEntry> PlotboekAuditTrail { get; set; } = [];

    public List<AuditTrailEntry> PaginaAuditTrail { get; set; } = [];

    private async Task LoadPlotboekAuditTrailAsync()
    {
        if (!PlotboekId.HasValue || !ShowPlotboekAuditTrail)
        {
            PlotboekAuditTrail.Clear();
            return;
        }

        PlotboekAuditTrail = [.. await auditTrailService.GetAuditTrailAsync((Uuid)PlotboekId)];
    }

    private async Task LoadPaginaAuditTrailAsync()
    {
        if (!PaginaId.HasValue || !ShowPaginaAuditTrail)
        {
            PaginaAuditTrail.Clear();
            return;
        }

        PaginaAuditTrail = [.. await auditTrailService.GetAuditTrailAsync((Uuid)PaginaId)];
    }
}
