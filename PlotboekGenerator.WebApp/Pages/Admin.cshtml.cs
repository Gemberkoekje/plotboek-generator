using System.Collections.Immutable;
using System.Globalization;
using PlotboekGenerator.WebApp.Resources;

namespace PlotboekGenerator.WebApp.Pages;

[Authorize(Roles = UserRoles.Admin)]
public sealed partial class AdminModel(
    IDocumentStore documentStore,
    PlotboekPaginaRepository paginaRepository,
    UserManager<PlotboekUser> userManager,
    IGoogleDriveService googleDriveService,
    UserRoleMaterializer roleMaterializer,
    IReferenceDataSeed referenceData,
    ILogger<AdminModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Section { get; set; } = "users";

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty]
    public string? EditUserId { get; set; }

    [BindProperty]
    public string? EditUserName { get; set; }

    [BindProperty]
    public string? EditUserRole { get; set; }

    [BindProperty]
    public string? Action { get; set; }

    [BindProperty]
    public string? SourceUserId { get; set; }

    [BindProperty]
    public string? TargetUserId { get; set; }

    [BindProperty]
    public string? LoginIdToUnlink { get; set; }

    // Domain management properties
    [BindProperty]
    public Guid EditDomainId { get; set; }

    [BindProperty]
    public string? EditDomainShorthand { get; set; }

    [BindProperty]
    public string? EditDomainDescription { get; set; }

    [BindProperty]
    public int EditDomainOrder { get; set; }

    [BindProperty]
    public Guid DeleteDomainId { get; set; }

    [BindProperty]
    public Guid EditUserPrimaryDomainId { get; set; }

    // Meeting management properties
    [BindProperty]
    public Guid EditMeetingId { get; set; }

    [BindProperty]
    public string? EditMeetingTijd { get; set; }

    [BindProperty]
    public string? EditMeetingNaam { get; set; }

    [BindProperty]
    public MeetingDagType EditMeetingDagType { get; set; }

    [BindProperty]
    public Guid DeleteMeetingId { get; set; }

    // Character name management properties
    [BindProperty]
    public Guid EditCharacterNameId { get; set; }

    [BindProperty]
    public string? EditCharacterNameNaam { get; set; }

    [BindProperty]
    public Guid DeleteCharacterNameId { get; set; }

    // World name management properties
    [BindProperty]
    public Guid EditWorldNameId { get; set; }

    [BindProperty]
    public string? EditWorldNameNaam { get; set; }

    [BindProperty]
    public Guid DeleteWorldNameId { get; set; }

    // Dummy SL creation properties
    [BindProperty]
    public string? NewDummySlUserName { get; set; }

    // Deleted pagina management properties
    [BindProperty]
    public string? HerstellenPaginaId { get; set; }

    public Dictionary<string, List<DeletedPaginaInfo>> DeletedPaginasByPlotboek { get; set; } = [];

    public Dictionary<string, List<UserInfo>> UsersByRole { get; set; } = [];

    public List<string> AvailableRoles { get; set; } = [.. UserRoles.All];

    public List<UserInfo> AllUsers { get; set; } = [];

    public List<Domain> Domains { get; set; } = [];

    public List<Meeting> EersteDagMeetings { get; set; } = [];

    public List<Meeting> TussendagMeetings { get; set; } = [];

    public List<Meeting> LaatseDagMeetings { get; set; } = [];

    public List<CharacterName> CharacterNames { get; set; } = [];

    public List<WorldName> WorldNames { get; set; } = [];

    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGet()
    {
        await LoadDomains();
        await LoadMeetings();
        await LoadCharacterNames();
        await LoadWorldNames();
        await LoadStandaardNpcs();
        await LoadQuestLocaties();
        await LoadStarttijden();
        await LoadUsers();
        await LoadDeletedPaginas();
        await LoadGoogleDriveStatus();
    }

    public async Task<IActionResult> OnPost()
    {
        // The questlocatie handlers read the current lists to validate against, so load first.
        await LoadQuestLocaties();

        switch (Action)
        {
            case "save":
                await HandleSaveUser();
                break;
            case "linkAccounts":
                await HandleLinkAccounts();
                break;
            case "unlinkLogin":
                await HandleUnlinkLogin();
                break;
            case "saveDomain":
                await HandleSaveDomain();
                break;
            case "deleteDomain":
                await HandleDeleteDomain();
                break;
            case "reorderDomains":
                await HandleReorderDomains();
                break;
            case "createDummySl":
                await HandleCreateDummySl();
                break;
            case "saveMeeting":
                await HandleSaveMeeting();
                break;
            case "deleteMeeting":
                await HandleDeleteMeeting();
                break;
            case "reorderMeetings":
                await HandleReorderMeetings();
                break;
            case "saveCharacterName":
                await HandleSaveCharacterName();
                break;
            case "deleteCharacterName":
                await HandleDeleteCharacterName();
                break;
            case "saveWorldName":
                await HandleSaveWorldName();
                break;
            case "deleteWorldName":
                await HandleDeleteWorldName();
                break;
            case "saveStandaardNpc":
                await HandleSaveStandaardNpc();
                break;
            case "deleteStandaardNpc":
                await HandleDeleteStandaardNpc();
                break;
            case "saveQuestLocatieCategorie":
                await HandleSaveQuestLocatieCategorie();
                break;
            case "saveQuestLocatie":
                await HandleSaveQuestLocatie();
                break;
            case "saveStarttijd":
                await HandleSaveStarttijd();
                break;
            case "herstellenPagina":
                await HandleHerstellenPagina();
                break;
            case "syncGoogleDrive":
                await HandleSyncGoogleDrive();
                break;
        }

        await LoadDomains();
        await LoadMeetings();
        await LoadCharacterNames();
        await LoadWorldNames();
        await LoadStandaardNpcs();
        await LoadQuestLocaties();
        await LoadStarttijden();
        await LoadUsers();
        await LoadDeletedPaginas();
        return Page();
    }

    private async Task HandleSaveDomain()
    {
        if (string.IsNullOrWhiteSpace(EditDomainShorthand) || string.IsNullOrWhiteSpace(EditDomainDescription))
        {
            ErrorMessage = AdminUi.DomainVerplicht;
            return;
        }

        using var session = documentStore.OpenSession();

        if (EditDomainId != Guid.Empty)
        {
            // Update existing domain
            var existingDomain = await session.LoadAsync<Domain>(EditDomainId);
            if (existingDomain != null)
            {
                var updatedDomain = existingDomain with
                {
                    Shorthand = EditDomainShorthand.Trim(),
                    Description = EditDomainDescription.Trim(),
                };
                session.Store(updatedDomain);
                logger.LogInformation("Admin {AdminUser} updated domain {DomainId}: {Shorthand} - {Description}", User.Identity?.Name, EditDomainId, EditDomainShorthand, EditDomainDescription);
                SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.DomainBijgewerkt, EditDomainShorthand);
            }
            else
            {
                ErrorMessage = AdminUi.DomainNietGevonden;
                return;
            }
        }
        else
        {
            // Create new domain
            var existingDomains = await session.QueryAllAsync<Domain>();
            var maxOrder = existingDomains.Count > 0 ? existingDomains.Max(d => d.Order) : 0;

            var newDomain = Domain.Create(EditDomainShorthand.Trim(), EditDomainDescription.Trim(), maxOrder + 1);
            session.Store(newDomain);
            logger.LogInformation("Admin {AdminUser} created domain: {Shorthand} - {Description}", User.Identity?.Name, EditDomainShorthand, EditDomainDescription);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.DomainAangemaakt, EditDomainShorthand);
        }

        await session.SaveChangesAsync();

        // Clear form fields
        EditDomainId = Guid.Empty;
        EditDomainShorthand = null;
        EditDomainDescription = null;
    }

    private async Task HandleDeleteDomain()
    {
        if (DeleteDomainId == Guid.Empty)
        {
            ErrorMessage = AdminUi.DomainNietGeselecteerd;
            return;
        }

        using var session = documentStore.OpenSession();
        var domain = await session.LoadAsync<Domain>(DeleteDomainId);

        if (domain == null)
        {
            ErrorMessage = AdminUi.DomainNietGevonden;
            return;
        }

        session.Delete(domain);
        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} deleted domain {DomainId}: {Shorthand}", User.Identity?.Name, DeleteDomainId, domain.Shorthand);
        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.DomainVerwijderd, domain.Shorthand);
    }

    private async Task HandleReorderDomains()
    {
        // Reorder is handled via JavaScript + form submission with new order values
        // This handler processes a JSON string of domain ID + order pairs from a hidden field
        var reorderData = Request.Form["DomainReorderData"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(reorderData))
        {
            return;
        }

        try
        {
            var reorderItems = System.Text.Json.JsonSerializer.Deserialize<List<DomainReorderItem>>(reorderData);
            if (reorderItems == null || reorderItems.Count == 0)
            {
                return;
            }

            using var session = documentStore.OpenSession();

            foreach (var item in reorderItems)
            {
                var domain = await session.LoadAsync<Domain>(item.Id);
                if (domain != null && domain.Order != item.Order)
                {
                    session.Store(domain with { Order = item.Order });
                }
            }

            await session.SaveChangesAsync();
            logger.LogInformation("Admin {AdminUser} reordered domains", User.Identity?.Name);
            SuccessMessage = AdminUi.DomainsHerordend;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reorder domains");
            ErrorMessage = AdminUi.DomainsHerordenenMislukt;
        }
    }

    private async Task LoadDomains()
    {
        using var session = documentStore.OpenSession();
        var domains = await session.QueryAllAsync<Domain>();
        Domains = [.. domains.OrderBy(d => d.Order)];

        // Seed default domains if none exist
        if (Domains.Count == 0)
        {
            await SeedDefaultDomains();
            domains = await session.QueryAllAsync<Domain>();
            Domains = [.. domains.OrderBy(d => d.Order)];
        }
    }

    private async Task SeedDefaultDomains()
    {
        var defaultDomains = referenceData.Domeinen;

        using var session = documentStore.OpenSession();
        var order = 1;
        foreach (var (shorthand, description) in defaultDomains)
        {
            var domain = Domain.Create(shorthand, description, order++);
            session.Store(domain);
        }
        await session.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} default domains", defaultDomains.Count);
    }

    private sealed record DomainReorderItem(Guid Id, int Order);

    private async Task HandleSaveMeeting()
    {
        if (string.IsNullOrWhiteSpace(EditMeetingTijd) || string.IsNullOrWhiteSpace(EditMeetingNaam))
        {
            ErrorMessage = AdminUi.MeetingVerplicht;
            return;
        }

        using var session = documentStore.OpenSession();

        if (EditMeetingId != Guid.Empty)
        {
            var existing = await session.LoadAsync<Meeting>(EditMeetingId);
            if (existing != null)
            {
                var updated = existing with
                {
                    Tijd = EditMeetingTijd.Trim(),
                    Naam = EditMeetingNaam.Trim(),
                    DagType = EditMeetingDagType,
                };
                session.Store(updated);
                logger.LogInformation("Admin {AdminUser} updated meeting {MeetingId}: {Tijd} {Naam}", User.Identity?.Name, EditMeetingId, EditMeetingTijd, EditMeetingNaam);
                SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.MeetingBijgewerkt, EditMeetingTijd, EditMeetingNaam);
            }
            else
            {
                ErrorMessage = AdminUi.MeetingNietGevonden;
                return;
            }
        }
        else
        {
            var existingMeetings = await session.QueryAsync<Meeting>(m => m.DagType == EditMeetingDagType);
            var maxOrder = existingMeetings.Count > 0 ? existingMeetings.Max(m => m.Order) : 0;

            var newMeeting = Meeting.Create(EditMeetingDagType, EditMeetingTijd.Trim(), EditMeetingNaam.Trim(), maxOrder + 1);
            session.Store(newMeeting);
            logger.LogInformation("Admin {AdminUser} created meeting: {Tijd} {Naam} ({DagType})", User.Identity?.Name, EditMeetingTijd, EditMeetingNaam, EditMeetingDagType);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.MeetingAangemaakt, EditMeetingTijd, EditMeetingNaam);
        }

        await session.SaveChangesAsync();

        EditMeetingId = Guid.Empty;
        EditMeetingTijd = null;
        EditMeetingNaam = null;
    }

    private async Task HandleDeleteMeeting()
    {
        if (DeleteMeetingId == Guid.Empty)
        {
            ErrorMessage = AdminUi.MeetingNietGeselecteerd;
            return;
        }

        using var session = documentStore.OpenSession();
        var meeting = await session.LoadAsync<Meeting>(DeleteMeetingId);

        if (meeting == null)
        {
            ErrorMessage = AdminUi.MeetingNietGevonden;
            return;
        }

        session.Delete(meeting);
        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} deleted meeting {MeetingId}: {Tijd} {Naam}", User.Identity?.Name, DeleteMeetingId, meeting.Tijd, meeting.Naam);
        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.MeetingVerwijderd, meeting.Tijd, meeting.Naam);
    }

    private sealed record MeetingReorderItem(Guid Id, int Order);

    private async Task HandleReorderMeetings()
    {
        var reorderData = Request.Form["MeetingReorderData"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(reorderData))
        {
            return;
        }

        try
        {
            var reorderItems = System.Text.Json.JsonSerializer.Deserialize<List<MeetingReorderItem>>(reorderData);
            if (reorderItems == null || reorderItems.Count == 0)
            {
                return;
            }

            using var session = documentStore.OpenSession();

            foreach (var item in reorderItems)
            {
                var meeting = await session.LoadAsync<Meeting>(item.Id);
                if (meeting != null && meeting.Order != item.Order)
                {
                    session.Store(meeting with { Order = item.Order });
                }
            }

            await session.SaveChangesAsync();
            logger.LogInformation("Admin {AdminUser} reordered meetings", User.Identity?.Name);
            SuccessMessage = AdminUi.MeetingsHerordend;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reorder meetings");
            ErrorMessage = AdminUi.VolgordeOpslaanMislukt;
        }
    }

    private async Task LoadMeetings()
    {
        using var session = documentStore.OpenSession();
        var meetings = await session.QueryAllAsync<Meeting>();

        if (meetings.Count == 0)
        {
            await SeedDefaultMeetings();
            meetings = await session.QueryAllAsync<Meeting>();
        }

        EersteDagMeetings = [.. meetings.Where(m => m.DagType == MeetingDagType.EersteDag).OrderBy(m => m.Order)];
        TussendagMeetings = [.. meetings.Where(m => m.DagType == MeetingDagType.Tussendag).OrderBy(m => m.Order)];
        LaatseDagMeetings = [.. meetings.Where(m => m.DagType == MeetingDagType.LaatseDag).OrderBy(m => m.Order)];
    }

    private async Task SeedDefaultMeetings()
    {
        var defaults = referenceData.Meetings;

        using var session = documentStore.OpenSession();
        var orderByType = new Dictionary<MeetingDagType, int>();

        foreach (var (dagType, tijd, naam) in defaults)
        {
            if (!orderByType.TryGetValue(dagType, out var order))
            {
                order = 0;
            }

            order++;
            orderByType[dagType] = order;

            var meeting = Meeting.Create(dagType, tijd, naam, order);
            session.Store(meeting);
        }

        await session.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} default meetings", defaults.Count);
    }

    private async Task HandleSaveCharacterName()
    {
        if (string.IsNullOrWhiteSpace(EditCharacterNameNaam))
        {
            ErrorMessage = AdminUi.NaamVerplicht;
            return;
        }

        using var session = documentStore.OpenSession();

        if (EditCharacterNameId != Guid.Empty)
        {
            var existing = await session.LoadAsync<CharacterName>(EditCharacterNameId);
            if (existing != null)
            {
                var updated = existing with { Naam = EditCharacterNameNaam.Trim() };
                session.Store(updated);
                logger.LogInformation("Admin {AdminUser} updated character name {Id}: {Naam}", User.Identity?.Name, EditCharacterNameId, EditCharacterNameNaam);
                SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.NaamBijgewerkt, EditCharacterNameNaam);
            }
            else
            {
                ErrorMessage = AdminUi.NaamNietGevonden;
                return;
            }
        }
        else
        {
            var newName = CharacterName.Create(EditCharacterNameNaam.Trim());
            session.Store(newName);
            logger.LogInformation("Admin {AdminUser} created character name: {Naam}", User.Identity?.Name, EditCharacterNameNaam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.NaamAangemaakt, EditCharacterNameNaam);
        }

        await session.SaveChangesAsync();

        EditCharacterNameId = Guid.Empty;
        EditCharacterNameNaam = null;
    }

    private async Task HandleDeleteCharacterName()
    {
        if (DeleteCharacterNameId == Guid.Empty)
        {
            ErrorMessage = AdminUi.NaamNietGeselecteerd;
            return;
        }

        using var session = documentStore.OpenSession();
        var name = await session.LoadAsync<CharacterName>(DeleteCharacterNameId);

        if (name == null)
        {
            ErrorMessage = AdminUi.NaamNietGevonden;
            return;
        }

        session.Delete(name);
        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} deleted character name {Id}: {Naam}", User.Identity?.Name, DeleteCharacterNameId, name.Naam);
        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.NaamVerwijderd, name.Naam);
    }

    private async Task LoadCharacterNames()
    {
        using var session = documentStore.OpenSession();
        var names = await session.QueryAllAsync<CharacterName>();

        if (names.Count == 0)
        {
            await SeedDefaultCharacterNames();
            names = await session.QueryAllAsync<CharacterName>();
        }

        CharacterNames = [.. names.OrderBy(n => n.Naam)];
    }

    private async Task SeedDefaultCharacterNames()
    {
        var defaults = referenceData.KarakterNamen;

        using var session = documentStore.OpenSession();
        foreach (var naam in defaults)
        {
            session.Store(CharacterName.Create(naam));
        }
        await session.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} default character names", defaults.Count);
    }

    private async Task HandleSaveWorldName()
    {
        if (string.IsNullOrWhiteSpace(EditWorldNameNaam))
        {
            ErrorMessage = AdminUi.NaamVerplicht;
            return;
        }

        using var session = documentStore.OpenSession();

        if (EditWorldNameId != Guid.Empty)
        {
            var existing = await session.LoadAsync<WorldName>(EditWorldNameId);
            if (existing != null)
            {
                var updated = existing with { Naam = EditWorldNameNaam.Trim() };
                session.Store(updated);
                logger.LogInformation("Admin {AdminUser} updated world name {Id}: {Naam}", User.Identity?.Name, EditWorldNameId, EditWorldNameNaam);
                SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.WereldBijgewerkt, EditWorldNameNaam);
            }
            else
            {
                ErrorMessage = AdminUi.WereldNietGevonden;
                return;
            }
        }
        else
        {
            var newName = WorldName.Create(EditWorldNameNaam.Trim());
            session.Store(newName);
            logger.LogInformation("Admin {AdminUser} created world name: {Naam}", User.Identity?.Name, EditWorldNameNaam);
            SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.WereldAangemaakt, EditWorldNameNaam);
        }

        await session.SaveChangesAsync();

        EditWorldNameId = Guid.Empty;
        EditWorldNameNaam = null;
    }

    private async Task HandleDeleteWorldName()
    {
        if (DeleteWorldNameId == Guid.Empty)
        {
            ErrorMessage = AdminUi.WereldNietGeselecteerd;
            return;
        }

        using var session = documentStore.OpenSession();
        var name = await session.LoadAsync<WorldName>(DeleteWorldNameId);

        if (name == null)
        {
            ErrorMessage = AdminUi.WereldNietGevonden;
            return;
        }

        session.Delete(name);
        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} deleted world name {Id}: {Naam}", User.Identity?.Name, DeleteWorldNameId, name.Naam);
        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.WereldVerwijderd, name.Naam);
    }

    private async Task LoadWorldNames()
    {
        using var session = documentStore.OpenSession();
        var names = await session.QueryAllAsync<WorldName>();

        if (names.Count == 0)
        {
            await SeedDefaultWorldNames();
            names = await session.QueryAllAsync<WorldName>();
        }

        WorldNames = [.. names.OrderBy(n => n.Naam)];
    }

    private async Task SeedDefaultWorldNames()
    {
        var defaults = referenceData.WereldNamen;

        using var session = documentStore.OpenSession();
        foreach (var naam in defaults)
        {
            session.Store(WorldName.Create(naam));
        }
        await session.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} default world names", defaults.Count);
    }

    private async Task HandleCreateDummySl()
    {
        if (string.IsNullOrWhiteSpace(NewDummySlUserName))
        {
            ErrorMessage = AdminUi.DummySlNaamVerplicht;
            return;
        }

        var userName = NewDummySlUserName.Trim();

        var dummyUser = new PlotboekUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Lidnummer = string.Empty,
            FirstName = string.Empty,
            LastName = string.Empty,
            CreatedAt = Clock.UtcNow(),
            UpdatedAt = Clock.UtcNow(),
            IsDummy = true,
            PrimaryDomainId = Guid.Empty,

            // Pin the dummy as a "User" override so it shows up as an available SL.
            OverrideRole = UserRoles.User,
        };

        using var session = documentStore.OpenSession();
        session.Store(dummyUser);

        // Materialize the effective role ("User") as the dummy's single UserRole document.
        var role = new UserRole
        {
            UserId = dummyUser.Id,
            RoleName = dummyUser.EffectiveRole(),
        };
        session.Store(role);

        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} created dummy SL '{UserName}' with ID {UserId}", User.Identity?.Name, userName, dummyUser.Id);
        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.DummySlAangemaakt, userName);

        // Clear form fields
        NewDummySlUserName = null;
    }

    private async Task HandleSaveUser()
    {
        if (!ValidateEditUserSelected())
            return;

        var user = await userManager.FindByIdAsync(EditUserId!);
        if (user == null)
        {
            ErrorMessage = AdminUi.UserNietGevonden;
            return;
        }

        if (!await TryUpdateUserNameAsync(user))
            return;

        if (!await TryUpdateUserRoleAsync(user))
            return;

        await TryUpdateUserPrimaryDomainAsync(user);

        logger.LogInformation("Admin {AdminUser} updated user {UserId} - UserName: {UserName}, Role: {Role}", User.Identity?.Name, EditUserId, EditUserName, EditUserRole);

        SuccessMessage = AdminUi.UserBijgewerkt;
    }

    internal bool ValidateEditUserSelected()
    {
        if (string.IsNullOrEmpty(EditUserId))
        {
            ErrorMessage = AdminUi.UserNietGeselecteerd;
            return false;
        }
        return true;
    }

    private async Task<bool> TryUpdateUserNameAsync(PlotboekUser user)
    {
        if (string.IsNullOrEmpty(EditUserName) || user.UserName == EditUserName)
            return true;

        var setUserNameResult = await userManager.SetUserNameAsync(user, EditUserName);
        if (!setUserNameResult.Succeeded)
        {
            ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.UsernameBijwerkenMislukt, string.Join(", ", setUserNameResult.Errors.Select(e => e.Description)));
            return false;
        }
        return true;
    }

    private async Task TryUpdateUserPrimaryDomainAsync(PlotboekUser user)
    {
        if (user.PrimaryDomainId == EditUserPrimaryDomainId)
            return;

        user.PrimaryDomainId = EditUserPrimaryDomainId;
        user.UpdatedAt = Clock.UtcNow();

        using var session = documentStore.OpenSession();
        session.Store(user);
        await session.SaveChangesAsync();
    }

    private async Task<bool> TryUpdateUserRoleAsync(PlotboekUser user)
    {
        // EditUserRole is the OVERRIDE role. Empty means "no override" — the effective role then
        // follows the provider's role. Any value pins the effective role regardless of the provider.
        var newOverride = string.IsNullOrWhiteSpace(EditUserRole) ? null : EditUserRole;
        if (newOverride is not null && !AvailableRoles.Contains(newOverride))
        {
            ErrorMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.OnbekendeRol, newOverride);
            return false;
        }

        if (string.Equals(user.OverrideRole, newOverride, StringComparison.Ordinal))
            return true;

        user.OverrideRole = newOverride;
        user.UpdatedAt = Clock.UtcNow();

        using (var session = documentStore.OpenSession())
        {
            session.Store(user);
            await session.SaveChangesAsync();
        }

        // Re-materialize the single UserRole document from the new effective role.
        await roleMaterializer.ApplyAsync(user);
        return true;
    }

    private async Task HandleLinkAccounts()
    {
        if (!ValidateLinkAccountsParameters())
            return;

        await ExecuteLinkAccountsAsync(SourceUserId!, TargetUserId!);
    }

    internal bool ValidateLinkAccountsParameters()
    {
        if (string.IsNullOrEmpty(SourceUserId) || string.IsNullOrEmpty(TargetUserId))
        {
            ErrorMessage = AdminUi.LinkAccountsKiesBeide;
            return false;
        }

        if (SourceUserId == TargetUserId)
        {
            ErrorMessage = AdminUi.LinkAccountsZelfdeUser;
            return false;
        }

        return true;
    }

    private async Task ExecuteLinkAccountsAsync(string sourceUserId, string targetUserId)
    {
        using var session = documentStore.OpenSession();

        var sourceUser = await session.LoadAsync<PlotboekUser>(sourceUserId);
        var targetUser = await session.LoadAsync<PlotboekUser>(targetUserId);

        if (!ValidateUsersExist(sourceUser, targetUser))
            return;

        var sourceLogins = await session.QueryAsync<UserExternalLogin>(l => l.UserId == sourceUserId);

        if (SourceHasLogins(sourceLogins))
        {
            TransferExternalLogins(session, sourceLogins, targetUserId);
        }

        await TransferRolesAsync(session, sourceUserId, targetUserId);
        await UpdatePlotboekPaginaReferencesAsync(session, sourceUserId, targetUserId, targetUser!, User.Identity?.Name ?? "system");
        await DeleteSourceUserAsync(session, sourceUser!, sourceUserId);

        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} linked accounts: merged user {SourceUserId} ({SourceUserName}) into {TargetUserId} ({TargetUserName})", User.Identity?.Name, sourceUserId, sourceUser!.UserName, targetUserId, targetUser!.UserName);

        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.AccountsSamengevoegd, sourceUser.UserName, targetUser.UserName);
    }

    internal bool ValidateUsersExist(PlotboekUser? sourceUser, PlotboekUser? targetUser)
    {
        if (sourceUser == null || targetUser == null)
        {
            ErrorMessage = AdminUi.UsersNietGevonden;
            return false;
        }
        return true;
    }

    internal static bool SourceHasLogins(IReadOnlyList<UserExternalLogin> sourceLogins)
    {
        return sourceLogins.Count > 0;
    }

    internal static void TransferExternalLogins(IDocumentSession session, IReadOnlyList<UserExternalLogin> sourceLogins, string targetUserId)
    {
        foreach (var login in sourceLogins)
        {
            session.Store(login with { UserId = targetUserId });
        }
    }

    private static async Task TransferRolesAsync(IDocumentSession session, string sourceUserId, string targetUserId)
    {
        var sourceRoles = await session.QueryAsync<UserRole>(r => r.UserId == sourceUserId);
        var targetRoles = await session.QueryAsync<UserRole>(r => r.UserId == targetUserId);

        foreach (var sourceRole in sourceRoles.Where(s => !targetRoles.Any(t => t.RoleName.Equals(s.RoleName, StringComparison.OrdinalIgnoreCase))))
        {
            var newRole = new UserRole
            {
                UserId = targetUserId,
                RoleName = sourceRole.RoleName,
            };
            session.Store(newRole);
        }
    }

    /// <summary>
    /// Updates all plotboek pagina references from the source user ID to the target user ID
    /// by loading each affected aggregate, applying proper domain events, and saving through the repository.
    /// This ensures that VerantwoordelijkeSlId, ExtraSlIds, and prop/loot SL references are updated
    /// with a full audit trail in the event store.
    /// </summary>
    private async Task UpdatePlotboekPaginaReferencesAsync(
        IDocumentSession session, string sourceUserId, string targetUserId, PlotboekUser targetUser, string who)
    {
        var targetDisplayName = targetUser.UserName ?? targetUser.Email ?? targetUser.Id;

        var allPaginas = await session.QueryAllAsync<PlotboekPagina.ExportData>();
        var affectedPaginaIds = allPaginas
            .Where(p => p.VerantwoordelijkeSl == sourceUserId
                || (p.ExtraSlIds?.Contains(sourceUserId) == true)
                || (p.PropsEnLoot.Props?.Any(prop => prop.VerantwoordelijkeSlId == sourceUserId) == true)
                || (p.PropsEnLoot.Loot?.Any(loot => loot.VerantwoordelijkeSlId == sourceUserId) == true))
            .Select(p => p.PaginaId)
            .ToList();

        if (affectedPaginaIds.Count == 0)
        {
            return;
        }

        // Build a user name lookup for audit trail names in the events
        var allUsers = await session.QueryAllAsync<PlotboekUser>();
        var userNameLookup = allUsers.ToDictionary(u => u.Id, u => u.UserName ?? u.Email ?? u.Id);
        userNameLookup[targetUserId] = targetDisplayName;

        foreach (var paginaId in affectedPaginaIds)
        {
            var loadResult = await paginaRepository.LoadAsync(paginaId);
            if (!loadResult.IsValid)
            {
                continue;
            }

            var aggregate = loadResult.Value;
            var result = aggregate.VervangenSlUser(sourceUserId, targetUserId, targetDisplayName, userNameLookup, who);
            if (!result.IsValid)
            {
                continue;
            }

            var saveResult = await paginaRepository.SaveAsync(result.Value, CancellationToken.None);
            if (saveResult.IsValid)
            {
                session.Store(result.Value.GetData());
            }
        }
    }

    private static async Task DeleteSourceUserAsync(IDocumentSession session, PlotboekUser sourceUser, string sourceUserId)
    {
        var sourceRoles = await session.QueryAsync<UserRole>(r => r.UserId == sourceUserId);

        foreach (var role in sourceRoles)
        {
            session.Delete(role);
        }
        session.Delete(sourceUser);
    }

    private async Task HandleUnlinkLogin()
    {
        if (!ValidateUnlinkLoginParameters())
            return;

        await ExecuteUnlinkLoginAsync(LoginIdToUnlink!);
    }

    internal bool ValidateUnlinkLoginParameters()
    {
        if (string.IsNullOrEmpty(LoginIdToUnlink))
        {
            ErrorMessage = AdminUi.LoginNietGeselecteerd;
            return false;
        }
        return true;
    }

    private async Task ExecuteUnlinkLoginAsync(string loginId)
    {
        using var session = documentStore.OpenSession();

        var login = await session.LoadAsync<UserExternalLogin>(loginId);
        if (!ValidateExternalLoginExists(login))
            return;

        if (!await ValidateUserHasMultipleLoginsAsync(session, login!))
            return;

        session.Delete(login!);
        await session.SaveChangesAsync();

        logger.LogInformation("Admin {AdminUser} unlinked {Provider} login from user {UserId}", User.Identity?.Name, login!.LoginProvider, login.UserId);

        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.LoginOntkoppeld, login.LoginProvider);
    }

    internal bool ValidateExternalLoginExists(UserExternalLogin? login)
    {
        if (login == null)
        {
            ErrorMessage = AdminUi.LoginNietGevonden;
            return false;
        }
        return true;
    }

    private async Task<bool> ValidateUserHasMultipleLoginsAsync(IDocumentSession session, UserExternalLogin login)
    {
        var userLogins = await session.QueryAsync<UserExternalLogin>(l => l.UserId == login.UserId);

        if (userLogins.Count <= 1)
        {
            ErrorMessage = AdminUi.LaatsteLoginOntkoppelen;
            return false;
        }
        return true;
    }

    private async Task LoadUsers()
    {
        using var session = documentStore.OpenSession();

        var allUsers = await session.QueryAllAsync<PlotboekUser>();
        var allUserRoles = await session.QueryAllAsync<UserRole>();
        var allExternalLogins = await session.QueryAllAsync<UserExternalLogin>();

        var userRolesLookup = BuildUserRolesLookup(allUserRoles);
        var externalLoginsLookup = BuildExternalLoginsLookup(allExternalLogins);

        var filteredUsers = ApplyUserFilter(allUsers);
        PopulateUsersByRole(filteredUsers, userRolesLookup, externalLoginsLookup);
        RemoveEmptyRoleGroups();
    }

    internal static Dictionary<string, List<string>> BuildUserRolesLookup(IReadOnlyList<UserRole> allUserRoles)
    {
        return allUserRoles
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RoleName).ToList());
    }

    internal static Dictionary<string, List<ExternalLoginInfo>> BuildExternalLoginsLookup(IReadOnlyList<UserExternalLogin> allExternalLogins)
    {
        return allExternalLogins
            .GroupBy(l => l.UserId)
            .ToDictionary(g => g.Key, g => g.Select(l => new ExternalLoginInfo
            {
                Id = l.Id,
                Provider = l.LoginProvider,
                Email = l.Email ?? l.ProviderDisplayName ?? AdminUi.LoginEmailOnbekend,
            }).ToList());
    }

    private IEnumerable<PlotboekUser> ApplyUserFilter(IReadOnlyList<PlotboekUser> allUsers)
    {
        if (string.IsNullOrWhiteSpace(Filter))
            return allUsers;

        var filterLower = Filter.ToLowerInvariant();
        return allUsers.Where(u => UserMatchesFilter(u, filterLower));
    }

    internal static bool UserMatchesFilter(PlotboekUser user, string filterLower)
    {
        return (user.UserName?.Contains(filterLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (user.Email?.Contains(filterLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (user.FirstName?.Contains(filterLower, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (user.LastName?.Contains(filterLower, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void PopulateUsersByRole(
        IEnumerable<PlotboekUser> filteredUsers,
        Dictionary<string, List<string>> userRolesLookup,
        Dictionary<string, List<ExternalLoginInfo>> externalLoginsLookup)
    {
        InitializeUsersByRole();
        AllUsers = [];

        var domainLookup = Domains.ToDictionary(d => d.Id, d => d.DisplayName);

        foreach (var user in filteredUsers)
        {
            var userInfo = CreateUserInfo(user, userRolesLookup, externalLoginsLookup, domainLookup);
            AllUsers.Add(userInfo);
            AddUserToRoleGroup(userInfo);
        }
    }

    internal void InitializeUsersByRole()
    {
        UsersByRole = [];
        foreach (var role in AvailableRoles)
        {
            UsersByRole[role] = [];
        }
    }

    internal static UserInfo CreateUserInfo(
        PlotboekUser user,
        Dictionary<string, List<string>> userRolesLookup,
        Dictionary<string, List<ExternalLoginInfo>> externalLoginsLookup,
        Dictionary<Guid, string> domainLookup)
    {
        var roles = userRolesLookup.TryGetValue(user.Id, out var userRoles) ? userRoles : [];
        var logins = externalLoginsLookup.TryGetValue(user.Id, out var userLogins) ? userLogins : [];
        var domainName = user.PrimaryDomainId != Guid.Empty && domainLookup.TryGetValue(user.PrimaryDomainId, out var name) ? name : null;

        return new UserInfo
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = [.. roles],
            ExternalLogins = [.. logins],
            CreatedAt = user.CreatedAt,
            IsDummy = user.IsDummy,
            PrimaryDomainId = user.PrimaryDomainId,
            PrimaryDomainDisplayName = domainName,
            ProviderRole = user.ProviderRole,
            OverrideRole = user.OverrideRole,
        };
    }

    internal void AddUserToRoleGroup(UserInfo userInfo)
    {
        var primaryRole = userInfo.Roles.FirstOrDefault() ?? "Guest";

        if (UsersByRole.TryGetValue(primaryRole, out var list))
        {
            list.Add(userInfo);
        }
        else
        {
            UsersByRole["Guest"].Add(userInfo);
        }
    }

    internal void RemoveEmptyRoleGroups()
    {
        var emptyRoles = UsersByRole.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
        foreach (var emptyRole in emptyRoles)
        {
            UsersByRole.Remove(emptyRole);
        }
    }

    public sealed record UserInfo
    {
        required public string Id { get; init; }
        required public string UserName { get; init; }
        required public string Email { get; init; }
        required public string FirstName { get; init; }
        required public string LastName { get; init; }
        required public ImmutableList<string> Roles { get; init; }
        required public ImmutableList<ExternalLoginInfo> ExternalLogins { get; init; }
        required public DateTime CreatedAt { get; init; }
        required public bool IsDummy { get; init; }
        public Guid PrimaryDomainId { get; init; }
        public string? PrimaryDomainDisplayName { get; init; }

        /// <summary>The role the sign-in provider granted on the last login with it (null if never).</summary>
        public string? ProviderRole { get; init; }

        /// <summary>The manual override role (null = no override; effective role follows the provider).</summary>
        public string? OverrideRole { get; init; }

        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayRoles => Roles.Count != 0 ? string.Join(", ", Roles) : "Guest";
        public string DisplayProviderRole => ProviderRole ?? AdminUi.GeenRol;
        public string DisplayOverrideRole => OverrideRole ?? AdminUi.GeenRol;
        public string DisplayLogins => ExternalLogins.Count != 0
            ? string.Join(", ", ExternalLogins.Select(l => l.Provider))
            : AdminUi.LoginsGeen;
    }

    public sealed record ExternalLoginInfo
    {
        required public string Id { get; init; }
        required public string Provider { get; init; }
        required public string Email { get; init; }
    }

    private async Task LoadDeletedPaginas()
    {
        using var session = documentStore.OpenSession();
        var deletedPaginas = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.IsVerwijderd);

        var plotboeken = await session.QueryAllAsync<Plotboek.ExportData>();
        var plotboekLookup = plotboeken.ToDictionary(p => p.PlotboekId.ToString(), p => p.DisplayName);

        DeletedPaginasByPlotboek = deletedPaginas
            .GroupBy(p => p.PlotboekIdQueryable)
            .ToDictionary(
                g => plotboekLookup.TryGetValue(g.Key, out var name) ? name : g.Key,
                g => g.OrderBy(p => p.Titel).Select(p => new DeletedPaginaInfo
                {
                    PaginaId = p.PaginaId,
                    Titel = p.Titel,
                    DomainShorthand = p.DomainShorthand,
                }).ToList());
    }

    private async Task HandleHerstellenPagina()
    {
        if (string.IsNullOrEmpty(HerstellenPaginaId))
        {
            ErrorMessage = AdminUi.PaginaNietGeselecteerd;
            return;
        }

        var paginaId = PlotboekPaginaId.Parse(HerstellenPaginaId);

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            ErrorMessage = AdminUi.GebruikerNietGevonden;
            return;
        }

        if (await paginaRepository.LoadAsync(paginaId) is not { } plotboekPagina || !plotboekPagina.IsValid)
        {
            ErrorMessage = AdminUi.PaginaNietGevonden;
            return;
        }

        plotboekPagina = plotboekPagina.Value.SetVerwijderd(false, user);
        await paginaRepository.SaveAsync(plotboekPagina.Value, CancellationToken.None);
        session.Store(plotboekPagina.Value.GetData());
        await session.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("Admin {AdminUser} restored deleted pagina {PaginaId}", User.Identity?.Name, paginaId);
        SuccessMessage = string.Format(CultureInfo.CurrentCulture, AdminUi.PaginaHersteld, plotboekPagina.Value.Titel);
    }

    public sealed record DeletedPaginaInfo
    {
        required public PlotboekPaginaId PaginaId { get; init; }
        required public string Titel { get; init; }
        public string? DomainShorthand { get; init; }
    }
}
