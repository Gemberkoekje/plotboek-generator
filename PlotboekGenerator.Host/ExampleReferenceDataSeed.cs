using PlotboekGenerator.Application.Ports;

namespace PlotboekGenerator.Host;

/// <summary>
/// The reference data of an example LARP, "De Verloren Kroon": three weekend events a year on a
/// scouting terrain. A fresh deployment starts from it; admins change it to their own from the
/// admin pages. It is also the data of the public demo.
/// </summary>
internal sealed class ExampleReferenceDataSeed : IReferenceDataSeed
{
    // The seed service derives Order from the time, so the numbers here only document the sequence.
    // An event runs from an evening (the first day) over full days to an afternoon (the last day).
    public IReadOnlyList<StarttijdConfig> Starttijden { get; } =
    [
        new StarttijdConfig { Id = "None",     EnumValue = 0, DisplayName = "-",     IsNone = true,  IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = false, IsAvailableOnLaatsteDag = false, Order = 0,
            BlokVensterEersteDag = string.Empty,    BlokVensterMiddelDagen = string.Empty,    BlokVensterLaatsteDag = string.Empty },
        new StarttijdConfig { Id = "Tijd1000", EnumValue = 1, DisplayName = "10:00", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true,  IsAvailableOnLaatsteDag = true,  Order = 1,
            BlokVensterEersteDag = string.Empty,    BlokVensterMiddelDagen = "9:00 - 11:00",  BlokVensterLaatsteDag = "9:00 - 11:00" },
        new StarttijdConfig { Id = "Tijd1300", EnumValue = 2, DisplayName = "13:00", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true,  IsAvailableOnLaatsteDag = true,  Order = 2,
            BlokVensterEersteDag = string.Empty,    BlokVensterMiddelDagen = "12:00 - 14:00", BlokVensterLaatsteDag = "12:00 - 14:00" },
        new StarttijdConfig { Id = "Tijd1600", EnumValue = 3, DisplayName = "16:00", IsNone = false, IsAvailableOnEersteDag = false, IsAvailableOnMiddelDagen = true,  IsAvailableOnLaatsteDag = false, Order = 3,
            BlokVensterEersteDag = string.Empty,    BlokVensterMiddelDagen = "15:00 - 17:00", BlokVensterLaatsteDag = string.Empty },
        new StarttijdConfig { Id = "Tijd2000", EnumValue = 4, DisplayName = "20:00", IsNone = false, IsAvailableOnEersteDag = true,  IsAvailableOnMiddelDagen = true,  IsAvailableOnLaatsteDag = false, Order = 4,
            BlokVensterEersteDag = "19:00 - 21:00", BlokVensterMiddelDagen = "19:00 - 21:00", BlokVensterLaatsteDag = string.Empty },
        new StarttijdConfig { Id = "Tijd2230", EnumValue = 5, DisplayName = "22:30", IsNone = false, IsAvailableOnEersteDag = true,  IsAvailableOnMiddelDagen = true,  IsAvailableOnLaatsteDag = false, Order = 5,
            BlokVensterEersteDag = "21:30 - 23:30", BlokVensterMiddelDagen = "21:30 - 23:30", BlokVensterLaatsteDag = string.Empty },
    ];

    public IReadOnlyList<AantalBlokkenConfig> AantalBlokken { get; } =
    [
        new AantalBlokkenConfig { Id = "None",        EnumValue = 0, DisplayName = "-",                       IsNone = true,  OverflowBlokken = 0, Order = 0 },
        new AantalBlokkenConfig { Id = "EenBlok",     EnumValue = 1, DisplayName = "1 blok (max 1 uur)",      IsNone = false, OverflowBlokken = 0, Order = 1 },
        new AantalBlokkenConfig { Id = "TweeBlokken", EnumValue = 2, DisplayName = "2 blokken (+/- 2.5 uur)", IsNone = false, OverflowBlokken = 1, Order = 2 },
        new AantalBlokkenConfig { Id = "DrieBlokken", EnumValue = 3, DisplayName = "3 blokken (+/- 4.5 uur)", IsNone = false, OverflowBlokken = 2, Order = 3 },
    ];

    public IReadOnlyList<SoortEncounterConfig> SoortEncounters { get; } =
    [
        new SoortEncounterConfig { Id = "None",     EnumValue = 0, DisplayName = "-",        IsNone = true,  Order = 0 },
        new SoortEncounterConfig { Id = "Vechten",  EnumValue = 1, DisplayName = "Vechten",  IsNone = false, Order = 1 },
        new SoortEncounterConfig { Id = "Roleplay", EnumValue = 2, DisplayName = "Roleplay", IsNone = false, Order = 2 },
        new SoortEncounterConfig { Id = "Ritueel",  EnumValue = 3, DisplayName = "Ritueel",  IsNone = false, Order = 3 },
        new SoortEncounterConfig { Id = "Handel",   EnumValue = 4, DisplayName = "Handel",   IsNone = false, Order = 4 },
        new SoortEncounterConfig { Id = "Politiek", EnumValue = 5, DisplayName = "Politiek", IsNone = false, Order = 5 },
        new SoortEncounterConfig { Id = "Magie",    EnumValue = 6, DisplayName = "Magie",    IsNone = false, Order = 6 },
        new SoortEncounterConfig { Id = "Puzzel",   EnumValue = 7, DisplayName = "Puzzel",   IsNone = false, Order = 7 },
    ];

    // NextEventId encodes the event cycle: Voorjaar -> Zomer -> Najaar -> next year's Voorjaar. The
    // summer event starts a day earlier, on Thursday.
    public IReadOnlyList<EventConfig> Events { get; } =
    [
        new EventConfig { Id = "None",      EnumValue = 0, DisplayName = "-",         Abbreviation = "-",  IsNone = true,  IsEvergreen = false, StartDag = Dag.None,      EindDag = Dag.None,   Order = 0, NextEventId = string.Empty, NextEventIncrementsYear = false },
        new EventConfig { Id = "Voorjaar",  EnumValue = 1, DisplayName = "Voorjaar",  Abbreviation = "VJ", IsNone = false, IsEvergreen = false, StartDag = Dag.Vrijdag,   EindDag = Dag.Zondag, Order = 1, NextEventId = "Zomer",      NextEventIncrementsYear = false },
        new EventConfig { Id = "Zomer",     EnumValue = 2, DisplayName = "Zomer",     Abbreviation = "ZO", IsNone = false, IsEvergreen = false, StartDag = Dag.Donderdag, EindDag = Dag.Zondag, Order = 2, NextEventId = "Najaar",     NextEventIncrementsYear = false },
        new EventConfig { Id = "Najaar",    EnumValue = 3, DisplayName = "Najaar",    Abbreviation = "NJ", IsNone = false, IsEvergreen = false, StartDag = Dag.Vrijdag,   EindDag = Dag.Zondag, Order = 3, NextEventId = "Voorjaar",   NextEventIncrementsYear = true  },
        new EventConfig { Id = "Evergreen", EnumValue = 4, DisplayName = "Evergreen", Abbreviation = "EG", IsNone = false, IsEvergreen = true,  StartDag = Dag.None,      EindDag = Dag.None,   Order = 4, NextEventId = string.Empty, NextEventIncrementsYear = false },
    ];

    public IReadOnlyList<QuestLocatieCategorieConfig> QuestLocatieCategorieen { get; } =
    [
        new QuestLocatieCategorieConfig { Id = "Altijd",        DisplayName = "Altijd te gebruiken",         Notitie = string.Empty,                                                   Ernst = QuestLocatieErnst.Geen,      Order = 1 },
        new QuestLocatieCategorieConfig { Id = "InOverleg",     DisplayName = "In overleg met het terrein",  Notitie = "Vraag de terreinbeheerder vooraf of het veld vrij is.",        Ernst = QuestLocatieErnst.LetOp,     Order = 2 },
        new QuestLocatieCategorieConfig { Id = "LieverNiet",    DisplayName = "Liever niet gebruiken",       Notitie = "Ver van het kamp, langs een openbare weg. Alleen als het echt niet anders kan.", Ernst = QuestLocatieErnst.Afgeraden, Order = 3 },
    ];

    // ConflicteertMet is only filled in on one side; ReferenceDataExtensions.BuildSymmetrischeOverlap
    // makes the relation symmetric when the data is read.
    public IReadOnlyList<QuestLocatieConfig> QuestLocaties { get; } =
    [
        new QuestLocatieConfig { Id = "Kampvuurkuil", DisplayName = "Kampvuurkuil",   CategorieId = "Altijd",     Notitie = string.Empty,                       ConflicteertMet = [],             IsActief = true, Order = 1 },
        new QuestLocatieConfig { Id = "Bosrand",      DisplayName = "Bosrand",        CategorieId = "Altijd",     Notitie = string.Empty,                       ConflicteertMet = ["OudeEik"],    IsActief = true, Order = 2 },
        new QuestLocatieConfig { Id = "OudeEik",      DisplayName = "De oude eik",    CategorieId = "Altijd",     Notitie = string.Empty,                       ConflicteertMet = [],             IsActief = true, Order = 3 },
        new QuestLocatieConfig { Id = "Ruine",        DisplayName = "Ruïne",          CategorieId = "InOverleg",  Notitie = "Alleen overdag: er is geen licht.", ConflicteertMet = [],             IsActief = true, Order = 1 },
        new QuestLocatieConfig { Id = "Beekdal",      DisplayName = "Beekdal",        CategorieId = "InOverleg",  Notitie = string.Empty,                       ConflicteertMet = [],             IsActief = true, Order = 2 },
        new QuestLocatieConfig { Id = "Heideveld",    DisplayName = "Heideveld",      CategorieId = "LieverNiet", Notitie = string.Empty,                       ConflicteertMet = [],             IsActief = true, Order = 1 },
    ];

    public IReadOnlyList<(string Shorthand, string Description)> Domeinen { get; } =
    [
        ("H", "Hof"),
        ("G", "Gilden"),
        ("T", "Tempel"),
        ("W", "Wildernis"),
        ("O", "Overig"),
    ];

    public IReadOnlyList<(MeetingDagType DagType, string Tijd, string Naam)> Meetings { get; } =
    [
        (MeetingDagType.EersteDag, "16:00", "Begin wapencheck"),
        (MeetingDagType.EersteDag, "18:00", "SL Overleg"),
        (MeetingDagType.EersteDag, "18:30", "Blok 20:00 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.EersteDag, "19:00", "Officiële tijd-in"),
        (MeetingDagType.EersteDag, "21:00", "Blok 22:30 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.EersteDag, "01:00", "Officiële tijd-uit"),

        (MeetingDagType.Tussendag, "08:30", "Blok 10:00 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.Tussendag, "09:00", "SL Overleg"),
        (MeetingDagType.Tussendag, "09:00", "Officiële tijd-in"),
        (MeetingDagType.Tussendag, "11:30", "Blok 13:00 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.Tussendag, "14:30", "Blok 16:00 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.Tussendag, "17:30", "Avondeten"),
        (MeetingDagType.Tussendag, "18:30", "Blok 20:00 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.Tussendag, "21:00", "Blok 22:30 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.Tussendag, "01:00", "Officiële tijd-uit"),

        (MeetingDagType.LaatseDag, "08:30", "Blok 10:00 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.LaatseDag, "09:00", "Officiële tijd-in"),
        (MeetingDagType.LaatseDag, "11:30", "Blok 13:00 rolverdeling, aankleding en daarna briefing"),
        (MeetingDagType.LaatseDag, "15:00", "Officiële tijd-uit en opruimen"),
    ];

    public IReadOnlyList<string> KarakterNamen { get; } =
    [
        "Adalric", "Agnes", "Alard", "Aleid", "Ansfried", "Aric", "Bertrada", "Bodo", "Brunhild", "Cuno",
        "Dietrich", "Edda", "Egbert", "Elsbeth", "Folkert", "Gerlinde", "Godfried", "Hadewych", "Hartmut", "Ida",
        "Isolde", "Jorwerd", "Klaas", "Liutgard", "Machteld", "Meinhard", "Notburga", "Odo", "Ortrud", "Radboud",
        "Reinout", "Ricarda", "Sibrand", "Swanhild", "Tancred", "Udo", "Ulrike", "Volkmar", "Wendel", "Wiborada",
    ];

    public IReadOnlyList<string> WereldNamen { get; } =
    [
        "Aldmoor", "Brakenveld", "Dorrewaard", "Elsterhaven", "Grauwe Kust", "Hollandsweerd", "Kroonwoud",
        "Lindeburg", "Morgenrood", "Nevelmeer", "Oudewaal", "Ravensteen", "Steenbergen", "Valkenhorst", "Wolvenhoek",
    ];
}
