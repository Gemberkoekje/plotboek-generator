using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PlotboekGenerator.WebApp.Extensions;

/// <summary>
/// The visitor's light/dark choice, in a cookie the layout's theme script writes
/// (<c>Theme:CookieName</c>, default <c>plotboek-theme</c>). The layout reads it to render
/// <c>data-bs-theme</c> before the first paint; without the cookie the script follows the system.
/// With <c>Theme:SharedDomain</c> set, the cookie is shared by every site on that domain, so one
/// choice holds for all of them.
/// </summary>
public sealed class ThemeCookie
{
    /// <summary>The dark theme.</summary>
    public const string Dark = "dark";

    /// <summary>The light theme.</summary>
    public const string Light = "light";

    /// <summary>400 days: the longest a browser keeps a cookie.</summary>
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(400);

    /// <summary>Creates a theme cookie.</summary>
    /// <param name="cookieName">The cookie's name.</param>
    /// <param name="sharedDomain">The domain to share it on; empty for a host-only cookie.</param>
    public ThemeCookie(string cookieName, string sharedDomain)
    {
        CookieName = string.IsNullOrWhiteSpace(cookieName) ? throw new ArgumentException("A cookie needs a name.", nameof(cookieName)) : cookieName;
        SharedDomain = sharedDomain ?? string.Empty;
    }

    /// <summary>The cookie's name.</summary>
    public string CookieName { get; }

    /// <summary>The domain the cookie is shared on; empty for a host-only cookie.</summary>
    public string SharedDomain { get; }

    /// <summary>Reads <c>Theme:CookieName</c> and <c>Theme:SharedDomain</c>.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The deployment's theme cookie.</returns>
    public static ThemeCookie Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var name = configuration["Theme:CookieName"];
        return new ThemeCookie(
            string.IsNullOrWhiteSpace(name) ? "plotboek-theme" : name,
            configuration["Theme:SharedDomain"] ?? string.Empty);
    }

    /// <summary>Reads the theme the visitor chose.</summary>
    /// <param name="request">The current request.</param>
    /// <returns>
    /// <see cref="Light"/> or <see cref="Dark"/>; <c>null</c> when the cookie is absent or holds
    /// anything else, which means "follow the system". Only the two literals are ever returned,
    /// never the cookie's own value.
    /// </returns>
    public string? Read(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Cookies[CookieName] switch
        {
            Light => Light,
            Dark => Dark,
            _ => null,
        };
    }

    /// <summary>
    /// Moves the dark mode an account chose in the old, per-account toggle into the cookie, once,
    /// and clears the account's flag so "follow the system" can be chosen afterwards.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <param name="users">The Identity user manager, to clear the flag.</param>
    /// <param name="user">The signed-in account, or <c>null</c>.</param>
    /// <returns>The theme to render: the cookie's, or <see cref="Dark"/> when it was just moved over.</returns>
    public Task<string?> AdoptAccountPreferenceAsync(HttpContext context, UserManager<PlotboekUser> users, PlotboekUser? user)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(users);

        return AdoptAsync(context, users, user);
    }

    /// <summary>Writes the cookie the way the theme script does, so the script reads it back.</summary>
    /// <param name="context">The current request.</param>
    /// <param name="theme"><see cref="Light"/> or <see cref="Dark"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="theme"/> is anything else.</exception>
    [SuppressMessage("Security", "S3330:Set the 'HttpOnly' flag on this cookie", Justification = "The theme script reads and rewrites it; it holds only light or dark.")]
    [SuppressMessage("Security", "S2092:Set the 'Secure' flag on this cookie", Justification = "Secure on https, as the script sets it; localhost runs on http, where a Secure cookie would be refused.")]
    public void Write(HttpContext context, string theme)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.Equals(theme, Light, StringComparison.Ordinal) && !string.Equals(theme, Dark, StringComparison.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "The theme is light or dark; no cookie means the system setting.");
        }

        context.Response.Cookies.Append(CookieName, theme, new CookieOptions
        {
            HttpOnly = false,
            Path = "/",
            SameSite = SameSiteMode.Lax,
            MaxAge = MaxAge,
            Secure = context.Request.IsHttps,
            Domain = DomainFor(context.Request.Host.Host),
        });
    }

    /// <summary>
    /// The shared domain when <paramref name="host"/> belongs to it, else <c>null</c>: localhost and
    /// other hosts get a host-only cookie, because a Domain they do not belong to would make the
    /// browser drop the cookie altogether.
    /// </summary>
    private string? DomainFor(string host)
    {
        if (string.IsNullOrWhiteSpace(SharedDomain))
        {
            return null;
        }

        var shared = string.Equals(host, SharedDomain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith($".{SharedDomain}", StringComparison.OrdinalIgnoreCase);
        return shared ? SharedDomain : null;
    }

    private async Task<string?> AdoptAsync(HttpContext context, UserManager<PlotboekUser> users, PlotboekUser? user)
    {
        var theme = Read(context.Request);
        if (theme is not null || user is not { PrefersDarkMode: true })
        {
            return theme;
        }

        Write(context, Dark);
        user.PrefersDarkMode = false;
        await users.UpdateAsync(user);

        return Dark;
    }
}
