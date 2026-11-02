using Marten;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PlotboekGenerator.WebApp;

/// <summary>
/// The one seam between the product and a deployment: what a host adds to, or chooses for, the
/// shared composition in <see cref="PlotboekApp"/>. Everything not reachable from here is
/// configuration or reference data.
/// </summary>
public interface IDeploymentProfile
{
    /// <summary>
    /// Registers the deployment's own services. It must register an
    /// <see cref="IDocumentTemplateProvider"/> and an <see cref="IReferenceDataSeed"/>, and may
    /// register an <see cref="Authentication.IRoleResolver"/> in place of the default.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Adds sign-in providers beyond the OpenID Connect providers listed in configuration.
    /// </summary>
    /// <param name="authentication">The authentication builder.</param>
    /// <param name="configuration">The application configuration.</param>
    void ConfigureAuthentication(AuthenticationBuilder authentication, IConfiguration configuration);

    /// <summary>
    /// Adjusts the document store, for instance to keep the tables of documents that were renamed.
    /// </summary>
    /// <param name="options">The Marten options, after the product's own mappings.</param>
    void ConfigureMarten(Marten.StoreOptions options);

    /// <summary>
    /// Runs once after the container is built, before the reference data is seeded and before the
    /// first request: the place for a one-off migration.
    /// </summary>
    /// <param name="app">The built application.</param>
    void ConfigureApplication(WebApplication app);
}
