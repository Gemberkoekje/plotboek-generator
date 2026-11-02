using System.Globalization;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Models;

/// <summary>
/// Represents the complete NPC configuration for an encounter, supporting multiple NPC roles.
/// </summary>
/// <remarks>
/// Examples:
/// - 1-4 handelaars en 0-2 bodyguards = total 1-6 NPCs needed
/// - 2 guards (TERUGKEREND) en 1 edelman (eerder gespeeld door Jan)
/// - ALLE NPCs: Vechters, 4-6 Chaos wezens (for big battles)
/// </remarks>
public readonly record struct AantalNpcsData : IEquatable<AantalNpcsData>
{
    /// <summary>
    /// Indicates that ALL available NPCs are needed for this encounter (e.g., big battles).
    /// </summary>
    public bool AlleNpcs { get; init; }

    /// <summary>
    /// Indicates that this encounter explicitly needs NO NPCs. Mutually exclusive with
    /// <see cref="AlleNpcs"/> and <see cref="Rollen"/>, and counts as fully-specified NPC info
    /// (so the pagina can be marked "volledig klaar").
    /// </summary>
    public bool GeenNpcsNodig { get; init; }

    public IReadOnlyList<NpcRolData> Rollen { get; init; }

    public bool HasValue => GeenNpcsNodig || AlleNpcs || (Rollen?.Count > 0 && Rollen.Any(r => r.HasValue));

    /// <summary>
    /// Whether at least one role needs something agreed with the figurant beforehand. Derived, never
    /// stored: it drives the 🤝 icon in the plot list and in the docx, so no extra flag or event is needed.
    /// </summary>
    public bool HeeftAfstemmingNodig => Rollen?.Any(r => r.HasValue && r.VereistAfstemming) == true;

    /// <summary>
    /// Gets the total minimum number of NPCs needed across all roles.
    /// </summary>
    public int TotaalMinimum => Rollen?.Sum(r => r.MinimumAantal) ?? 0;

    /// <summary>
    /// Gets the total maximum number of NPCs needed across all roles (null if any role has no maximum).
    /// </summary>
    public int? TotaalMaximum => Rollen?.All(r => r.MaximumAantal.HasValue) == true
        ? Rollen.Sum(r => r.MaximumAantal ?? r.MinimumAantal)
        : Rollen?.Sum(r => r.MaximumAantal ?? r.MinimumAantal);

    public static AantalNpcsData Empty => new ()
    {
        AlleNpcs = false,
        Rollen = [],
    };

    public static AantalNpcsData Create(IEnumerable<NpcRolData> rollen, bool alleNpcs = false, bool geenNpcsNodig = false)
    {
        // "Geen NPC's nodig" is mutually exclusive with any roles or "ALLE NPCs".
        if (geenNpcsNodig)
        {
            return new AantalNpcsData
            {
                GeenNpcsNodig = true,
                AlleNpcs = false,
                Rollen = [],
            };
        }

        return new AantalNpcsData
        {
            AlleNpcs = alleNpcs,
            Rollen = rollen?.Where(r => r.HasValue).ToList() ?? [],
        };
    }

    public bool Equals(AantalNpcsData other)
    {
        return GeenNpcsNodig == other.GeenNpcsNodig && AlleNpcs == other.AlleNpcs && SequenceEqual(Rollen, other.Rollen);
    }

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(GeenNpcsNodig);
        hash.Add(AlleNpcs);
        if (Rollen != null)
        {
            foreach (var item in Rollen)
                hash.Add(item);
        }
        return hash.ToHashCode();
    }

    private static bool SequenceEqual(IReadOnlyList<NpcRolData> left, IReadOnlyList<NpcRolData> right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        if (left.Count != right.Count)
            return false;
        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
                return false;
        }
        return true;
    }

    public override string ToString()
    {
        if (GeenNpcsNodig)
            return CoreTekst.AantalNpcsGeenNodig;

        if (!HasValue)
            return string.Empty;

        var parts = new List<string>();

        if (AlleNpcs)
        {
            parts.Add(CoreTekst.NpcsAlle);
        }

        var validRollen = Rollen?.Where(r => r.HasValue).ToList() ?? [];
        if (validRollen.Count > 0)
        {
            parts.AddRange(validRollen.Select(r => $"• {r}"));

            // Add total if multiple roles and not "all NPCs"
            if (!AlleNpcs && validRollen.Count > 1)
            {
                var totalText = string.Format(
                    CultureInfo.CurrentCulture,
                    IsPreciesEen ? CoreTekst.AantalNpcsTotaalOne : CoreTekst.AantalNpcsTotaalMany,
                    AantalTekst());
                parts.Add(totalText);
            }
        }

        return string.Join("\n", parts);
    }

    /// <summary>
    /// Returns a compact single-line string representation for display in lists.
    /// </summary>
    public string ToShortString()
    {
        if (GeenNpcsNodig)
            return "0";

        if (!HasValue)
            return string.Empty;

        if (AlleNpcs)
        {
            var validRollen = Rollen?.Where(r => r.HasValue).ToList() ?? [];
            if (validRollen.Count == 0)
                return CoreTekst.NpcsAlle;

            var metRollen = validRollen.Count == 1 ? CoreTekst.NpcsAlleMetRollenOne : CoreTekst.NpcsAlleMetRollenMany;
            return string.Format(CultureInfo.CurrentCulture, metRollen, validRollen.Count);
        }

        var roles = Rollen?.Where(r => r.HasValue).ToList() ?? [];
        if (roles.Count == 0)
            return string.Empty;

        if (roles.Count == 1)
        {
            var r = roles[0];
            return r.IsRange ? $"{r.MinimumAantal}-{r.MaximumAantal} {r.Rol}" : $"{r.MinimumAantal} {r.Rol}";
        }

        // Multiple roles: show summary
        return string.Format(
            CultureInfo.CurrentCulture,
            IsPreciesEen ? CoreTekst.AantalNpcsMeerdereRollenOne : CoreTekst.AantalNpcsMeerdereRollenMany,
            AantalTekst(),
            roles.Count);
    }

    /// <summary>
    /// Returns the per-role casting, agreement and clothing/makeup descriptions for the docx export,
    /// as up to three blocks separated by a blank line and ordered by urgency.
    /// </summary>
    /// <remarks>
    /// The headings are dropped in exactly one case: nothing but clothing, which is the shape every
    /// pre-casting plotboek has. Those therefore read byte-for-byte as they always did:
    /// <code>
    /// Guards: malienkolder, wapens, schilden
    /// Edelman: deftige kleding
    /// </code>
    /// As soon as any casting or agreement text is present every block keeps its heading — including when
    /// it is the only block — so the row label in the template ("Kleding &amp; Schmink") no longer being the
    /// whole truth stays visible inside the cell, and an agreement note can never pass for a costume note:
    /// <code>
    /// 🤝 Vereist afstemming vooraf:
    /// Guards: wordt gemarteld in scène 2
    ///
    /// Casting:
    /// Guards: goede roleplayer, stevig postuur
    ///
    /// Kleding &amp; schmink:
    /// Guards: malienkolder, wapens, schilden
    /// </code>
    /// </remarks>
    /// <returns>The formatted cell text, or an empty string when no role carries any of the three.</returns>
    public string ToKledingEnSchminkString()
    {
        if (GeenNpcsNodig)
            return CoreTekst.AantalNpcsGeenNodig;

        var validRollen = Rollen?.Where(r => r.HasValue).ToList() ?? [];
        if (validRollen.Count == 0)
            return string.Empty;

        // A role that only needs agreeing on still belongs in the list, hence the role name on its own
        // when nobody wrote down what has to be agreed.
        var afstemming = validRollen
            .Where(r => r.VereistAfstemming)
            .Select(r => string.IsNullOrWhiteSpace(r.AfstemmingToelichting)
                ? OneLine(r.Rol)
                : $"{OneLine(r.Rol)}: {OneLine(r.AfstemmingToelichting)}")
            .ToList();
        var casting = validRollen
            .Where(r => !string.IsNullOrWhiteSpace(r.CastingInstructie))
            .Select(r => $"{OneLine(r.Rol)}: {OneLine(r.CastingInstructie)}")
            .ToList();
        var kleding = validRollen
            .Where(r => !string.IsNullOrWhiteSpace(r.KledingEnSchmink))
            .Select(r => $"{OneLine(r.Rol)}: {OneLine(r.KledingEnSchmink)}")
            .ToList();

        if (afstemming.Count == 0 && casting.Count == 0 && kleding.Count == 0)
            return string.Empty;

        // Only the kleding-only case may drop its heading. That is the pre-casting shape, so those
        // plotboeken stay byte-for-byte what they always were. Casting and — above all — afstemming
        // always keep their heading, even when alone: without it a consent note lands unlabelled in a
        // cell called "Kleding & Schmink" and reads as a costume detail.
        if (afstemming.Count == 0 && casting.Count == 0)
            return string.Join("\n", kleding);

        var blokken = new[]
        {
            ($"\U0001F91D {CoreTekst.KledingEnSchminkAfstemmingKop}", afstemming),
            (CoreTekst.KledingEnSchminkCastingKop, casting),
            (CoreTekst.KledingEnSchminkKledingKop, kleding),
        }
        .Where(b => b.Item2.Count > 0)
        .ToList();

        return string.Join("\n\n", blokken.Select(b => $"{b.Item1}\n{string.Join("\n", b.Item2)}"));
    }

    /// <summary>
    /// Collapses any line break in user-typed text to a single space. The export cell uses "\n" and
    /// "\n\n" as its line and block separators, so a newline that survives into a value would forge a
    /// block boundary or a heading that no role actually asked for.
    /// </summary>
    /// <param name="value">The raw text as the user typed (or pasted, or POSTed) it.</param>
    /// <returns>The same text on a single line.</returns>
    private static string OneLine(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a compact count display string for use in list views.
    /// Examples: "ALLE", "3", "2-5", or empty string when no value.
    /// </summary>
    public string ToCountDisplayString()
    {
        if (GeenNpcsNodig)
            return "0";

        if (!HasValue)
            return string.Empty;

        if (AlleNpcs)
            return CoreTekst.NpcsAlleKort;

        return AantalTekst();
    }

    /// <summary>Whether exactly one NPC is needed, which picks the singular text.</summary>
    private bool IsPreciesEen => TotaalMinimum == 1 && TotaalMaximum == 1;

    /// <summary>The total as a number or a range, e.g. "3" or "2-5".</summary>
    private string AantalTekst() => TotaalMinimum == TotaalMaximum
        ? $"{TotaalMinimum}"
        : $"{TotaalMinimum}-{TotaalMaximum}";
}
