using Microsoft.AspNetCore.Authentication;
using PlotboekGenerator.WebApp.Authentication;
using System.Security.Claims;

namespace PlotboekGenerator.WebApp.Areas.Identity.Pages.Account;

[AllowAnonymous] // Add this attribute to allow anonymous access
public sealed class LoginModel(
    SignInManager<PlotboekUser> signInManager,
    UserManager<PlotboekUser> userManager,
    UserRoleMaterializer roleMaterializer,
    IRoleResolver roleResolver,
    ILogger<LoginModel> logger) : PageModel
{
    public IList<AuthenticationScheme> ExternalLogins { get; set; } = default!;

    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];

        logger.LogInformation("External logins available: {ExternalLoginsCount}", ExternalLogins.Count);
        foreach (var login in ExternalLogins)
        {
            logger.LogInformation("  - {LoginName} ({LoginDisplayName})", login.Name, login.DisplayName);
        }

        ReturnUrl = returnUrl;
    }

    // Handle OAuth login initiation
    public async Task<IActionResult> OnPostAsync(string provider, string? returnUrl = null)
    {
        logger.LogInformation("Login.OnPost called with provider: {Provider}", provider);

        // The provider name arrives on the form, and Google/Microsoft are only registered when they
        // are configured, so a page left open across a deploy that retired one can ask for a scheme
        // that no longer exists. Challenging an unregistered scheme throws, which would surface as a
        // 500; check first and re-render the login page showing what is actually on offer.
        var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
        ExternalLogins = [.. schemes];

        if (!ExternalLogins.Any(scheme => string.Equals(scheme.Name, provider, StringComparison.Ordinal)))
        {
            logger.LogWarning("Login requested for provider {Provider}, which is not registered.", provider);
            ErrorMessage = "Deze inlogmethode is niet meer beschikbaar. Kies een van de onderstaande opties.";
            ReturnUrl = returnUrl;
            return Page();
        }

        // Use absolute URL to avoid path base issues
        var redirectUrl = Url.Page("./Login", pageHandler: "Callback", values: new { returnUrl }, protocol: Request.Scheme);
        logger.LogInformation("Redirect URL: {RedirectUrl}", redirectUrl);

        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    // Handle OAuth callback
    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            logger.LogError("External authentication error: {RemoteError}", remoteError);
            // Clear external cookie to prevent stale state on subsequent attempts
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];
            return Page();
        }

        // Get external login info BEFORE clearing the external cookie
        var info = await signInManager.GetExternalLoginInfoAsync();

        // Clear external cookie after getting the info to prevent stale state
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        if (info == null)
        {
            ErrorMessage = "Error loading external login information.";
            logger.LogError("Failed to load external login info");
            ExternalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];
            return Page();
        }

        // Try to sign in the user with this external login provider key
        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity?.Name, info.LoginProvider);

            // Refresh the role the provider grants on each login. A provider that grants none and
            // manual NPC/Admin overrides are left untouched.
            var signedInUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (signedInUser != null)
            {
                await RefreshProviderRoleAsync(signedInUser, info);
                await SignInWithIdTokenAsync(signedInUser, info);
            }

            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out.");
            ErrorMessage = "User account is locked out.";
            ExternalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];
            return Page();
        }

        // If the user does not have an account, create one automatically
        return await CreateUserAndSignInAsync(info, returnUrl);
    }

    private async Task<IActionResult> CreateUserAndSignInAsync(ExternalLoginInfo info, string returnUrl)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;

        if (string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Email claim not received from external provider.";
            logger.LogError("No email claim received from {Provider}", info.LoginProvider);
            ExternalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];
            return Page();
        }

        // If a user with this email already exists (e.g. they previously signed in with a
        // different provider), link this external login to that account instead of creating a
        // duplicate. This preserves the returning user's existing roles (e.g. Admin).
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            var linkResult = await userManager.AddLoginAsync(existingUser, info);
            if (linkResult.Succeeded)
            {
                logger.LogInformation(
                    "Linked {Provider} login to existing user {Id} ({Email})",
                    info.LoginProvider,
                    existingUser.Id,
                    email);

                await RefreshProviderRoleAsync(existingUser, info);
                await SignInWithIdTokenAsync(existingUser, info);
                return LocalRedirect(returnUrl);
            }

            foreach (var error in linkResult.Errors)
            {
                logger.LogError("AddLogin (link to existing) error: {Code} - {Description}", error.Code, error.Description);
            }

            ErrorMessage = "Failed to link your login to the existing account.";
            ExternalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];
            return Page();
        }

        // Create new user
        var user = new PlotboekUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Lidnummer = string.Empty,
            CreatedAt = Clock.UtcNow(),
            UpdatedAt = Clock.UtcNow(),
            IsDummy = false,
            PrimaryDomainId = Guid.Empty,
        };

        var createResult = await userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            logger.LogInformation("User {Email} created with ID {Id}", email, user.Id);

            var addLoginResult = await userManager.AddLoginAsync(user, info);
            if (addLoginResult.Succeeded)
            {
                logger.LogInformation("External login added for user {Id}", user.Id);

                // Materialize the default effective role (Guest, since both role fields are
                // null); RefreshProviderRoleAsync then sets ProviderRole when the provider
                // grants one.
                await roleMaterializer.ApplyAsync(user);
                logger.LogInformation("Default Guest role materialized for user {Id}", user.Id);

                await RefreshProviderRoleAsync(user, info);

                // Reload the user from the database to ensure we have the latest state
                // This is important because UserManager may have updated fields after our store operations
                var freshUser = await userManager.FindByIdAsync(user.Id);
                if (freshUser != null)
                {
                    await SignInWithIdTokenAsync(freshUser, info);
                    logger.LogInformation("User {Email} signed in successfully", email);

                    // Redirect to the return URL
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    logger.LogError("Could not reload user {Id} from database", user.Id);
                }
            }
            else
            {
                foreach (var error in addLoginResult.Errors)
                {
                    logger.LogError("AddLogin error: {Code} - {Description}", error.Code, error.Description);
                }
            }

            await userManager.DeleteAsync(user);
        }

        foreach (var error in createResult.Errors)
        {
            logger.LogError("User creation error: {Code} - {Description}", error.Code, error.Description);
        }

        ErrorMessage = "Failed to create user account.";
        ExternalLogins = [.. await signInManager.GetExternalAuthenticationSchemesAsync()];
        return Page();
    }

    /// <summary>
    /// Signs the user into the local Identity cookie, persisting the provider's <c>id_token</c> so
    /// logout can end the session there too (RP-initiated logout). Also refreshes the role claims.
    /// </summary>
    private async Task SignInWithIdTokenAsync(PlotboekUser user, ExternalLoginInfo info)
    {
        var properties = new AuthenticationProperties { IsPersistent = false };

        var idToken = info.AuthenticationTokens?.FirstOrDefault(token => token.Name == "id_token")?.Value;
        if (idToken is not null)
        {
            properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = idToken }]);
        }

        await signInManager.SignInAsync(user, properties, info.LoginProvider);
    }

    /// <summary>
    /// Refreshes the role the provider grants (see <see cref="IRoleResolver"/>) and materializes the
    /// effective role. A login whose provider grants no role changes nothing. A manual
    /// <see cref="PlotboekUser.OverrideRole"/> keeps winning; only the provider role changes.
    /// </summary>
    private async Task RefreshProviderRoleAsync(PlotboekUser user, ExternalLoginInfo info)
    {
        var resolvedRole = roleResolver.ResolveRole(info);
        if (resolvedRole is null)
        {
            return;
        }

        // Safety net on a user's first login with a role-granting provider: only when it grants no
        // access, preserve their existing elevated role (carried over from another login) as an
        // override so access isn't lost. Users the provider grants a role simply follow — and keep
        // auto-updating from — that role; admins can clear an override later.
        if (user.OverrideRole is null && user.ProviderRole is null &&
            string.Equals(resolvedRole, UserRoles.Guest, StringComparison.Ordinal))
        {
            var currentRole = (await userManager.GetRolesAsync(user)).FirstOrDefault();
            if (currentRole is not null && !string.Equals(currentRole, UserRoles.Guest, StringComparison.Ordinal))
            {
                user.OverrideRole = currentRole;
            }
        }

        user.ProviderRole = resolvedRole;
        await userManager.UpdateAsync(user);
        await roleMaterializer.ApplyAsync(user);

        logger.LogInformation(
            "{Provider} login: ProviderRole={ProviderRole}, override={OverrideRole}, effective={Effective} for user {Id}",
            info.LoginProvider,
            user.ProviderRole,
            user.OverrideRole ?? "(none)",
            user.EffectiveRole(),
            user.Id);
    }
}
