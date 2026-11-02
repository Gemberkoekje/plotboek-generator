namespace PlotboekGenerator.WebApp.Areas.Identity.Pages.Account.Manage;

[Authorize]
public sealed class IndexModel(
    UserManager<PlotboekUser> userManager,
    SignInManager<PlotboekUser> signInManager,
    ILogger<IndexModel> logger) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public string? UserName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        UserName = user.UserName;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (!ValidateUserNameInput())
        {
            return Page();
        }

        var currentUserName = user.UserName;
        if (UserName == currentUserName)
        {
            StatusMessage = "No changes were made.";
            return RedirectToPage();
        }

        if (await IsUserNameTakenByAnotherUserAsync(user.Id))
        {
            return Page();
        }

        if (!await TryApplyUserNameChangeAsync(user, currentUserName))
        {
            return Page();
        }

        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your username has been updated.";
        return RedirectToPage();
    }

    private bool ValidateUserNameInput()
    {
        if (string.IsNullOrWhiteSpace(UserName))
        {
            ModelState.AddModelError(nameof(UserName), "Username is required.");
            return false;
        }

        if (UserName.Length < 3 || UserName.Length > 100)
        {
            ModelState.AddModelError(nameof(UserName), "Username must be between 3 and 100 characters.");
            return false;
        }

        return true;
    }

    private async Task<bool> IsUserNameTakenByAnotherUserAsync(string currentUserId)
    {
        var existingUser = await userManager.FindByNameAsync(UserName!);
        if (existingUser != null && existingUser.Id != currentUserId)
        {
            ModelState.AddModelError(nameof(UserName), "This username is already taken.");
            return true;
        }
        return false;
    }

    private async Task<bool> TryApplyUserNameChangeAsync(PlotboekUser user, string? previousUserName)
    {
        if (!await TrySetUserNameAsync(user))
        {
            return false;
        }

        if (!await TryPersistUserAsync(user, previousUserName))
        {
            return false;
        }

        logger.LogInformation("User {UserId} changed their username from '{OldUserName}' to '{NewUserName}'", user.Id, previousUserName, UserName);
        return true;
    }

    private async Task<bool> TrySetUserNameAsync(PlotboekUser user)
    {
        var result = await userManager.SetUserNameAsync(user, UserName);
        if (!result.Succeeded)
        {
            AddIdentityErrorsToModelState(result.Errors);
            return false;
        }
        return true;
    }

    private async Task<bool> TryPersistUserAsync(PlotboekUser user, string? previousUserName)
    {
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrorsToModelState(result.Errors);
            UserName = previousUserName;
            return false;
        }
        return true;
    }

    private void AddIdentityErrorsToModelState(IEnumerable<IdentityError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
