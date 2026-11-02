using PlotboekGenerator.core;

namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public string SamenvattingPlot { get; private set; }

    private Result<PlotboekPagina> AanpassenSamenvattingPlot(string samenvattingPlot, string who)
    {
        var current = TextNormalization.Normalize(SamenvattingPlot);
        var incoming = TextNormalization.Normalize(samenvattingPlot);

        // Emit debug information to help diagnose false-positive change detection
        TextNormalization.DebugLogComparison("SamenvattingPlot", current, incoming);
        if (string.Equals(current, incoming, StringComparison.Ordinal))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaSamenvattingPlotAangepast
        {
            SamenvattingPlot = incoming,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaSamenvattingPlotAangepast @event)
    {
        SamenvattingPlot = @event.SamenvattingPlot;
    }
}
