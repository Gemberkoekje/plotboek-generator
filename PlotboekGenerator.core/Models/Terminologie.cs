using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Models;

/// <summary>
/// The words for the few things every LARP names its own way: the crew that runs music, light and
/// smoke, an encounter that needs every NPC, the people who hand out rights. A deployment overrides
/// them under <c>Terminology:*</c>; a word it leaves alone is the neutral one, in the reader's
/// language (<see cref="CoreTekst"/>).
/// </summary>
/// <remarks>
/// One process serves one deployment, so the host picks the terminology once, at startup, with
/// <see cref="Gebruik"/>; everything else reads <see cref="Huidig"/>. An override is one word for every
/// language, since it is the LARP's own name for the thing. The stored names of fields and events do
/// not change with it (an instruction for the crew is still stored as <c>InstructiePentagon</c>): only
/// what people read does.
/// </remarks>
public sealed record Terminologie
{
    private readonly string _techniek;
    private readonly string _instructieTechniek;
    private readonly string _instructieTechniekUitleg;
    private readonly string _geenTechniekInformatie;
    private readonly string _techniekInzet;
    private readonly string _techniekInstructie;
    private readonly string _muziekVanuitTechniek;
    private readonly string _lichtOfRookVanuitTechniek;
    private readonly string _eventBreed;
    private readonly string _eventBreedUitleg;
    private readonly string _organisatie;

    /// <summary>The neutral words.</summary>
    public static Terminologie Standaard { get; } = new ();

    /// <summary>The words of the deployment this process serves.</summary>
    public static Terminologie Huidig { get; private set; } = Standaard;

    /// <summary>The section about the crew that runs music, light and smoke.</summary>
    public string Techniek { get => _techniek ?? CoreTekst.TerminologieTechniek; init => _techniek = Override(value); }

    /// <summary>The field with the instructions for that crew.</summary>
    public string InstructieTechniek { get => _instructieTechniek ?? CoreTekst.TerminologieInstructieTechniek; init => _instructieTechniek = Override(value); }

    /// <summary>The help text under that field.</summary>
    public string InstructieTechniekUitleg { get => _instructieTechniekUitleg ?? CoreTekst.TerminologieInstructieTechniekUitleg; init => _instructieTechniekUitleg = Override(value); }

    /// <summary>Shown when an encounter asks nothing of that crew.</summary>
    public string GeenTechniekInformatie { get => _geenTechniekInformatie ?? CoreTekst.TerminologieGeenTechniekInformatie; init => _geenTechniekInformatie = Override(value); }

    /// <summary>What that crew is asked to do, in a filter ("Met techniek-inzet").</summary>
    public string TechniekInzet { get => _techniekInzet ?? CoreTekst.TerminologieTechniekInzet; init => _techniekInzet = Override(value); }

    /// <summary>The instruction to that crew, in a filter ("Met techniek-instructie").</summary>
    public string TechniekInstructie { get => _techniekInstructie ?? CoreTekst.TerminologieTechniekInstructie; init => _techniekInstructie = Override(value); }

    /// <summary>The icon for music played by that crew.</summary>
    public string MuziekVanuitTechniek { get => _muziekVanuitTechniek ?? CoreTekst.TerminologieMuziekVanuitTechniek; init => _muziekVanuitTechniek = Override(value); }

    /// <summary>The icon for light or smoke from that crew.</summary>
    public string LichtOfRookVanuitTechniek { get => _lichtOfRookVanuitTechniek ?? CoreTekst.TerminologieLichtOfRookVanuitTechniek; init => _lichtOfRookVanuitTechniek = Override(value); }

    /// <summary>An encounter that needs every NPC.</summary>
    public string EventBreed { get => _eventBreed ?? CoreTekst.TerminologieEventBreed; init => _eventBreed = Override(value); }

    /// <summary>The tooltip of that icon.</summary>
    public string EventBreedUitleg { get => _eventBreedUitleg ?? CoreTekst.TerminologieEventBreedUitleg; init => _eventBreedUitleg = Override(value); }

    /// <summary>Who hands out rights, as it reads in a sentence ("Neem contact op met de organisatie").</summary>
    public string Organisatie { get => _organisatie ?? CoreTekst.TerminologieOrganisatie; init => _organisatie = Override(value); }

    /// <summary>Makes <paramref name="terminologie"/> the words of this process.</summary>
    /// <param name="terminologie">The deployment's words.</param>
    public static void Gebruik(Terminologie terminologie)
        => Huidig = terminologie ?? throw new ArgumentNullException(nameof(terminologie));

    /// <summary>An empty override keeps the neutral word.</summary>
    private static string Override(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
