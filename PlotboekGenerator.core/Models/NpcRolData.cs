namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents a single NPC role with a minimum/maximum count and additional metadata.
/// </summary>
/// <remarks>
/// Examples:
/// - 1-4 handelaars → MinimumAantal: 1, MaximumAantal: 4, Rol: "handelaars"
/// - 1 guard → MinimumAantal: 1, MaximumAantal: null, Rol: "guard"
/// - 0-2 bodyguards (optional) → MinimumAantal: 0, MaximumAantal: 2, Rol: "bodyguards"
/// - Vechters (no count, for "all NPCs") → MinimumAantal: 0, MaximumAantal: null, Rol: "Vechters"
///
/// Validation rules:
/// - If no maximum is set and not in "all NPCs" mode, minimum cannot be 0
/// - If minimum equals maximum, display as single value
/// - For "all NPCs" mode, both min and max can be empty (just role name)
/// </remarks>
public sealed record NpcRolData
{
    /// <summary>
    /// The name/description of the NPC role (e.g., "handelaar", "bodyguard", "edelman").
    /// </summary>
    required public string Rol { get; init; }

    /// <summary>
    /// Minimum number of NPCs needed for this role (can be 0 if optional with a maximum set, or for "all NPCs" mode).
    /// </summary>
    required public int MinimumAantal { get; init; }

    /// <summary>
    /// Maximum number of NPCs for this role (optional, if not set the minimum is treated as exact).
    /// </summary>
    public int? MaximumAantal { get; init; }

    /// <summary>
    /// Whether this role is recurring after this plot.
    /// </summary>
    public bool IsTerukerend { get; init; }

    /// <summary>
    /// Whether this role was played before.
    /// </summary>
    public bool IsEerderGespeeld { get; init; }

    /// <summary>
    /// Who played this role before (only relevant if IsEerderGespeeld is true).
    /// </summary>
    public string GespeeldDoor { get; init; }

    /// <summary>
    /// Whether someone is already chosen/briefed for this role.
    /// </summary>
    public bool IsAlGekozen { get; init; }

    /// <summary>
    /// Who is chosen/briefed for this role (only relevant if IsAlGekozen is true).
    /// </summary>
    public string GekozenPersoon { get; init; }

    /// <summary>
    /// Clothing and makeup description for this NPC role (e.g., "malienkolder, wapens, schilden").
    /// </summary>
    public string KledingEnSchmink { get; init; }

    /// <summary>
    /// What kind of figurant is wanted for this role (e.g., "vrouwelijk uiterlijk, goede roleplayer,
    /// stevig postuur"). Free text; kept apart from <see cref="KledingEnSchmink"/> and <see cref="Rol"/>
    /// so casting wishes no longer have to be squeezed into either.
    /// </summary>
    public string CastingInstructie { get; init; }

    /// <summary>
    /// Whether this role asks something of the figurant that has to be agreed on beforehand
    /// (e.g., being tortured, intimacy, heavy fear content).
    /// </summary>
    public bool VereistAfstemming { get; init; }

    /// <summary>
    /// What exactly has to be agreed on beforehand (only relevant if <see cref="VereistAfstemming"/> is true).
    /// </summary>
    public string AfstemmingToelichting { get; init; }

    public bool HasValue => !string.IsNullOrWhiteSpace(Rol);
    public bool IsRange => MaximumAantal.HasValue && MinimumAantal != MaximumAantal;
    public bool IsOptional => MinimumAantal == 0 && MaximumAantal.HasValue;
    public bool HasCount => MinimumAantal > 0 || MaximumAantal.HasValue;

    public static NpcRolData Empty => new ()
    {
        Rol = string.Empty,
        MinimumAantal = 0,
    };

    public static NpcRolData Create(
        string rol,
        int minimumAantal,
        int? maximumAantal = null,
        bool isTerukerend = false,
        bool isEerderGespeeld = false,
        string gespeeldDoor = null,
        bool isAlGekozen = false,
        string gekozenPersoon = null,
        string kledingEnSchmink = null,
        string castingInstructie = null,
        bool vereistAfstemming = false,
        string afstemmingToelichting = null)
    {
        return new NpcRolData
        {
            Rol = rol ?? string.Empty,
            MinimumAantal = minimumAantal,
            MaximumAantal = maximumAantal,
            IsTerukerend = isTerukerend,
            IsEerderGespeeld = isEerderGespeeld,
            GespeeldDoor = isEerderGespeeld ? gespeeldDoor : null,
            IsAlGekozen = isAlGekozen,
            GekozenPersoon = isAlGekozen ? gekozenPersoon : null,
            KledingEnSchmink = kledingEnSchmink,
            CastingInstructie = castingInstructie,
            VereistAfstemming = vereistAfstemming,

            // Same convention as GespeeldDoor/GekozenPersoon: the free text only survives while its flag is on.
            AfstemmingToelichting = vereistAfstemming ? afstemmingToelichting : null,
        };
    }

    public override string ToString()
    {
        if (!HasValue)
            return string.Empty;

        var parts = new List<string>();

        // Build the count + role part
        string countPart;
        if (!HasCount)
        {
            // No count specified (for "all NPCs" scenarios) - just the role name
            countPart = Rol;
        }
        else if (IsRange)
        {
            countPart = $"{MinimumAantal}-{MaximumAantal} {Rol}";
        }
        else
        {
            countPart = $"{MinimumAantal} {Rol}";
        }

        parts.Add(countPart);

        // Add metadata annotations
        var annotations = new List<string>();

        if (IsTerukerend)
            annotations.Add("TERUGKEREND");

        if (IsEerderGespeeld)
        {
            annotations.Add(string.IsNullOrWhiteSpace(GespeeldDoor)
                ? "eerder gespeeld"
                : $"eerder gespeeld door {GespeeldDoor}");
        }

        if (IsAlGekozen)
        {
            annotations.Add(string.IsNullOrWhiteSpace(GekozenPersoon)
                ? "al gekozen"
                : $"gekozen: {GekozenPersoon}");
        }

        if (annotations.Count > 0)
        {
            parts.Add($"({string.Join(", ", annotations)})");
        }

        return string.Join(" ", parts);
    }
}
