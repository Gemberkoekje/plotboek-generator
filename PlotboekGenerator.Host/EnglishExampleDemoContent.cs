using PlotboekGenerator.Application.DTOs;
using PlotboekGenerator.Application.Ports;
using PlotboekGenerator.core.Models;

namespace PlotboekGenerator.Host;

/// <summary>
/// <see cref="ExampleDemoContent"/> in English, for a demo whose <c>Localization:DefaultCulture</c> is
/// <c>en</c>: the same spring event, plot line and states, with the same spelleider ids. It uses the ids
/// and domain shorthands of <see cref="EnglishExampleReferenceDataSeed"/>.
/// </summary>
internal sealed class EnglishExampleDemoContent : IDemoContent
{
    private const string Hof = "demo-sl-hof";
    private const string Gilden = "demo-sl-gilden";
    private const string Tempel = "demo-sl-tempel";
    private const string Wildernis = "demo-sl-wildernis";
    private const string Hoofdspelleiding = "demo-sl-hoofdspelleiding";

    /// <inheritdoc />
    public IReadOnlyList<DemoSpelleider> Spelleiders { get; } =
    [
        new DemoSpelleider { Id = Hof, Voornaam = "Mira", Achternaam = "Holloway", DomainShorthand = "C" },
        new DemoSpelleider { Id = Gilden, Voornaam = "James", Achternaam = "Carter", DomainShorthand = "G" },
        new DemoSpelleider { Id = Tempel, Voornaam = "Fiona", Achternaam = "Hartley", DomainShorthand = "T" },
        new DemoSpelleider { Id = Wildernis, Voornaam = "Reuben", Achternaam = "Ashford", DomainShorthand = "W" },
        new DemoSpelleider { Id = Hoofdspelleiding, Voornaam = "Nora", Achternaam = "Baker", DomainShorthand = "O" },
    ];

    /// <inheritdoc />
    public IReadOnlyList<DemoPlotboek> Plotboeken { get; } =
    [
        new DemoPlotboek
        {
            EventId = "Voorjaar",
            Titel = "The Mist Crown",
            AangemaaktDoorId = Hoofdspelleiding,
            SamenvattingAlgemeenPlot =
                "<p>For the first time in a generation the mist lifts from Mistmere, and with the mist comes a messenger who " +
                "claims that the lost crown of Mistmere has been found. The court wants the crown for itself, the guilds " +
                "want to know what it is worth, and the temple warns that it should never have been found.</p>" +
                "<p>The weekend turns on one question: who wears the crown when it awakens on Saturday night?</p>",
            Paginas =
            [
                new DemoPagina
                {
                    DomainShorthand = "C",
                    Plotlijn = "crown",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "The messenger from Mistmere",
                        Dag = Dag.Vrijdag,
                        StarttijdId = "Tijd2000",
                        Order = 1,
                        VerantwoordelijkeSlId = Hof,
                        AantalBlokkenId = "EenBlok",
                        EncounterTypeIds = EncounterTypeIds.Create(["Roleplay"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld),
                        AantalNpcs = AantalNpcsData.Create(
                        [
                            NpcRolData.Create("Messenger of Mistmere", 1, kledingEnSchmink: "Grey travelling cloak, muddy boots, pale make-up"),
                            NpcRolData.Create("Escort", 2, kledingEnSchmink: "Leather armour with a blue sash"),
                        ]),
                        ElevatorPitch = "An exhausted messenger walks out of the mist and asks the court for protection: he brings word that the crown of Mistmere has been found.",
                        SamenvattingPlot =
                            "<p>At time in, the messenger and his escort walk onto the main field. He asks for the steward and refuses to speak to anyone else.</p>" +
                            "<p>His letter is sealed with the arms of Mistmere. Whoever reads it learns that the crown lies in the ruin, but not exactly where: that is in the archive.</p>",
                        InstructiePentagon = "<p>Mist over the main field as soon as the messenger arrives; let it clear slowly after ten minutes.</p>",
                        Iconen = PlotboekPaginaIcons.TimingCruciaal | PlotboekPaginaIcons.PlotCruciaal | PlotboekPaginaIcons.LichtOfRookVanuitTechniek,
                        PropsEnLoot = PropsEnLootData.Create([PropLootItemData.Create("Sealed letter bearing the arms of Mistmere", Hof)], []),
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "W",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "Wisps in the brook valley",
                        Dag = Dag.Vrijdag,
                        StarttijdId = "Tijd2230",
                        Order = 1,
                        VerantwoordelijkeSlId = Wildernis,
                        AantalBlokkenId = "TweeBlokken",
                        EncounterTypeIds = EncounterTypeIds.Create(["Vechten"], ["Magie"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Questveld, locatieId: "Beekdal"),
                        AantalNpcs = AantalNpcsData.Create(
                        [
                            NpcRolData.Create("Wisp", 4, 6, kledingEnSchmink: "White robe, a small lamp in hand, everything else dark"),
                            NpcRolData.Create("Lost hunter", 1, kledingEnSchmink: "Green clothes with a hood", castingInstructie: "Someone who can play frightened well"),
                        ]),
                        ElevatorPitch = "In the brook valley little lights dance above the water. Whoever follows them loses their way; whoever catches one is left with a shard of mist crystal.",
                        SamenvattingPlot =
                            "<p>A lost hunter stumbles into the camp and begs for help: his companion followed the lights into the brook valley and has not come back.</p>" +
                            "<p>In the valley the wisps lure the players into the water. A light that is struck goes out and leaves a crystal behind.</p>",
                        PropsEnLoot = PropsEnLootData.Create([], [PropLootItemData.Create("Three shards of mist crystal", Wildernis)]),
                        IsFlexibel = true,
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "G",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "The spring market",
                        Dag = Dag.Zaterdag,
                        StarttijdId = "Tijd1000",
                        Order = 1,
                        VerantwoordelijkeSlId = Gilden,
                        AantalBlokkenId = "DrieBlokken",
                        EncounterTypeIds = EncounterTypeIds.Create(["Handel"], ["Politiek"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld, "Market square by the guild hall"),
                        AantalNpcs = AantalNpcsData.Create(
                        [
                            NpcRolData.Create("Pedlar", 3, 5, kledingEnSchmink: "Colourful townsfolk clothes with an apron"),
                            NpcRolData.Create("Market master", 1, kledingEnSchmink: "Black gown with a chain of office"),
                        ]),
                        ElevatorPitch = "The guilds open the season with a market. Everyone wants to buy mist crystal, and the market master wants to know where it comes from.",
                        SamenvattingPlot =
                            "<p>An ordinary market with trade and gossip, until the market master arrests a buyer of mist crystal.</p>" +
                            "<p>Players with crystal from the brook valley can sell it here, or take it to the temple.</p>",
                        Iconen = PlotboekPaginaIcons.MagAlsEersteCancelled,
                        PropsEnLoot = PropsEnLootData.Create(
                            [PropLootItemData.Create("Stall with wares", verantwoordelijkeSource: "Anders", verantwoordelijkeVrijeTekst: "Guild chest")],
                            []),
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "T",
                    Plotlijn = "crown",
                    Feedback =
                    [
                        new DemoFeedback
                        {
                            GegevenDoorId = Hoofdspelleiding,
                            Tekst = "How do the players know they have to go to the ruin? I'm missing the clue from Friday's letter.",
                        },
                    ],
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "The sealed archive",
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
                                "Archivist",
                                1,
                                kledingEnSchmink: "Grey habit, ink stains on the hands",
                                vereistAfstemming: true,
                                afstemmingToelichting: "Plays blind: has to walk through the archive with the GM beforehand"),
                        ]),
                        ElevatorPitch = "Beneath the ruin lies the archive of Mistmere, and the archivist lets in only those who solve his riddles.",
                        SamenvattingPlot =
                            "<p>The archivist guards the map that shows where the crown lies. He hands it only to those who solve three riddles about the history of Mistmere.</p>",
                        MeerInformatie = "<p>The riddles and their answers are in the GM's attachment; bring them printed.</p>",
                        IsVolledigKlaar = false,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "C",
                    Plotlijn = "crown",
                    Feedback =
                    [
                        new DemoFeedback
                        {
                            GegevenDoorId = Hoofdspelleiding,
                            Tekst = "Is there a fallback for rain? The main field floods when it rains.",
                            Reactie = "Yes: in the big tent when it rains. The music comes along.",
                            IsAfgehandeld = true,
                        },
                    ],
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "The Mist Crown awakens",
                        Dag = Dag.Zaterdag,
                        StarttijdId = "Tijd2000",
                        Order = 1,
                        VerantwoordelijkeSlId = Hof,
                        ExtraSlIds = [Tempel],
                        AantalBlokkenId = "DrieBlokken",
                        EncounterTypeIds = EncounterTypeIds.Create(["Ritueel"], ["Vechten"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Hoofdveld),
                        AantalNpcs = AantalNpcsData.Create(
                            [NpcRolData.Create("Mist spirit", 0, kledingEnSchmink: "Grey gauze over dark clothes, white make-up")],
                            alleNpcs: true),
                        ElevatorPitch = "Whoever puts on the crown gains the voice of Mistmere. The whole field takes sides as the mist spirits return.",
                        SamenvattingPlot =
                            "<p>The crown is brought to the main field. As soon as someone puts it on, the mist spirits rise and demand a promise.</p>" +
                            "<p>What is promised decides the plot of the next event.</p>",
                        InstructiePentagon = "<p>A slowly swelling choir from the moment the crown is lifted; silence as soon as someone puts it on.</p>",
                        Iconen = PlotboekPaginaIcons.EventBreed | PlotboekPaginaIcons.PlotCruciaal | PlotboekPaginaIcons.TimingCruciaal
                            | PlotboekPaginaIcons.UitgebreideBriefing | PlotboekPaginaIcons.MuziekVanuitTechniek,
                        Debrief = DebriefData.Create(true, ["Who wore the crown at the end?", "What promises were made?"]),
                        IsVolledigKlaar = false,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "G",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "A favour from the guilds",
                        Dag = Dag.None,
                        StarttijdId = "None",
                        Order = 1,
                        VerantwoordelijkeSlId = Gilden,
                        AantalBlokkenId = "EenBlok",
                        EncounterTypeIds = EncounterTypeIds.Create(["Roleplay"]),
                        LocatieEncounter = LocatieEncounterData.Create(LocatieEncounter.Groeperingsveld, "With the group that asks for it"),
                        AantalNpcs = AantalNpcsData.Create([NpcRolData.Create("Guild master", 1, kledingEnSchmink: "Rich cloak with a guild token")]),
                        ElevatorPitch = "Whoever asks the guilds for a favour gets a visit from a guild master: with an offer, and a price.",
                        SamenvattingPlot = "<p>Plays when a group asks the guilds for help. The guild master helps, but wants a favour in return.</p>",
                        IsSpelerGedreven = true,
                        IsVolledigKlaar = true,
                    },
                },
                new DemoPagina
                {
                    DomainShorthand = "W",
                    Data = new PlotboekPagina.Data
                    {
                        Titel = "Farewell at the old oak",
                        Dag = Dag.Zondag,
                        StarttijdId = "Tijd1000",
                        Order = 1,
                        VerantwoordelijkeSlId = Wildernis,
                        ElevatorPitch = "The mist withdraws. At the old oak the wisps say farewell, or come back if something was promised.",
                        Opmerkingen = "Still to be worked out after Saturday's debrief.",
                    },
                },
            ],
        },
    ];
}
