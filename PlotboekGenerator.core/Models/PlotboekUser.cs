#nullable enable
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace PlotboekGenerator.infrastructure.Models;

/// <summary>
/// A person who signs in to the Plotboek Generator.
/// </summary>
/// <remarks>
/// The JSON names of renamed properties are pinned with <see cref="JsonPropertyNameAttribute"/>, so
/// documents stored under the old names keep loading.
/// </remarks>
public sealed class PlotboekUser : IdentityUser
{
    /// <summary>
    /// The member number a LARP gives its players, when it uses one. Stored, not shown anywhere yet.
    /// </summary>
    [JsonPropertyName("PLIN")]
    required public string Lidnummer { get; set; }

    required public string FirstName { get; set; }

    required public string LastName { get; set; }

    required public DateTime CreatedAt { get; set; }

    required public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates this is a placeholder user created by an admin,
    /// without real login credentials. Can be merged with a real user later.
    /// </summary>
    required public bool IsDummy { get; set; }

    /// <summary>
    /// The ID of the user's primary domain, used as default when creating new plotboek paginas.
    /// </summary>
    required public Guid PrimaryDomainId { get; set; }

    /// <summary>
    /// Whether this user switched dark mode on in the old, per-account toggle.
    /// </summary>
    /// <remarks>
    /// Retired: the theme is now a cookie. The layout moves a <c>true</c> into that cookie once and
    /// clears it here.
    /// </remarks>
    public bool PrefersDarkMode { get; set; }

    /// <summary>
    /// The role the sign-in provider granted on the user's last login with it (e.g. "User" for a
    /// game master, "Guest" otherwise). <c>null</c> until a provider that grants roles has been used.
    /// Refreshed on every such login, so a permission changed at the provider applies automatically.
    /// </summary>
    [JsonPropertyName("KeycloakRole")]
    public string? ProviderRole { get; set; }

    /// <summary>
    /// A manually assigned role that, when set, overrides <see cref="ProviderRole"/>. <c>null</c>
    /// means "no override": the provider's role is used. Cleared from the admin pages.
    /// </summary>
    public string? OverrideRole { get; set; }
}
