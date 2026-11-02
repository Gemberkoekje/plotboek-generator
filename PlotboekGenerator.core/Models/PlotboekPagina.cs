using Qowaiv.DomainModel;
using PlotboekGenerator.core.Validators;

namespace PlotboekGenerator.core.Models;

public sealed partial class PlotboekPagina(PlotboekPaginaId aggregateId) : Aggregate<PlotboekPagina, PlotboekPaginaId>(aggregateId, new PlotboekPaginaValidator())
{
    /// <summary>String-based encounter type IDs, set by V2 events. Decoupled from the <see cref="SoortEncounter"/> enum.</summary>
    public EncounterTypeIds EncounterTypeIds { get; private set; }

    public PlotboekPagina() : this(PlotboekPaginaId.Next())
    {
    }

    public void MarkAllAsCommitted()
    {
        Buffer = Buffer.MarkAllAsCommitted();
    }
}
