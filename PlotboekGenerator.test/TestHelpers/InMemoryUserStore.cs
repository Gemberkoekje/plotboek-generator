using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PlotboekGenerator.infrastructure;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// An Identity user store over a <see cref="IDocumentStore"/>, for the integration specs: it keeps
/// users, claims, logins and roles as the Marten store does, without a database.
/// </summary>
internal sealed class InMemoryUserStore(IDocumentStore documentStore) : IUserEmailStore<PlotboekUser>,
    IUserPasswordStore<PlotboekUser>, IUserSecurityStampStore<PlotboekUser>,
    IUserClaimStore<PlotboekUser>, IUserLoginStore<PlotboekUser>, IUserRoleStore<PlotboekUser>
{
    public Task<IdentityResult> CreateAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.Id))
        {
            user.Id = Guid.NewGuid().ToString();
        }

        using var session = documentStore.OpenSession();
        session.Store(user);
        return session.SaveChangesAsync(cancellationToken).ContinueWith(_ => IdentityResult.Success, cancellationToken);
    }

    public Task<IdentityResult> UpdateAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        user.UpdatedAt = Clock.UtcNow();
        using var session = documentStore.OpenSession();
        session.Store(user);
        return session.SaveChangesAsync(cancellationToken).ContinueWith(_ => IdentityResult.Success, cancellationToken);
    }

    public Task<IdentityResult> DeleteAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        session.Delete(user);
        return session.SaveChangesAsync(cancellationToken).ContinueWith(_ => IdentityResult.Success, cancellationToken);
    }

    public async Task<PlotboekUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        return await session.LoadAsync<PlotboekUser>(userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlotboekUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        return await session.FirstOrDefaultAsync<PlotboekUser>(u => u.NormalizedUserName == normalizedUserName, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlotboekUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        return await session.FirstOrDefaultAsync<PlotboekUser>(u => u.NormalizedEmail == normalizedEmail, cancellationToken).ConfigureAwait(false);
    }

    public Task<string> GetUserIdAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

    public Task<string> GetUserNameAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(PlotboekUser user, string userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string> GetNormalizedUserNameAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(PlotboekUser user, string normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(PlotboekUser user, string email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string> GetEmailAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(PlotboekUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<string> GetNormalizedEmailAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(PlotboekUser user, string normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(PlotboekUser user, string passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string> GetPasswordHashAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetSecurityStampAsync(PlotboekUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string> GetSecurityStampAsync(PlotboekUser user, CancellationToken cancellationToken) => Task.FromResult(user.SecurityStamp ?? string.Empty);

    public async Task<IList<Claim>> GetClaimsAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var userClaims = await session.QueryAsync<UserClaim>(c => c.UserId == user.Id, cancellationToken).ConfigureAwait(false);
        return [.. userClaims.Select(c => c.ToClaim())];
    }

    public async Task AddClaimsAsync(PlotboekUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        foreach (var claim in claims)
        {
            session.Store(new UserClaim
            {
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value,
            });
        }
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReplaceClaimAsync(PlotboekUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        await RemoveClaimsAsync(user, [claim], cancellationToken).ConfigureAwait(false);
        await AddClaimsAsync(user, [newClaim], cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveClaimsAsync(PlotboekUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        foreach (var claim in claims)
        {
            var existing = await session.FirstOrDefaultAsync<UserClaim>(c =>
                c.UserId == user.Id &&
                c.ClaimType == claim.Type &&
                c.ClaimValue == claim.Value,
                cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                session.Delete(existing);
            }
        }
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IList<PlotboekUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var userClaims = await session.QueryAsync<UserClaim>(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value, cancellationToken).ConfigureAwait(false);
        var userIds = userClaims.Select(c => c.UserId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var users = await session.QueryAsync<PlotboekUser>(u => userIds.Contains(u.Id), cancellationToken).ConfigureAwait(false);
        return [.. users];
    }

    public async Task AddLoginAsync(PlotboekUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        session.Store(new UserExternalLogin
        {
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName,
            Email = user.Email,
        });
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveLoginAsync(PlotboekUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var login = await session.FirstOrDefaultAsync<UserExternalLogin>(l => l.UserId == user.Id && l.LoginProvider == loginProvider && l.ProviderKey == providerKey, cancellationToken).ConfigureAwait(false);
        if (login != null)
        {
            session.Delete(login);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var logins = await session.QueryAsync<UserExternalLogin>(l => l.UserId == user.Id, cancellationToken).ConfigureAwait(false);
        return [.. logins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))];
    }

    public async Task<PlotboekUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var login = await session.FirstOrDefaultAsync<UserExternalLogin>(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey, cancellationToken).ConfigureAwait(false);
        return login == null ? null : await session.LoadAsync<PlotboekUser>(login.UserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddToRoleAsync(PlotboekUser user, string roleName, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var existing = await session.QueryAsync<UserRole>(r => r.UserId == user.Id, cancellationToken).ConfigureAwait(false);
        if (!existing.Any(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
        {
            session.Store(new UserRole
            {
                UserId = user.Id,
                RoleName = roleName,
            });
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RemoveFromRoleAsync(PlotboekUser user, string roleName, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var role = await session.FirstOrDefaultAsync<UserRole>(r => r.UserId == user.Id && r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase), cancellationToken).ConfigureAwait(false);
        if (role != null)
        {
            session.Delete(role);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IList<string>> GetRolesAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var userRoles = await session.QueryAsync<UserRole>(r => r.UserId == user.Id, cancellationToken).ConfigureAwait(false);
        return [.. userRoles.Select(r => r.RoleName)];
    }

    public async Task<bool> IsInRoleAsync(PlotboekUser user, string roleName, CancellationToken cancellationToken)
    {
        var roles = await GetRolesAsync(user, cancellationToken).ConfigureAwait(false);
        return roles.Any(role => role.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IList<PlotboekUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenSession();
        var roles = await session.QueryAsync<UserRole>(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase), cancellationToken).ConfigureAwait(false);
        var userIds = roles.Select(r => r.UserId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var users = await session.QueryAsync<PlotboekUser>(u => userIds.Contains(u.Id), cancellationToken).ConfigureAwait(false);
        return [.. users];
    }

    public void Dispose()
    {
    }
}
