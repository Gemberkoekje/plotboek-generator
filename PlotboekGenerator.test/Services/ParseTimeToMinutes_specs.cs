namespace PlotboekGenerator.test.Services;

public sealed class ParseTimeToMinutes_specs
{
    [Test]
    public void Parses_regular_afternoon_time()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("14:30").Should().Be((14 * 60) + 30);
    }

    [Test]
    public void Parses_morning_time()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("10:30").Should().Be((10 * 60) + 30);
    }

    [Test]
    public void After_midnight_wraps_by_adding_24_hours()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("00:00").Should().Be(24 * 60);
    }

    [Test]
    public void Early_morning_wraps_by_adding_24_hours()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("02:30").Should().Be((2 * 60) + 30 + (24 * 60));
    }

    [Test]
    public void Time_at_6_does_not_wrap()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("06:00").Should().Be(6 * 60);
    }

    [Test]
    public void Null_returns_MaxValue()
    {
        PlotboekExportBuilder.ParseTimeToMinutes(null!).Should().Be(int.MaxValue);
    }

    [Test]
    public void Empty_string_returns_MaxValue()
    {
        PlotboekExportBuilder.ParseTimeToMinutes(string.Empty).Should().Be(int.MaxValue);
    }

    [Test]
    public void Whitespace_returns_MaxValue()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("   ").Should().Be(int.MaxValue);
    }

    [Test]
    public void Non_time_string_returns_MaxValue()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("geen tijd").Should().Be(int.MaxValue);
    }

    [Test]
    public void Time_embedded_in_text()
    {
        PlotboekExportBuilder.ParseTimeToMinutes("19:30: Introquest").Should().Be((19 * 60) + 30);
    }
}

