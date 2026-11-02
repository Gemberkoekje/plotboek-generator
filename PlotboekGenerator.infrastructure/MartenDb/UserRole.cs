namespace PlotboekGenerator.infrastructure.MartenDb;

public sealed record UserRole
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    required public string UserId { get; init; }

    required public string RoleName { get; init; }
}
