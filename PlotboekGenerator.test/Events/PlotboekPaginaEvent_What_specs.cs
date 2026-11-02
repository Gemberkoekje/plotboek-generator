using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Events;

public sealed class PlotboekPaginaEvent_What_specs
{
    [Test]
    public void IconenAangepast_none_returns_verwijderd()
    {
        var e = new PlotboekPaginaIconenAangepast
        {
            Iconen = PlotboekPaginaIcons.None,
            Who = "user-1",
        };

        e.What.Should().Be("Iconen verwijderd");
    }

    [Test]
    public void IconenAangepast_with_flags_returns_list()
    {
        var e = new PlotboekPaginaIconenAangepast
        {
            Iconen = PlotboekPaginaIcons.TimingCruciaal | PlotboekPaginaIcons.PlotCruciaal,
            Who = "user-1",
        };

        e.What.Should().Contain("Timing Cruciaal");
        e.What.Should().Contain("Plot Cruciaal");
    }

    [Test]
    public void IconenAangepast_event_breed()
    {
        var e = new PlotboekPaginaIconenAangepast
        {
            Iconen = PlotboekPaginaIcons.EventBreed,
            Who = "user-1",
        };

        e.What.Should().Contain(Terminologie.Huidig.EventBreed);
    }

    [Test]
    public void IconenAangepast_all_icons()
    {
        var e = new PlotboekPaginaIconenAangepast
        {
            Iconen = PlotboekPaginaIcons.TimingCruciaal
                   | PlotboekPaginaIcons.PlotCruciaal
                   | PlotboekPaginaIcons.MagAlsEersteCancelled
                   | PlotboekPaginaIcons.UitgebreideBriefing
                   | PlotboekPaginaIcons.GrootPakOfKostuum
                   | PlotboekPaginaIcons.MuziekVanuitTechniek
                   | PlotboekPaginaIcons.LichtOfRookVanuitTechniek
                   | PlotboekPaginaIcons.EventBreed,
            Who = "user-1",
        };

        e.What.Should().Contain("Timing Cruciaal");
        e.What.Should().Contain(Terminologie.Huidig.MuziekVanuitTechniek);
        e.What.Should().Contain(Terminologie.Huidig.LichtOfRookVanuitTechniek);
        e.What.Should().Contain(Terminologie.Huidig.EventBreed);
    }

    [Test]
    public void AantalBlokkenAangepast_what_contains_display_name()
    {
        var e = new PlotboekPaginaAantalBlokkenAangepastV2
        {
            AantalBlokkenId = AantalBlokkenIds.TweeBlokken,
            Who = "user-1",
        };

        e.What.Should().Contain("Aantal blokken aangepast");
        e.What.Should().Contain(AantalBlokkenIds.TweeBlokken);
    }

    [Test]
    public void LocatieEncounterAangepast_with_value()
    {
        var e = new PlotboekPaginaLocatieEncounterAangepast
        {
            Locatie = LocatieEncounter.Questveld,
            Specificatie = "Bij de boom",
            Who = "user-1",
        };

        e.What.Should().Contain("Locatie encounter aangepast");
    }

    [Test]
    public void LocatieEncounterAangepast_empty_value()
    {
        var e = new PlotboekPaginaLocatieEncounterAangepast
        {
            Locatie = LocatieEncounter.None,
            Who = "user-1",
        };

        e.What.Should().Contain("(leeg)");
    }

    [Test]
    public void LocatieEncounterAangepastV2_mentions_the_raw_locatie_id()
    {
        var e = new PlotboekPaginaLocatieEncounterAangepastV2
        {
            Locatie = LocatieEncounter.Questveld,
            LocatieId = "Leeuwenkuil",
            Who = "user-1",
        };

        e.What.Should().Contain("Locatie encounter aangepast");
        e.What.Should().Contain("Leeuwenkuil", "the audit trail records the stable id, not a frozen display name");
    }

    [Test]
    public void LocatieEncounterAangepastV2_with_free_text_shows_the_specificatie()
    {
        var e = new PlotboekPaginaLocatieEncounterAangepastV2
        {
            Locatie = LocatieEncounter.Questveld,
            Specificatie = "Bij de boom",
            Who = "user-1",
        };

        e.What.Should().Contain("Questveld (Bij de boom)");
    }

    [Test]
    public void LocatieEncounterAangepastV2_encodes_markup_typed_in_the_specificatie()
    {
        // The audit modal writes What with Html.Raw, so the free-text half must arrive encoded.
        var e = new PlotboekPaginaLocatieEncounterAangepastV2
        {
            Locatie = LocatieEncounter.Groeperingsveld,
            Specificatie = "<img src=x onerror=alert(1)>",
            Who = "user-1",
        };

        e.What.Should().NotContain("<img");
        e.What.Should().Contain("&lt;img");
    }

    [Test]
    public void LocatieEncounterAangepast_encodes_markup_typed_in_the_specificatie()
    {
        // Same hole in the v1 event: old streams keep replaying through it, so a payload stored
        // before V2 existed would otherwise still fire in the audit modal.
        var e = new PlotboekPaginaLocatieEncounterAangepast
        {
            Locatie = LocatieEncounter.Groeperingsveld,
            Specificatie = "<img src=x onerror=alert(1)>",
            Who = "user-1",
        };

        e.What.Should().NotContain("<img");
        e.What.Should().Contain("&lt;img");
    }

    [Test]
    public void LocatieEncounterAangepastV2_empty_value()
    {
        var e = new PlotboekPaginaLocatieEncounterAangepastV2
        {
            Locatie = LocatieEncounter.None,
            Who = "user-1",
        };

        e.What.Should().Contain("(leeg)");
    }

    [Test]
    public void PlotlijnAangepast_with_plotlijn()
    {
        var e = new PlotboekPaginaPlotlijnAangepast
        {
            PlotlijnId = PlotlijnId.Next(),
            Who = "user-1",
        };

        e.What.Should().Contain("Toegevoegd aan plotlijn");
    }

    [Test]
    public void PlotlijnAangepast_removed_plotlijn()
    {
        var e = new PlotboekPaginaPlotlijnAangepast
        {
            PlotlijnId = default,
            Who = "user-1",
        };

        e.What.Should().Contain("zelfstandige encounter");
    }

    [Test]
    public void AantalNpcsAangepast_empty()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [],
            Who = "user-1",
        };

        e.What.Should().Contain("(leeg)");
    }

    [Test]
    public void AantalNpcsAangepast_with_roles()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 2, 4)],
            Who = "user-1",
        };

        e.What.Should().Contain("Aantal NPCs aangepast");
    }

    [Test]
    public void AantalNpcsAangepast_alle_npcs()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = true,
            Rollen = [],
            Who = "user-1",
        };

        e.What.Should().Contain("Aantal NPCs aangepast");
    }

    [Test]
    public void AantalNpcsAangepast_details_with_alle_npcs()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = true,
            Rollen = [NpcRolData.Create("guard", 2, 4)],
            Who = "user-1",
        };

        e.Details.Should().Contain("ALLE NPCs");
    }

    [Test]
    public void AantalNpcsAangepast_details_with_roles()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 3, 5)],
            Who = "user-1",
        };

        e.Details.Should().Contain("guard");
    }

    [Test]
    public void AantalNpcsAangepast_details_empty_returns_null()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [],
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void AantalNpcsAangepastV2_geen_npcs_nodig()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            GeenNpcsNodig = true,
            Rollen = [],
            Who = "user-1",
        };

        e.What.Should().Contain("Aantal NPCs aangepast");
        e.Details.Should().Contain("Geen NPC's nodig");
    }

    [Test]
    public void AantalNpcsAangepastV2_details_casting_instructie()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("edelman", 1, castingInstructie: "vrouwelijk uiterlijk")],
            Who = "user-1",
        };

        e.Details.Should().Contain("Casting: vrouwelijk uiterlijk");
    }

    [Test]
    public void AantalNpcsAangepastV2_details_vereist_afstemming_with_toelichting()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("slachtoffer", 1, vereistAfstemming: true, afstemmingToelichting: "wordt gemarteld in scene 2")],
            Who = "user-1",
        };

        e.Details.Should().Contain("Vereist afstemming: wordt gemarteld in scene 2");
    }

    [Test]
    public void AantalNpcsAangepastV2_details_vereist_afstemming_without_toelichting()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("slachtoffer", 1, vereistAfstemming: true)],
            Who = "user-1",
        };

        e.Details.Should().Contain("Vereist afstemming<");
    }

    // Details is written with Html.Raw, so free text the user typed may never come back as live markup.
    [Test]
    public void AantalNpcsAangepastV2_details_encodes_casting_text()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("edelman", 1, castingInstructie: "<script>alert(1)</script>")],
            Who = "user-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    // Every freely-typed field on a role reaches the same Html.Raw-rendered blob, so each one needs its own
    // guard — the role name most of all, since it is required on every role.
    [Test]
    public void AantalNpcsAangepastV2_details_encodes_afstemming_toelichting()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("slachtoffer", 1, vereistAfstemming: true, afstemmingToelichting: "<script>alert(1)</script>")],
            Who = "user-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    [Test]
    public void AantalNpcsAangepastV2_details_encodes_rol_name()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("<img src=x onerror=alert(1)>", 1)],
            Who = "user-1",
        };

        e.Details.Should().NotContain("<img");
        e.Details.Should().Contain("&lt;img");
    }

    [Test]
    public void AantalNpcsAangepastV2_details_encodes_kleding_text()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 1, kledingEnSchmink: "<script>alert(1)</script>")],
            Who = "user-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    [Test]
    public void AantalNpcsAangepastV2_details_encodes_gespeeld_door()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 1, isEerderGespeeld: true, gespeeldDoor: "<script>alert(1)</script>")],
            Who = "user-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    [Test]
    public void AantalNpcsAangepastV2_details_encodes_gekozen_persoon()
    {
        var e = new PlotboekPaginaAantalNpcsAangepastV2
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 1, isAlGekozen: true, gekozenPersoon: "<script>alert(1)</script>")],
            Who = "user-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    // The v1 event still renders historical streams, and those carry the same freely-typed fields.
    [Test]
    public void AantalNpcsAangepast_v1_details_encodes_rol_name()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("<img src=x onerror=alert(1)>", 1, kledingEnSchmink: "<b>malienkolder</b>")],
            Who = "user-1",
        };

        e.Details.Should().NotContain("<img");
        e.Details.Should().NotContain("<b>");
        e.Details.Should().Contain("&lt;img");
        e.Details.Should().Contain("&lt;b&gt;");
    }

    [Test]
    public void GeschatteTijdsduurAangepast_with_value()
    {
        var e = new PlotboekPaginaGeschatteTijdsduurAangepast
        {
            MinimumMinuten = 60,
            MaximumMinuten = 120,
            Who = "user-1",
        };

        e.What.Should().Contain("Geschatte tijdsduur aangepast");
    }

    [Test]
    public void GeschatteTijdsduurAangepast_empty_value()
    {
        var e = new PlotboekPaginaGeschatteTijdsduurAangepast
        {
            MinimumMinuten = null,
            MaximumMinuten = null,
            Who = "user-1",
        };

        e.What.Should().Contain("(leeg)");
    }

    [Test]
    public void AantalNpcsAangepast_details_format_range()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 2, 5)],
            Who = "user-1",
        };

        e.Details.Should().Contain("2-5");
        e.Details.Should().Contain("guard");
    }

    [Test]
    public void AantalNpcsAangepast_details_format_exact()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("spy", 3)],
            Who = "user-1",
        };

        e.Details.Should().Contain("3 spy");
    }

    [Test]
    public void AantalNpcsAangepast_details_format_no_count()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = true,
            Rollen = [NpcRolData.Create("Vechters", 0)],
            Who = "user-1",
        };

        e.Details.Should().Contain("Vechters");
    }

    [Test]
    public void AantalNpcsAangepast_details_terugkerend()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 2, isTerukerend: true)],
            Who = "user-1",
        };

        e.Details.Should().Contain("terugkerend");
    }

    [Test]
    public void AantalNpcsAangepast_details_eerder_gespeeld_without_name()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("edelman", 1, isEerderGespeeld: true)],
            Who = "user-1",
        };

        e.Details.Should().Contain("eerder gespeeld");
    }

    [Test]
    public void AantalNpcsAangepast_details_eerder_gespeeld_with_name()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("edelman", 1, isEerderGespeeld: true, gespeeldDoor: "Jan")],
            Who = "user-1",
        };

        e.Details.Should().Contain("eerder gespeeld door Jan");
    }

    [Test]
    public void AantalNpcsAangepast_details_al_gekozen_without_name()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("ridder", 1, isAlGekozen: true)],
            Who = "user-1",
        };

        e.Details.Should().Contain("al gekozen");
    }

    [Test]
    public void AantalNpcsAangepast_details_al_gekozen_with_name()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("ridder", 1, isAlGekozen: true, gekozenPersoon: "Piet")],
            Who = "user-1",
        };

        e.Details.Should().Contain("gekozen: Piet");
    }

    [Test]
    public void AantalNpcsAangepast_details_kleding_en_schmink()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("guard", 2, kledingEnSchmink: "malienkolder, wapens")],
            Who = "user-1",
        };

        e.Details.Should().Contain("Kleding: malienkolder, wapens");
    }

    [Test]
    public void AantalNpcsAangepast_details_multiple_annotations()
    {
        var e = new PlotboekPaginaAantalNpcsAangepast
        {
            AlleNpcs = false,
            Rollen = [NpcRolData.Create("edelman", 1, 3, isTerukerend: true, isEerderGespeeld: true, gespeeldDoor: "Jan", kledingEnSchmink: "adellijk gewaad")],
            Who = "user-1",
        };

        e.Details.Should().Contain("terugkerend");
        e.Details.Should().Contain("eerder gespeeld door Jan");
        e.Details.Should().Contain("Kleding: adellijk gewaad");
    }

    [Test]
    public void TitelAangepast_with_value()
    {
        var e = new PlotboekPaginaTitelAangepast
        {
            Titel = "De Verloren Tempel",
            Who = "user-1",
        };

        e.What.Should().Contain("Titel aangepast");
        e.What.Should().Contain("De Verloren Tempel");
    }

    [Test]
    public void DagAangepast_with_value()
    {
        var e = new PlotboekPaginaDagAangepast
        {
            Dag = Dag.Zaterdag,
            Who = "user-1",
        };

        e.What.Should().Contain("Dag aangepast");
        e.What.Should().Contain("Zaterdag");
    }

    [Test]
    public void DagAangepast_none()
    {
        var e = new PlotboekPaginaDagAangepast
        {
            Dag = Dag.None,
            Who = "user-1",
        };

        e.What.Should().Contain("Dag aangepast");
    }

    [Test]
    public void DomainAangepast_with_domain()
    {
        var e = new PlotboekPaginaDomainAangepast
        {
            DomainId = Guid.NewGuid(),
            DomainShorthand = "Ti",
            DomainDescription = "Titania",
            Who = "user-1",
        };

        e.What.Should().Contain("Domein aangepast");
        e.What.Should().Contain("Ti");
        e.What.Should().Contain("Titania");
    }

    [Test]
    public void DomainAangepast_removed()
    {
        var e = new PlotboekPaginaDomainAangepast
        {
            DomainId = Guid.Empty,
            Who = "user-1",
        };

        e.What.Should().Contain("Domein verwijderd");
    }

    [Test]
    public void ElevatorPitchAangepast_with_value()
    {
        var e = new PlotboekPaginaElevatorPitchAangepast
        {
            ElevatorPitch = "Een spannende encounter in het bos",
            Who = "user-1",
        };

        e.What.Should().Contain("Elevator pitch aangepast");
        e.What.Should().Contain("Een spannende encounter in het bos");
    }

    [Test]
    public void EncounterTypesAangepast_with_types()
    {
        var e = new PlotboekPaginaEncounterTypesAangepastV2
        {
            Major = [SoortEncounterIds.Vechten],
            Minor = [SoortEncounterIds.Roleplay],
            Who = "user-1",
        };

        e.What.Should().Contain("Soort encounter aangepast");
    }

    [Test]
    public void EncounterTypesAangepast_empty()
    {
        var e = new PlotboekPaginaEncounterTypesAangepast
        {
            Major = [],
            Minor = [],
            Who = "user-1",
        };

        e.What.Should().Contain("(leeg)");
    }

    [Test]
    public void InstructiePentagonAangepast_what()
    {
        var e = new PlotboekPaginaInstructiePentagonAangepast
        {
            InstructiePentagon = "<p>Instructies voor de techniek</p>",
            Who = "user-1",
        };

        e.What.Should().Contain($"{Terminologie.Huidig.InstructieTechniek} aangepast");
    }

    [Test]
    public void InstructiePentagonAangepast_details_with_content()
    {
        var e = new PlotboekPaginaInstructiePentagonAangepast
        {
            InstructiePentagon = "<p>Instructies voor de techniek</p>",
            Who = "user-1",
        };

        e.Details.Should().Contain("Instructies voor de techniek");
    }

    [Test]
    public void InstructiePentagonAangepast_details_empty_returns_null()
    {
        var e = new PlotboekPaginaInstructiePentagonAangepast
        {
            InstructiePentagon = "  ",
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void IsFlexibelAangepast_true()
    {
        var e = new PlotboekPaginaIsFlexibelAangepast
        {
            IsFlexibel = true,
            Who = "user-1",
        };

        e.What.Should().Contain("flexibel");
    }

    [Test]
    public void IsFlexibelAangepast_false()
    {
        var e = new PlotboekPaginaIsFlexibelAangepast
        {
            IsFlexibel = false,
            Who = "user-1",
        };

        e.What.Should().Contain("niet flexibel");
    }

    [Test]
    public void IsFlexibelNachtPlotAangepast_true()
    {
        var e = new PlotboekPaginaIsFlexibelNachtPlotAangepast
        {
            IsFlexibelNachtPlot = true,
            Who = "user-1",
        };

        e.What.Should().Contain("flexibel nacht plot");
    }

    [Test]
    public void IsFlexibelNachtPlotAangepast_false()
    {
        var e = new PlotboekPaginaIsFlexibelNachtPlotAangepast
        {
            IsFlexibelNachtPlot = false,
            Who = "user-1",
        };

        e.What.Should().Contain("niet flexibel nacht plot");
    }

    [Test]
    public void IsGedraaidAangepast_true()
    {
        var e = new PlotboekPaginaIsGedraaidAangepast
        {
            IsGedraaid = true,
            Who = "user-1",
        };

        e.What.Should().Contain("gedraaid");
    }

    [Test]
    public void IsGedraaidAangepast_false()
    {
        var e = new PlotboekPaginaIsGedraaidAangepast
        {
            IsGedraaid = false,
            Who = "user-1",
        };

        e.What.Should().Contain("niet gedraaid");
    }

    [Test]
    public void IsSpelerGedrevenAangepast_true()
    {
        var e = new PlotboekPaginaIsSpelerGedrevenAangepast
        {
            IsSpelerGedreven = true,
            Who = "user-1",
        };

        e.What.Should().Contain("speler gedreven");
    }

    [Test]
    public void IsSpelerGedrevenAangepast_false()
    {
        var e = new PlotboekPaginaIsSpelerGedrevenAangepast
        {
            IsSpelerGedreven = false,
            Who = "user-1",
        };

        e.What.Should().Contain("niet speler gedreven");
    }

    [Test]
    public void IsVerwijderdAangepast_true()
    {
        var e = new PlotboekPaginaIsVerwijderdAangepast
        {
            IsVerwijderd = true,
            Who = "user-1",
        };

        e.What.Should().Contain("Pagina verwijderd");
    }

    [Test]
    public void IsVerwijderdAangepast_false()
    {
        var e = new PlotboekPaginaIsVerwijderdAangepast
        {
            IsVerwijderd = false,
            Who = "user-1",
        };

        e.What.Should().Contain("Pagina hersteld");
    }

    [Test]
    public void MeerInformatieAangepast_what()
    {
        var e = new PlotboekPaginaMeerInformatieAangepast
        {
            MeerInformatie = "<p>Extra informatie</p>",
            Who = "user-1",
        };

        e.What.Should().Contain("Meer informatie aangepast");
    }

    [Test]
    public void MeerInformatieAangepast_details_with_content()
    {
        var e = new PlotboekPaginaMeerInformatieAangepast
        {
            MeerInformatie = "<p>Extra informatie</p>",
            Who = "user-1",
        };

        e.Details.Should().Contain("Extra informatie");
    }

    [Test]
    public void MeerInformatieAangepast_details_empty_returns_null()
    {
        var e = new PlotboekPaginaMeerInformatieAangepast
        {
            MeerInformatie = "  ",
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void OpmerkingenAangepast_what()
    {
        var e = new PlotboekPaginaOpmerkingenAangepast
        {
            Opmerkingen = "Let op de timing",
            Who = "user-1",
        };

        e.What.Should().Contain("Opmerkingen aangepast");
    }

    [Test]
    public void OpmerkingenAangepast_details_with_content()
    {
        var e = new PlotboekPaginaOpmerkingenAangepast
        {
            Opmerkingen = "Let op de timing",
            Who = "user-1",
        };

        e.Details.Should().Contain("Let op de timing");
    }

    [Test]
    public void OpmerkingenAangepast_details_empty_returns_null()
    {
        var e = new PlotboekPaginaOpmerkingenAangepast
        {
            Opmerkingen = "  ",
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void OrderAangepast_what()
    {
        var e = new PlotboekPaginaOrderAangepastV2
        {
            Order = 5,
            Dag = Dag.Vrijdag,
            StarttijdId = StarttijdIds.Tijd2000,
            Who = "user-1",
        };

        e.What.Should().Contain("Volgorde aangepast");
        e.What.Should().Contain("positie 5");
        e.What.Should().Contain("Vrijdag");
    }

    [Test]
    public void PropsEnLootAangepast_with_items()
    {
        var e = new PlotboekPaginaPropsEnLootAangepast
        {
            Props = [PropLootItemData.Create("Zwaard")],
            Loot = [PropLootItemData.Create("Goudstukken")],
            Who = "user-1",
        };

        e.What.Should().Contain("Props &amp; Loot aangepast");
    }

    [Test]
    public void PropsEnLootAangepast_empty()
    {
        var e = new PlotboekPaginaPropsEnLootAangepast
        {
            Props = [],
            Loot = [],
            Who = "user-1",
        };

        e.What.Should().Contain("(leeg)");
    }

    [Test]
    public void PropsEnLootAangepast_details_with_props_and_loot()
    {
        var e = new PlotboekPaginaPropsEnLootAangepast
        {
            Props = [PropLootItemData.Create("Zwaard", verantwoordelijkeSlName: "Jan")],
            Loot = [PropLootItemData.Create("Goudstukken")],
            Who = "user-1",
        };

        e.Details.Should().Contain("Props:");
        e.Details.Should().Contain("Zwaard");
        e.Details.Should().Contain("Jan");
        e.Details.Should().Contain("Loot:");
        e.Details.Should().Contain("Goudstukken");
    }

    [Test]
    public void PropsEnLootAangepast_details_empty_returns_null()
    {
        var e = new PlotboekPaginaPropsEnLootAangepast
        {
            Props = [],
            Loot = [],
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void SamenvattingPlotAangepast_what()
    {
        var e = new PlotboekPaginaSamenvattingPlotAangepast
        {
            SamenvattingPlot = "<p>De held redt het dorp</p>",
            Who = "user-1",
        };

        e.What.Should().Contain("Samenvatting plot aangepast");
    }

    [Test]
    public void SamenvattingPlotAangepast_details_with_content()
    {
        var e = new PlotboekPaginaSamenvattingPlotAangepast
        {
            SamenvattingPlot = "<p>De held redt het dorp</p>",
            Who = "user-1",
        };

        e.Details.Should().Contain("De held redt het dorp");
    }

    [Test]
    public void SamenvattingPlotAangepast_details_empty_returns_null()
    {
        var e = new PlotboekPaginaSamenvattingPlotAangepast
        {
            SamenvattingPlot = "  ",
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void StarttijdAangepast_with_value()
    {
        var e = new PlotboekPaginaStarttijdAangepastV2
        {
            StarttijdId = StarttijdIds.Tijd2000,
            Who = "user-1",
        };

        e.What.Should().Contain("Starttijd aangepast");
        e.What.Should().Contain(StarttijdIds.Tijd2000);
    }

    [Test]
    public void VerantwoordelijkeSlAangepast_with_name()
    {
        var e = new PlotboekPaginaVerantwoordelijkeSlAangepast
        {
            VerantwoordelijkeSlId = "sl-1",
            VerantwoordelijkeSlName = "Jan",
            Who = "user-1",
        };

        e.What.Should().Contain("Verantwoordelijke SL aangepast");
        e.What.Should().Contain("Jan");
    }

    [Test]
    public void VerantwoordelijkeSlAangepast_empty()
    {
        var e = new PlotboekPaginaVerantwoordelijkeSlAangepast
        {
            VerantwoordelijkeSlId = string.Empty,
            VerantwoordelijkeSlName = string.Empty,
            Who = "user-1",
        };

        e.What.Should().Contain("Niet ingevuld");
    }

    [Test]
    public void VerantwoordelijkeSlAangepast_with_extra_sls()
    {
        var e = new PlotboekPaginaVerantwoordelijkeSlAangepast
        {
            VerantwoordelijkeSlId = "sl-1",
            VerantwoordelijkeSlName = "Jan",
            ExtraSlIds = ["sl-2"],
            ExtraSlNames = ["Piet"],
            Who = "user-1",
        };

        e.What.Should().Contain("Jan");
        e.What.Should().Contain("Piet");
    }

    [Test]
    public void VolledigKlaarAangepast_true()
    {
        var e = new PlotboekPaginaVolledigKlaarAangepast
        {
            IsVolledigKlaar = true,
            Who = "user-1",
        };

        e.What.Should().Contain("volledig klaar");
    }

    [Test]
    public void VolledigKlaarAangepast_false()
    {
        var e = new PlotboekPaginaVolledigKlaarAangepast
        {
            IsVolledigKlaar = false,
            Who = "user-1",
        };

        e.What.Should().Contain("nog niet klaar");
    }

    [Test]
    public void AanPlotboekToegevoegd_what()
    {
        var e = new PlotboekPaginaAanPlotboekToegevoegd
        {
            PlotboekId = PlotboekId.Create(Year.Create(2025), EventIds.Zomer),
            PlotboekName = "Zomer 2025",
            Who = "user-1",
        };

        e.What.Should().Contain("Pagina toegevoegd");
        e.What.Should().Contain("Zomer 2025");
    }

    [Test]
    public void DebriefAangepast_uitgeschakeld()
    {
        var e = new PlotboekPaginaDebriefAangepast
        {
            IsDebriefGewenst = false,
            Vragen = [],
            Who = "user-1",
        };

        e.What.Should().Contain("Debrief uitgeschakeld");
    }

    [Test]
    public void DebriefAangepast_ingeschakeld_without_vragen()
    {
        var e = new PlotboekPaginaDebriefAangepast
        {
            IsDebriefGewenst = true,
            Vragen = [],
            Who = "user-1",
        };

        e.What.Should().Contain("Debrief ingeschakeld");
        e.What.Should().NotContain("vragen");
    }

    [Test]
    public void DebriefAangepast_ingeschakeld_with_vragen()
    {
        var e = new PlotboekPaginaDebriefAangepast
        {
            IsDebriefGewenst = true,
            Vragen = ["Hoe ging het gevecht?", "Was de NPC overtuigend?"],
            Who = "user-1",
        };

        e.What.Should().Contain("Debrief ingeschakeld met vragen");
    }

    [Test]
    public void DebriefAangepast_details_with_vragen()
    {
        var e = new PlotboekPaginaDebriefAangepast
        {
            IsDebriefGewenst = true,
            Vragen = ["Hoe ging het gevecht?", "Was de NPC overtuigend?"],
            Who = "user-1",
        };

        e.Details.Should().Contain("Hoe ging het gevecht?");
        e.Details.Should().Contain("Was de NPC overtuigend?");
    }

    [Test]
    public void DebriefAangepast_details_uitgeschakeld_returns_null()
    {
        var e = new PlotboekPaginaDebriefAangepast
        {
            IsDebriefGewenst = false,
            Vragen = ["Hoe ging het gevecht?"],
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void DebriefAangepast_details_no_vragen_returns_null()
    {
        var e = new PlotboekPaginaDebriefAangepast
        {
            IsDebriefGewenst = true,
            Vragen = [],
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void BijlagenAangepast_what()
    {
        var e = new PlotboekPaginaBijlagenAangepast
        {
            BijlageIds = [BijlageId.Next()],
            Who = "user-1",
        };

        e.What.Should().Contain("Gekoppelde bijlagen aangepast");
    }

    [Test]
    public void BijlagenAangepast_details_with_ids()
    {
        var id = BijlageId.Next();
        var e = new PlotboekPaginaBijlagenAangepast
        {
            BijlageIds = [id],
            Who = "user-1",
        };

        e.Details.Should().Contain(id.ToString());
    }

    [Test]
    public void BijlagenAangepast_details_empty_returns_null()
    {
        var e = new PlotboekPaginaBijlagenAangepast
        {
            BijlageIds = [],
            Who = "user-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void FeedbackToegevoegd_what_and_details()
    {
        var e = new PlotboekPaginaFeedbackToegevoegd
        {
            Id = FeedbackId.Next(),
            Tekst = "De NPC-rollen kloppen niet.",
            Who = "hsl-1",
        };

        e.What.Should().Contain("Feedback toegevoegd");
        e.Details.Should().Be("De NPC-rollen kloppen niet.");
    }

    [Test]
    public void FeedbackToegevoegd_details_encodes_markup_typed_by_the_user()
    {
        // The audit modal writes Details with Html.Raw, so plain-text fields must arrive encoded.
        var e = new PlotboekPaginaFeedbackToegevoegd
        {
            Id = FeedbackId.Next(),
            Tekst = "<script>alert(1)</script>",
            Who = "hsl-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    [Test]
    public void FeedbackToegevoegd_details_turns_line_breaks_into_br()
    {
        var e = new PlotboekPaginaFeedbackToegevoegd
        {
            Id = FeedbackId.Next(),
            Tekst = "Eerste regel\nTweede regel",
            Who = "hsl-1",
        };

        e.Details.Should().Be("Eerste regel<br />Tweede regel");
    }

    [Test]
    public void FeedbackToegevoegd_details_empty_returns_null()
    {
        var e = new PlotboekPaginaFeedbackToegevoegd
        {
            Id = FeedbackId.Next(),
            Tekst = "   ",
            Who = "hsl-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void FeedbackBeantwoord_what_and_details()
    {
        var e = new PlotboekPaginaFeedbackBeantwoord
        {
            Id = FeedbackId.Next(),
            Reactie = "Aangepast, zie de nieuwe rollen.",
            Who = "sl-1",
        };

        e.What.Should().Contain("Op feedback gereageerd");
        e.Details.Should().Be("Aangepast, zie de nieuwe rollen.");
    }

    [Test]
    public void FeedbackBeantwoord_details_encodes_markup_typed_by_the_user()
    {
        // Same contract as FeedbackToegevoegd: a benign-text assertion alone would pass even if the
        // encoding were dropped, so the payload case has to be spelled out per event.
        var e = new PlotboekPaginaFeedbackBeantwoord
        {
            Id = FeedbackId.Next(),
            Reactie = "<script>alert(1)</script>",
            Who = "sl-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    [Test]
    public void FeedbackAfgehandeld_what_and_no_details()
    {
        var e = new PlotboekPaginaFeedbackAfgehandeld
        {
            Id = FeedbackId.Next(),
            Who = "hsl-1",
        };

        e.What.Should().Contain("Feedback afgehandeld");
        e.Details.Should().BeNull();
    }

    [Test]
    public void FeedbackHeropend_what_and_details()
    {
        var e = new PlotboekPaginaFeedbackHeropend
        {
            Id = FeedbackId.Next(),
            Reden = "Toch nog niet goed.",
            Who = "hsl-1",
        };

        e.What.Should().Contain("Feedback heropend");
        e.Details.Should().Be("Toch nog niet goed.");
    }

    [Test]
    public void FeedbackHeropend_details_encodes_markup_typed_by_the_user()
    {
        var e = new PlotboekPaginaFeedbackHeropend
        {
            Id = FeedbackId.Next(),
            Reden = "<script>alert(1)</script>",
            Who = "hsl-1",
        };

        e.Details.Should().NotContain("<script>");
        e.Details.Should().Contain("&lt;script&gt;");
    }

    [Test]
    public void FeedbackHeropend_without_reason_has_no_details()
    {
        var e = new PlotboekPaginaFeedbackHeropend
        {
            Id = FeedbackId.Next(),
            Reden = string.Empty,
            Who = "hsl-1",
        };

        e.Details.Should().BeNull();
    }

    [Test]
    public void FeedbackVerwijderd_what_and_no_details()
    {
        var e = new PlotboekPaginaFeedbackVerwijderd
        {
            Id = FeedbackId.Next(),
            Who = "hsl-1",
        };

        e.What.Should().Contain("Feedback verwijderd");
        e.Details.Should().BeNull();
    }
}
