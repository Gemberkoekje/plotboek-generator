namespace PlotboekGenerator.test.Services;

public sealed class BuildInstructiePentagon_specs
{
    [Test]
    public void Returns_empty_when_no_text_and_no_icons()
    {
        var page = CreatePage();
        PlotboekExportBuilder.BuildInstructiePentagon(page).Should().BeEmpty();
    }

    [Test]
    public void Returns_user_text_as_is_when_no_icons()
    {
        var page = CreatePage(instructie: "Briefing tekst");
        PlotboekExportBuilder.BuildInstructiePentagon(page).Should().Be("Briefing tekst");
    }

    [Test]
    public void Appends_muziek_remark_when_icon_set()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.MuziekVanuitTechniek);
        var result = PlotboekExportBuilder.BuildInstructiePentagon(page);
        result.Should().Contain(Terminologie.Huidig.MuziekVanuitTechniek);
    }

    [Test]
    public void Appends_licht_remark_when_icon_set()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.LichtOfRookVanuitTechniek);
        var result = PlotboekExportBuilder.BuildInstructiePentagon(page);
        result.Should().Contain(Terminologie.Huidig.LichtOfRookVanuitTechniek);
    }

    [Test]
    public void Appends_both_remarks_as_html_paragraphs_when_user_text_present()
    {
        var page = CreatePage(
            instructie: "Bestaande instructie",
            iconen: PlotboekPaginaIcons.MuziekVanuitTechniek | PlotboekPaginaIcons.LichtOfRookVanuitTechniek);
        var result = PlotboekExportBuilder.BuildInstructiePentagon(page);
        result.Should().StartWith("Bestaande instructie");
        result.Should().Contain("<p>");
        result.Should().Contain(Terminologie.Huidig.MuziekVanuitTechniek);
        result.Should().Contain(Terminologie.Huidig.LichtOfRookVanuitTechniek);
    }

    [Test]
    public void Returns_newline_separated_remarks_when_no_user_text()
    {
        var page = CreatePage(
            iconen: PlotboekPaginaIcons.MuziekVanuitTechniek | PlotboekPaginaIcons.LichtOfRookVanuitTechniek);
        var result = PlotboekExportBuilder.BuildInstructiePentagon(page);
        result.Should().Contain("\n");
    }

    [Test]
    public void Preserves_html_content_and_appends_remarks()
    {
        var page = CreatePage(
            instructie: "<p>HTML content</p>",
            iconen: PlotboekPaginaIcons.MuziekVanuitTechniek);
        var result = PlotboekExportBuilder.BuildInstructiePentagon(page);
        result.Should().StartWith("<p>HTML content</p>");
        result.Should().Contain($"<p>\u2705 {Terminologie.Huidig.MuziekVanuitTechniek}</p>");
    }

    [Test]
    public void Non_techniek_icons_do_not_add_remarks()
    {
        var page = CreatePage(iconen: PlotboekPaginaIcons.TimingCruciaal | PlotboekPaginaIcons.PlotCruciaal);
        PlotboekExportBuilder.BuildInstructiePentagon(page).Should().BeEmpty();
    }

    private static PlotboekPagina.ExportData CreatePage(
        string instructie = "",
        PlotboekPaginaIcons iconen = PlotboekPaginaIcons.None)
    {
        return new PlotboekPagina.ExportData
        {
            Titel = "Test",
            DomainShorthand = string.Empty,
            DomainDescription = string.Empty,
            VerantwoordelijkeSl = string.Empty,
            ElevatorPitch = string.Empty,
            SamenvattingPlot = string.Empty,
            MeerInformatie = string.Empty,
            InstructiePentagon = instructie,
            Opmerkingen = string.Empty,
            Iconen = iconen,

            LocatieEncounter = LocatieEncounterData.Empty,
            AantalNpcs = AantalNpcsData.Empty,
        };
    }
}
