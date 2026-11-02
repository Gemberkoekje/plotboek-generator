using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Marten;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PlotboekGenerator.infrastructure.Adapters.Application;
using PlotboekGenerator.WebApp.Authentication;

namespace PlotboekGenerator.WebApp;

/// <summary>
/// The composition root of the Plotboek Generator, shared by every host: a host's
/// <c>Program.cs</c> is <c>await PlotboekApp.RunAsync(args, new MyProfile());</c>.
/// </summary>
/// <remarks>
/// A documented exception to "register everything inline in Program.cs": the registrations moved
/// one level down so that more than one host can run the same product. They are still made one by
/// one, in the order the project CLAUDE.md prescribes, and everything a deployment decides goes
/// through its <see cref="IDeploymentProfile"/>.
/// </remarks>
public sealed class PlotboekApp
{
    private PlotboekApp()
    {
    }

    /// <summary>Builds the application and serves it until the host shuts down.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="profile">What the deployment adds to the product.</param>
    /// <returns>A task that completes when the host has stopped.</returns>
    public static async Task RunAsync(string[] args, IDeploymentProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var builder = WebApplication.CreateBuilder(args);
        AddServices(builder, profile);

        var app = builder.Build();
        profile.ConfigureApplication(app);
        await EnsureSignInIsPossibleAsync(app);
        WarnAboutUnencryptedKeyRing(app);

        // Seed reference data on startup
        using (var scope = app.Services.CreateScope())
        {
            var seedService = scope.ServiceProvider.GetRequiredService<ReferenceDataSeedService>();
            await seedService.SeedAsync();

            // A demo starts with its example. Resolving the seeder needs the deployment's
            // IDemoContent, so a demo without one stops here rather than at its first reset.
            if (app.Services.GetRequiredService<DemoMode>().IsEnabled)
            {
                var demoSeed = await scope.ServiceProvider.GetRequiredService<DemoContentSeedService>().SeedIfEmptyAsync();
                if (!demoSeed.IsValid)
                {
                    app.Services.GetRequiredService<ILogger<PlotboekApp>>().LogError(
                        "The demo's example could not be written: {Reasons}",
                        string.Join(", ", demoSeed.Messages.Select(m => m.Message)));
                }
            }
        }

        UseMiddleware(app);
        MapEndpoints(app);

        await app.RunAsync();
    }

    /// <summary>
    /// Maps the product's documents. Public so a spec can check a mapping without a database.
    /// </summary>
    /// <param name="options">The Marten options.</param>
    public static void ConfigureDocuments(Marten.StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AutoCreateSchemaObjects = JasperFx.AutoCreate.CreateOrUpdate;

        // Configure document mapping for PlotboekPaginaData
        options.Schema.For<PlotboekPagina.ExportData>()
            .Identity(x => x.Id);
        options.Schema.For<Plotboek.ExportData>()
            .Identity(x => x.Id);
        options.Schema.For<PlotboekVoorblad>()
            .Identity(x => x.Id);
        options.Schema.For<Plotlijn>()
            .Identity(x => x.Id);

        // Configure Domain entity
        options.Schema.For<Domain>()
            .Identity(x => x.Id);

        // Configure CharacterName and WorldName entities
        options.Schema.For<CharacterName>()
            .Identity(x => x.Id);
        options.Schema.For<WorldName>()
            .Identity(x => x.Id);

        // Standaard-NPC's: templates an SL copies into a plot's NPC roles
        options.Schema.For<StandaardNpc>()
            .Identity(x => x.Id);

        // Configure PlotboekUser to use string Id from IdentityUser
        options.Schema.For<PlotboekUser>()
            .Identity(x => x.Id);

        // Google Drive sync state tracking
        options.Schema.For<GoogleDriveSyncState>()
            .Identity(x => x.Id);

        // Configure reference data entities
        options.Schema.For<StarttijdConfig>()
            .Identity(x => x.Id);
        options.Schema.For<AantalBlokkenConfig>()
            .Identity(x => x.Id);
        options.Schema.For<SoortEncounterConfig>()
            .Identity(x => x.Id);
        options.Schema.For<EventConfig>()
            .Identity(x => x.Id);
        options.Schema.For<QuestLocatieCategorieConfig>()
            .Identity(x => x.Id);
        options.Schema.For<QuestLocatieConfig>()
            .Identity(x => x.Id);
    }

    private static void AddServices(WebApplicationBuilder builder, IDeploymentProfile profile)
    {
        var configuration = builder.Configuration;
        var services = builder.Services;

        // Configure forwarded headers for reverse proxy scenarios
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Accept forwarded headers from any source in Kubernetes
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            ConfigureDocuments(options);
            profile.ConfigureMarten(options);
        });

        // For Identity, we'll need a custom implementation or use a lightweight alternative
        services.AddScoped<IUserStore<PlotboekUser>, MartenUserStore>();
        services.AddScoped<IRoleStore<IdentityRole>, MartenRoleStore>();

        // Materializes a user's effective role (override ?? provider ?? guest) into their UserRole doc.
        services.AddScoped<UserRoleMaterializer>();

        services.AddIdentity<PlotboekUser, IdentityRole>(options =>
        {
            // Simplified options for OAuth-only login
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 0;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Stores.MaxLengthForKeys = 128;
        })
            .AddDefaultTokenProviders();

        services.AddRazorPages();

        // What the deployment calls things, how it presents itself and where it keeps the theme.
        // The terminology is process-wide: the core's audit texts read it too.
        var terminologie = ReadTerminologie(configuration);
        Terminologie.Gebruik(terminologie);
        services.AddSingleton(terminologie);
        services.AddSingleton(Branding.Read(configuration));
        services.AddSingleton(ThemeCookie.Read(configuration));
        services.AddSingleton(TerreinKaart.Read(configuration));
        var demo = DemoMode.Read(configuration);
        services.AddSingleton(demo);
        services.AddSingleton(LocalizationSettings.Read(configuration));

        AddDataProtection(services, configuration);

        // Configure authentication to work with Identity
        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/"; // Redirect to Index instead of AccessDenied
            options.ReturnUrlParameter = "returnUrl";
        });

        // The OpenID Connect providers the configuration lists, then whatever the deployment adds.
        // There are no local accounts: a deployment that ends up with no provider at all is
        // stopped on boot (EnsureSignInIsPossibleAsync) rather than serve a login page without buttons.
        var authentication = services.AddAuthentication();
        foreach (var provider in OidcProviderSettings.ReadAll(configuration))
        {
            OidcProviders.Add(authentication, provider);
        }

        profile.ConfigureAuthentication(authentication, configuration);

        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"))
            .AddPolicy("UserOrAdmin", policy =>
                policy.RequireRole("User", "Admin"));

        services.AddSingleton<IEventStore<Uuid>, MartenEventStore>();
        services.AddSingleton<infrastructure.IDocumentStore, MartenDocumentStore>();
        services.AddScoped<PlotboekPaginaRepository>();
        services.AddScoped<IAggregateRepository<PlotboekPaginaId, PlotboekPagina>>(a => a.GetRequiredService<PlotboekPaginaRepository>());
        services.AddScoped<PlotboekRepository>();
        services.AddScoped<IAggregateRepository<PlotboekId, Plotboek>>(a => a.GetRequiredService<PlotboekRepository>());
        services.AddScoped<IPlotboekRepository, MartenPlotboekRepositoryAdapter>();
        services.AddScoped<IPlotboekReadStore, MartenPlotboekReadStore>();
        services.AddScoped<IAuditTrailWriter, EventStoreAuditTrailWriter>();
        services.AddSingleton<GoogleDriveService>();
        services.AddSingleton<IGoogleDriveService>(sp => sp.GetRequiredService<GoogleDriveService>());
        services.AddSingleton<PageNumberCorrectionService>();
        services.AddSingleton<IPageNumberCorrectionService>(sp => sp.GetRequiredService<PageNumberCorrectionService>());
        services.AddSingleton<PlotboekExportBuilder>();
        services.AddSingleton<IPlotboekExportDataBuilder>(sp => sp.GetRequiredService<PlotboekExportBuilder>());
        services.AddSingleton<IDocumentGenerator, DocumentGenerationService>();
        services.AddScoped<ExportPlotboekCommandHandler>();
        services.AddScoped<CreatePlotboekCommandHandler>();
        services.AddScoped<UpdatePlotboekTitleCommandHandler>();
        services.AddScoped<GetPlotboekByIdQueryHandler>();
        services.AddScoped<GetPlotboekListQueryHandler>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<PlotboekGoogleDriveSyncService>();

        // Configure GeminiSummaryService as a typed HttpClient service so HttpClient lifecycle is managed by DI
        services.AddHttpClient<GeminiSummaryService>();
        services.AddScoped<IGeminiSummaryService>(sp => sp.GetRequiredService<GeminiSummaryService>());

        services.AddScoped<ReferenceDataSeedService>();

        // A demo's reset and the example it writes. Registered only for a demo: the example comes
        // from the deployment's IDemoContent, which a deployment that never runs as one leaves out.
        if (demo.IsEnabled)
        {
            services.AddSingleton<MartenEventStoreWiper>();
            services.AddSingleton<IEventStoreWiper>(sp => sp.GetRequiredService<MartenEventStoreWiper>());
            services.AddScoped<DemoContentSeedService>();
            services.AddScoped<DemoResetService>();
        }

        // The deployment's own services: its template, its reference data, and optionally its own
        // role resolver. The default resolver is added only when the deployment brought none.
        profile.ConfigureServices(services, configuration);

        // Read once here so a mistyped default role stops the deployment on boot, not at the first sign-in.
        ClaimRoleResolver.ReadDefaultRole(configuration);
        services.AddSingleton<ClaimRoleResolver>();
        services.TryAddSingleton<IRoleResolver>(sp => sp.GetRequiredService<ClaimRoleResolver>());
    }

    /// <summary>
    /// Reads <c>Terminology:*</c>, one key per <see cref="Terminologie"/> property; a key left out
    /// keeps the neutral word, in the reader's language.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The deployment's words.</returns>
    public static Terminologie ReadTerminologie(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? Read(string key) => configuration[$"Terminology:{key}"];

        return new Terminologie
        {
            Techniek = Read(nameof(Terminologie.Techniek)),
            InstructieTechniek = Read(nameof(Terminologie.InstructieTechniek)),
            InstructieTechniekUitleg = Read(nameof(Terminologie.InstructieTechniekUitleg)),
            GeenTechniekInformatie = Read(nameof(Terminologie.GeenTechniekInformatie)),
            TechniekInzet = Read(nameof(Terminologie.TechniekInzet)),
            TechniekInstructie = Read(nameof(Terminologie.TechniekInstructie)),
            MuziekVanuitTechniek = Read(nameof(Terminologie.MuziekVanuitTechniek)),
            LichtOfRookVanuitTechniek = Read(nameof(Terminologie.LichtOfRookVanuitTechniek)),
            EventBreed = Read(nameof(Terminologie.EventBreed)),
            EventBreedUitleg = Read(nameof(Terminologie.EventBreedUitleg)),
            Organisatie = Read(nameof(Terminologie.Organisatie)),
        };
    }

    /// <summary>
    /// Sign-in cookies and anti-forgery tokens are signed with the data-protection key ring. Left
    /// unconfigured that ring lives inside the container, so recreating it — which every redeploy
    /// does — signs every user out and invalidates the anti-forgery token on any page still open.
    /// Pointing DataProtection:KeyPath at a volume keeps the ring across deploys. Unset, the framework
    /// default is left alone, so local development and existing deployments behave exactly as before.
    /// </summary>
    private static void AddDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var dataProtectionKeyPath = configuration["DataProtection:KeyPath"];
        var dataProtectionCertificatePath = configuration["DataProtection:CertificatePath"];

        // A certificate on its own protects nothing: it encrypts a *persisted* key ring, and without
        // KeyPath there is no ring to encrypt. Silently ignoring it would leave someone believing the keys
        // are safe at rest when they are the framework default, which is the one way to get this wrong that
        // nothing else would report.
        if (string.IsNullOrWhiteSpace(dataProtectionKeyPath) && !string.IsNullOrWhiteSpace(dataProtectionCertificatePath))
        {
            throw new InvalidOperationException(
                "DataProtection:CertificatePath is set but DataProtection:KeyPath is not. The certificate " +
                "only encrypts a persisted key ring, so on its own it protects nothing: set KeyPath as " +
                "well, or remove the certificate.");
        }

        if (string.IsNullOrWhiteSpace(dataProtectionKeyPath))
        {
            return;
        }

        var dataProtection = services.AddDataProtection()
            // Pinned so the ring stays readable if the content root ever moves; the default
            // discriminator is derived from that path.
            .SetApplicationName("PlotboekGenerator")
            .PersistKeysToFileSystem(Directory.CreateDirectory(dataProtectionKeyPath));

        // Without a certificate the ring is plain XML on that volume, and whoever can read it can forge
        // a sign-in cookie for any user, Admin included. There is no DPAPI to fall back on — the
        // runtime image is Linux.
        if (!string.IsNullOrWhiteSpace(dataProtectionCertificatePath))
        {
            dataProtection.ProtectKeysWithCertificate(
                X509CertificateLoader.LoadPkcs12FromFile(
                    dataProtectionCertificatePath,
                    configuration["DataProtection:CertificatePassword"]));
        }
    }

    /// <summary>
    /// There are no local accounts, so a deployment without a single external sign-in provider is a
    /// misconfiguration that would leave nobody able to sign in, not a feature switched off.
    /// </summary>
    private static async Task EnsureSignInIsPossibleAsync(WebApplication app)
    {
        var schemes = app.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        if (!(await schemes.GetRequestHandlerSchemesAsync()).Any())
        {
            throw new InvalidOperationException(
                $"No sign-in provider is configured, so nobody could sign in. List one under {OidcProviderSettings.SectionKey}.");
        }
    }

    private static void WarnAboutUnencryptedKeyRing(WebApplication app)
    {
        var dataProtectionKeyPath = app.Configuration["DataProtection:KeyPath"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath) && string.IsNullOrWhiteSpace(app.Configuration["DataProtection:CertificatePath"]))
        {
            app.Services.GetRequiredService<ILogger<PlotboekApp>>().LogWarning(
                "Data protection keys in {KeyPath} are not encrypted at rest; anyone who can read that volume can forge a sign-in cookie. Set DataProtection:CertificatePath.",
                dataProtectionKeyPath);
        }
    }

    private static void UseMiddleware(WebApplication app)
    {
        app.UseForwardedHeaders();

        // A deployment served below a path (e.g. https://example.org/plotboek) sets Hosting:PathBase.
        var pathBase = app.Configuration["Hosting:PathBase"];
        if (!string.IsNullOrWhiteSpace(pathBase))
        {
            app.UsePathBase(pathBase);
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        // The language of the text: the switcher's cookie, the browser, or Localization:DefaultCulture.
        app.UseRequestLocalization(app.Services.GetRequiredService<LocalizationSettings>().ToRequestLocalizationOptions());

        // Add static files BEFORE authentication/authorization
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();
    }

    private static void MapEndpoints(WebApplication app)
    {
        // Google Drive sync endpoint - called by external cronjob
        app.MapPost("/api/sync-google-drive", async (
            HttpContext context,
            PlotboekGoogleDriveSyncService syncService,
            IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            var configuredKey = configuration["GoogleDrive:SyncApiKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                // Fail closed: without a configured key the endpoint is disabled, not open.
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            if (!HasApiKey(context, configuredKey))
            {
                return Results.Unauthorized();
            }

            var result = await syncService.SyncAsync(cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();

        // Puts a demo back to its example - called nightly by an external cronjob. Only a demo has it.
        if (app.Services.GetRequiredService<DemoMode>().IsEnabled)
        {
            app.MapPost("/api/demo/reset", async (
                HttpContext context,
                DemoResetService resetService,
                IConfiguration configuration,
                CancellationToken cancellationToken) =>
            {
                var configuredKey = configuration[DemoMode.ResetApiKeyKey];
                if (string.IsNullOrWhiteSpace(configuredKey))
                {
                    // Fail closed, as above.
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                if (!HasApiKey(context, configuredKey))
                {
                    return Results.Unauthorized();
                }

                var result = await resetService.ResetAsync(cancellationToken);
                return result.IsValid
                    ? Results.Ok(result.Value)
                    : Results.Problem(string.Join(", ", result.Messages.Select(m => m.Message)));
            }).AllowAnonymous();
        }

        // Simple health check endpoint
        app.MapGet("/health", () => Results.Ok("Healthy")).AllowAnonymous();

        // The language switcher: remembers the choice for a year and goes back to the page it came from.
        app.MapGet("/culture/{culture}", (string culture, string? returnUrl, HttpContext context, LocalizationSettings localization) =>
        {
            if (localization.Find(culture) is { } chosen)
            {
                context.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(System.Globalization.CultureInfo.CurrentCulture, chosen)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = context.Request.IsHttps,
                    });
            }

            return Results.LocalRedirect(IsLocalUrl(returnUrl) ? returnUrl! : "~/");
        }).AllowAnonymous();
    }

    /// <summary>
    /// Whether <paramref name="url"/> stays on this site: a path from the root, not a scheme-relative
    /// <c>//host</c> or <c>/\host</c> that a browser would take elsewhere.
    /// </summary>
    internal static bool IsLocalUrl(string? url)
        => !string.IsNullOrEmpty(url)
        && url[0] == '/'
        && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));

    /// <summary>Whether the request's <c>X-Api-Key</c> header is the configured key, compared in fixed time.</summary>
    private static bool HasApiKey(HttpContext context, string configuredKey)
    {
        var providedKey = context.Request.Headers["X-Api-Key"].FirstOrDefault() ?? string.Empty;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedKey),
            Encoding.UTF8.GetBytes(configuredKey));
    }
}
