using System.Collections.Immutable;
using PlotboekGenerator.core.Resources;

namespace PlotboekGenerator.core.Generator.Templates;

public sealed record PlotboekTemplateData
{
    required public string Naam { get; init; }

    /// <summary>
    /// Raw image bytes for the front page picture content control.
    /// When set, the Voorblad picture field in the DOCX template is replaced with this image.
    /// </summary>
#pragma warning disable QW0012 // Use immutable types for properties
    public byte[] Voorblad { get; init; }
#pragma warning restore QW0012 // Use immutable types for properties

    required public string SamenvattingAlgemeenPlot { get; init; }
    required public ImmutableList<TijdschemasData> Tijdschemas { get; init; }
    required public ImmutableList<DayData> Days { get; init; }
    required public ImmutableList<ExtraInfoData> ExtraInfo { get; init; }

    /// <summary>
    /// The reusable bijlagen rendered once in the "Bijlagen" appendix. Every authored bijlage
    /// appears here, whether or not any encounter references it.
    /// </summary>
    public ImmutableList<BijlageAppendixData> Bijlagen { get; init; } = [];

    required public ImmutableList<DomeinData> AslDomeinen { get; init; }
    required public ImmutableList<DomeinData> GslDomeinen { get; init; }

    required public ImmutableList<PageData> EvergreenPages { get; init; }
    required public ImmutableList<NaamData> Namen { get; init; }
    required public ImmutableList<NaamData> Werelden { get; init; }

    /// <summary>Number of the static "Evergreen encounters" appendix (dynamic bijlagen count + 1).</summary>
    public string EvergreenBijlageNummer { get; init; } = string.Empty;

    /// <summary>Number of the static "Random namen" appendix (dynamic bijlagen count + 2).</summary>
    public string NamenBijlageNummer { get; init; } = string.Empty;
}

public sealed record NaamData
{
    required public string Naam { get; init; }

}

public sealed record TijdschemasData
{
    required public string Day { get; init; }

    required public ImmutableList<TijdschemaData> Tijdschema { get; init; }

}

public sealed record TijdschemaData
{
    required public string Tijd { get; init; }

    required public string Icons { get; init; }

    required public string NaamEncounter { get; init; }

    required public string SL { get; init; }

    required public string NPCs { get; init; }

    required public string Pagina { get; init; }

    /// <summary>
    /// Bookmark name targeting the page's section in the Days repeater via a PAGEREF field.
    /// </summary>
    public string PaginaBookmark { get; init; } = string.Empty;

    /// <summary>
    /// When true, the top border of this row and the bottom border of the preceding row
    /// are removed to visually group rows within the same time block.
    /// </summary>
    public bool HideTopBorder { get; init; }

    /// <summary>
    /// When true, this row represents a meeting/organisational item rather than an encounter.
    /// Meeting rows are rendered with bold text and a light gray background.
    /// </summary>
    public bool IsMeeting { get; init; }

    /// <summary>
    /// When true, a page break is forced before this row so the day's evening
    /// programme (everything after "Avondeten") starts on a fresh page and the
    /// daytime and evening plots each get a full page of room.
    /// </summary>
    public bool PageBreakBefore { get; init; }
}

public sealed record DayData
{
    required public string Day { get; init; }

    required public ImmutableList<PageData> Pages { get; init; }
}
public sealed record PageData
{
    /// <summary>The placeholder for a field nobody filled in, in the language of the export.</summary>
    public static string NogNietIngevuld => CoreTekst.ExportNogNietIngevuld;

    required public string NaamEncounter { get; init; }
    required public string VerantwoordelijkeSl { get; init; }
    required public string Dagdeel { get; init; }
    required public string GeschatteTijdsduur { get; init; }
    required public string SoortEncounter { get; init; }
    required public string LocatieEncounter { get; init; }
    required public string AantalNpcs { get; init; }
    required public string KledingEnSchmink { get; init; }
    required public string ElevatorPitch { get; init; }
    required public string SamenvattingPlot { get; init; }
    required public string BenodigdeProps { get; init; }
    required public string GewensteLoot { get; init; }
    required public string InstructiePentagon { get; init; }
    required public string MeerInformatie { get; init; }
    required public string Opmerkingen { get; init; }
    required public string Debrief { get; init; }
    required public bool IsVolledigKlaar { get; init; }

    /// <summary>
    /// Bookmark name pointing to the Extra Info section for this page.
    /// When set, the MeerInformatie field will render as text followed by a PAGEREF field.
    /// </summary>
    public string MeerInformatieBookmark { get; init; } = string.Empty;

    /// <summary>
    /// Cross-references to the plotboek-level bijlagen this encounter uses. Each renders as
    /// "Voor {Naam} zie bijlage op pagina {PAGEREF}" pointing at the bijlage's appendix anchor.
    /// </summary>
    public ImmutableList<BijlageVerwijzingData> BijlageVerwijzingen { get; init; } = [];

    /// <summary>
    /// Bookmark name used for cross-referencing from the Tijdschema PAGEREF field.
    /// </summary>
    public string BookmarkName { get; init; } = string.Empty;

    /// <summary>
    /// Icon string to prepend to the encounter name in the DOCX export.
    /// Contains Unicode symbols for all applicable icons (auto-determined + explicit).
    /// </summary>
    public string IconenPrefix { get; init; } = string.Empty;

    /// <summary>
    /// The full encounter name with icon prefix for display in the DOCX export.
    /// </summary>
    public string NaamEncounterMetIconen => string.IsNullOrEmpty(IconenPrefix)
        ? NaamEncounter
        : $"{IconenPrefix} {NaamEncounter}";
}

public sealed record ExtraInfoData
{
    required public string NaamEncounter { get; init; }
    required public string MeerInformatie { get; init; }

    /// <summary>
    /// Bookmark name used for cross-referencing from the page data PAGEREF field.
    /// </summary>
    public string BookmarkName { get; init; } = string.Empty;
}

/// <summary>
/// A single entry in the "Bijlagen" appendix. Named distinctly from the domain
/// <c>BijlageData</c> so the export builder (which imports both namespaces) has no collision.
/// </summary>
public sealed record BijlageAppendixData
{
    /// <summary>1-based appendix number, rendered as "Bijlage {Nummer}: {Titel}".</summary>
    public string Nummer { get; init; } = string.Empty;

    required public string Titel { get; init; }
    required public string Inhoud { get; init; }

    /// <summary>
    /// Bookmark name the encounter PAGEREF fields target. Must be literally named <c>BookmarkName</c>
    /// so the docx engine anchors this entry, exactly like <see cref="ExtraInfoData.BookmarkName"/>.
    /// </summary>
    public string BookmarkName { get; init; } = string.Empty;
}

/// <summary>
/// A single "Voor {Naam} zie bijlage op pagina {PAGEREF}" cross-reference on an encounter,
/// mirroring <see cref="TijdschemaData.Pagina"/>/<see cref="TijdschemaData.PaginaBookmark"/>.
/// </summary>
public sealed record BijlageVerwijzingData
{
    required public string Naam { get; init; }

    /// <summary>The referenced bijlage's number, rendered as "zie bijlage {Nummer} op pagina ...".</summary>
    public string Nummer { get; init; } = string.Empty;

    public string Pagina { get; init; } = string.Empty;

    /// <summary>Bookmark name of the referenced bijlage's appendix entry; the PAGEREF resolves it to a page number.</summary>
    public string PaginaBookmark { get; init; } = string.Empty;
}

public sealed record TableOfContentsData
{
    required public string ToCItem { get; init; }
    required public string ToCPage { get; init; }

    /// <summary>
    /// Bookmark name that the ToCPage field targets via a PAGEREF field.
    /// When set, ToCPage renders as a PAGEREF pointing to this bookmark
    /// so Word fills in the page number on open.
    /// </summary>
    public string ToCPageBookmark { get; init; } = string.Empty;
}

public sealed record DomeinData
{
    required public string DomeinLetter { get; init; }
    required public string DomeinNaam { get; init; }
}
