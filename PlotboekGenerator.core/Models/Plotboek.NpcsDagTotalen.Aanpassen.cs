namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    public Dictionary<Dag, int> NpcsDagTotalen { get; private set; } = [];

    public Result<Plotboek> Aanpassen(NpcsDagTotalenData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        var nieuw = data.NpcsDagTotalen
            .Where(kv => kv.Value > 0)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Vergelijk huidige waarden met nieuwe — alleen event opslaan als er iets veranderd is
        var veranderd = nieuw.Count != NpcsDagTotalen.Count
            || nieuw.Any(kv => !NpcsDagTotalen.TryGetValue(kv.Key, out var huidig) || huidig != kv.Value)
            || NpcsDagTotalen.Any(kv => !nieuw.ContainsKey(kv.Key));

        return veranderd
            ? ApplyEvent(new PlotboekNpcsDagTotalenAangepast
              {
                  NpcsDagTotalen = nieuw,
                  Who = updatedBy.Id,
              })
            : Result.For(this);
    }

    internal void When(PlotboekNpcsDagTotalenAangepast @event)
    {
        NpcsDagTotalen = @event.NpcsDagTotalen;
    }

    public sealed class NpcsDagTotalenData
    {
        public IReadOnlyDictionary<Dag, int> NpcsDagTotalen { get; init; } = new Dictionary<Dag, int>();
    }
}
