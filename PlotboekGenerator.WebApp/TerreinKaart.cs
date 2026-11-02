using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp;

/// <summary>
/// The map of the event terrain that the pagina's "?" next to "Locatie Encounter" opens, from
/// <c>QuestLocaties:Kaart:*</c>. Without a <see cref="Url"/> there is no map and no button.
/// </summary>
public sealed record TerreinKaart
{
    /// <summary>The image, app-relative (<c>~/…</c>) or absolute; empty for no map.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>The title above the map.</summary>
    public string Titel { get; init; } = "Plattegrond van het terrein";

    /// <summary>A line under the map, such as its source and year; empty for none.</summary>
    public string Bijschrift { get; init; } = string.Empty;

    /// <summary>The image's alternative text.</summary>
    public string AltTekst { get; init; } = "Plattegrond van het terrein met alle veldnamen";

    /// <summary>Whether the deployment has a map.</summary>
    public bool IsIngesteld => !string.IsNullOrWhiteSpace(Url);

    /// <summary>Reads <c>QuestLocaties:Kaart:*</c>; a key left out keeps its default.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The deployment's terrain map.</returns>
    public static TerreinKaart Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var standaard = new TerreinKaart();
        return new TerreinKaart
        {
            Url = Value(configuration, nameof(Url)) ?? standaard.Url,
            Titel = Value(configuration, nameof(Titel)) ?? standaard.Titel,
            Bijschrift = Value(configuration, nameof(Bijschrift)) ?? standaard.Bijschrift,
            AltTekst = Value(configuration, nameof(AltTekst)) ?? standaard.AltTekst,
        };
    }

    private static string? Value(IConfiguration configuration, string key)
        => string.IsNullOrWhiteSpace(configuration[$"QuestLocaties:Kaart:{key}"]) ? null : configuration[$"QuestLocaties:Kaart:{key}"];
}
