using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

        // Construct a prompt that instructs the model to return a single-sentence elevator pitch in Dutch.
        // The pitch should be: "1 zin die MSL gebruikt om het plot te verkopen richting de figuranten tijdens het verdelen van de rollen."
        // Samenvatting is the main source; other fields are optional context.
        var promptBuilder = new System.Text.StringBuilder();
        promptBuilder.AppendLine("Geef één korte elevator pitch in één zin (geen opsommingen of meerdere opties). ");
        promptBuilder.AppendLine("Doel: 1 zin die MSL gebruikt om het plot te verkopen richting de figuranten tijdens het verdelen van de rollen.");
        promptBuilder.AppendLine("Gebruik de SAMENVATTING als de HOOFDbron en gebruik de overige velden alleen als extra context. Formuleer slechts ÉÉN zin als antwoord (Nederlandstalig).\n");
        if (!string.IsNullOrWhiteSpace(titel))
        {
            promptBuilder.AppendLine($"Titel: {titel}\n");
        }

        promptBuilder.AppendLine("Samenvatting:");
        promptBuilder.AppendLine(samenvattingText ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(npcSummary))
        {
            promptBuilder.AppendLine($"\nNPCs: {npcSummary}");
        }

        if (!string.IsNullOrWhiteSpace(encounterSummary))
        {
            promptBuilder.AppendLine($"\nEncounter: {encounterSummary}");
        }

        if (!string.IsNullOrWhiteSpace(propsAndLoot))
        {
            promptBuilder.AppendLine($"\nProps & Loot: {propsAndLoot}");
        }

        if (!string.IsNullOrWhiteSpace(techniekInstructions))
        {
            promptBuilder.AppendLine($"\n{Terminologie.Huidig.Techniek}: {techniekInstructions}");
        }

        if (!string.IsNullOrWhiteSpace(extraInformation))
        {
            promptBuilder.AppendLine($"\nExtra informatie: {extraInformation}");
        }

        if (!string.IsNullOrWhiteSpace(debrief))
        {
            promptBuilder.AppendLine($"\nDebrief: {debrief}");
        }

        var prompt = promptBuilder.ToString();

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

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("^.*?[\\.!?](?=( |$))")]
    private static partial Regex SentenceRegex();

    [GeneratedRegex("[\\.!?]$")]
    private static partial Regex TerminatorRegex();
}
