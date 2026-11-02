using System.Globalization;
using System.Resources;

namespace PlotboekGenerator.WebApp;

/// <summary>Reads a whole resource file at once, for text that leaves the server in one piece.</summary>
public static class ResourceTekst
{
    /// <summary>
    /// Every string of <paramref name="resources"/> in <paramref name="culture"/>; a key the culture
    /// doesn't translate keeps its neutral (Dutch) text.
    /// </summary>
    /// <param name="resources">The resource file, e.g. <c>ScriptTekst.ResourceManager</c>.</param>
    /// <param name="culture">The language to read.</param>
    /// <returns>The strings by key, sorted.</returns>
    public static IReadOnlyDictionary<string, string> Alle(ResourceManager resources, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(culture);

        var neutral = resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        var keys = neutral?.Cast<System.Collections.DictionaryEntry>().Select(entry => (string)entry.Key) ?? [];

        return new SortedDictionary<string, string>(
            keys.ToDictionary(key => key, key => resources.GetString(key, culture) ?? string.Empty),
            StringComparer.Ordinal);
    }
}
