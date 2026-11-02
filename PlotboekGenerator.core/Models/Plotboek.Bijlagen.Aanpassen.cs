using PlotboekGenerator.core;

namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    /// <summary>
    /// The reusable bijlagen defined on this plotboek. Shared by reference across encounters and
    /// rendered once in the "Bijlagen" appendix. Empty when the plotboek has no bijlagen.
    /// </summary>
    public IReadOnlyList<BijlageData> Bijlagen { get; private set; } = [];

    public Result<Plotboek> Aanpassen(BijlagenData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Drop junk rows (no id / no title), trim titles, normalize the rich-text content so
        // editor round-trips do not register as changes, and renumber Order to list position.
        var nieuw = (data.Bijlagen ?? [])
            .Where(b => b.Id.HasValue && !string.IsNullOrWhiteSpace(b.Titel))
            .Select((b, i) => b with
            {
                Titel = b.Titel.Trim(),
                Inhoud = TextNormalization.Normalize(b.Inhoud),
                Order = i,
            })
            .ToList();

        // Only append an event when something actually changed (BijlageData is a record, so
        // SequenceEqual compares by value).
        if (Bijlagen.SequenceEqual(nieuw))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekBijlagenAangepast
        {
            Bijlagen = nieuw,
            Who = updatedBy.Id,
        });
    }

    internal void When(PlotboekBijlagenAangepast @event)
    {
        Bijlagen = @event.Bijlagen;
    }

    public sealed record BijlagenData
    {
        public IReadOnlyList<BijlageData> Bijlagen { get; init; } = [];
    }
}
