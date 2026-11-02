namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    public bool IsVolledigKlaar { get; private set; }

    public Result<Plotboek> Aanpassen(VolledigKlaarData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (IsVolledigKlaar != data.IsVolledigKlaar)
        {
            return ApplyEvent(new PlotboekVolledigKlaarAangepast
            {
                IsVolledigKlaar = data.IsVolledigKlaar,
                Who = updatedBy.Id,
            });
        }

        return Result.For(this);
    }

    internal void When(PlotboekVolledigKlaarAangepast @event)
    {
        IsVolledigKlaar = @event.IsVolledigKlaar;
    }

    public sealed class VolledigKlaarData
    {
        public bool IsVolledigKlaar { get; init; }
    }
}
