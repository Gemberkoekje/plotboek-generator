using System.Threading;
using System.Threading.Tasks;

namespace PlotboekGenerator.infrastructure.Services;

public interface IGeminiSummaryService
{
    /// <summary>
    /// Whether Gemini:ApiKey and Gemini:RequestUrl are set. The integration is optional: without them
    /// the page offers no elevator pitch generation.
    /// </summary>
    bool IsConfigured { get; }

    Task<string> GenereerElevatorPitchAsync(
        string titel,
        string samenvattingText,
        string npcSummary,
        string encounterSummary,
        string propsAndLoot,
        string techniekInstructions,
        string extraInformation,
        string debrief,
        CancellationToken ct);
}
