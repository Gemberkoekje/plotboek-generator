using System.Globalization;
using System.Text;

namespace PlotboekGenerator.core.Models.ReferenceData;

/// <summary>
/// Derives the string key of a reference-data record from the name an admin or SL typed.
/// </summary>
public static class ReferenceDataId
{
    /// <summary>
    /// Derives a stable, readable key from a name: diacritics removed, every word capitalised and glued
    /// together ("Jongensveld voor" → "JongensveldVoor", "Ruïne" → "Ruine"). A key that starts with a digit
    /// gets an "N" prefix so it stays a usable identifier.
    /// </summary>
    /// <remarks>
    /// Only the first letter of each word is changed, so "STADSWACHT" and "Stadswacht" give different
    /// keys. Lower-case the name first when case must not matter (see <see cref="StandaardNpc.IdVoor"/>).
    /// Questlocaties only derive their key on creation: renaming must not change it, because stored plots
    /// reference it.
    /// </remarks>
    /// <param name="naam">The name to derive the key from.</param>
    /// <returns>The key, or an empty string when nothing usable is left.</returns>
    public static string Derive(string naam)
    {
        if (string.IsNullOrWhiteSpace(naam))
        {
            return string.Empty;
        }

        var zonderDiakrieten = new StringBuilder();
        foreach (var ch in naam.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                zonderDiakrieten.Append(ch);
            }
        }

        var id = new StringBuilder();
        var startOfWord = true;
        foreach (var ch in zonderDiakrieten.ToString().Normalize(NormalizationForm.FormC))
        {
            // Apostrophes sit inside a word ("Henry's"), so drop them without starting a new one —
            // otherwise the key reads "HenrySHoekje".
            if (ch is '\'' or '’')
            {
                continue;
            }

            if (!char.IsLetterOrDigit(ch))
            {
                startOfWord = true;
                continue;
            }

            id.Append(startOfWord ? char.ToUpperInvariant(ch) : ch);
            startOfWord = false;
        }

        if (id.Length == 0)
        {
            return string.Empty;
        }

        return char.IsDigit(id[0]) ? $"N{id}" : id.ToString();
    }
}
