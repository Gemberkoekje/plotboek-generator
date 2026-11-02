namespace PlotboekGenerator.test.TestHelpers;

/// <summary>
/// Stable string IDs used in tests in place of enum member names.
/// These match the seeded reference-data IDs and must not be changed
/// without a corresponding migration of the persisted reference data.
/// </summary>
public static class StarttijdIds
{
    public const string None = "None";
    public const string Tijd1030 = "Tijd1030";
    public const string Tijd1130 = "Tijd1130";
    public const string Tijd1230 = "Tijd1230";
    public const string Tijd1330 = "Tijd1330";
    public const string Tijd1430 = "Tijd1430";
    public const string Tijd1630 = "Tijd1630";
    public const string Tijd1930Introquest = "Tijd1930Introquest";
    public const string Tijd2000 = "Tijd2000";
    public const string Tijd2200 = "Tijd2200";
    public const string Tijd0000 = "Tijd0000";
}

/// <summary>
/// Stable string IDs for AantalBlokken used in tests.
/// These match the seeded reference-data IDs.
/// </summary>
public static class AantalBlokkenIds
{
    public const string None = "None";
    public const string EenBlok = "EenBlok";
    public const string TweeBlokken = "TweeBlokken";
    public const string DrieBlokken = "DrieBlokken";
}

/// <summary>
/// Stable string IDs for events used in tests: the example LARP's.
/// These match the seeded reference-data IDs.
/// </summary>
public static class EventIds
{
    public const string None = "None";
    public const string Voorjaar = "Voorjaar";
    public const string Najaar = "Najaar";
    public const string Zomer = "Zomer";
    public const string Evergreen = "Evergreen";
}

/// <summary>
/// Stable string IDs for encounter types used in tests.
/// These match the seeded reference-data IDs.
/// </summary>
public static class SoortEncounterIds
{
    public const string None = "None";
    public const string Vechten = "Vechten";
    public const string Roleplay = "Roleplay";
    public const string Handel = "Handel";
    public const string Magie = "Magie";
}
