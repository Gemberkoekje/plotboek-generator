namespace PlotboekGenerator.test.Models;

/// <summary>
/// A duration reads in the reader's language, singular for one hour or one minute. NUnit puts the
/// thread's UI culture back after each spec.
/// </summary>
[SetUICulture("en")]
public sealed class GeschatteTijdsduur_English_specs
{
    [TestCase(1, "1 minute")]
    [TestCase(45, "45 minutes")]
    [TestCase(60, "1 hour")]
    [TestCase(150, "2 hours and 30 minutes")]
    public void A_duration_reads_in_english(int minuten, string expected)
        => GeschatteTijdsduur.Create(minuten).ToString().Should().Be(expected);
}
