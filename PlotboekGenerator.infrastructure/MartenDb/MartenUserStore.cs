using Marten;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace PlotboekGenerator.infrastructure.MartenDb;

public class MartenUserStore(Marten.IDocumentStore documentStore) : IUserEmailStore<PlotboekUser>,
    IUserPasswordStore<PlotboekUser>, IUserSecurityStampStore<PlotboekUser>,
    IUserClaimStore<PlotboekUser>, IUserLoginStore<PlotboekUser>, IUserRoleStore<PlotboekUser>
{
    public async Task<IdentityResult> CreateAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        // Ensure user has an ID - IdentityUser uses string IDs
        if (string.IsNullOrEmpty(user.Id))
        {
            user.Id = Guid.NewGuid().ToString();
        }

        using var session = documentStore.LightweightSession();
        session.Store(user);
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        user.UpdatedAt = Clock.UtcNow();
        using var session = documentStore.LightweightSession();
        session.Store(user);
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();
        session.Delete(user);
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<PlotboekUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();
        return await session.LoadAsync<PlotboekUser>(userId, cancellationToken);
    }

    public async Task<PlotboekUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();
        return await session.Query<PlotboekUser>()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public async Task<PlotboekUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();
        return await session.Query<PlotboekUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    // Implement other required methods...
    public Task<string> GetUserIdAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string> GetUserNameAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(PlotboekUser user, string userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string> GetNormalizedUserNameAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(PlotboekUser user, string normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    // Add other required interface implementations...
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    // Email store methods
    public Task SetEmailAsync(PlotboekUser user, string email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string> GetEmailAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(PlotboekUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<string> GetNormalizedEmailAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(PlotboekUser user, string normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // Password store methods
    public Task SetPasswordHashAsync(PlotboekUser user, string passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string> GetPasswordHashAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    // Security stamp methods
    public Task SetSecurityStampAsync(PlotboekUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string> GetSecurityStampAsync(PlotboekUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp ?? string.Empty);

    // Claims store methods
    public async Task<IList<Claim>> GetClaimsAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();
        var userClaims = await session.Query<UserClaim>()
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);

        return [.. userClaims.Select(c => c.ToClaim())];
    }

    public async Task AddClaimsAsync(PlotboekUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        foreach (var claim in claims)
        {
            var userClaim = new UserClaim
            {
                UserId = user.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value,
            };
            session.Store(userClaim);
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceClaimAsync(PlotboekUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var existingClaim = await session.Query<UserClaim>()
            .FirstOrDefaultAsync(
                c => c.UserId == user.Id &&
                c.ClaimType == claim.Type &&
                c.ClaimValue == claim.Value,
                cancellationToken);

        if (existingClaim != null)
        {
            session.Delete(existingClaim);
        }

        var replacement = new UserClaim
        {
            UserId = user.Id,
            ClaimType = newClaim.Type,
            ClaimValue = newClaim.Value,
        };
        session.Store(replacement);

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveClaimsAsync(PlotboekUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        foreach (var claim in claims)
        {
            var userClaim = await session.Query<UserClaim>()
                .FirstOrDefaultAsync(
                    c => c.UserId == user.Id &&
                    c.ClaimType == claim.Type &&
                    c.ClaimValue == claim.Value,
                    cancellationToken);

            if (userClaim != null)
            {
                session.Delete(userClaim);
            }
        }

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task<IList<PlotboekUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        var userClaims = await session.Query<UserClaim>()
            .Where(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value)
            .ToListAsync(cancellationToken);

        var userIds = userClaims.Select(c => c.UserId).ToList();
        var users = await session.Query<PlotboekUser>()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return [.. users];
    }

    // Login store methods
    public async Task AddLoginAsync(PlotboekUser user, UserLoginInfo login, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var externalLogin = new UserExternalLogin
        {
            UserId = user.Id,
            LoginProvider = login.LoginProvider,
            ProviderKey = login.ProviderKey,
            ProviderDisplayName = login.ProviderDisplayName,
            Email = user.Email, // Store the user's email with the external login
        };

        session.Store(externalLogin);
        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveLoginAsync(PlotboekUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var loginToRemove = await session.Query<UserExternalLogin>()
            .FirstOrDefaultAsync(
                l => l.UserId == user.Id &&
                l.LoginProvider == loginProvider &&
                l.ProviderKey == providerKey, cancellationToken);

        if (loginToRemove != null)
        {
            session.Delete(loginToRemove);
            await session.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IList<UserLoginInfo>> GetLoginsAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        var externalLogins = await session.Query<UserExternalLogin>()
            .Where(l => l.UserId == user.Id)
            .ToListAsync(cancellationToken);

        return [.. externalLogins.Select(l => new UserLoginInfo(
            l.LoginProvider,
            l.ProviderKey,
            l.ProviderDisplayName))];
    }

    public async Task<PlotboekUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        // First find the external login
        var externalLogin = await session.Query<UserExternalLogin>()
            .FirstOrDefaultAsync(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey, cancellationToken);

        if (externalLogin == null)
            return null;

        // Then find the user by ID
        return await session.LoadAsync<PlotboekUser>(externalLogin.UserId, cancellationToken);
    }

    // Role store methods
    public async Task AddToRoleAsync(PlotboekUser user, string roleName, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var userRoles = await session.Query<UserRole>()
            .Where(r => r.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (!userRoles.Any(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleName = roleName,
            };

            session.Store(userRole);
            await session.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveFromRoleAsync(PlotboekUser user, string roleName, CancellationToken cancellationToken)
    {
        using var session = documentStore.LightweightSession();

        var userRoles = await session.Query<UserRole>()
            .Where(r => r.UserId == user.Id)
            .ToListAsync(cancellationToken);

        var roleToRemove = userRoles.FirstOrDefault(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));

        if (roleToRemove != null)
        {
            session.Delete(roleToRemove);
            await session.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IList<string>> GetRolesAsync(PlotboekUser user, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        var userRoles = await session.Query<UserRole>()
            .Where(r => r.UserId == user.Id)
            .ToListAsync(cancellationToken);

        return [.. userRoles.Select(r => r.RoleName)];
    }

    public async Task<bool> IsInRoleAsync(PlotboekUser user, string roleName, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        var userRoles = await session.Query<UserRole>()
            .Where(r => r.UserId == user.Id)
            .ToListAsync(cancellationToken);

        return userRoles.Any(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IList<PlotboekUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        using var session = documentStore.QuerySession();

        var allRoles = await session.Query<UserRole>().ToListAsync(cancellationToken);
        var userIds = allRoles
            .Where(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.UserId)
            .Distinct()
            .ToList();

        var users = await session.Query<PlotboekUser>()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return [.. users];
    }
}
