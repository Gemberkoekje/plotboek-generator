using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp.Pages;

/// <summary>
/// What the product keeps about the people who use it. Generic on purpose: it names only what this
/// deployment actually switched on, so it stays true without each deployment rewriting it. A
/// deployment that needs more says so in its own <c>Privacy.cshtml</c>.
/// </summary>
[AllowAnonymous]
public sealed class PrivacyModel(Branding branding, DemoMode demo, LocalizationSettings localization, IConfiguration configuration) : PageModel
{
    /// <summary>The name of the deployment, as in the title bar.</summary>
    public string Naam => branding.Name;

    /// <summary>Where to reach whoever runs the deployment; empty for nowhere.</summary>
    public string ContactUrl => branding.ContactUrl;

    /// <summary>Whether this deployment is a demo that resets every night.</summary>
    public bool IsDemo => demo.IsEnabled;

    /// <summary>Whether visitors can pick a language, which sets a cookie.</summary>
    public bool HeeftTaalkeuze => localization.HasChoice;

    /// <summary>Whether a plot's text can be sent to Google Gemini for a summary.</summary>
    public bool GebruiktGemini => !string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"]);

    /// <summary>Whether exports can be uploaded to Google Drive.</summary>
    public bool GebruiktGoogleDrive => !string.IsNullOrWhiteSpace(configuration["GoogleDrive:RefreshToken"]);
}
