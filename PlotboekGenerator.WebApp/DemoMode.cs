using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp;

/// <summary>
/// Whether this deployment is a public demo, from <c>Demo:*</c>. A demo shows a banner on every
/// page, fills an empty database with the deployment's <see cref="IDemoContent"/>, and offers
/// <c>POST /api/demo/reset</c> (guarded by <c>Demo:ResetApiKey</c>) to put that example back.
/// </summary>
/// <remarks>
/// A demo usually also sets <c>Authentication:DefaultRole</c> to Reader, so whoever signs in can
/// look around without changing anything, and runs the reset nightly.
/// </remarks>
public sealed record DemoMode
{
    /// <summary>The configuration key of the reset endpoint's API key.</summary>
    public const string ResetApiKeyKey = "Demo:ResetApiKey";

    /// <summary>Whether this deployment is a demo.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Reads <c>Demo:Enabled</c>; left out, the deployment is not a demo.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The demo mode.</returns>
    public static DemoMode Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new DemoMode { IsEnabled = configuration.GetValue<bool>("Demo:Enabled") };
    }
}
