using System.Security.Claims;

namespace PlotboekGenerator.infrastructure.MartenDb;

public sealed record UserClaim
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    required public string UserId { get; init; }

    required public string ClaimType { get; init; }

    required public string ClaimValue { get; init; }

    public Claim ToClaim()
    {
        return new Claim(ClaimType, ClaimValue);
    }
}
