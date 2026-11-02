namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public string VerantwoordelijkeSl { get; private set; }

    public IReadOnlyList<string> ExtraSlIds { get; private set; } = [];

    private Result<PlotboekPagina> AanpassenVerantwoordelijkeSl(
        string slId,
        string slName,
        IReadOnlyList<string> extraSlIds,
        IReadOnlyList<string> extraSlNames,
        string who)
    {
        if (VerantwoordelijkeSl == slId && ExtraSlIds.SequenceEqual(extraSlIds ?? []))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaVerantwoordelijkeSlAangepast
        {
            VerantwoordelijkeSlId = slId,
            VerantwoordelijkeSlName = slName,
            ExtraSlIds = extraSlIds?.ToList() ?? [],
            ExtraSlNames = extraSlNames?.ToList() ?? [],
            Who = who,
        });
    }

    internal void When(PlotboekPaginaVerantwoordelijkeSlAangepast @event)
    {
        VerantwoordelijkeSl = @event.VerantwoordelijkeSlId;
        ExtraSlIds = @event.ExtraSlIds ?? [];
    }
}
