namespace PlotboekGenerator.test.Services;

/// <summary>
/// The elevator-pitch prompt is written in the reader's language and asks for the answer in it, so the
/// pitch comes back in the language of the page that asked for it. NUnit puts the thread's UI culture
/// back after each spec.
/// </summary>
public sealed class GeminiSummaryService_specs
{
    [Test]
    [SetUICulture("en-GB")]
    public void An_english_reader_gets_an_english_prompt_asking_for_english()
    {
        var prompt = GeminiSummaryService.BuildPrompt("The lost temple", "<p>Heroes find a temple.</p>", "2 guard", null, null, null, null, null);

        prompt.Should().Contain("Write the answer in this language: English.", "the answer has to come back in the reader's language")
            .And.Contain("Title: The lost temple")
            .And.Contain("NPCs: 2 guard")
            .And.NotContain("Samenvatting", "the prompt itself is in the reader's language too");
    }

    [Test]
    [SetUICulture("nl-NL")]
    public void A_dutch_reader_gets_a_dutch_prompt_asking_for_dutch()
    {
        var prompt = GeminiSummaryService.BuildPrompt("De verloren tempel", "<p>Helden vinden een tempel.</p>", null, null, null, null, null, null);

        prompt.Should().Contain("Schrijf het antwoord in deze taal: Nederlands.", "the answer has to come back in the reader's language")
            .And.Contain("Titel: De verloren tempel")
            .And.NotContain("NPCs:", "an empty field is left out");
    }
}
