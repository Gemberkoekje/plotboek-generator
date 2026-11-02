namespace PlotboekGenerator.test.Pages;

public sealed class PlotboekModel_PlotboekSettings_specs
{
    private static PlotboekModel CreateModel()
        => new (null!, null!, null!, null!, null!, null!, null!, null!, null!);

    [Test]
    public void BuildPlotboekBasisData_maps_titel()
    {
        var model = CreateModel();
        model.EditablePlotboekTitel = "Voorjaar 2026";

        var result = model.BuildPlotboekBasisData();

        result.Titel.Should().Be("Voorjaar 2026");
    }

    [Test]
    public void BuildPlotboekBasisData_null_titel_becomes_empty()
    {
        var model = CreateModel();
        model.EditablePlotboekTitel = null;

        var result = model.BuildPlotboekBasisData();

        result.Titel.Should().BeEmpty();
    }

    [Test]
    public void BuildPlotboekBasisData_maps_start_and_eind_dag()
    {
        var model = CreateModel();
        model.EditablePlotboekTitel = "Test";
        model.EditablePlotboekStartDag = Dag.Vrijdag;
        model.EditablePlotboekEindDag = Dag.Zondag;

        var result = model.BuildPlotboekBasisData();

        result.StartDag.Should().Be(Dag.Vrijdag);
        result.EindDag.Should().Be(Dag.Zondag);
    }

    [Test]
    public void BuildPlotboekKalenderData_maps_dates()
    {
        var model = CreateModel();
        model.EditablePlotboekStartDatumString = "2026-06-01";
        model.EditablePlotboekEindDatumString = "2026-06-03";

        var result = model.BuildPlotboekKalenderData();

        result.StartDatum.Should().Be(new DateOnly(2026, 6, 1));
        result.EindDatum.Should().Be(new DateOnly(2026, 6, 3));
    }

    [Test]
    public void BuildPlotboekKalenderData_defaults_when_date_strings_null()
    {
        var model = CreateModel();
        model.EditablePlotboekStartDatumString = null;
        model.EditablePlotboekEindDatumString = null;

        var result = model.BuildPlotboekKalenderData();

        result.StartDatum.Should().Be(default);
        result.EindDatum.Should().Be(default);
    }

    [Test]
    public void EditablePlotboekStartDatum_parses_valid_date()
    {
        var model = CreateModel();
        model.EditablePlotboekStartDatumString = "2026-06-01";

        model.EditablePlotboekStartDatum.Should().Be(new DateOnly(2026, 6, 1));
    }

    [Test]
    public void EditablePlotboekStartDatum_returns_null_for_invalid_string()
    {
        var model = CreateModel();
        model.EditablePlotboekStartDatumString = "not-a-date";

        model.EditablePlotboekStartDatum.Should().BeNull();
    }

    [Test]
    public void EditablePlotboekStartDatum_returns_null_for_null_string()
    {
        var model = CreateModel();
        model.EditablePlotboekStartDatumString = null;

        model.EditablePlotboekStartDatum.Should().BeNull();
    }

    [Test]
    public void EditablePlotboekEindDatum_parses_valid_date()
    {
        var model = CreateModel();
        model.EditablePlotboekEindDatumString = "2026-12-31";

        model.EditablePlotboekEindDatum.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Test]
    public void EditablePlotboekEindDatum_returns_null_for_invalid_string()
    {
        var model = CreateModel();
        model.EditablePlotboekEindDatumString = "xyz";

        model.EditablePlotboekEindDatum.Should().BeNull();
    }

    [Test]
    public void CaptureEditablePlotboekTextFields_returns_all_keys()
    {
        var model = CreateModel();

        var result = model.CaptureEditablePlotboekTextFields();

        result.Should().ContainKey("Titel");
        result.Should().ContainKey("Samenvatting Algemeen Plot");
        result.Should().ContainKey("Google Drive Folder URL");
    }

    [Test]
    public void CaptureEditablePlotboekTextFields_captures_values()
    {
        var model = CreateModel();
        model.EditablePlotboekTitel = "My Plotboek";
        model.EditableSamenvattingAlgemeenPlot = "A summary";
        model.EditableGoogleDriveFolderUrl = "https://drive.google.com/folder/abc";

        var result = model.CaptureEditablePlotboekTextFields();

        result["Titel"].Should().Be("My Plotboek");
        result["Samenvatting Algemeen Plot"].Should().Be("A summary");
        result["Google Drive Folder URL"].Should().Be("https://drive.google.com/folder/abc");
    }

    [Test]
    public void CaptureEditablePlotboekTextFields_null_values_are_null()
    {
        var model = CreateModel();

        var result = model.CaptureEditablePlotboekTextFields();

        result["Titel"].Should().BeNull();
        result["Samenvatting Algemeen Plot"].Should().BeNull();
        result["Google Drive Folder URL"].Should().BeNull();
    }

    [Test]
    public void InitializePlotboekEditableFields_sets_defaults_when_no_settings()
    {
        var model = CreateModel();
        model.PlotboekSettings = null;

        model.InitializePlotboekEditableFields();

        model.PlotboekVersion.Should().Be(0);
        model.EditablePlotboekTitel.Should().BeNull();
        model.EditablePlotboekStartDag.Should().Be(Dag.None);
        model.EditablePlotboekEindDag.Should().Be(Dag.None);
        model.EditablePlotboekStartDatumString.Should().BeNull();
        model.EditablePlotboekEindDatumString.Should().BeNull();
        model.EditableSamenvattingAlgemeenPlot.Should().BeNull();
        model.EditableGoogleDriveFolderUrl.Should().BeNull();
    }

    [Test]
    public void InitializePlotboekEditableFields_copies_from_settings()
    {
        var model = CreateModel();
        model.PlotboekSettings = new PlotboekModel.PlotboekSettingsInfo
        {
            EventVersion = 17,
            Titel = "Summer Event",
            StartDag = Dag.Donderdag,
            EindDag = Dag.Zondag,
            StartDatum = new DateOnly(2026, 7, 10),
            EindDatum = new DateOnly(2026, 7, 13),
            SamenvattingAlgemeenPlot = "A great adventure",
            GoogleDriveFolderUrl = "https://drive.google.com/folder/xyz",
        };

        model.InitializePlotboekEditableFields();

        model.PlotboekVersion.Should().Be(17);
        model.EditablePlotboekTitel.Should().Be("Summer Event");
        model.EditablePlotboekStartDag.Should().Be(Dag.Donderdag);
        model.EditablePlotboekEindDag.Should().Be(Dag.Zondag);
        model.EditablePlotboekStartDatumString.Should().Be("2026-07-10");
        model.EditablePlotboekEindDatumString.Should().Be("2026-07-13");
        model.EditableSamenvattingAlgemeenPlot.Should().Be("A great adventure");
        model.EditableGoogleDriveFolderUrl.Should().Be("https://drive.google.com/folder/xyz");
    }

    [Test]
    public void InitializePlotboekEditableFields_null_dates_produce_null_strings()
    {
        var model = CreateModel();
        model.PlotboekSettings = new PlotboekModel.PlotboekSettingsInfo
        {
            Titel = "Test",
            StartDatum = null,
            EindDatum = null,
        };

        model.InitializePlotboekEditableFields();

        model.EditablePlotboekStartDatumString.Should().BeNull();
        model.EditablePlotboekEindDatumString.Should().BeNull();
    }

    [Test]
    public void PlotboekSettingsInfo_HasDatums_true_when_both_set()
    {
        var settings = new PlotboekModel.PlotboekSettingsInfo
        {
            StartDatum = new DateOnly(2026, 6, 1),
            EindDatum = new DateOnly(2026, 6, 3),
        };

        settings.HasDatums.Should().BeTrue();
    }

    [Test]
    public void PlotboekSettingsInfo_HasDatums_false_when_start_missing()
    {
        var settings = new PlotboekModel.PlotboekSettingsInfo
        {
            StartDatum = null,
            EindDatum = new DateOnly(2026, 6, 3),
        };

        settings.HasDatums.Should().BeFalse();
    }

    [Test]
    public void PlotboekSettingsInfo_HasDatums_false_when_end_missing()
    {
        var settings = new PlotboekModel.PlotboekSettingsInfo
        {
            StartDatum = new DateOnly(2026, 6, 1),
            EindDatum = null,
        };

        settings.HasDatums.Should().BeFalse();
    }

    [Test]
    public void PlotboekSettingsInfo_HasDatums_false_when_both_missing()
    {
        var settings = new PlotboekModel.PlotboekSettingsInfo();

        settings.HasDatums.Should().BeFalse();
    }

    [Test]
    public void BuildNpcsDagTotalenData_maps_filled_days()
    {
        var model = CreateModel();
        model.EditableNpcsVrijdag = 12;
        model.EditableNpcsZaterdag = 14;
        model.EditableNpcsZondag = 10;

        var result = model.BuildNpcsDagTotalenData();

        result.NpcsDagTotalen.Should().ContainKey(Dag.Vrijdag).WhoseValue.Should().Be(12);
        result.NpcsDagTotalen.Should().ContainKey(Dag.Zaterdag).WhoseValue.Should().Be(14);
        result.NpcsDagTotalen.Should().ContainKey(Dag.Zondag).WhoseValue.Should().Be(10);
        result.NpcsDagTotalen.Should().NotContainKey(Dag.Donderdag);
    }

    [Test]
    public void BuildNpcsDagTotalenData_excludes_zero_and_null_values()
    {
        var model = CreateModel();
        model.EditableNpcsDonderdag = 0;
        model.EditableNpcsVrijdag = null;
        model.EditableNpcsZaterdag = 8;

        var result = model.BuildNpcsDagTotalenData();

        result.NpcsDagTotalen.Should().NotContainKey(Dag.Donderdag);
        result.NpcsDagTotalen.Should().NotContainKey(Dag.Vrijdag);
        result.NpcsDagTotalen.Should().ContainKey(Dag.Zaterdag).WhoseValue.Should().Be(8);
    }

    [Test]
    public void BuildNpcsDagTotalenData_returns_empty_when_all_null()
    {
        var model = CreateModel();

        var result = model.BuildNpcsDagTotalenData();

        result.NpcsDagTotalen.Should().BeEmpty();
    }
}

