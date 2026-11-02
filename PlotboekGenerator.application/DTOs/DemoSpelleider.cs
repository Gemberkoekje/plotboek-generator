namespace PlotboekGenerator.Application.DTOs;

/// <summary>A spelleider in a demo's example, written as a dummy user who cannot sign in.</summary>
public sealed record DemoSpelleider
{
    /// <summary>The user id; a stable key the example's paginas and feedback refer to.</summary>
    required public string Id { get; init; }

    /// <summary>The first name.</summary>
    required public string Voornaam { get; init; }

    /// <summary>The last name.</summary>
    required public string Achternaam { get; init; }

    /// <summary>The shorthand of the domain the spelleider belongs to.</summary>
    required public string DomainShorthand { get; init; }

    /// <summary>The name shown in lists: first and last name.</summary>
    public string DisplayName => $"{Voornaam} {Achternaam}";
}
