namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Props and loot configuration for this encounter.
    /// </summary>
    public PropsEnLootData PropsEnLoot { get; private set; }

    private Result<PlotboekPagina> AanpassenPropsEnLoot(PropsEnLootData propsEnLoot, string who)
    {
        if (PropsEnLoot == propsEnLoot)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaPropsEnLootAangepast
        {
            Props = propsEnLoot.Props?.ToList() ?? [],
            Loot = propsEnLoot.Loot?.ToList() ?? [],
            Who = who,
        });
    }

    internal void When(PlotboekPaginaPropsEnLootAangepast @event)
    {
        PropsEnLoot = PropsEnLootData.Create(@event.Props, @event.Loot);
    }
}
