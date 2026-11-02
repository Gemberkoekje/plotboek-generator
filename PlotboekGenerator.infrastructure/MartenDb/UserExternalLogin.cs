namespace PlotboekGenerator.infrastructure.MartenDb;

public sealed record UserExternalLogin
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public string UserId { get; init; } = string.Empty;

    public string LoginProvider { get; init; } = string.Empty;

    public string ProviderKey { get; init; } = string.Empty;

    public string ProviderDisplayName { get; init; }

    public string Email { get; init; }
}
