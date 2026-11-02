using PlotboekGenerator.Application.Ports;

namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// The reference data the seed specs start from: the one the host under test ships.
/// </summary>
internal static class TestReferenceData
{
    /// <summary>The host's seed.</summary>
    public static IReferenceDataSeed Seed { get; } = new PlotboekGenerator.Host.ExampleReferenceDataSeed();
}
