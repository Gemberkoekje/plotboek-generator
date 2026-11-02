namespace PlotboekGenerator.core.Models;

public partial class Plotboek
{
    public string SamenvattingAlgemeenPlot { get; private set; }

    public Result<Plotboek> Aanpassen(SamenvattingAlgemeenPlotData data, PlotboekUser updatedBy)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!string.Equals(SamenvattingAlgemeenPlot ?? string.Empty, data.SamenvattingAlgemeenPlot ?? string.Empty, StringComparison.Ordinal))
        {
            return ApplyEvent(new PlotboekSamenvattingAlgemeenPlotAangepast
            {
                SamenvattingAlgemeenPlot = data.SamenvattingAlgemeenPlot,
                Who = updatedBy.Id,
            });
        }

        return Result.For(this);
    }

    internal void When(PlotboekSamenvattingAlgemeenPlotAangepast @event)
    {
        SamenvattingAlgemeenPlot = @event.SamenvattingAlgemeenPlot;
    }

    public sealed class SamenvattingAlgemeenPlotData
    {
        public string SamenvattingAlgemeenPlot { get; init; }
    }
}
