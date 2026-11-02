namespace PlotboekGenerator.test.Models;

public sealed class BlokNpcTotaal_specs
{
    [Test]
    public void HasValue_is_false_when_no_data()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
        };
        blok.HasValue.Should().BeFalse();
    }

    [Test]
    public void HasValue_when_alle_npcs()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            HeeftAlleNpcs = true,
        };
        blok.HasValue.Should().BeTrue();
    }

    [Test]
    public void HasValue_when_direct_minimum_set()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 3,
        };
        blok.HasValue.Should().BeTrue();
    }

    [Test]
    public void TotaalMinimum_includes_overflow()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1230",
            DirectMinimum = 2,
            OverflowVanVorigeBlokken =
            [
                new OverflowNpcs { VanStarttijdId = "Tijd1030", Minimum = 3 },
            ],
        };
        blok.TotaalMinimum.Should().Be(5);
    }

    [Test]
    public void TotaalMaximum_includes_overflow()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1230",
            DirectMinimum = 2,
            DirectMaximum = 4,
            OverflowVanVorigeBlokken =
            [
                new OverflowNpcs { VanStarttijdId = "Tijd1030", Minimum = 1, Maximum = 3 },
            ],
        };
        blok.TotaalMaximum.Should().Be(7);
    }

    [Test]
    public void TotaalMaximum_is_null_when_direct_has_no_maximum()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 2,
        };
        blok.TotaalMaximum.Should().BeNull();
    }

    [Test]
    public void ToDisplayString_empty_returns_empty()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
        };
        blok.ToDisplayString().Should().BeEmpty();
    }

    [Test]
    public void ToDisplayString_alle_npcs()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            HeeftAlleNpcs = true,
        };
        blok.ToDisplayString().Should().Be("ALLE NPCs");
    }

    [Test]
    public void ToDisplayString_exact_count()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 3,
            DirectMaximum = 3,
        };
        blok.ToDisplayString().Should().Be("3 NPCs");
    }

    [Test]
    public void ToDisplayString_range()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 2,
            DirectMaximum = 5,
        };
        blok.ToDisplayString().Should().Be("2-5 NPCs");
    }

    [Test]
    public void ToDisplayString_with_overflow()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1230",
            DirectMinimum = 2,
            DirectMaximum = 4,
            OverflowVanVorigeBlokken =
            [
                new OverflowNpcs { VanStarttijdId = "Tijd1030", Minimum = 1, Maximum = 3 },
            ],
        };
        var result = blok.ToDisplayString();
        result.Should().Contain("NPCs");
        result.Should().Contain("incl.");
        result.Should().Contain("Tijd1030");
    }

    [Test]
    public void ToShortString_empty_returns_empty()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
        };
        blok.ToShortString().Should().BeEmpty();
    }

    [Test]
    public void ToShortString_alle_npcs()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            HeeftAlleNpcs = true,
        };
        blok.ToShortString().Should().Be("ALLE");
    }

    [Test]
    public void ToShortString_range()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 2,
            DirectMaximum = 5,
        };
        blok.ToShortString().Should().Be("2-5");
    }

    [Test]
    public void ToShortString_exact()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 3,
            DirectMaximum = 3,
        };
        blok.ToShortString().Should().Be("3");
    }

    [Test]
    public void GetOverflowString_no_overflow_returns_empty()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 3,
        };
        blok.GetOverflowString().Should().BeEmpty();
    }

    [Test]
    public void GetOverflowString_single_overflow()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1230",
            OverflowVanVorigeBlokken =
            [
                new OverflowNpcs { VanStarttijdId = "Tijd1030", Minimum = 2 },
            ],
        };
        blok.GetOverflowString().Should().Be("+2");
    }

    [Test]
    public void GetOverflowString_range_overflow()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1230",
            OverflowVanVorigeBlokken =
            [
                new OverflowNpcs { VanStarttijdId = "Tijd1030", Minimum = 1, Maximum = 3 },
            ],
        };
        blok.GetOverflowString().Should().Be("+1-3");
    }

    [Test]
    public void HasOverflow_is_true_when_overflow_exists()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1230",
            OverflowVanVorigeBlokken =
            [
                new OverflowNpcs { VanStarttijdId = "Tijd1030", Minimum = 1 },
            ],
        };
        blok.HasOverflow.Should().BeTrue();
    }

    [Test]
    public void HasOverflow_is_false_when_no_overflow()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
        };
        blok.HasOverflow.Should().BeFalse();
    }

    [Test]
    public void GetNpcStatus_returns_null_when_verwacht_is_zero()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 5,
        };

        var (css, arrow) = blok.GetNpcStatus(0);

        css.Should().BeEmpty();
        arrow.Should().BeEmpty();
    }

    [Test]
    public void GetNpcStatus_returns_green_when_within_green_threshold()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 10, // verwacht 10, verschil = 0
        };

        var (css, arrow) = blok.GetNpcStatus(10);

        css.Should().Be("text-bg-success");
        arrow.Should().BeEmpty();
    }

    [Test]
    public void GetNpcStatus_returns_green_with_one_npc_difference()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 11, // verwacht 10, verschil = 1 <= max(1, 10/10=1)
        };

        var (css, arrow) = blok.GetNpcStatus(10);

        // A deficit of 1 when minimum is higher should be considered a warning (yellow)
        css.Should().Be("text-bg-warning");
        arrow.Should().Be("▲");
    }

    [Test]
    public void GetNpcStatus_returns_yellow_when_slightly_over()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 13, // verwacht 10, verschil = 3 <= max(3, 10/4=2) = 3
        };

        var (css, arrow) = blok.GetNpcStatus(10);

        css.Should().Be("text-bg-warning");
        arrow.Should().Be("▲");
    }

    [Test]
    public void GetNpcStatus_returns_red_when_significantly_over()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zondag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 20, // verwacht 10, verschil = 10 > max(3, 2) = 3
        };

        var (css, arrow) = blok.GetNpcStatus(10);

        css.Should().Be("text-bg-danger");
        arrow.Should().Be("▲");
    }

    [Test]
    public void GetNpcStatus_returns_yellow_when_slightly_under()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 7, // verwacht 10, verschil = -3 => |3| <= max(3, 2) = 3
        };

        var (css, arrow) = blok.GetNpcStatus(10);

        css.Should().Be("text-bg-warning");
        arrow.Should().Be("▼");
    }

    [Test]
    public void GetNpcStatus_returns_red_when_significantly_under()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Vrijdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 4, // verwacht 10, verschil = -6 > drempel
        };

        var (css, arrow) = blok.GetNpcStatus(10);

        css.Should().Be("text-bg-danger");
        arrow.Should().Be("▼");
    }

    [Test]
    public void GetNpcStatus_returns_yellow_when_expected_one_above_max()
    {
        // Bereik 1-3, verwacht 4 -> klein overschot, moet geel zijn
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 1,
            DirectMaximum = 3,
        };

        var (css, arrow) = blok.GetNpcStatus(4);

        css.Should().Be("text-bg-warning");
        arrow.Should().Be("▼");
    }

    [Test]
    public void GetNpcStatus_returns_green_for_alle_npcs_when_verwacht_set()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            HeeftAlleNpcs = true,
        };

        var (css, arrow) = blok.GetNpcStatus(12);

        css.Should().Be("text-bg-success");
        arrow.Should().BeEmpty();
    }

    [Test]
    public void GetNpcStatus_returns_green_when_verwacht_within_npc_range()
    {
        // Reproduceert de bug: bereik 1-6 NPCs, verwacht 4 → moet groen zijn
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd2000",
            DirectMinimum = 1,
            DirectMaximum = 6,
        };

        var (css, arrow) = blok.GetNpcStatus(4);

        css.Should().Be("text-bg-success");
        arrow.Should().BeEmpty();
    }

    [Test]
    public void GetNpcStatus_returns_green_when_verwacht_at_minimum_of_range()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 2,
            DirectMaximum = 8,
        };

        var (css, arrow) = blok.GetNpcStatus(2);

        css.Should().Be("text-bg-success");
        arrow.Should().BeEmpty();
    }

    [Test]
    public void GetNpcStatus_returns_green_when_verwacht_at_maximum_of_range()
    {
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 2,
            DirectMaximum = 8,
        };

        var (css, arrow) = blok.GetNpcStatus(8);

        css.Should().Be("text-bg-success");
        arrow.Should().BeEmpty();
    }

    [Test]
    public void GetNpcStatus_returns_yellow_when_verwacht_slightly_above_max()
    {
        // Bereik 1-6, verwacht 9: afstand boven max = 9-6=3, geelDrempel = max(3, 9/4=2) = 3
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zaterdag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 1,
            DirectMaximum = 6,
        };

        var (css, arrow) = blok.GetNpcStatus(9);

        css.Should().Be("text-bg-warning");
        arrow.Should().Be("▼");
    }

    [Test]
    public void GetNpcStatus_returns_red_when_verwacht_significantly_above_max()
    {
        // Bereik 1-6, verwacht 20: afstand boven max = 20-6=14, geelDrempel = max(3, 20/4=5) = 5
        var blok = new BlokNpcTotaal
        {
            Dag = Dag.Zondag,
            StarttijdId = "Tijd1030",
            DirectMinimum = 1,
            DirectMaximum = 6,
        };

        var (css, arrow) = blok.GetNpcStatus(20);

        css.Should().Be("text-bg-danger");
        arrow.Should().Be("▼");
    }
}

