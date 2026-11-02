using System.Globalization;
using Microsoft.Extensions.Configuration;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.test.Localization;

public sealed class LocalizationSettings_specs
{
    [Test]
    public void Without_settings_a_deployment_speaks_only_dutch()
        => Read([]).Should().BeEquivalentTo(new
        {
            DefaultCulture = CultureInfo.GetCultureInfo("nl"),
            SupportedCultures = new[] { CultureInfo.GetCultureInfo("nl") },
            HasChoice = false,
        });

    [Test]
    public void The_default_comes_first_among_the_supported_languages()
        => Read(new() { [LocalizationSettings.DefaultCultureKey] = "EN", [LocalizationSettings.SupportedCulturesKey] = "nl; en" })
            .SupportedCultures.Select(c => c.Name).Should().Equal("en", "nl");

    [Test]
    public void The_supported_languages_can_be_an_array()
        => Read(new() { [$"{LocalizationSettings.SupportedCulturesKey}:0"] = "nl", [$"{LocalizationSettings.SupportedCulturesKey}:1"] = "en" })
            .HasChoice.Should().BeTrue();

    [TestCase("fr", "")]
    [TestCase("nl", "nl,de")]
    [TestCase("en", "nl")]
    public void A_language_without_translations_or_a_default_it_does_not_support_stops_the_deployment(string defaultCulture, string supported)
    {
        var read = () => Read(new() { [LocalizationSettings.DefaultCultureKey] = defaultCulture, [LocalizationSettings.SupportedCulturesKey] = supported });

        read.Should().Throw<InvalidOperationException>("a typo must not quietly fall back to another language")
            .WithMessage("*Localization:*");
    }

    [Test]
    public void Only_the_language_of_the_text_follows_the_visitor()
    {
        var options = Read(new() { [LocalizationSettings.SupportedCulturesKey] = "nl,en" }).ToRequestLocalizationOptions();

        options.SupportedCultures.Should().Equal([CultureInfo.CurrentCulture], "dates and numbers keep the process's format, so posted forms parse as before");
        options.SupportedUICultures!.Select(c => c.Name).Should().Equal("nl", "en");
    }

    private static LocalizationSettings Read(Dictionary<string, string?> settings)
        => LocalizationSettings.Read(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
}
