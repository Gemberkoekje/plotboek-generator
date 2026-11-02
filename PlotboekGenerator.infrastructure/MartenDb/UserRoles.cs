namespace PlotboekGenerator.infrastructure.MartenDb;

/// <summary>The roles a user can have.</summary>
public static class UserRoles
{
    /// <summary>Signed in, but sees no plotboek.</summary>
    public const string Guest = "Guest";

    /// <summary>Reads the plotboeken that are marked fully done, without their feedback.</summary>
    public const string NPC = "NPC";

    /// <summary>Reads every plotboek, finished or not, with its feedback, and changes nothing.</summary>
    public const string Reader = "Reader";

    /// <summary>Writes plots: a spelleider.</summary>
    public const string User = "User";

    /// <summary>Everything, the admin page included.</summary>
    public const string Admin = "Admin";

    /// <summary>Every role, from the most to the least a user may do.</summary>
    public static IReadOnlyList<string> All { get; } = [Admin, User, Reader, NPC, Guest];
}
