namespace PlotboekGenerator.core.Models;

/// <summary>
/// A generic NPC type that comes back in many plots (a stadswacht, a bandiet), saved once so an SL can
/// add it to a plot as a filled-in role instead of copying costume and casting text around.
/// </summary>
/// <remarks>
/// A template, not a reference: picking one copies its fields into a new <see cref="NpcRolData"/>, and
/// nothing in a plot points back here. Editing or deleting a standaard-NPC therefore never changes a
/// plot, which is also why the <see cref="Id"/> may follow the name (see <see cref="IdVoor"/>).
/// </remarks>
public sealed record StandaardNpc
{
    /// <summary>The key, derived from <see cref="Rol"/> by <see cref="IdVoor"/>.</summary>
    required public string Id { get; init; }

    /// <summary>The role name as the SL typed it, trimmed (e.g. "Stadswacht").</summary>
    required public string Rol { get; init; }

    /// <summary>Clothing and makeup, copied into <see cref="NpcRolData.KledingEnSchmink"/>.</summary>
    public string KledingEnSchmink { get; init; } = string.Empty;

    /// <summary>What kind of figurant is wanted, copied into <see cref="NpcRolData.CastingInstructie"/>.</summary>
    public string CastingInstructie { get; init; } = string.Empty;

    /// <summary>
    /// Display name of whoever saved it last. Any SL may overwrite a standaard-NPC, so the admin wants to
    /// see who did.
    /// </summary>
    public string GewijzigdDoor { get; init; } = string.Empty;

    /// <summary>When it was saved last (UTC).</summary>
    public DateTime GewijzigdOp { get; init; }

    /// <summary>
    /// The key for a role name. Case, surrounding spaces and diacritics don't count, so "Stadswacht",
    /// " stadswacht " and "STADSWACHT" are the same standaard-NPC.
    /// </summary>
    /// <param name="rol">The role name.</param>
    /// <returns>The key, or an empty string when the name holds no letters or digits.</returns>
    public static string IdVoor(string rol)
        => ReferenceDataId.Derive((rol ?? string.Empty).Trim().ToLowerInvariant());

    /// <summary>
    /// Creates a standaard-NPC from the fields of a role.
    /// </summary>
    /// <param name="rol">The role name; must contain a letter or digit.</param>
    /// <param name="kledingEnSchmink">Clothing and makeup; may be empty.</param>
    /// <param name="castingInstructie">Casting wishes; may be empty.</param>
    /// <param name="gewijzigdDoor">Display name of the user saving it.</param>
    /// <param name="gewijzigdOp">When it is saved (UTC).</param>
    /// <returns>The new standaard-NPC with every text trimmed.</returns>
    /// <exception cref="ArgumentException">When no key can be derived from <paramref name="rol"/>; callers validate that first.</exception>
    public static StandaardNpc Create(string rol, string kledingEnSchmink, string castingInstructie, string gewijzigdDoor, DateTime gewijzigdOp)
    {
        var id = IdVoor(rol);
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("A standaard-NPC needs a role name with at least one letter or digit.", nameof(rol));
        }

        return new StandaardNpc
        {
            Id = id,
            Rol = rol.Trim(),
            KledingEnSchmink = kledingEnSchmink?.Trim() ?? string.Empty,
            CastingInstructie = castingInstructie?.Trim() ?? string.Empty,
            GewijzigdDoor = gewijzigdDoor ?? string.Empty,
            GewijzigdOp = gewijzigdOp,
        };
    }
}
