namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Indicates whether this encounter has a flexible time slot.
    /// When true, "(Flexibel)" is appended to the Dagdeel display text.
    /// </summary>
    public bool IsFlexibel { get; private set; }

    private Result<PlotboekPagina> AanpassenIsFlexibel(bool isFlexibel, string who)
    {
        if (IsFlexibel == isFlexibel)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaIsFlexibelAangepast
        {
            IsFlexibel = isFlexibel,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaIsFlexibelAangepast @event)
    {
        IsFlexibel = @event.IsFlexibel;
    }
}
