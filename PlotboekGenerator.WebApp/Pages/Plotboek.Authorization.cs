namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel
{
    private bool CanEditCurrentPlotboek()
    {
        if (IsAdmin)
        {
            return true;
        }

        if (IsNpc)
        {
            return false;
        }

        return PlotboekSettings?.IsVolledigKlaar != true;
    }

    // Pagina-level "volledig klaar" is a soft, client-side self-lock only: the UI marks a
    // finished pagina read-only (see IsSelectedPaginaReadOnly and isPaginaReadOnly in
    // _PlotboekPaginaDetails.cshtml), but the server intentionally does NOT enforce it. A
    // regular user can always toggle the pagina back to editable, so blocking edits here
    // would just get in the way. Only the plotboek-level lock and the NPC role are enforced
    // server-side, via CanEditCurrentPlotboek.
    private bool CanEditCurrentPagina()
    {
        return CanEditCurrentPlotboek();
    }

    private IActionResult ReadOnlyMutationResult()
    {
        ModelState.AddModelError(string.Empty, "Dit plotboek is volledig klaar en kan niet meer aangepast worden.");
        return Page();
    }
}
