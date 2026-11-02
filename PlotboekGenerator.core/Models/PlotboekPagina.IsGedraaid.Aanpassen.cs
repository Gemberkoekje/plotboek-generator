namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Indicates whether this evergreen encounter has already been run at an event.
    /// When true, the encounter is excluded from subsequent plotboek exports.
    /// </summary>
    public bool IsGedraaid { get; private set; }

    private Result<PlotboekPagina> AanpassenIsGedraaid(bool isGedraaid, string who)
    {
        if (IsGedraaid == isGedraaid)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaIsGedraaidAangepast
        {
            IsGedraaid = isGedraaid,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaIsGedraaidAangepast @event)
    {
        IsGedraaid = @event.IsGedraaid;
    }
}
