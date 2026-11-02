using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp;

/// <summary>
/// The languages a deployment speaks, from <c>Localization:*</c>: the one it starts in, and the ones a
/// visitor may pick, by their browser or with the switcher in the menu.
/// </summary>
/// <remarks>
/// Only the language of the text changes. Dates and numbers keep the format of the process, as before
/// the product spoke more than one language, so nothing a form posts is parsed differently.
/// </remarks>
public sealed record LocalizationSettings
{
    /// <summary>The configuration key of the language a deployment starts in.</summary>
    public const string DefaultCultureKey = "Localization:DefaultCulture";

    /// <summary>The configuration key of the languages a visitor may pick.</summary>
    public const string SupportedCulturesKey = "Localization:SupportedCultures";

    /// <summary>The languages the product has translations for; Dutch is the neutral one.</summary>
    public static IReadOnlyList<string> Available { get; } = ["nl", "en"];

    /// <summary>The language a deployment starts in, and the language of its Word export.</summary>
    public CultureInfo DefaultCulture { get; init; } = CultureInfo.GetCultureInfo("nl");

    /// <summary>The languages a visitor may pick, the default first.</summary>
    public IReadOnlyList<CultureInfo> SupportedCultures { get; init; } = [CultureInfo.GetCultureInfo("nl")];

    /// <summary>Whether a visitor can choose between languages, which shows the switcher.</summary>
    public bool HasChoice => SupportedCultures.Count > 1;

    /// <summary>
    /// Reads <c>Localization:DefaultCulture</c> (default <c>nl</c>) and
    /// <c>Localization:SupportedCultures</c> (an array or a comma-separated list; default: only the
    /// default culture).
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The deployment's languages.</returns>
    /// <exception cref="InvalidOperationException">
    /// A language the product has no translation for, or a default that is not among the supported ones.
    /// </exception>
    public static LocalizationSettings Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var defaultName = string.IsNullOrWhiteSpace(configuration[DefaultCultureKey]) ? "nl" : configuration[DefaultCultureKey]!.Trim();
        var defaultCulture = Culture(defaultName, DefaultCultureKey);

        var supportedNames = configuration.GetSection(SupportedCulturesKey).GetChildren().Select(child => child.Value)
            .Concat((configuration[SupportedCulturesKey] ?? string.Empty).Split([',', ';']))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .ToList();
        var supported = supportedNames.Count == 0
            ? [defaultCulture]
            : supportedNames.Select(name => Culture(name, SupportedCulturesKey)).Distinct().ToList();

        if (!supported.Contains(defaultCulture))
        {
            throw new InvalidOperationException(
                $"{DefaultCultureKey} is '{defaultName}', which is not one of {SupportedCulturesKey} ({string.Join(", ", supported.Select(c => c.Name))}).");
        }

        return new LocalizationSettings
        {
            DefaultCulture = defaultCulture,
            SupportedCultures = [defaultCulture, .. supported.Where(culture => !culture.Equals(defaultCulture))],
        };
    }

    /// <summary>The supported language called <paramref name="name"/>, if there is one.</summary>
    /// <param name="name">A language name such as <c>en</c>; any case.</param>
    /// <returns>The language, or <c>null</c> when the deployment doesn't speak it.</returns>
    public CultureInfo? Find(string? name)
        => SupportedCultures.FirstOrDefault(culture => string.Equals(culture.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The request localization for these languages: the switcher's cookie first, then the browser's
    /// <c>Accept-Language</c>, then the default. It sets only the language of the text.
    /// </summary>
    /// <returns>The options for <c>UseRequestLocalization</c>.</returns>
    public RequestLocalizationOptions ToRequestLocalizationOptions()
    {
        // The formatting culture stays whatever the process runs in: the only one "supported", so no
        // header or cookie can change how a date or number is written or parsed.
        var formatting = CultureInfo.CurrentCulture;
        return new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(formatting, DefaultCulture),
            SupportedCultures = [formatting],
            SupportedUICultures = [.. SupportedCultures],
            FallBackToParentUICultures = true,
            RequestCultureProviders =
            [
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider(),
            ],
        };
    }

    private static CultureInfo Culture(string name, string key)
    {
        if (!Available.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{key} names '{name}', which the product has no translation for. Use: {string.Join(", ", Available)}.");
        }

        return CultureInfo.GetCultureInfo(name.ToLowerInvariant());
    }
}
