using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Aggregates;

/// <summary>
/// Guards the feedback list on a pagina: the four targeted lifecycle methods plus withdrawal, the
/// invariant that open feedback blocks "volledig klaar", and the deliberate absence of feedback from
/// <see cref="PlotboekPagina.Data"/> — a full <see cref="PlotboekPagina.Aanpassen"/> must leave the
/// feedback list completely alone, which is what makes the reset-trap impossible.
/// </summary>
public sealed class PlotboekPagina_Feedback_specs
{
    private static PlotboekUser Hsl => new ()
    {
        Id = "hsl-1",
        Lidnummer = "10001",
        FirstName = "Hoofd",
        LastName = "Spelleider",
        UserName = "hsl",
        Email = "hsl@example.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    private static PlotboekUser Sl => new ()
    {
        Id = "sl-1",
        Lidnummer = "10002",
        FirstName = "Schrijvende",
        LastName = "Spelleider",
        UserName = "sl",
        Email = "sl@example.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    private static PlotboekPagina CreatePopulatedPagina()
    {
        var pagina = new PlotboekPagina();
        var plotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Voorjaar);
        pagina = pagina.AanPlotboekToevoegen(plotboekId, "Voorjaar 2025", Hsl).Value;

        pagina = pagina.Aanpassen(BasisData(), Sl).Value;
        pagina.MarkAllAsCommitted();
        return pagina;
    }

    private static PlotboekPagina.Data BasisData(bool isVolledigKlaar = false) => new ()
    {
        Titel = "Handelaar",
        Dag = Dag.Zaterdag,
        StarttijdId = StarttijdIds.Tijd1430,
        Order = 1,
        ElevatorPitch = "Koop en verkoop",
        MeerInformatie = "<p>Details</p>",
        IsVolledigKlaar = isVolledigKlaar,
    };

    /// <summary>A pagina with one open feedback point, committed so later buffers start empty.</summary>
    private static (PlotboekPagina Pagina, FeedbackId Id) MetOpenFeedback()
    {
        var pagina = CreatePopulatedPagina().FeedbackToevoegen("De NPC-rollen kloppen niet.", Hsl).Value;
        var id = pagina.Feedback[0].Id;
        pagina.MarkAllAsCommitted();
        return (pagina, id);
    }

    [Test]
    public void FeedbackToevoegen_adds_an_open_point()
    {
        var pagina = CreatePopulatedPagina();

        var result = pagina.FeedbackToevoegen("De NPC-rollen kloppen niet.", Hsl);

        result.IsValid.Should().BeTrue();
        result.Value.Feedback.Should().ContainSingle();
        result.Value.Feedback[0].Tekst.Should().Be("De NPC-rollen kloppen niet.");
        result.Value.Feedback[0].GegevenDoor.Should().Be("hsl-1");
        result.Value.Feedback[0].Status.Should().Be(FeedbackStatus.Open);
        result.Value.HeeftOpenstaandeFeedback.Should().BeTrue();
    }

    [Test]
    public void FeedbackToevoegen_appends_the_event()
    {
        var pagina = CreatePopulatedPagina();

        var result = pagina.FeedbackToevoegen("Graag meer detail.", Hsl);

        result.Value.Buffer.Uncommitted.Should().ContainSingle()
            .Which.Should().BeOfType<PlotboekPaginaFeedbackToegevoegd>();
    }

    [Test]
    public void FeedbackToevoegen_trims_the_text()
    {
        var pagina = CreatePopulatedPagina();

        var result = pagina.FeedbackToevoegen("   Graag meer detail.   ", Hsl);

        result.Value.Feedback[0].Tekst.Should().Be("Graag meer detail.");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void FeedbackToevoegen_without_text_fails(string tekst)
    {
        var pagina = CreatePopulatedPagina();

        var result = pagina.FeedbackToevoegen(tekst, Hsl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Contain("zonder tekst");
    }

    [Test]
    public void FeedbackToevoegen_keeps_earlier_points_in_order()
    {
        var pagina = CreatePopulatedPagina();

        pagina = pagina.FeedbackToevoegen("Eerste punt", Hsl).Value;
        pagina = pagina.FeedbackToevoegen("Tweede punt", Hsl).Value;

        pagina.Feedback.Select(f => f.Tekst).Should().Equal("Eerste punt", "Tweede punt");
    }

    [Test]
    public void FeedbackBeantwoorden_records_the_response_and_stops_blocking()
    {
        var (pagina, id) = MetOpenFeedback();

        var result = pagina.FeedbackBeantwoorden(id, "Aangepast, zie de nieuwe rollen.", Sl);

        result.IsValid.Should().BeTrue();
        result.Value.Feedback[0].Status.Should().Be(FeedbackStatus.Beantwoord);
        result.Value.Feedback[0].Reactie.Should().Be("Aangepast, zie de nieuwe rollen.");
        result.Value.Feedback[0].ReactieDoor.Should().Be("sl-1");
        result.Value.Feedback[0].ReactieOp.Should().NotBe(default);
        result.Value.HeeftOpenstaandeFeedback.Should().BeFalse();
    }

    [Test]
    public void FeedbackBeantwoorden_leaves_the_original_text_intact()
    {
        var (pagina, id) = MetOpenFeedback();

        var result = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl);

        result.Value.Feedback[0].Tekst.Should().Be("De NPC-rollen kloppen niet.");
        result.Value.Feedback[0].GegevenDoor.Should().Be("hsl-1");
    }

    [Test]
    public void FeedbackBeantwoorden_with_unknown_id_fails()
    {
        var (pagina, _) = MetOpenFeedback();

        var result = pagina.FeedbackBeantwoorden(FeedbackId.Next(), "Aangepast.", Sl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Contain("bestaat niet");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void FeedbackBeantwoorden_without_text_fails(string reactie)
    {
        var (pagina, id) = MetOpenFeedback();

        var result = pagina.FeedbackBeantwoorden(id, reactie, Sl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Contain("telt niet als reageren");
    }

    [Test]
    public void FeedbackBeantwoorden_on_an_afgehandeld_point_fails()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl).Value;
        pagina = pagina.FeedbackAfhandelen(id, Hsl).Value;

        var result = pagina.FeedbackBeantwoorden(id, "Nog een reactie.", Sl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Contain("al afgehandeld");
    }

    [Test]
    public void FeedbackAfhandelen_signs_the_point_off()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl).Value;

        var result = pagina.FeedbackAfhandelen(id, Hsl);

        result.IsValid.Should().BeTrue();
        result.Value.Feedback[0].Status.Should().Be(FeedbackStatus.Afgehandeld);
        result.Value.HeeftOpenstaandeFeedback.Should().BeFalse();
    }

    [Test]
    public void FeedbackAfhandelen_on_an_open_point_fails()
    {
        var (pagina, id) = MetOpenFeedback();

        var result = pagina.FeedbackAfhandelen(id, Hsl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Contain("Alleen beantwoorde feedback");
    }

    [Test]
    public void FeedbackAfhandelen_with_unknown_id_fails()
    {
        var (pagina, _) = MetOpenFeedback();

        var result = pagina.FeedbackAfhandelen(FeedbackId.Next(), Hsl);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void FeedbackHeropenen_puts_the_point_back_on_open_and_keeps_the_response()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl).Value;
        pagina = pagina.FeedbackAfhandelen(id, Hsl).Value;

        var result = pagina.FeedbackHeropenen(id, "Toch nog niet goed.", Hsl);

        result.IsValid.Should().BeTrue();
        result.Value.Feedback[0].Status.Should().Be(FeedbackStatus.Open);
        result.Value.Feedback[0].Reactie.Should().Be("Aangepast.");
        result.Value.HeeftOpenstaandeFeedback.Should().BeTrue();
    }

    [Test]
    public void FeedbackHeropenen_works_on_a_beantwoord_point()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl).Value;

        var result = pagina.FeedbackHeropenen(id, string.Empty, Hsl);

        result.IsValid.Should().BeTrue();
        result.Value.Feedback[0].Status.Should().Be(FeedbackStatus.Open);
    }

    [Test]
    public void FeedbackHeropenen_on_an_open_point_fails()
    {
        var (pagina, id) = MetOpenFeedback();

        var result = pagina.FeedbackHeropenen(id, "Nogmaals.", Hsl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Contain("staat al open");
    }

    [Test]
    public void FeedbackVerwijderen_removes_the_point()
    {
        var (pagina, id) = MetOpenFeedback();

        var result = pagina.FeedbackVerwijderen(id, Hsl);

        result.IsValid.Should().BeTrue();
        result.Value.Feedback.Should().BeEmpty();
        result.Value.HeeftOpenstaandeFeedback.Should().BeFalse();
    }

    [Test]
    public void FeedbackVerwijderen_leaves_the_other_points_alone()
    {
        var pagina = CreatePopulatedPagina();
        pagina = pagina.FeedbackToevoegen("Eerste punt", Hsl).Value;
        pagina = pagina.FeedbackToevoegen("Tweede punt", Hsl).Value;

        var result = pagina.FeedbackVerwijderen(pagina.Feedback[0].Id, Hsl);

        result.Value.Feedback.Should().ContainSingle()
            .Which.Tekst.Should().Be("Tweede punt");
    }

    [Test]
    public void FeedbackVerwijderen_with_unknown_id_fails()
    {
        var (pagina, _) = MetOpenFeedback();

        var result = pagina.FeedbackVerwijderen(FeedbackId.Next(), Hsl);

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void A_full_Aanpassen_leaves_the_feedback_list_untouched()
    {
        var (pagina, id) = MetOpenFeedback();

        // A regular pagina save carries no feedback at all — this is the reset-trap that cannot happen.
        var result = pagina.Aanpassen(BasisData() with { Titel = "Handelaar (herzien)" }, Sl);

        result.IsValid.Should().BeTrue();
        result.Value.Titel.Should().Be("Handelaar (herzien)");
        result.Value.Feedback.Should().ContainSingle()
            .Which.Id.Should().Be(id);
        result.Value.Feedback[0].Status.Should().Be(FeedbackStatus.Open);
    }

    [Test]
    public void Feedback_survives_a_replay_of_the_full_event_stream()
    {
        var pagina = CreatePopulatedPagina();
        pagina = pagina.FeedbackToevoegen("Eerste punt", Hsl).Value;
        pagina = pagina.FeedbackToevoegen("Tweede punt", Hsl).Value;
        var eersteId = pagina.Feedback[0].Id;
        pagina = pagina.FeedbackBeantwoorden(eersteId, "Aangepast.", Sl).Value;
        pagina = pagina.FeedbackAfhandelen(eersteId, Hsl).Value;
        pagina = pagina.FeedbackVerwijderen(pagina.Feedback[1].Id, Hsl).Value;

        pagina.Feedback.Should().ContainSingle();
        pagina.Feedback[0].Status.Should().Be(FeedbackStatus.Afgehandeld);
        pagina.Feedback[0].Reactie.Should().Be("Aangepast.");
    }

    [Test]
    public void GetData_carries_the_feedback_into_the_read_model()
    {
        var (pagina, id) = MetOpenFeedback();

        var data = pagina.GetData();

        data.Feedback.Should().ContainSingle().Which.Id.Should().Be(id);
        data.HeeftOpenstaandeFeedback.Should().BeTrue();
    }

    [Test]
    public void Open_feedback_blocks_volledig_klaar()
    {
        var (pagina, _) = MetOpenFeedback();

        var result = pagina.Aanpassen(BasisData(isVolledigKlaar: true), Sl);

        result.IsValid.Should().BeFalse();
        result.Messages.Should().ContainSingle()
            .Which.Message.Should().Contain("openstaande feedback");
    }

    [Test]
    public void Beantwoorde_feedback_no_longer_blocks_volledig_klaar()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl).Value;

        var result = pagina.Aanpassen(BasisData(isVolledigKlaar: true), Sl);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeTrue();
    }

    [Test]
    public void Afgehandelde_feedback_no_longer_blocks_volledig_klaar()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl).Value;
        pagina = pagina.FeedbackAfhandelen(id, Hsl).Value;

        var result = pagina.Aanpassen(BasisData(isVolledigKlaar: true), Sl);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeTrue();
    }

    [Test]
    public void Withdrawing_the_last_open_point_unblocks_volledig_klaar()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackVerwijderen(id, Hsl).Value;

        var result = pagina.Aanpassen(BasisData(isVolledigKlaar: true), Sl);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeTrue();
    }

    [Test]
    public void A_plot_with_open_feedback_can_still_be_saved_with_the_flag_off()
    {
        // The guard must only block SETTING the flag. An ordinary save of an unfinished plot that
        // happens to carry open feedback has to keep working, or the SL cannot respond to it at all.
        var (pagina, _) = MetOpenFeedback();

        var result = pagina.Aanpassen(BasisData() with { Titel = "Handelaar (herzien)" }, Sl);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeFalse();
        result.Value.Titel.Should().Be("Handelaar (herzien)");
    }

    [Test]
    public void The_full_loop_from_klaar_through_feedback_and_back_to_klaar()
    {
        var pagina = CreatePopulatedPagina().Aanpassen(BasisData(isVolledigKlaar: true), Sl).Value;

        pagina = pagina.FeedbackToevoegen("Toch nog iets.", Hsl).Value;
        pagina.IsVolledigKlaar.Should().BeFalse("nieuwe feedback zet het plot terug op niet-klaar");

        pagina = pagina.FeedbackBeantwoorden(pagina.Feedback[0].Id, "Aangepast.", Sl).Value;
        pagina = pagina.Aanpassen(BasisData(isVolledigKlaar: true), Sl).Value;
        pagina.IsVolledigKlaar.Should().BeTrue("na een reactie mag het plot weer klaar");

        pagina = pagina.FeedbackHeropenen(pagina.Feedback[0].Id, "Nee.", Hsl).Value;
        pagina.IsVolledigKlaar.Should().BeFalse("heropenen blokkeert opnieuw");

        var opnieuw = pagina.Aanpassen(BasisData(isVolledigKlaar: true), Sl);
        opnieuw.IsValid.Should().BeFalse("een heropend punt staat weer open");
    }

    [Test]
    public void FeedbackToevoegen_unticks_a_plot_that_was_already_volledig_klaar()
    {
        var pagina = CreatePopulatedPagina().Aanpassen(BasisData(isVolledigKlaar: true), Sl).Value;
        pagina.IsVolledigKlaar.Should().BeTrue();
        pagina.MarkAllAsCommitted();

        var result = pagina.FeedbackToevoegen("Toch nog iets.", Hsl);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeFalse();
        result.Value.Buffer.Uncommitted.Should().HaveCount(2);
        result.Value.Buffer.Uncommitted.Last().Should().BeOfType<PlotboekPaginaVolledigKlaarAangepast>();
    }

    [Test]
    public void FeedbackToevoegen_does_not_touch_the_flag_on_a_plot_that_was_not_klaar()
    {
        var pagina = CreatePopulatedPagina();
        pagina.MarkAllAsCommitted();

        var result = pagina.FeedbackToevoegen("Toch nog iets.", Hsl);

        result.Value.IsVolledigKlaar.Should().BeFalse();
        result.Value.Buffer.Uncommitted.Should().ContainSingle();
    }

    [Test]
    public void FeedbackHeropenen_unticks_a_plot_that_was_volledig_klaar()
    {
        var (pagina, id) = MetOpenFeedback();
        pagina = pagina.FeedbackBeantwoorden(id, "Aangepast.", Sl).Value;
        pagina = pagina.Aanpassen(BasisData(isVolledigKlaar: true), Sl).Value;
        pagina.IsVolledigKlaar.Should().BeTrue();

        var result = pagina.FeedbackHeropenen(id, "Toch niet.", Hsl);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeFalse();
    }

    [Test]
    public void Feedback_methods_reject_a_null_user()
    {
        var (pagina, id) = MetOpenFeedback();

        var toevoegen = () => pagina.FeedbackToevoegen("Tekst", null);
        var beantwoorden = () => pagina.FeedbackBeantwoorden(id, "Reactie", null);
        var afhandelen = () => pagina.FeedbackAfhandelen(id, null);
        var heropenen = () => pagina.FeedbackHeropenen(id, "Reden", null);
        var verwijderen = () => pagina.FeedbackVerwijderen(id, null);

        toevoegen.Should().Throw<ArgumentNullException>();
        beantwoorden.Should().Throw<ArgumentNullException>();
        afhandelen.Should().Throw<ArgumentNullException>();
        heropenen.Should().Throw<ArgumentNullException>();
        verwijderen.Should().Throw<ArgumentNullException>();
    }
}
