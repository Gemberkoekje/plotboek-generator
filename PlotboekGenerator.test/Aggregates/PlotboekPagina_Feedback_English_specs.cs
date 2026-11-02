namespace PlotboekGenerator.test.Aggregates;

/// <summary>
/// The messages the feedback rules hand back reach the page as they are, so they speak the reader's
/// language. NUnit puts the thread's UI culture back after each spec.
/// </summary>
[SetUICulture("en")]
public sealed class PlotboekPagina_Feedback_English_specs
{
    private static PlotboekUser Hsl => new ()
    {
        Id = "hsl-1",
        Lidnummer = "10001",
        FirstName = "Head",
        LastName = "GM",
        UserName = "hsl",
        Email = "hsl@example.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    [Test]
    public void Feedback_without_text_is_refused_in_english()
    {
        var result = new PlotboekPagina().FeedbackToevoegen("   ", Hsl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Be("Feedback without text cannot be added.");
    }

    [Test]
    public void Answering_unknown_feedback_is_refused_in_english()
    {
        var result = new PlotboekPagina().FeedbackBeantwoorden(FeedbackId.Next(), "Done", Hsl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Be("This feedback does not exist on this plot (any more).");
    }
}
