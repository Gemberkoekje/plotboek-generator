using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel
{
    private bool CanEditCurrentPlotboek()
    {
        if (IsAdmin)
        {
            return true;
        }

        if (IsReadOnlyRole)
        {
            return false;
        }

        return PlotboekSettings?.IsVolledigKlaar != true;
    }

    // Pagina-level "volledig klaar" is a soft, client-side self-lock only: the UI marks a
    // finished pagina read-only (see IsSelectedPaginaReadOnly and isPaginaReadOnly in
    // _PlotboekPaginaDetails.cshtml), but the server intentionally does NOT enforce it. A
    // regular user can always toggle the pagina back to editable, so blocking edits here
    // would just get in the way. Only the plotboek-level lock and the reading roles (NPC,
    // Reader) are enforced server-side, via CanEditCurrentPlotboek.
    private bool CanEditCurrentPagina()
    {
        return CanEditCurrentPlotboek();
    }

    private IActionResult ReadOnlyMutationResult()
    {
        ModelState.AddModelError(string.Empty, IsReadOnlyRole
            ? PlotboekUi.AlleenLezen
            : PlotboekUi.PlotboekVergrendeld);
        return Page();
    }
}
