namespace PlotboekGenerator.WebApp.Authentication;

/// <summary>
/// Decides which Plotboek role a sign-in grants. The login page stores the answer as the user's
/// <see cref="PlotboekUser.ProviderRole"/> on every login, so a permission changed at the provider
/// applies on the next one; an admin's <see cref="PlotboekUser.OverrideRole"/> keeps winning.
/// </summary>
public interface IRoleResolver
{
    /// <summary>Resolves the role for one external login.</summary>
    /// <param name="login">The external login that just completed, with its claims and tokens.</param>
    /// <returns>
    /// One of the <see cref="UserRoles"/>, or <c>null</c> when this login says nothing about roles:
    /// the user's provider role is then left as it is.
    /// </returns>
    string? ResolveRole(ExternalLoginInfo login);
}
