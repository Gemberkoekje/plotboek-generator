namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Indicates whether this plot has been marked as fully complete.
    /// </summary>
    public bool IsVolledigKlaar { get; private set; }

    private Result<PlotboekPagina> AanpassenVolledigKlaar(bool isVolledigKlaar, string who)
    {
        if (IsVolledigKlaar == isVolledigKlaar)
        {
            return Result.For(this);
        }

        // The real gate behind "open feedback ⇒ plot is not fully done", and the first server-side
        // enforcement on this flag. The *edit* lock on a finished pagina stays client-side on purpose
        // (see Plotboek.Authorization.cs); this is only about setting the flag. Switching the flag back
        // OFF is always allowed, so a plot can never get stuck.
        if (isVolledigKlaar && HeeftOpenstaandeFeedback)
        {
            return Result.WithMessages<PlotboekPagina>(
                ValidationMessage.Error("Dit plot heeft nog openstaande feedback; reageer daar eerst op."));
        }

        return ApplyEvent(new PlotboekPaginaVolledigKlaarAangepast
        {
            IsVolledigKlaar = isVolledigKlaar,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaVolledigKlaarAangepast @event)
    {
        IsVolledigKlaar = @event.IsVolledigKlaar;
    }
}
