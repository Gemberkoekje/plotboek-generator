using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure.Generator;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.Host;

/// <summary>
/// The generic deployment: a neutral export template and an example LARP to start from. Sign-in is
/// the OpenID Connect providers in configuration, roles come from <see cref="WebApp.Authentication.ClaimRoleResolver"/>.
/// </summary>
internal sealed class DefaultProfile : IDeploymentProfile
{
    private const string TemplateResource = "PlotboekGenerator.Host.Templates.PlotboekTemplate.docx";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDocumentTemplateProvider>(new EmbeddedResourceTemplateProvider(typeof(DefaultProfile).Assembly, TemplateResource));
        services.AddSingleton<ExampleReferenceDataSeed>();
        services.AddSingleton<IReferenceDataSeed>(sp => sp.GetRequiredService<ExampleReferenceDataSeed>());
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
}
