using PlotboekGenerator.core;

namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    /// <summary>
    /// Rich-text instructions for the crew that runs music, light and smoke. Stored under its old name.
    /// </summary>
    public string InstructiePentagon { get; private set; }

    private Result<PlotboekPagina> AanpassenInstructiePentagon(string instructie, string who)
    {
        var current = TextNormalization.Normalize(InstructiePentagon);
        var incoming = TextNormalization.Normalize(instructie);
        TextNormalization.DebugLogComparison("InstructiePentagon", current, incoming);
        if (string.Equals(current, incoming, StringComparison.Ordinal))
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaInstructiePentagonAangepast
        {
            InstructiePentagon = incoming,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaInstructiePentagonAangepast @event)
    {
        InstructiePentagon = @event.InstructiePentagon;
    }
}
