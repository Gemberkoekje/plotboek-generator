using System.Reflection;
using PlotboekGenerator.core.Events;

namespace PlotboekGenerator.test.Events;

/// <summary>
/// Guard tests to protect the event schema from accidental changes.
///
/// These events are persisted as the source of truth for aggregate state.
/// Changing them incorrectly will break existing aggregates.
///
/// Rules:
/// - ADDING a new event is fine, but its fingerprint must be added to the expected list below.
/// - DELETING an event is NOT allowed — existing aggregates that contain
///   the deleted event will fail to deserialize / replay.
/// - CHANGING the fields (properties) on an existing event is NOT allowed —
///   it is better to keep the event as-is, and introduce a v2 event (or similar)
///   with the new field set.
/// </summary>
public sealed class AuditTrailEvent_schema_guard_specs
{
    /// <summary>
    /// Sorted list of "TypeName:PropertyName1(PropertyType1),PropertyName2(PropertyType2),..."
    /// fingerprints for every concrete <see cref="AuditTrailEvent"/> subclass.
    ///
    /// When you add a new event, add its fingerprint here (the test failure message will show it).
    /// Never remove or modify an existing line.
    /// </summary>
    private static readonly string[] ExpectedFingerprints =
    [
        "PlotboekAangemaakt:EindDag(Dag),Event(LegacyEvent),Jaar(Year),StartDag(Dag)",
        "PlotboekAangemaaktV2:EindDag(Dag),EventId(String),Jaar(Year),StartDag(Dag)",
        "PlotboekDagenAangepast:EindDag(Dag),StartDag(Dag)",
        "PlotboekDatumsAangepast:EindDatum(Nullable`1),StartDatum(Nullable`1)",
        "PlotboekGoogleDriveFileUrlAangepast:GoogleDriveFileUrl(String)",
        "PlotboekGoogleDriveFolderUrlAangepast:GoogleDriveFolderUrl(String)",
        "PlotboekPaginaAanPlotboekToegevoegd:PlotboekId(PlotboekId),PlotboekName(String)",
        "PlotboekPaginaAantalBlokkenAangepast:AantalBlokken(AantalBlokken)",
        "PlotboekPaginaAantalBlokkenAangepastV2:AantalBlokkenId(String)",
        "PlotboekPaginaAantalNpcsAangepast:AlleNpcs(Boolean),Rollen(IReadOnlyList`1)",
        "PlotboekPaginaAantalNpcsAangepastV2:AlleNpcs(Boolean),GeenNpcsNodig(Boolean),Rollen(IReadOnlyList`1)",
        "PlotboekPaginaDagAangepast:Dag(Dag)",
        "PlotboekPaginaDebriefAangepast:IsDebriefGewenst(Boolean),Vragen(IReadOnlyList`1)",
        "PlotboekPaginaDomainAangepast:DomainDescription(String),DomainId(Guid),DomainShorthand(String)",
        "PlotboekPaginaElevatorPitchAangepast:ElevatorPitch(String)",
        "PlotboekPaginaEncounterTypesAangepast:Major(IReadOnlyList`1),Minor(IReadOnlyList`1)",
        "PlotboekPaginaEncounterTypesAangepastV2:Major(IReadOnlyList`1),Minor(IReadOnlyList`1)",
        "PlotboekPaginaGeschatteTijdsduurAangepast:MaximumMinuten(Nullable`1),MinimumMinuten(Nullable`1)",
        "PlotboekPaginaIconenAangepast:Iconen(PlotboekPaginaIcons)",
        "PlotboekPaginaInstructiePentagonAangepast:InstructiePentagon(String)",
        "PlotboekPaginaIsFlexibelAangepast:IsFlexibel(Boolean)",
        "PlotboekPaginaIsFlexibelNachtPlotAangepast:IsFlexibelNachtPlot(Boolean)",
        "PlotboekPaginaIsGedraaidAangepast:IsGedraaid(Boolean)",
        "PlotboekPaginaIsSpelerGedrevenAangepast:IsSpelerGedreven(Boolean)",
        "PlotboekPaginaIsVerwijderdAangepast:IsVerwijderd(Boolean)",
        "PlotboekPaginaLocatieEncounterAangepast:Locatie(LocatieEncounter),Specificatie(String)",
        "PlotboekPaginaLocatieEncounterAangepastV2:Locatie(LocatieEncounter),LocatieId(String),Specificatie(String)",
        "PlotboekPaginaMeerInformatieAangepast:MeerInformatie(String)",
        "PlotboekPaginaOpmerkingenAangepast:Opmerkingen(String)",
        "PlotboekPaginaOrderAangepast:Dag(Dag),Order(Int32),Starttijd(Starttijd)",
        "PlotboekPaginaOrderAangepastV2:Dag(Dag),Order(Int32),StarttijdId(String)",
        "PlotboekPaginaPlotlijnAangepast:PlotlijnId(PlotlijnId)",
        "PlotboekPaginaPropsEnLootAangepast:Loot(IReadOnlyList`1),Props(IReadOnlyList`1)",
        "PlotboekPaginaSamenvattingPlotAangepast:SamenvattingPlot(String)",
        "PlotboekPaginaStarttijdAangepast:Starttijd(Starttijd)",
        "PlotboekPaginaStarttijdAangepastV2:StarttijdId(String)",
        "PlotboekPaginaTitelAangepast:Titel(String)",
        "PlotboekPaginaVerantwoordelijkeSlAangepast:ExtraSlIds(List`1),ExtraSlNames(List`1),VerantwoordelijkeSlId(String),VerantwoordelijkeSlName(String)",
        "PlotboekPaginaVolledigKlaarAangepast:IsVolledigKlaar(Boolean)",
        "PlotboekSamenvattingAlgemeenPlotAangepast:SamenvattingAlgemeenPlot(String)",
        "PlotboekTitelAangepast:Titel(String)",
        "PlotboekVolledigKlaarAangepast:IsVolledigKlaar(Boolean)",
        "PlotboekNpcsDagTotalenAangepast:NpcsDagTotalen(Dictionary`2)",
        "PlotboekBijlagenAangepast:Bijlagen(IReadOnlyList`1)",
        "PlotboekPaginaBijlagenAangepast:BijlageIds(IReadOnlyList`1)",
        "PlotboekPaginaFeedbackToegevoegd:Id(FeedbackId),Tekst(String)",
        "PlotboekPaginaFeedbackBeantwoord:Id(FeedbackId),Reactie(String)",
        "PlotboekPaginaFeedbackAfgehandeld:Id(FeedbackId)",
        "PlotboekPaginaFeedbackHeropend:Id(FeedbackId),Reden(String)",
        "PlotboekPaginaFeedbackVerwijderd:Id(FeedbackId)",
    ];

    [Test]
    public void All_events_match_expected_fingerprints()
    {
        var actual = GetEventFingerprints();

        actual.Should().BeEquivalentTo(ExpectedFingerprints);
    }

    [Test]
    public void No_events_have_been_removed()
    {
        // Events should never be deleted, because existing aggregates
        // that contain those events would break during deserialization / replay.
        var actual = GetEventFingerprints();

        var removed = ExpectedFingerprints.Except(actual).ToList();

        removed.Should().BeEmpty(
            "events must never be deleted — existing aggregates depend on them. " +
            "If an event is obsolete, keep the type and mark it [Obsolete].");
    }

    [Test]
    public void New_events_are_detected()
    {
        // New events are fine, but they must be added to ExpectedFingerprints
        // so future changes are tracked.
        var actual = GetEventFingerprints();

        var added = actual.Except(ExpectedFingerprints).ToList();

        added.Should().BeEmpty(
            "new events were found that are not yet listed in ExpectedFingerprints. " +
            "This is expected when adding events — please add the following fingerprints " +
            "to the ExpectedFingerprints array:\n" +
            string.Join("\n", added.Select(a => $"  \"{a}\",")));
    }

    private static string[] GetEventFingerprints()
    {
        var baseType = typeof(AuditTrailEvent);

        return [.. baseType.Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsClass: true } && t.IsSubclassOf(baseType))
            .Select(ToFingerprint)
            .Order()];
    }

    private static string ToFingerprint(Type eventType)
    {
        // Only include properties declared on the event itself (not inherited Who/When/What/Details).
        var props = eventType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.Name is not "What" and not "Details" and not "EqualityContract")
            .OrderBy(p => p.Name)
            .Select(p => $"{p.Name}({p.PropertyType.Name})")
            .ToList();

        return $"{eventType.Name}:{string.Join(",", props)}";
    }
}
