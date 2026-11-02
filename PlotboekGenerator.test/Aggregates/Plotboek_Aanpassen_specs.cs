namespace PlotboekGenerator.test.Aggregates;

public sealed class Plotboek_Aanpassen_specs
{
    private static PlotboekUser TestUser => new ()
    {
        Id = "user-1",
        Lidnummer = "12345",
        FirstName = "Test",
        LastName = "User",
        UserName = "testuser",
        Email = "test@example.test",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsDummy = false,
        PrimaryDomainId = Guid.Empty,
    };

    private static Plotboek CreatePlotboek()
    {
        var result = Plotboek.Create(Year.Create(2025), EventIds.Voorjaar, Dag.Vrijdag, Dag.Zondag, TestUser);
        var plotboek = result.Value;
        plotboek.MarkAllAsCommitted();
        return plotboek;
    }

    [Test]
    public void BasisData_updates_titel()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.BasisData
        {
            Titel = "Mijn Plotboek",
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.Titel.Should().Be("Mijn Plotboek");
    }

    [Test]
    public void BasisData_noop_when_titel_unchanged()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.BasisData
        {
            Titel = "Titel",
            StartDag = Dag.Vrijdag,
            EindDag = Dag.Zondag,
        };

        plotboek = plotboek.Aanpassen(data, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void BasisData_updates_dagen_when_no_datums_set()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.BasisData
        {
            StartDag = Dag.Donderdag,
            EindDag = Dag.Zaterdag,
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.StartDag.Should().Be(Dag.Donderdag);
        result.Value.EindDag.Should().Be(Dag.Zaterdag);
    }

    [Test]
    public void BasisData_throws_on_null_data()
    {
        var plotboek = CreatePlotboek();

        var action = () => plotboek.Aanpassen((Plotboek.BasisData)null!, TestUser);

        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void KalenderData_updates_datums()
    {
        var plotboek = CreatePlotboek();
        var start = new DateOnly(2025, 7, 4);
        var eind = new DateOnly(2025, 7, 6);
        var data = new Plotboek.KalenderData
        {
            StartDatum = start,
            EindDatum = eind,
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.StartDatum.Should().Be(start);
        result.Value.EindDatum.Should().Be(eind);
    }

    [Test]
    public void KalenderData_noop_when_datums_unchanged()
    {
        var plotboek = CreatePlotboek();
        var start = new DateOnly(2025, 7, 4);
        var eind = new DateOnly(2025, 7, 6);
        var data = new Plotboek.KalenderData
        {
            StartDatum = start,
            EindDatum = eind,
        };

        plotboek = plotboek.Aanpassen(data, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void KalenderData_throws_on_null_data()
    {
        var plotboek = CreatePlotboek();

        var action = () => plotboek.Aanpassen((Plotboek.KalenderData)null!, TestUser);

        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void StartDag_derived_from_StartDatum_when_set()
    {
        var plotboek = CreatePlotboek();

        // 2025-07-04 is a Friday
        var data = new Plotboek.KalenderData
        {
            StartDatum = new DateOnly(2025, 7, 4),
            EindDatum = new DateOnly(2025, 7, 6),
        };
        plotboek = plotboek.Aanpassen(data, TestUser).Value;

        plotboek.StartDag.Should().Be(Dag.Vrijdag);
        plotboek.EindDag.Should().Be(Dag.Zondag);
    }

    [Test]
    public void SamenvattingAlgemeenPlot_updates_text()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.SamenvattingAlgemeenPlotData
        {
            SamenvattingAlgemeenPlot = "De wereld staat op het punt...",
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.SamenvattingAlgemeenPlot.Should().Be("De wereld staat op het punt...");
    }

    [Test]
    public void SamenvattingAlgemeenPlot_noop_when_unchanged()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.SamenvattingAlgemeenPlotData
        {
            SamenvattingAlgemeenPlot = "Samenvatting",
        };

        plotboek = plotboek.Aanpassen(data, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void SamenvattingAlgemeenPlot_throws_on_null_data()
    {
        var plotboek = CreatePlotboek();

        var action = () => plotboek.Aanpassen((Plotboek.SamenvattingAlgemeenPlotData)null!, TestUser);

        action.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void GoogleDriveFileUrl_updates_url()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.GoogleDriveFileUrlData
        {
            GoogleDriveFileUrl = "https://drive.google.com/file/123",
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.GoogleDriveFileUrl.Should().Be("https://drive.google.com/file/123");
    }

    [Test]
    public void GoogleDriveFileUrl_noop_when_unchanged()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.GoogleDriveFileUrlData
        {
            GoogleDriveFileUrl = "https://drive.google.com/file/123",
        };

        plotboek = plotboek.Aanpassen(data, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void GoogleDriveFolderUrl_updates_url()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.GoogleDriveFolderUrlData
        {
            GoogleDriveFolderUrl = "https://drive.google.com/folder/abc",
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.GoogleDriveFolderUrl.Should().Be("https://drive.google.com/folder/abc");
    }

    [Test]
    public void GoogleDriveFolderUrl_noop_when_unchanged()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.GoogleDriveFolderUrlData
        {
            GoogleDriveFolderUrl = "https://drive.google.com/folder/abc",
        };

        plotboek = plotboek.Aanpassen(data, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void NpcsDagTotalen_worden_opgeslagen()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.NpcsDagTotalenData
        {
            NpcsDagTotalen = new Dictionary<Dag, int>
            {
                [Dag.Vrijdag] = 12,
                [Dag.Zaterdag] = 14,
                [Dag.Zondag] = 10,
            },
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.NpcsDagTotalen.Should().ContainKey(Dag.Vrijdag).WhoseValue.Should().Be(12);
        result.Value.NpcsDagTotalen.Should().ContainKey(Dag.Zaterdag).WhoseValue.Should().Be(14);
        result.Value.NpcsDagTotalen.Should().ContainKey(Dag.Zondag).WhoseValue.Should().Be(10);
    }

    [Test]
    public void NpcsDagTotalen_noop_when_unchanged()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.NpcsDagTotalenData
        {
            NpcsDagTotalen = new Dictionary<Dag, int> { [Dag.Vrijdag] = 12 },
        };
        plotboek = plotboek.Aanpassen(data, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(data, TestUser);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void NpcsDagTotalen_zero_values_worden_gefilterd()
    {
        var plotboek = CreatePlotboek();
        var data = new Plotboek.NpcsDagTotalenData
        {
            NpcsDagTotalen = new Dictionary<Dag, int>
            {
                [Dag.Vrijdag] = 0,
                [Dag.Zaterdag] = 8,
            },
        };

        var result = plotboek.Aanpassen(data, TestUser);

        result.Value.NpcsDagTotalen.Should().NotContainKey(Dag.Vrijdag);
        result.Value.NpcsDagTotalen.Should().ContainKey(Dag.Zaterdag).WhoseValue.Should().Be(8);
    }

    [Test]
    public void VolledigKlaar_aanzetten_slaat_status_op()
    {
        var plotboek = CreatePlotboek();

        var result = plotboek.Aanpassen(new Plotboek.VolledigKlaarData { IsVolledigKlaar = true }, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeTrue();
    }

    [Test]
    public void VolledigKlaar_uitzetten_slaat_status_op()
    {
        var plotboek = CreatePlotboek().Aanpassen(new Plotboek.VolledigKlaarData { IsVolledigKlaar = true }, TestUser).Value;
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(new Plotboek.VolledigKlaarData { IsVolledigKlaar = false }, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeFalse();
    }

    [Test]
    public void VolledigKlaar_noop_when_unchanged()
    {
        var plotboek = CreatePlotboek();
        plotboek.MarkAllAsCommitted();

        var result = plotboek.Aanpassen(new Plotboek.VolledigKlaarData { IsVolledigKlaar = false }, TestUser);

        result.IsValid.Should().BeTrue();
        result.Value.IsVolledigKlaar.Should().BeFalse();
    }

    [Test]
    public void GetData_bevat_IsVolledigKlaar()
    {
        var plotboek = CreatePlotboek().Aanpassen(new Plotboek.VolledigKlaarData { IsVolledigKlaar = true }, TestUser).Value;

        var data = plotboek.GetData();

        data.IsVolledigKlaar.Should().BeTrue();
    }
}
