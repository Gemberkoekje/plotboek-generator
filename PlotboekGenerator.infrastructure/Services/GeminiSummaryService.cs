using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlotboekGenerator.infrastructure.Resources;

namespace PlotboekGenerator.infrastructure.Services;

public sealed partial class GeminiSummaryService : IGeminiSummaryService
{
    private readonly HttpClient _client;
    private readonly ILogger<GeminiSummaryService> _logger;
    private readonly string _apiKey;
    private readonly string _requestUrl;

    public GeminiSummaryService(HttpClient client, IConfiguration configuration, ILogger<GeminiSummaryService> logger)
    {
        _client = client;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
        _requestUrl = configuration["Gemini:RequestUrl"] ?? string.Empty;
    }

    /// <inheritdoc />
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_requestUrl);

    public async Task<string> GenereerElevatorPitchAsync(
        string titel,
        string samenvattingText,
        string npcSummary,
        string encounterSummary,
        string propsAndLoot,
        string techniekInstructions,
        string extraInformation,
        string debrief,
        CancellationToken ct)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Gemini is not configured. Set Gemini:ApiKey and Gemini:RequestUrl.");
        }

        var requestUrl = _requestUrl;

        var prompt = BuildPrompt(titel, samenvattingText, npcSummary, encounterSummary, propsAndLoot, techniekInstructions, extraInformation, debrief);

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt, },
                    },
                },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("x-goog-api-key", _apiKey);
        request.Content = JsonContent.Create(body);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(15));
        using var response = await _client.SendAsync(request, cts.Token);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync(cts.Token);
            _logger.LogError("Gemini API returned {Status}: {Body}", response.StatusCode, msg);
            throw new InvalidOperationException($"Gemini API request failed: {response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);

        try
        {
            // navigate to candidates[0].content.parts[0].text
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var part0 = parts[0];
                    if (part0.TryGetProperty("text", out var textProp))
                    {
                        var raw = textProp.GetString() ?? string.Empty;

                        // Normalize whitespace
                        var cleaned = WhitespaceRegex().Replace(raw, " ").Trim();

                        // Extract the first sentence (ending with . ! or ?) to ensure we return exactly one sentence.
                        var m = SentenceRegex().Match(cleaned);
                        if (m.Success)
                        {
                            var sentence = m.Value.Trim();
                            // Ensure it ends with a single period if needed
                            if (!TerminatorRegex().IsMatch(sentence))
                                sentence += ".";
                            return sentence;
                        }

                        // Fallback: if no sentence terminator found, return the whole cleaned text but truncate to a single line.
                        var singleLine = cleaned.Split(['\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(singleLine) && !TerminatorRegex().IsMatch(singleLine))
                        {
                            singleLine = singleLine.TrimEnd() + ".";
                        }
                        return singleLine;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Wrap and throw to avoid upstream double-logging of the same exception
            throw new InvalidOperationException("Failed to parse Gemini response", ex);
        }

        return string.Empty;
    }

    /// <summary>
    /// Builds the prompt for a single-sentence elevator pitch: the sentence the GM handing out the NPC
    /// roles uses to sell the plot. The summary is the main source; the other fields are optional
    /// context. The prompt is written in the reader's language and asks for the answer in it, so the
    /// pitch comes back in the language of the page that asked for it.
    /// </summary>
    /// <returns>The prompt text.</returns>
    internal static string BuildPrompt(
        string titel,
        string samenvattingText,
        string npcSummary,
        string encounterSummary,
        string propsAndLoot,
        string techniekInstructions,
        string extraInformation,
        string debrief)
    {
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine(InfraTekst.GeminiPromptOpdracht);
        promptBuilder.AppendLine(InfraTekst.GeminiPromptDoel);
        promptBuilder.AppendLine(InfraTekst.GeminiPromptBron);
        promptBuilder.AppendLine(string.Format(CultureInfo.CurrentCulture, InfraTekst.GeminiPromptTaal, AntwoordTaal()));
        promptBuilder.AppendLine();
        if (!string.IsNullOrWhiteSpace(titel))
        {
            promptBuilder.AppendLine(string.Format(CultureInfo.CurrentCulture, InfraTekst.GeminiPromptTitel, titel));
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine(InfraTekst.GeminiPromptSamenvatting);
        promptBuilder.AppendLine(samenvattingText ?? string.Empty);

        AppendVeld(promptBuilder, InfraTekst.GeminiPromptNpcs, npcSummary);
        AppendVeld(promptBuilder, InfraTekst.GeminiPromptEncounter, encounterSummary);
        AppendVeld(promptBuilder, InfraTekst.GeminiPromptPropsEnLoot, propsAndLoot);
        if (!string.IsNullOrWhiteSpace(techniekInstructions))
        {
            // The deployment's own word for the crew, so not a format string.
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"{Terminologie.Huidig.Techniek}: {techniekInstructions}");
        }

        AppendVeld(promptBuilder, InfraTekst.GeminiPromptExtraInformatie, extraInformation);
        AppendVeld(promptBuilder, InfraTekst.GeminiPromptDebrief, debrief);

        return promptBuilder.ToString();
    }

    /// <summary>Adds an optional field after a blank line, or nothing when it is empty.</summary>
    private static void AppendVeld(StringBuilder promptBuilder, string format, string waarde)
    {
        if (string.IsNullOrWhiteSpace(waarde))
        {
            return;
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine(string.Format(CultureInfo.CurrentCulture, format, waarde));
    }

    /// <summary>
    /// The name of the reader's language in that language ("Nederlands", "English"): the language the
    /// pitch has to come back in. A thread without a culture of its own gets the neutral language.
    /// </summary>
    private static string AntwoordTaal()
    {
        var taal = CultureInfo.CurrentUICulture;
        if (string.IsNullOrEmpty(taal.Name)
            && typeof(InfraTekst).Assembly.GetCustomAttribute<NeutralResourcesLanguageAttribute>() is { } neutraal)
        {
            taal = CultureInfo.GetCultureInfo(neutraal.CultureName);
        }

        if (!taal.IsNeutralCulture && !string.IsNullOrEmpty(taal.Parent.Name))
        {
            taal = taal.Parent;
        }

        return taal.NativeName;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("^.*?[\\.!?](?=( |$))")]
    private static partial Regex SentenceRegex();

    [GeneratedRegex("[\\.!?]$")]
    private static partial Regex TerminatorRegex();
}
