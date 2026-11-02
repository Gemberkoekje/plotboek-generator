namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Indicates whether this pagina has been soft-deleted.
    /// When true, the pagina is hidden from the frontend and excluded from exports.
    /// </summary>
    public bool IsVerwijderd { get; private set; }

    public Result<PlotboekPagina> SetVerwijderd(bool isVerwijderd, PlotboekUser updatedBy)
    {
        if (IsVerwijderd == isVerwijderd)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaIsVerwijderdAangepast
        {
            IsVerwijderd = isVerwijderd,
            Who = updatedBy.Id,
        });
    }

    internal void When(PlotboekPaginaIsVerwijderdAangepast @event)
    {
        IsVerwijderd = @event.IsVerwijderd;
    }
}
