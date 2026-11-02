namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// The IDs of the plotboek-level bijlagen this encounter references. Empty when it references none.
    /// These are loose references (no hard foreign key): resolution to an appendix page happens at export.
    /// </summary>
    public IReadOnlyList<BijlageId> BijlageRefs { get; private set; } = [];

    /// <summary>
    /// Adjusts only the referenced bijlagen of this pagina, leaving every other field untouched.
    /// Use this when (re)linking bijlagen: a full <see cref="Aanpassen(Data, PlotboekUser)"/> would reset
    /// every field the caller does not repopulate, wiping the pagina's content.
    /// </summary>
    public Result<PlotboekPagina> AanpassenBijlagen(IReadOnlyList<BijlageId> bijlageIds, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(updatedBy);
        return AanpassenBijlagen(bijlageIds, updatedBy.Id);
    }

    private Result<PlotboekPagina> AanpassenBijlagen(IReadOnlyList<BijlageId> bijlageIds, string who)
    {
        var nieuw = (bijlageIds ?? [])
            .Where(id => id.HasValue)
            .Distinct()
            .ToList();

        // References are a set: order is irrelevant (the export orders bijlagen by their own Order),
        // so a pure reorder must not append a redundant event.
        if (nieuw.Count == BijlageRefs.Count && new HashSet<BijlageId>(BijlageRefs).SetEquals(nieuw))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaBijlagenAangepast
        {
            BijlageIds = nieuw,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaBijlagenAangepast @event)
    {
        BijlageRefs = @event.BijlageIds;
    }
}
