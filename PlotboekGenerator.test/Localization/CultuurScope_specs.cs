using System.Globalization;
using PlotboekGenerator.WebApp;

namespace PlotboekGenerator.test.Localization;

/// <summary>The Word export runs in the deployment's language without changing the reader's.</summary>
public sealed class CultuurScope_specs
{
    [Test]
    public async Task The_work_runs_in_the_given_language_and_the_caller_keeps_its_own()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("nl");

            var inside = await CultuurScope.InAsync(CultureInfo.GetCultureInfo("en"), async () =>
            {
                await Task.Yield();
                return CultureInfo.CurrentUICulture.Name;
            });

            inside.Should().Be("en");
            CultureInfo.CurrentUICulture.Name.Should().Be("nl", "the reader goes on in their own language");
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Test]
    public async Task Without_a_language_the_work_keeps_the_current_one()
    {
        var name = await CultuurScope.InAsync(null, () => Task.FromResult(CultureInfo.CurrentUICulture.Name));

        name.Should().Be(CultureInfo.CurrentUICulture.Name);
    }

    [Test]
    public void The_neutral_words_follow_the_readers_language_and_a_configured_word_does_not()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");

            new Terminologie().Techniek.Should().Be("Tech crew");
            new Terminologie { Techniek = "Lichtploeg" }.Techniek.Should().Be("Lichtploeg", "a LARP's own word is the same in every language");
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }
}
