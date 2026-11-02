using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.infrastructure;
using PlotboekGenerator.infrastructure.MartenDb;
using PlotboekGenerator.infrastructure.Services;
using PlotboekGenerator.test.TestHelpers;
using JasperFx.CommandLine.Descriptions;

namespace PlotboekGenerator.test.Integration;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces
/// PostgreSQL/Marten with in-memory implementations and adds a test
/// authentication handler so requests pass auth checks.
/// </summary>
internal sealed class PlotboekWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestUserId = TestAuthHandler.UserId;
    public const string TestUserName = TestAuthHandler.UserName;
    public const string TestScheme = TestAuthHandler.SchemeName;

    /// <summary>The scheme of the one sign-in provider the factory configures.</summary>
    public const string OidcScheme = "TestOidc";

    /// <summary>The name on that provider's login button.</summary>
    public const string OidcDisplayName = "Test login";

    public TestDocumentStore TestStore { get; } = new ();

    public InMemoryEventStore EventStore { get; } = new ();

    private readonly IReadOnlyDictionary<string, string> _settingOverrides;
    private readonly bool _withSignInProvider;

    public PlotboekWebApplicationFactory()
        : this(new Dictionary<string, string>())
    {
    }

    /// <summary>
    /// Applies <paramref name="settingOverrides"/> on top of the defaults in
    /// <see cref="ConfigureWebHost"/>, so a spec can boot the app with a single configuration value
    /// changed — including to an empty string, which is how a blank environment variable reaches the
    /// app in production.
    /// </summary>
    /// <param name="settingOverrides">Settings applied last.</param>
    /// <param name="withSignInProvider">Whether to configure the test OpenID Connect provider.</param>
    public PlotboekWebApplicationFactory(IReadOnlyDictionary<string, string> settingOverrides, bool withSignInProvider = true)
    {
        _settingOverrides = settingOverrides ?? throw new ArgumentNullException(nameof(settingOverrides));
        _withSignInProvider = withSignInProvider;
    }

    /// <summary>
    /// Clears all in-memory state between tests, then writes the host's reference data back: a
    /// real deployment always has it, since every start seeds it.
    /// </summary>
    public void Reset()
    {
        TestStore.Clear();
        EventStore.Clear();
        new ReferenceDataSeedService(
                Services.GetRequiredService<IReferenceDataSeed>(),
                TestStore,
                Substitute.For<ILogger<ReferenceDataSeedService>>())
            .SeedAsync().GetAwaiter().GetResult();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide required config values so Program.cs doesn't throw.
        builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=fake");
        // One OpenID Connect provider, like the smallest real deployment. Set explicitly so the specs
        // do not depend on appsettings.json. Nothing fetches the discovery document at boot, so the
        // authority never has to resolve.
        if (_withSignInProvider)
        {
            builder.UseSetting("Authentication:Oidc:Providers:0:Scheme", OidcScheme);
            builder.UseSetting("Authentication:Oidc:Providers:0:DisplayName", OidcDisplayName);
            builder.UseSetting("Authentication:Oidc:Providers:0:Authority", "https://login.example.test/realm");
            builder.UseSetting("Authentication:Oidc:Providers:0:ClientId", "plotboek-test");
        }

        // Gemini is optional; with a key the page offers the elevator pitch, as production does. Do not
        // rely on a developer's `dotnet user-secrets`: that is exactly why these specs once passed on
        // Windows and failed on Linux CI. (Gemini:RequestUrl has a non-secret default in appsettings.json.)
        builder.UseSetting("Gemini:ApiKey", "fake-gemini-key");

        // Last, so a spec's override wins over the defaults above. UseSetting outranks
        // appsettings.json, which is what lets a spec blank a value that has a default there.
        foreach (var (key, value) in _settingOverrides)
        {
            builder.UseSetting(key, value);
        }

        // Use Development environment in tests to get detailed error pages for debugging.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            // ── Replace infrastructure with in-memory implementations ──
            services.RemoveAll<IEventStore<Uuid>>();
            services.AddSingleton<IEventStore<Uuid>>(EventStore);

            services.RemoveAll<IDocumentStore>();
            services.AddSingleton<IDocumentStore>(TestStore);

            services.RemoveAll<IUserStore<PlotboekUser>>();
            services.AddScoped<IUserStore<PlotboekUser>, InMemoryUserStore>();
            services.RemoveAll<ISystemPart>();

            // PlotboekGoogleDriveSyncService is sealed (can't be NSubstitute'd), so it's
            // reconstructed from already-faked/test dependencies instead - it only needs to
            // resolve so PlotboekModel's constructor is satisfiable; no test here exercises a
            // Google Drive upload directly (see PlotboekGoogleDriveSyncService_specs.cs for that).
            services.RemoveAll<PlotboekGoogleDriveSyncService>();
            services.AddScoped(sp => new PlotboekGoogleDriveSyncService(
                sp.GetRequiredService<IDocumentStore>(),
                Substitute.For<Marten.IDocumentStore>(),
                sp.GetRequiredService<IGoogleDriveService>(),
                sp.GetRequiredService<IPlotboekExportDataBuilder>(),
                sp.GetRequiredService<IDocumentGenerator>(),
                Substitute.For<IPageNumberCorrectionService>(),
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                Substitute.For<ILogger<PlotboekGoogleDriveSyncService>>()));

            services.RemoveAll<IGoogleDriveService>();
            var googleDrive = Substitute.For<IGoogleDriveService>();
            services.AddSingleton(googleDrive);

            services.RemoveAll<GoogleDriveService>();
            services.AddSingleton(googleDrive);

            services.RemoveAll<IAuditTrailService>();
            var auditTrail = Substitute.For<IAuditTrailService>();
            auditTrail.GetAuditTrailAsync(Arg.Any<Uuid>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<AuditTrailEntry>>([]));
            services.AddScoped<IAuditTrailService>(_ => auditTrail);

            services.RemoveAll<PlotboekExportBuilder>();
            services.AddSingleton<PlotboekExportBuilder>();
            services.RemoveAll<IPlotboekExportDataBuilder>();
            services.AddSingleton<IPlotboekExportDataBuilder>(sp => sp.GetRequiredService<PlotboekExportBuilder>());
            services.RemoveAll<IDocumentGenerator>();
            services.AddSingleton<IDocumentGenerator, DocumentGenerationService>();

            // Remove Marten hosted services that would try to connect to PostgreSQL.
            services.RemoveAll<IHostedService>();

            // ── Disable anti-forgery validation ──
            // Replace IAntiforgery with a mock that always succeeds.
            var antiforgery = Substitute.For<IAntiforgery>();
            antiforgery
                .GetAndStoreTokens(Arg.Any<HttpContext>())
                .Returns(new AntiforgeryTokenSet("fake-request", "fake-cookie", "__RequestVerificationToken", "X-CSRF"));
            antiforgery
                .GetTokens(Arg.Any<HttpContext>())
                .Returns(new AntiforgeryTokenSet("fake-request", "fake-cookie", "__RequestVerificationToken", "X-CSRF"));
            // ValidateRequestAsync returns Task — default NSubstitute behavior is Task.CompletedTask (no throw).
            services.RemoveAll<IAntiforgery>();
            services.AddSingleton(antiforgery);

            // ── Replace authentication with a test scheme ──
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestScheme;
                options.DefaultChallengeScheme = TestScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestScheme, _ => { });
        });
    }
}
