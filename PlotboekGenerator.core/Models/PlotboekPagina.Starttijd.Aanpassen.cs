namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>The starttijd ID as a string (e.g. "Tijd1030", "Tijd2000").</summary>
    public string StarttijdId { get; private set; }

    private Result<PlotboekPagina> AanpassenStarttijd(string starttijdId, string who)
    {
        if (StarttijdId == starttijdId)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaStarttijdAangepastV2
        {
            StarttijdId = starttijdId,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaStarttijdAangepast @event)
    {
        StarttijdId = @event.Starttijd.ToString();
    }

    internal void When(PlotboekPaginaStarttijdAangepastV2 @event)
    {
        StarttijdId = @event.StarttijdId;
    }
}
