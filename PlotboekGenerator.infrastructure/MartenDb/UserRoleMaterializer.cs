namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>
/// Effective-role resolution for <see cref="PlotboekUser"/>: a manual override wins, otherwise the
/// role the sign-in provider granted, otherwise <see cref="UserRoles.Guest"/>.
/// </summary>
public static class UserRoleResolver
{
    /// <summary>Resolves the effective role: <c>OverrideRole ?? ProviderRole ?? Guest</c>.</summary>
    public static string EffectiveRole(this PlotboekUser user)
        => user.OverrideRole ?? user.ProviderRole ?? UserRoles.Guest;
}

/// <summary>
/// Writes a <see cref="PlotboekUser"/>'s effective role as the single <see cref="UserRole"/>
/// document that the rest of the application reads for authorization and role grouping.
/// Materializing the effective role keeps every existing role query working unchanged after the
/// move to the two-role (provider + override) model.
/// </summary>
public sealed class UserRoleMaterializer(IDocumentStore documentStore)
{
    /// <summary>Recomputes the user's effective role and stores it as their single UserRole.</summary>
    public async Task ApplyAsync(PlotboekUser user, CancellationToken cancellationToken = default)
    {
        var effectiveRole = user.EffectiveRole();

        using var session = documentStore.OpenSession();
        var existing = await session.QueryAsync<UserRole>(r => r.UserId == user.Id, cancellationToken);
        foreach (var role in existing)
        {
            session.Delete(role);
        }

        session.Store(new UserRole { UserId = user.Id, RoleName = effectiveRole });
        await session.SaveChangesAsync(cancellationToken);
    }
}
