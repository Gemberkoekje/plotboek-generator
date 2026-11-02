using PlotboekGenerator.Application.Ports;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// The reference data the seed specs start from: the one the host under test ships.
/// </summary>
internal static class TestReferenceData
{
    /// <summary>The host's seed.</summary>
    public static IReferenceDataSeed Seed { get; } = new PlotboekGenerator.Host.ExampleReferenceDataSeed();

    /// <summary>The host's seed for a deployment whose default language is English.</summary>
    public static IReferenceDataSeed English { get; } = new PlotboekGenerator.Host.EnglishExampleReferenceDataSeed();

    /// <summary>The host's seed in <paramref name="language"/>.</summary>
    /// <param name="language"><c>nl</c> or <c>en</c>.</param>
    /// <returns>The seed a deployment with that default language starts from.</returns>
    public static IReferenceDataSeed For(string language)
        => language switch
        {
            "nl" => Seed,
            "en" => English,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "The host has an example in nl and en."),
        };
}
