using System.IO;
using Microsoft.Extensions.DependencyInjection;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.Host;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.test.Integration;

/// <summary>
/// The generic host speaks its deployment's default language: the export template, the example LARP
/// and the example plotboek are Dutch, or English when <c>Localization:DefaultCulture</c> is <c>en</c>.
/// </summary>
public sealed class DefaultProfile_specs
{
    [TestCase("", typeof(ExampleReferenceDataSeed), typeof(ExampleDemoContent), DefaultProfile.TemplateResource)]
    [TestCase("nl", typeof(ExampleReferenceDataSeed), typeof(ExampleDemoContent), DefaultProfile.TemplateResource)]
    [TestCase("en", typeof(EnglishExampleReferenceDataSeed), typeof(EnglishExampleDemoContent), DefaultProfile.EnglishTemplateResource)]
    public void The_default_language_picks_the_example_and_the_template(string culture, Type seed, Type demo, string template)
    {
        using var factory = new PlotboekWebApplicationFactory(new Dictionary<string, string> { [LocalizationSettings.DefaultCultureKey] = culture });

        factory.Services.GetRequiredService<IReferenceDataSeed>().Should().BeOfType(seed);
        factory.Services.GetRequiredService<IDemoContent>().Should().BeOfType(demo);
        using var registered = factory.Services.GetRequiredService<IDocumentTemplateProvider>().OpenTemplate();
        using var expected = typeof(DefaultProfile).Assembly.GetManifestResourceStream(template)!;
        Bytes(registered).Should().Equal(Bytes(expected), "the export uses the template of the default language");
    }

    [Test]
    public async Task An_English_demo_starts_with_the_English_example()
    {
        using var factory = new PlotboekWebApplicationFactory(new Dictionary<string, string>
        {
            [LocalizationSettings.DefaultCultureKey] = "en",
            ["Demo:Enabled"] = "true",
            [DemoMode.ResetApiKeyKey] = "reset-key",
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        factory.TestStore.GetAll<EventConfig>().Should().Contain(e => e.Id == "Voorjaar" && e.DisplayName == "Spring", "the reference data is the English seed");
        factory.TestStore.GetAll<Domain>().OrderBy(d => d.Order).Select(d => d.Description)
            .Should().Equal("Court", "Guilds", "Temple", "Wilderness", "Other");
        factory.TestStore.GetAll<Plotboek.ExportData>().Should().ContainSingle().Which.Titel.Should().Be("The Mist Crown");
        factory.TestStore.GetAll<PlotboekPagina.ExportData>().Should().HaveCount(7);
    }

    private static byte[] Bytes(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
