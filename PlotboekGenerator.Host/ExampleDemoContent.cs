using PlotboekGenerator.Application.DTOs;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.core.Models;

namespace PlotboekGenerator.Host;

/// <summary>
/// The example a demo of the generic deployment shows: one spring event with a plot line through it,
/// in every state a plot passes through. Some plots are finished, one waits on feedback, one only
/// needs its tick, and one has barely been started. It uses the ids of <see cref="ExampleReferenceDataSeed"/>.
/// </summary>
internal sealed class ExampleDemoContent : IDemoContent
{
    private const string Hof = "demo-sl-hof";
    private const string Gilden = "demo-sl-gilden";
    private const string Tempel = "demo-sl-tempel";
    private const string Wildernis = "demo-sl-wildernis";
    private const string Hoofdspelleiding = "demo-sl-hoofdspelleiding";

    /// <inheritdoc />
    public IReadOnlyList<DemoSpelleider> Spelleiders { get; } =
    [
        new DemoSpelleider { Id = Hof, Voornaam = "Mirthe", Achternaam = "Valkenburg", DomainShorthand = "H" },
        new DemoSpelleider { Id = Gilden, Voornaam = "Joost", Achternaam = "Kramer", DomainShorthand = "G" },
        new DemoSpelleider { Id = Tempel, Voornaam = "Femke", Achternaam = "Haverkamp", DomainShorthand = "T" },
        new DemoSpelleider { Id = Wildernis, Voornaam = "Ruben", Achternaam = "Ossendrijver", DomainShorthand = "W" },
        new DemoSpelleider { Id = Hoofdspelleiding, Voornaam = "Noor", Achternaam = "Bakker", DomainShorthand = "O" },
    ];

    /// <inheritdoc />
    public IReadOnlyList<DemoPlotboek> Plotboeken { get; } =
    [
        new DemoPlotboek
        {
            EventId = "Voorjaar",
            Titel = "De Nevelkroon",
            AangemaaktDoorId = Hoofdspelleiding,
            SamenvattingAlgemeenPlot =
                "<p>Voor het eerst in een generatie trekt de nevel op uit het Nevelmeer, en met de nevel komt een bode die " +
                "beweert dat de verloren kroon van Nevelmeer is teruggevonden. Het hof wil de kroon voor zich, de gilden " +
                "willen weten wat hij waard is, en de tempel waarschuwt dat hij nooit had mogen worden gevonden.</p>" +
                "<p>Het weekend draait om één vraag: wie draagt de kroon als hij op zaterdagavond ontwaakt?</p>",
            Paginas =
            [
                new DemoPagina
                {
                    DomainShorthand = "H",
                    Plotlijn = "kroon",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "De bode uit Nevelmeer",
                        Dag = Dag.Vrijdag,
                        StarttijdId = "Tijd2000",
                        Order = 1,
                        VerantwoordelijkeSlId = Hof,
                        AantalBlokkenId = "EenBlok",
                        EncounterTypeIds = EncounterTypeIds.Create(["Roleplay"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld),
                        AantalNpcs = AantalNpcsData.Create(
                        [
                            NpcRolData.Create("Bode van Nevelmeer", 1, kledingEnSchmink: "Grijze reismantel, modderige laarzen, bleke schmink"),
                            NpcRolData.Create("Escorte", 2, kledingEnSchmink: "Leren wapenrusting met een blauwe sjerp"),
                        ]),
                        ElevatorPitch = "Een uitgeputte bode komt uit de nevel en vraagt het hof om bescherming: hij draagt het bericht dat de kroon van Nevelmeer is gevonden.",
                        SamenvattingPlot =
                            "<p>Bij tijd-in loopt de bode met zijn escorte het hoofdveld op. Hij vraagt naar de hofmeester en weigert met iemand anders te spreken.</p>" +
                            "<p>Zijn brief is verzegeld met het wapen van Nevelmeer. Wie hem leest, weet dat de kroon in de ruïne ligt, maar niet waar precies: dat staat in het archief.</p>",
                        InstructiePentagon = "<p>Mist over het hoofdveld zodra de bode binnenkomt; na tien minuten langzaam laten wegtrekken.</p>",
                        Iconen = PlotboekPaginaIcons.TimingCruciaal | PlotboekPaginaIcons.PlotCruciaal | PlotboekPaginaIcons.LichtOfRookVanuitTechniek,
                        PropsEnLoot = PropsEnLootData.Create([PropLootItemData.Create("Verzegelde brief met het wapen van Nevelmeer", Hof)], []),
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "W",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "Dwaallichten in het Beekdal",
                        Dag = Dag.Vrijdag,
                        StarttijdId = "Tijd2230",
                        Order = 1,
                        VerantwoordelijkeSlId = Wildernis,
                        AantalBlokkenId = "TweeBlokken",
                        EncounterTypeIds = EncounterTypeIds.Create(["Vechten"], ["Magie"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Beekdal"),
                        AantalNpcs = AantalNpcsData.Create(
                        [
                            NpcRolData.Create("Dwaallicht", 4, 6, kledingEnSchmink: "Wit gewaad, een lampje in de hand, verder alles donker"),
                            NpcRolData.Create("Verdwaalde jager", 1, kledingEnSchmink: "Groene kleding met kap", castingInstructie: "Iemand die goed bang kan spelen"),
                        ]),
                        ElevatorPitch = "In het Beekdal dansen lichtjes boven het water. Wie ze volgt, raakt de weg kwijt; wie ze vangt, houdt een stukje nevelkristal over.",
                        SamenvattingPlot =
                            "<p>Een verdwaalde jager komt het kamp in en smeekt om hulp: zijn makker is de lichtjes in het Beekdal gevolgd en niet teruggekomen.</p>" +
                            "<p>In het dal lokken de dwaallichten de spelers het water in. Een licht dat geraakt wordt, dooft en laat een kristal achter.</p>",
                        PropsEnLoot = PropsEnLootData.Create([], [PropLootItemData.Create("Drie stukjes nevelkristal", Wildernis)]),
                        IsFlexibel = true,
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "G",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "De voorjaarsmarkt",
                        Dag = Dag.Zaterdag,
                        StarttijdId = "Tijd1000",
                        Order = 1,
                        VerantwoordelijkeSlId = Gilden,
                        AantalBlokkenId = "DrieBlokken",
                        EncounterTypeIds = EncounterTypeIds.Create(["Handel"], ["Politiek"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld, "Marktplein bij de gildenhal"),
                        AantalNpcs = AantalNpcsData.Create(
                        [
                            NpcRolData.Create("Marskramer", 3, 5, kledingEnSchmink: "Kleurige burgerkleding met een schort"),
                            NpcRolData.Create("Marktmeester", 1, kledingEnSchmink: "Zwarte tabberd met ambtsketen"),
                        ]),
                        ElevatorPitch = "De gilden openen het seizoen met een markt. Iedereen wil nevelkristal kopen, en de marktmeester wil weten waar het vandaan komt.",
                        SamenvattingPlot =
                            "<p>Een gewone markt met handel en praatjes, tot de marktmeester een opkoper van nevelkristal arresteert.</p>" +
                            "<p>Spelers met kristal uit het Beekdal kunnen het hier verkopen, of ermee naar de tempel gaan.</p>",
                        Iconen = PlotboekPaginaIcons.MagAlsEersteCancelled,
                        PropsEnLoot = PropsEnLootData.Create(
                            [PropLootItemData.Create("Kraam met koopwaar", verantwoordelijkeSource: "Anders", verantwoordelijkeVrijeTekst: "Gildenkist")],
                            []),
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "T",
                    Plotlijn = "kroon",
                    Feedback =
                    [
                        new DemoFeedback
                        {
                            GegevenDoorId = Hoofdspelleiding,
                            Tekst = "Hoe weten de spelers dat ze naar de ruïne moeten? Ik mis de aanwijzing uit de brief van vrijdag.",
                        },
                    ],
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "Het verzegelde archief",
                        Dag = Dag.Zaterdag,
                        StarttijdId = "Tijd1300",
                        Order = 1,
                        VerantwoordelijkeSlId = Tempel,
                        AantalBlokkenId = "TweeBlokken",
                        EncounterTypeIds = EncounterTypeIds.Create(["Puzzel"], ["Ritueel"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Ruine"),
                        AantalNpcs = AantalNpcsData.Create(
                        [
                            NpcRolData.Create(
                                "Archivaris",
                                1,
                                kledingEnSchmink: "Grauwe pij, inktvlekken op de handen",
                                vereistAfstemming: true,
                                afstemmingToelichting: "Speelt blind: moet het archief vooraf met de SL doorlopen"),
                        ]),
                        ElevatorPitch = "Onder de ruïne ligt het archief van Nevelmeer, en de archivaris laat alleen wie zijn raadsels oplost binnen.",
                        SamenvattingPlot =
                            "<p>De archivaris bewaakt de kaart die laat zien waar de kroon ligt. Hij geeft hem alleen aan wie drie raadsels over de geschiedenis van Nevelmeer oplost.</p>",
                        MeerInformatie = "<p>De raadsels en hun antwoorden staan in de bijlage van de SL; neem ze geprint mee.</p>",
                        IsVolledigKlaar = false,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "H",
                    Plotlijn = "kroon",
                    Feedback =
                    [
                        new DemoFeedback
                        {
                            GegevenDoorId = Hoofdspelleiding,
                            Tekst = "Is er een uitwijk bij regen? Het hoofdveld staat dan blank.",
                            Reactie = "Ja: bij regen in de grote tent. De muziek gaat mee.",
                            IsAfgehandeld = true,
                        },
                    ],
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "De Nevelkroon ontwaakt",
                        Dag = Dag.Zaterdag,
                        StarttijdId = "Tijd2000",
                        Order = 1,
                        VerantwoordelijkeSlId = Hof,
                        ExtraSlIds = [Tempel],
                        AantalBlokkenId = "DrieBlokken",
                        EncounterTypeIds = EncounterTypeIds.Create(["Ritueel"], ["Vechten"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld),
                        AantalNpcs = AantalNpcsData.Create(
                            [NpcRolData.Create("Nevelgeest", 0, kledingEnSchmink: "Grijs gaas over donkere kleding, witte schmink")],
                            alleNpcs: true),
                        ElevatorPitch = "Wie de kroon opzet, krijgt de stem van Nevelmeer. Het hele veld kiest partij terwijl de nevelgeesten terugkeren.",
                        SamenvattingPlot =
                            "<p>De kroon wordt op het hoofdveld gebracht. Zodra iemand hem opzet, stijgen de nevelgeesten op en eisen een belofte.</p>" +
                            "<p>Wat er beloofd wordt, bepaalt het plot van het volgende event.</p>",
                        InstructiePentagon = "<p>Langzaam aanzwellend koor vanaf het moment dat de kroon wordt opgetild; stil zodra iemand hem opzet.</p>",
                        Iconen = PlotboekPaginaIcons.EventBreed | PlotboekPaginaIcons.PlotCruciaal | PlotboekPaginaIcons.TimingCruciaal
                            | PlotboekPaginaIcons.UitgebreideBriefing | PlotboekPaginaIcons.MuziekVanuitTechniek,
                        Debrief = DebriefData.Create(true, ["Wie droeg de kroon aan het eind?", "Welke beloftes zijn er gedaan?"]),
                        IsVolledigKlaar = false,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "G",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "Een gunst van de gilden",
                        Dag = Dag.None,
                        StarttijdId = "None",
                        Order = 1,
                        VerantwoordelijkeSlId = Gilden,
                        AantalBlokkenId = "EenBlok",
                        EncounterTypeIds = EncounterTypeIds.Create(["Roleplay"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld, "Bij de groep die erom vraagt"),
                        AantalNpcs = AantalNpcsData.Create([NpcRolData.Create("Gildemeester", 1, kledingEnSchmink: "Rijke mantel met gildepenning")]),
                        ElevatorPitch = "Wie de gilden om een gunst vraagt, krijgt bezoek van een gildemeester: met een aanbod, en een prijs.",
                        SamenvattingPlot = "<p>Speelt wanneer een groep om hulp van de gilden vraagt. De gildemeester helpt, maar wil er een wederdienst voor.</p>",
                        IsSpelerGedreven = true,
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "W",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "Afscheid bij de oude eik",
                        Dag = Dag.Zondag,
                        StarttijdId = "Tijd1000",
                        Order = 1,
                        VerantwoordelijkeSlId = Wildernis,
                        ElevatorPitch = "De nevel trekt zich terug. Bij de oude eik nemen de dwaallichten afscheid, of komen ze terug als er iets beloofd is.",
                        Opmerkingen = "Nog uitwerken na de debrief van zaterdag.",
                    },
                },
            ],
        },
    ];
}
