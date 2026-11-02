namespace PlotboekGenerator.WebApp.Pages;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        LogUserAuthenticationDetails();

        if (ShouldRedirectToPlotboek())
        {
            return RedirectToPage("/Plotboek");
        }

        return Page();
    }

    private void LogUserAuthenticationDetails()
    {
        _logger.LogInformation("User authenticated: {UserIdentityIsAuthenticated}", User.Identity?.IsAuthenticated);
        _logger.LogInformation("User name: {UserIdentityName}", User.Identity?.Name);

        if (User.Identity?.IsAuthenticated != true)
            return;

        var userRoles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        _logger.LogInformation("User roles: {UserRoles}", string.Join(", ", userRoles));
        _logger.LogInformation("Is in User role: {UserIsInRoleUser}", User.IsInRole(UserRoles.User));
        _logger.LogInformation("Is in NPC role: {UserIsInRoleNpc}", User.IsInRole(UserRoles.NPC));
        _logger.LogInformation("Is in Admin role: {UserIsInRoleAdmin}", User.IsInRole(UserRoles.Admin));
    }

    private bool ShouldRedirectToPlotboek()
    {
        return User.Identity?.IsAuthenticated == true
            && (User.IsInRole(UserRoles.User)
                || User.IsInRole(UserRoles.Admin)
                || User.IsInRole(UserRoles.Reader)
                || User.IsInRole(UserRoles.NPC));
    }
}
