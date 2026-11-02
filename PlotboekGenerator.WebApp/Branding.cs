using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp;

/// <summary>
/// How a deployment presents itself in the product's own layout, from <c>Branding:*</c>. A host
/// that overrides <c>_Layout.cshtml</c> uses what it likes of it.
/// </summary>
public sealed record Branding
{
    /// <summary>The name in the title bar and the browser tab.</summary>
    public string Name { get; init; } = "Plotboek Generator";

    /// <summary>A logo next to the name; empty for none.</summary>
    public string LogoUrl { get; init; } = string.Empty;

    /// <summary>The browser-tab icon, app-relative (<c>~/…</c>) or absolute.</summary>
    public string FaviconUrl { get; init; } = "~/_content/PlotboekGenerator.WebApp/images/favicon.svg";

    /// <summary>The colour of the title bar and the browser chrome on phones.</summary>
    public string ThemeColor { get; init; } = "#2b3a55";

    /// <summary>The line in the footer.</summary>
    public string FooterText { get; init; } = "Plotboek Generator";

    /// <summary>
    /// Where visitors reach whoever runs this deployment: a <c>mailto:</c> or <c>https:</c> link;
    /// empty for none. The privacy page links to it, and so does a demo's banner.
    /// </summary>
    public string ContactUrl { get; init; } = string.Empty;

    /// <summary>Reads <c>Branding:*</c>; a key left out keeps its default.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The deployment's branding.</returns>
    public static Branding Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var standaard = new Branding();
        return new Branding
        {
            Name = Value(configuration, nameof(Name)) ?? standaard.Name,
            LogoUrl = Value(configuration, nameof(LogoUrl)) ?? standaard.LogoUrl,
            FaviconUrl = Value(configuration, nameof(FaviconUrl)) ?? standaard.FaviconUrl,
            ThemeColor = Value(configuration, nameof(ThemeColor)) ?? standaard.ThemeColor,
            FooterText = Value(configuration, nameof(FooterText)) ?? standaard.FooterText,
            ContactUrl = Value(configuration, nameof(ContactUrl)) ?? standaard.ContactUrl,
        };
    }

    private static string? Value(IConfiguration configuration, string key)
        => string.IsNullOrWhiteSpace(configuration[$"Branding:{key}"]) ? null : configuration[$"Branding:{key}"];
}
