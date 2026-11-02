using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.Generator;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.Host;

/// <summary>
/// The generic deployment: a neutral export template and an example LARP to start from, plus an
/// example plotboek for when it runs as a demo. Sign-in is the OpenID Connect providers in
/// configuration, roles come from <see cref="WebApp.Authentication.ClaimRoleResolver"/>.
/// </summary>
/// <remarks>
/// The template, the example LARP and the example plotboek speak the deployment's default language
/// (<c>Localization:DefaultCulture</c>): Dutch, or English when that is <c>en</c>.
/// </remarks>
internal sealed class DefaultProfile : IDeploymentProfile
{
    /// <summary>The manifest name of the Dutch export template.</summary>
    internal const string TemplateResource = "PlotboekGenerator.Host.Templates.PlotboekTemplate.docx";

    /// <summary>The manifest name of the English export template.</summary>
    internal const string EnglishTemplateResource = "PlotboekGenerator.Host.Templates.PlotboekTemplate.en.docx";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (IsEnglish(LocalizationSettings.Read(configuration).DefaultCulture))
        {
            AddExample<EnglishExampleReferenceDataSeed, EnglishExampleDemoContent>(services, EnglishTemplateResource);
        }
        else
        {
            AddExample<ExampleReferenceDataSeed, ExampleDemoContent>(services, TemplateResource);
        }
    }

    /// <inheritdoc />
    public void ConfigureAuthentication(AuthenticationBuilder authentication, IConfiguration configuration)
    {
        // The OpenID Connect providers listed under Authentication:Oidc:Providers are all there is.
    }

    /// <inheritdoc />
    public void ConfigureMarten(Marten.StoreOptions options)
    {
        // A fresh database: the product's own mappings are all there is.
    }

    /// <inheritdoc />
    public void ConfigureApplication(WebApplication app)
    {
        // No migration: nothing older than this product ever wrote to the database.
    }

    /// <summary>Whether a deployment in <paramref name="culture"/> gets the English example.</summary>
    /// <param name="culture">The deployment's default language.</param>
    /// <returns><c>true</c> for English; Dutch is the neutral language and every other one.</returns>
    internal static bool IsEnglish(CultureInfo culture)
        => string.Equals(culture.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase);

    /// <summary>Registers one language's template, example LARP and example plotboek.</summary>
    /// <typeparam name="TSeed">The example LARP.</typeparam>
    /// <typeparam name="TDemo">The example plotboek.</typeparam>
    /// <param name="services">The application's services.</param>
    /// <param name="templateResource">The manifest name of the export template.</param>
    private static void AddExample<TSeed, TDemo>(IServiceCollection services, string templateResource)
        where TSeed : class, IReferenceDataSeed
        where TDemo : class, IDemoContent
    {
        services.AddSingleton<IDocumentTemplateProvider>(new EmbeddedResourceTemplateProvider(typeof(DefaultProfile).Assembly, templateResource));
        services.AddSingleton<TSeed>();
        services.AddSingleton<IReferenceDataSeed>(sp => sp.GetRequiredService<TSeed>());
        services.AddSingleton<TDemo>();
        services.AddSingleton<IDemoContent>(sp => sp.GetRequiredService<TDemo>());
    }
}
