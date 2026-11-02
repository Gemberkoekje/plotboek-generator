namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_BuildIconen_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void Returns_None_when_no_flags_set()
    {
        var model = CreateModel();

        model.BuildIconen().Should().Be(PlotboekPaginaIcons.None);
    }

    [Test]
    public void Sets_TimingCruciaal()
    {
        var model = CreateModel();
        model.EditableTimingCruciaal = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.TimingCruciaal);
    }

    [Test]
    public void Sets_PlotCruciaal()
    {
        var model = CreateModel();
        model.EditablePlotCruciaal = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.PlotCruciaal);
    }

    [Test]
    public void Sets_MagAlsEersteCancelled()
    {
        var model = CreateModel();
        model.EditableMagAlsEersteCancelled = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.MagAlsEersteCancelled);
    }

    [Test]
    public void Sets_UitgebreideBriefing()
    {
        var model = CreateModel();
        model.EditableUitgebreideBriefing = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.UitgebreideBriefing);
    }

    [Test]
    public void Sets_GrootPakOfKostuum()
    {
        var model = CreateModel();
        model.EditableGrootPakOfKostuum = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.GrootPakOfKostuum);
    }

    [Test]
    public void Sets_MuziekVanuitTechniek()
    {
        var model = CreateModel();
        model.EditableMuziekVanuitTechniek = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.MuziekVanuitTechniek);
    }

    [Test]
    public void Sets_LichtOfRookVanuitTechniek()
    {
        var model = CreateModel();
        model.EditableLichtOfRookVanuitTechniek = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.LichtOfRookVanuitTechniek);
    }

    [Test]
    public void Sets_EventBreed_when_Hoofdveld_and_AlleNpcs()
    {
        var model = CreateModel();
        model.EditableLocatieEncounter = LocatieEncounter.Hoofdveld;
        model.EditableAlleNpcs = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.EventBreed);
    }

    [Test]
    public void Sets_EventBreed_when_Hoofdveld_and_EventBreed_checked()
    {
        var model = CreateModel();
        model.EditableLocatieEncounter = LocatieEncounter.Hoofdveld;
        model.EditableEventBreed = true;

        model.BuildIconen().Should().HaveFlag(PlotboekPaginaIcons.EventBreed);
    }

    [Test]
    public void Does_not_set_EventBreed_when_not_Hoofdveld()
    {
        var model = CreateModel();
        model.EditableLocatieEncounter = LocatieEncounter.Questveld;
        model.EditableAlleNpcs = true;

        model.BuildIconen().Should().NotHaveFlag(PlotboekPaginaIcons.EventBreed);
    }

    [Test]
    public void Does_not_set_EventBreed_when_Hoofdveld_but_no_flags()
    {
        var model = CreateModel();
        model.EditableLocatieEncounter = LocatieEncounter.Hoofdveld;

        model.BuildIconen().Should().NotHaveFlag(PlotboekPaginaIcons.EventBreed);
    }

    [Test]
    public void Combines_multiple_flags()
    {
        var model = CreateModel();
        model.EditableTimingCruciaal = true;
        model.EditablePlotCruciaal = true;
        model.EditableMuziekVanuitTechniek = true;

        var result = model.BuildIconen();
        result.Should().HaveFlag(PlotboekPaginaIcons.TimingCruciaal);
        result.Should().HaveFlag(PlotboekPaginaIcons.PlotCruciaal);
        result.Should().HaveFlag(PlotboekPaginaIcons.MuziekVanuitTechniek);
        result.Should().NotHaveFlag(PlotboekPaginaIcons.UitgebreideBriefing);
    }
}
