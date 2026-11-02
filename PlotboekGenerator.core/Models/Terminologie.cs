namespace PlotboekGenerator.core.Models;

/// <summary>
/// The words for the few things every LARP names its own way: the crew that runs music, light and
/// smoke, an encounter that needs every NPC, the people who hand out rights. The defaults are
/// neutral Dutch; a deployment overrides them under <c>Terminology:*</c>.
/// </summary>
/// <remarks>
/// One process serves one deployment, so the host picks the terminology once, at startup, with
/// <see cref="Gebruik"/>; everything else reads <see cref="Huidig"/>. The stored names of fields and
/// events do not change with it (an instruction for the crew is still stored as
/// <c>InstructiePentagon</c>): only what people read does.
/// </remarks>
public sealed record Terminologie
{
    /// <summary>The neutral words.</summary>
    public static Terminologie Standaard { get; } = new ();

    /// <summary>The words of the deployment this process serves.</summary>
    public static Terminologie Huidig { get; private set; } = Standaard;

    /// <summary>The section about the crew that runs music, light and smoke.</summary>
    public string Techniek { get; init; } = "Techniek";

    /// <summary>The field with the instructions for that crew.</summary>
    public string InstructieTechniek { get; init; } = "Instructie techniek";

    /// <summary>The help text under that field.</summary>
    public string InstructieTechniekUitleg { get; init; } = "Instructies voor de techniek (muziek, licht, rook, etc.).";

    /// <summary>Shown when an encounter asks nothing of that crew.</summary>
    public string GeenTechniekInformatie { get; init; } = "Geen techniek informatie";

    /// <summary>What that crew is asked to do, in a filter ("Met techniek-inzet").</summary>
    public string TechniekInzet { get; init; } = "techniek-inzet";

    /// <summary>The instruction to that crew, in a filter ("Met techniek-instructie").</summary>
    public string TechniekInstructie { get; init; } = "techniek-instructie";

    /// <summary>The icon for music played by that crew.</summary>
    public string MuziekVanuitTechniek { get; init; } = "Muziek vanuit de techniek";

    /// <summary>The icon for light or smoke from that crew.</summary>
    public string LichtOfRookVanuitTechniek { get; init; } = "Specifiek licht/rook vanuit de techniek";

    /// <summary>An encounter that needs every NPC.</summary>
    public string EventBreed { get; init; } = "Eventbreed";

    /// <summary>The tooltip of that icon.</summary>
    public string EventBreedUitleg { get; init; } = "Eventbreed (grote groepen spelers)";

    /// <summary>Who hands out rights, as it reads in a sentence ("Neem contact op met de organisatie").</summary>
    public string Organisatie { get; init; } = "de organisatie";

    /// <summary>Makes <paramref name="terminologie"/> the words of this process.</summary>
    /// <param name="terminologie">The deployment's words.</param>
    public static void Gebruik(Terminologie terminologie)
        => Huidig = terminologie ?? throw new ArgumentNullException(nameof(terminologie));
}
