using PlotboekGenerator.Application.Ports;

namespace PlotboekGenerator.Host;

/// <summary>
/// The reference data of the example LARP in English, "The Lost Crown": the same three weekend events
/// a year on a scouting site as <see cref="ExampleReferenceDataSeed"/>, for a deployment whose
/// <c>Localization:DefaultCulture</c> is <c>en</c>.
/// </summary>
/// <remarks>
/// Every row has the id of its Dutch counterpart: ids are stable keys that stored data refers to, so
/// both seeds carry the same ones and only what people read differs. The domains have their own
/// shorthands, which <see cref="EnglishExampleDemoContent"/> uses.
/// </remarks>
internal sealed class EnglishExampleReferenceDataSeed : IReferenceDataSeed
{
    /// <inheritdoc />
    /// <remarks>
    /// The seed service derives Order from the time, so the numbers here only document the sequence.
    /// An event runs from an evening (the first day) over full days to an afternoon (the last day).
    /// </remarks>
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

    /// <inheritdoc />
    public IReadOnlyList<AantalBlokkenConfig> AantalBlokken { get; } =
    [
        new AantalBlokkenConfig { Id = "None",        EnumValue = 0, DisplayName = "-",                          IsNone = true,  OverflowBlokken = 0, Order = 0 },
        new AantalBlokkenConfig { Id = "EenBlok",     EnumValue = 1, DisplayName = "1 block (1 hour at most)",   IsNone = false, OverflowBlokken = 0, Order = 1 },
        new AantalBlokkenConfig { Id = "TweeBlokken", EnumValue = 2, DisplayName = "2 blocks (about 2.5 hours)", IsNone = false, OverflowBlokken = 1, Order = 2 },
        new AantalBlokkenConfig { Id = "DrieBlokken", EnumValue = 3, DisplayName = "3 blocks (about 4.5 hours)", IsNone = false, OverflowBlokken = 2, Order = 3 },
    ];

    /// <inheritdoc />
    public IReadOnlyList<SoortEncounterConfig> SoortEncounters { get; } =
    [
        new SoortEncounterConfig { Id = "None",     EnumValue = 0, DisplayName = "-",        IsNone = true,  Order = 0 },
        new SoortEncounterConfig { Id = "Vechten",  EnumValue = 1, DisplayName = "Combat",   IsNone = false, Order = 1 },
        new SoortEncounterConfig { Id = "Roleplay", EnumValue = 2, DisplayName = "Roleplay", IsNone = false, Order = 2 },
        new SoortEncounterConfig { Id = "Ritueel",  EnumValue = 3, DisplayName = "Ritual",   IsNone = false, Order = 3 },
        new SoortEncounterConfig { Id = "Handel",   EnumValue = 4, DisplayName = "Trade",    IsNone = false, Order = 4 },
        new SoortEncounterConfig { Id = "Politiek", EnumValue = 5, DisplayName = "Politics", IsNone = false, Order = 5 },
        new SoortEncounterConfig { Id = "Magie",    EnumValue = 6, DisplayName = "Magic",    IsNone = false, Order = 6 },
        new SoortEncounterConfig { Id = "Puzzel",   EnumValue = 7, DisplayName = "Puzzle",   IsNone = false, Order = 7 },
    ];

    /// <inheritdoc />
    /// <remarks>
    /// NextEventId encodes the event cycle: spring, summer, autumn, then next year's spring. The summer
    /// event starts a day earlier, on Thursday.
    /// </remarks>
    public IReadOnlyList<EventConfig> Events { get; } =
    [
        new EventConfig { Id = "None",      EnumValue = 0, DisplayName = "-",         Abbreviation = "-",  IsNone = true,  IsEvergreen = false, StartDag = Dag.None,      EindDag = Dag.None,   Order = 0, NextEventId = string.Empty, NextEventIncrementsYear = false },
        new EventConfig { Id = "Voorjaar",  EnumValue = 1, DisplayName = "Spring",    Abbreviation = "SP", IsNone = false, IsEvergreen = false, StartDag = Dag.Vrijdag,   EindDag = Dag.Zondag, Order = 1, NextEventId = "Zomer",      NextEventIncrementsYear = false },
        new EventConfig { Id = "Zomer",     EnumValue = 2, DisplayName = "Summer",    Abbreviation = "SU", IsNone = false, IsEvergreen = false, StartDag = Dag.Donderdag, EindDag = Dag.Zondag, Order = 2, NextEventId = "Najaar",     NextEventIncrementsYear = false },
        new EventConfig { Id = "Najaar",    EnumValue = 3, DisplayName = "Autumn",    Abbreviation = "AU", IsNone = false, IsEvergreen = false, StartDag = Dag.Vrijdag,   EindDag = Dag.Zondag, Order = 3, NextEventId = "Voorjaar",   NextEventIncrementsYear = true  },
        new EventConfig { Id = "Evergreen", EnumValue = 4, DisplayName = "Evergreen", Abbreviation = "EG", IsNone = false, IsEvergreen = true,  StartDag = Dag.None,      EindDag = Dag.None,   Order = 4, NextEventId = string.Empty, NextEventIncrementsYear = false },
    ];

    /// <inheritdoc />
    public IReadOnlyList<QuestLocatieCategorieConfig> QuestLocatieCategorieen { get; } =
    [
        new QuestLocatieCategorieConfig { Id = "Altijd",     DisplayName = "Always available",             Notitie = string.Empty,                                                              Ernst = QuestLocatieErnst.Geen,      Order = 1 },
        new QuestLocatieCategorieConfig { Id = "InOverleg",  DisplayName = "By arrangement with the site", Notitie = "Ask the site manager beforehand whether the field is free.",            Ernst = QuestLocatieErnst.LetOp,     Order = 2 },
        new QuestLocatieCategorieConfig { Id = "LieverNiet", DisplayName = "Best avoided",                 Notitie = "Far from the camp, along a public road. Only when there is no other way.", Ernst = QuestLocatieErnst.Afgeraden, Order = 3 },
    ];

    /// <inheritdoc />
    /// <remarks>
    /// ConflicteertMet is only filled in on one side; ReferenceDataExtensions.BuildSymmetrischeOverlap
    /// makes the relation symmetric when the data is read.
    /// </remarks>
    public IReadOnlyList<QuestLocatieConfig> QuestLocaties { get; } =
    [
        new QuestLocatieConfig { Id = "Kampvuurkuil", DisplayName = "Campfire pit", CategorieId = "Altijd",     Notitie = string.Empty,                     ConflicteertMet = [],          IsActief = true, Order = 1 },
        new QuestLocatieConfig { Id = "Bosrand",      DisplayName = "Forest edge",  CategorieId = "Altijd",     Notitie = string.Empty,                     ConflicteertMet = ["OudeEik"], IsActief = true, Order = 2 },
        new QuestLocatieConfig { Id = "OudeEik",      DisplayName = "The old oak",  CategorieId = "Altijd",     Notitie = string.Empty,                     ConflicteertMet = [],          IsActief = true, Order = 3 },
        new QuestLocatieConfig { Id = "Ruine",        DisplayName = "Ruin",         CategorieId = "InOverleg",  Notitie = "Daytime only: there is no light.", ConflicteertMet = [],          IsActief = true, Order = 1 },
        new QuestLocatieConfig { Id = "Beekdal",      DisplayName = "Brook valley", CategorieId = "InOverleg",  Notitie = string.Empty,                     ConflicteertMet = [],          IsActief = true, Order = 2 },
        new QuestLocatieConfig { Id = "Heideveld",    DisplayName = "Heath",        CategorieId = "LieverNiet", Notitie = string.Empty,                     ConflicteertMet = [],          IsActief = true, Order = 1 },
    ];

    /// <inheritdoc />
    public IReadOnlyList<(string Shorthand, string Description)> Domeinen { get; } =
    [
        ("C", "Court"),
        ("G", "Guilds"),
        ("T", "Temple"),
        ("W", "Wilderness"),
        ("O", "Other"),
    ];

    /// <inheritdoc />
    public IReadOnlyList<(MeetingDagType DagType, string Tijd, string Naam)> Meetings { get; } =
    [
        (MeetingDagType.EersteDag, "16:00", "Weapon check opens"),
        (MeetingDagType.EersteDag, "18:00", "GM meeting"),
        (MeetingDagType.EersteDag, "18:30", "Block 20:00: casting, costume and then briefing"),
        (MeetingDagType.EersteDag, "19:00", "Official time in"),
        (MeetingDagType.EersteDag, "21:00", "Block 22:30: casting, costume and then briefing"),
        (MeetingDagType.EersteDag, "01:00", "Official time out"),

        (MeetingDagType.Tussendag, "08:30", "Block 10:00: casting, costume and then briefing"),
        (MeetingDagType.Tussendag, "09:00", "GM meeting"),
        (MeetingDagType.Tussendag, "09:00", "Official time in"),
        (MeetingDagType.Tussendag, "11:30", "Block 13:00: casting, costume and then briefing"),
        (MeetingDagType.Tussendag, "14:30", "Block 16:00: casting, costume and then briefing"),
        (MeetingDagType.Tussendag, "17:30", "Dinner"),
        (MeetingDagType.Tussendag, "18:30", "Block 20:00: casting, costume and then briefing"),
        (MeetingDagType.Tussendag, "21:00", "Block 22:30: casting, costume and then briefing"),
        (MeetingDagType.Tussendag, "01:00", "Official time out"),

        (MeetingDagType.LaatseDag, "08:30", "Block 10:00: casting, costume and then briefing"),
        (MeetingDagType.LaatseDag, "09:00", "Official time in"),
        (MeetingDagType.LaatseDag, "11:30", "Block 13:00: casting, costume and then briefing"),
        (MeetingDagType.LaatseDag, "15:00", "Official time out and clean-up"),
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> KarakterNamen { get; } =
    [
        "Adela", "Agatha", "Aldous", "Alric", "Aveline", "Baldwin", "Beatrix", "Bertram", "Cedric", "Cuthbert",
        "Edith", "Edmund", "Elfrida", "Emeline", "Godric", "Gwendolyn", "Hereward", "Hilda", "Isolde", "Jocelyn",
        "Leofric", "Mabel", "Matilda", "Merrick", "Odo", "Osric", "Oswin", "Percival", "Roland", "Rosamund",
        "Rowena", "Seward", "Sybil", "Tancred", "Thurstan", "Ursula", "Walter", "Wilfred", "Winifred", "Wulfstan",
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> WereldNamen { get; } =
    [
        "Aldmoor", "Brackenfield", "Crownwood", "Dunwarden", "Elderhaven", "Falconhurst", "Greycoast",
        "Hollowmere", "Lindenbury", "Mistmere", "Oldwall", "Ravenstone", "Redmorrow", "Stonehill", "Wolfholt",
    ];
}
