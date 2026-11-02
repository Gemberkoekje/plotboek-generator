using Microsoft.AspNetCore.Http;
using PlotboekGenerator.core;
using PlotboekGenerator.WebApp.Resources;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel
{
    [BindProperty]
    public int? EditableNpcsDonderdag { get; set; }

    [BindProperty]
    public int? EditableNpcsVrijdag { get; set; }

    [BindProperty]
    public int? EditableNpcsZaterdag { get; set; }

    [BindProperty]
    public int? EditableNpcsZondag { get; set; }

    [BindProperty]
    public string? EditablePlotboekTitel { get; set; }

    [BindProperty]
    public long PlotboekVersion { get; set; }

    [BindProperty]
    public Dag EditablePlotboekStartDag { get; set; }

    [BindProperty]
    public Dag EditablePlotboekEindDag { get; set; }

    [BindProperty]
    public string? EditablePlotboekStartDatumString { get; set; }

    [BindProperty]
    public string? EditablePlotboekEindDatumString { get; set; }

    [BindProperty]
    public IFormFile? VoorbladAfbeelding { get; set; }

    [BindProperty]
    public bool VerwijderVoorblad { get; set; }

    [BindProperty]
    public string? EditableSamenvattingAlgemeenPlot { get; set; }

    /// <summary>The plotboek's reusable bijlagen, edited inline in the settings form.</summary>
    [BindProperty]
    public List<BijlageInput> EditableBijlagen { get; set; } = [];

    [BindProperty]
    public string? EditableGoogleDriveFolderUrl { get; set; }

    [BindProperty]
    public bool EditablePlotboekIsVolledigKlaar { get; set; }

    public bool HasVoorblad { get; set; }

    public List<string> PlotboekNietKlaarWarnings { get; set; } = [];

    /// <summary>
    /// Set when the submitted bijlagen were rejected outright. Unlike <c>ConcurrencyError</c> the form
    /// still holds what the user typed, so the message asks them to fix it rather than announcing that
    /// the current values are shown.
    /// </summary>
    public string BijlagenValidationError { get; set; } = string.Empty;

    public DateOnly? EditablePlotboekStartDatum =>
        DateOnly.TryParse(EditablePlotboekStartDatumString, CultureInfo.InvariantCulture, out var date) ? date : null;

    public DateOnly? EditablePlotboekEindDatum =>
        DateOnly.TryParse(EditablePlotboekEindDatumString, CultureInfo.InvariantCulture, out var date) ? date : null;

    public PlotboekSettingsInfo? PlotboekSettings { get; set; }

    private async Task LoadPlotboekSettings(bool initializeEditableFields = true)
    {
        if (!PlotboekId.HasValue)
        {
            PlotboekSettings = null;
            return;
        }

        using var session = documentStore.OpenSession();
        var plotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == PlotboekId.ToString());
        if (plotboek != null)
        {
            var voorblad = await session.LoadAsync<PlotboekVoorblad>(PlotboekId.ToString());
            HasVoorblad = voorblad?.Afbeelding?.Length > 0;

            var syncState = await session.LoadAsync<GoogleDriveSyncState>(PlotboekId.ToString());

            PlotboekSettings = new PlotboekSettingsInfo
            {
                EventVersion = plotboek.EventVersion,
                Titel = plotboek.Titel,
                StartDag = plotboek.StartDag,
                EindDag = plotboek.EindDag,
                StartDatum = plotboek.StartDatum == default ? null : plotboek.StartDatum,
                EindDatum = plotboek.EindDatum == default ? null : plotboek.EindDatum,
                SamenvattingAlgemeenPlot = plotboek.SamenvattingAlgemeenPlot,
                GoogleDriveFolderUrl = plotboek.GoogleDriveFolderUrl,
                GoogleDriveFileUrl = plotboek.GoogleDriveFileUrl,
                GoogleDriveLastSyncedAt = syncState?.LastSyncedAt,
                NpcsDagTotalen = plotboek.NpcsDagTotalen ?? new Dictionary<Dag, int>(),
                Bijlagen = plotboek.Bijlagen ?? [],
                IsVolledigKlaar = plotboek.IsVolledigKlaar,
            };

            if (initializeEditableFields)
            {
                logger.LogDebug("Loaded plotboek {PlotboekId} with EventVersion {EventVersion}", PlotboekId, plotboek.EventVersion);
                InitializePlotboekEditableFields();
            }
            // Update the expected NPCs per day for the main Plotboek page usage
            // Note: LoadPlotboekSettings is called after LoadPlotboekPaginas in OnGet,
            // so set DagNpcVerwacht here so the main model gets the latest values.
            DagNpcVerwacht = PlotboekSettings?.NpcsDagTotalen ?? new Dictionary<Dag, int>();
        }
    }

    /// <summary>
    /// Text fields that the user attempted to save for plotboek settings but were rejected due to a concurrency conflict.
    /// </summary>
    public Dictionary<string, string?> AttemptedSettingsChanges { get; set; } = [];

    /// <summary>
    /// Snapshots every free-text settings field, including the bijlagen rows, so a rejected save can
    /// still show the user what they typed. Bijlagen are keyed by list position rather than by id:
    /// the position is what the reloaded form shows, and a position that shifted underneath the user
    /// only ever makes a value look changed — which errs towards showing the text, never towards
    /// dropping it. The keys are the labels the user reads, in the language of the request.
    /// </summary>
    internal Dictionary<string, string?> CaptureEditablePlotboekTextFields()
    {
        var fields = new Dictionary<string, string?>
        {
            [PlotboekUi.WijzigingTitel] = EditablePlotboekTitel,
            [PlotboekUi.WijzigingSamenvattingAlgemeenPlot] = EditableSamenvattingAlgemeenPlot,
            [PlotboekUi.WijzigingGoogleDriveFolderUrl] = EditableGoogleDriveFolderUrl,
            [NpcsDagLabel(Dag.Donderdag)] = EditableNpcsDonderdag?.ToString(),
            [NpcsDagLabel(Dag.Vrijdag)] = EditableNpcsVrijdag?.ToString(),
            [NpcsDagLabel(Dag.Zaterdag)] = EditableNpcsZaterdag?.ToString(),
            [NpcsDagLabel(Dag.Zondag)] = EditableNpcsZondag?.ToString(),
        };

        var bijlagen = EditableBijlagen ?? [];
        for (var i = 0; i < bijlagen.Count; i++)
        {
            var nummer = i + 1;
            fields[BijlageNaamLabel(nummer)] = bijlagen[i].Naam;

            // An untouched Quill editor posts back empty wrapper markup; storing it as null keeps the
            // recovery panel from offering "<p><br></p>" back as an unsaved change.
            var inhoud = bijlagen[i].InhoudHtml ?? string.Empty;
            fields[BijlageInhoudLabel(nummer)] = HasBijlageInhoud(inhoud) ? inhoud : null;
        }

        return fields;
    }

    /// <summary>Label under which the expected number of NPCs on <paramref name="dag"/> is captured for the recovery panel.</summary>
    /// <param name="dag">The day.</param>
    /// <returns>The dictionary key, which doubles as the label shown to the user.</returns>
    private static string NpcsDagLabel(Dag dag)
        => string.Format(CultureInfo.CurrentCulture, PlotboekUi.WijzigingNpcsDag, dag.Naam());

    /// <summary>Label under which bijlage <paramref name="nummer"/>'s name is captured for the recovery panel.</summary>
    /// <param name="nummer">The bijlage's 1-based position in the settings form.</param>
    /// <returns>The dictionary key, which doubles as the label shown to the user.</returns>
    internal static string BijlageNaamLabel(int nummer)
        => string.Format(CultureInfo.CurrentCulture, PlotboekUi.WijzigingBijlageNaam, nummer.ToString(CultureInfo.InvariantCulture));

    /// <summary>Label under which bijlage <paramref name="nummer"/>'s rich-text content is captured for the recovery panel.</summary>
    /// <param name="nummer">The bijlage's 1-based position in the settings form.</param>
    /// <returns>The dictionary key, which doubles as the label shown to the user.</returns>
    internal static string BijlageInhoudLabel(int nummer)
        => string.Format(CultureInfo.CurrentCulture, PlotboekUi.WijzigingBijlageInhoud, nummer.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Whether an <see cref="AttemptedSettingsChanges"/> entry holds rich-text HTML and must be rendered
    /// as markup instead of as escaped text.
    /// </summary>
    /// <param name="label">The dictionary key of the attempted change, in the language of the request.</param>
    /// <returns><c>true</c> for the Quill-backed fields, <c>false</c> for plain text ones.</returns>
    public static bool IsHtmlSettingsChangeLabel(string label)
        => string.Equals(label, PlotboekUi.WijzigingSamenvattingAlgemeenPlot, StringComparison.Ordinal)
        || IsBijlageInhoudLabel(label);

    /// <summary>
    /// Whether <paramref name="label"/> is a <see cref="BijlageInhoudLabel"/>: the translated label with a
    /// row number where its <c>{0}</c> is.
    /// </summary>
    private static bool IsBijlageInhoudLabel(string label)
    {
        ArgumentNullException.ThrowIfNull(label);

        var format = PlotboekUi.WijzigingBijlageInhoud;
        var plek = format.IndexOf("{0}", StringComparison.Ordinal);
        var voor = format[..plek];
        var na = format[(plek + "{0}".Length)..];

        return label.Length > voor.Length + na.Length
            && label.StartsWith(voor, StringComparison.Ordinal)
            && label.EndsWith(na, StringComparison.Ordinal)
            && label[voor.Length..^na.Length].All(char.IsAsciiDigit);
    }

    internal void InitializePlotboekEditableFields()
    {
        PlotboekVersion = PlotboekSettings?.EventVersion ?? 0;
        EditablePlotboekTitel = PlotboekSettings?.Titel;
        EditablePlotboekStartDag = PlotboekSettings?.StartDag ?? Dag.None;
        EditablePlotboekEindDag = PlotboekSettings?.EindDag ?? Dag.None;
        EditablePlotboekStartDatumString = PlotboekSettings?.StartDatum?.ToString("yyyy-MM-dd");
        EditablePlotboekEindDatumString = PlotboekSettings?.EindDatum?.ToString("yyyy-MM-dd");
        EditableSamenvattingAlgemeenPlot = PlotboekSettings?.SamenvattingAlgemeenPlot;
        EditableGoogleDriveFolderUrl = PlotboekSettings?.GoogleDriveFolderUrl;
        EditablePlotboekIsVolledigKlaar = PlotboekSettings?.IsVolledigKlaar ?? false;
        EditableNpcsDonderdag = PlotboekSettings?.NpcsDagTotalen.GetValueOrDefault(Dag.Donderdag) is > 0 ? PlotboekSettings.NpcsDagTotalen[Dag.Donderdag] : null;
        EditableNpcsVrijdag = PlotboekSettings?.NpcsDagTotalen.GetValueOrDefault(Dag.Vrijdag) is > 0 ? PlotboekSettings.NpcsDagTotalen[Dag.Vrijdag] : null;
        EditableNpcsZaterdag = PlotboekSettings?.NpcsDagTotalen.GetValueOrDefault(Dag.Zaterdag) is > 0 ? PlotboekSettings.NpcsDagTotalen[Dag.Zaterdag] : null;
        EditableNpcsZondag = PlotboekSettings?.NpcsDagTotalen.GetValueOrDefault(Dag.Zondag) is > 0 ? PlotboekSettings.NpcsDagTotalen[Dag.Zondag] : null;
        EditableBijlagen = PlotboekSettings?.Bijlagen
            .Select(b => new BijlageInput { Id = b.Id.ToString(), Naam = b.Titel, InhoudHtml = b.Inhoud })
            .ToList() ?? [];
    }

    private async Task<IActionResult> HandleSavePlotboek()
    {
        if (PlotboekId.HasValue)
        {
            // Validated up front, and answered without reloading: every other failure path below
            // re-reads the plotboek and resets the editable fields, which for this failure would
            // throw away the very content the message is asking the user to name.
            var bijlagenValidatie = ValidateEditableBijlagen();
            if (!bijlagenValidatie.IsValid)
            {
                logger.LogWarning(
                    "Rejected save of plotboek {PlotboekId}: bijlagen with content but without a name. {Messages}",
                    PlotboekId,
                    string.Join(", ", bijlagenValidatie.Messages.Select(m => m.Message)));

                BijlagenValidationError = string.Join(" ", bijlagenValidatie.Messages.Select(m => m.Message));
                return Page();
            }

            var result = await SavePlotboek();
            if (!result.IsValid)
            {
                logger.LogWarning("Concurrency conflict while saving plotboek {PlotboekId}", PlotboekId);
                ConcurrencyError = string.Join(" ", result.Messages.Select(m => m.Message));

                // Capture the user's attempted text values before reinitializing
                var attemptedValues = CaptureEditablePlotboekTextFields();

                // Reload the latest data and reinitialize form fields with current DB values
                await LoadPlotboekSettings();
                await LoadPlotboekPaginas();

                // Build the list of attempted changes that differ from the current DB values.
                // GetValueOrDefault, not the indexer: the reloaded plotboek can hold fewer bijlagen
                // than the form posted, so a submitted row simply has no counterpart to compare to.
                var currentValues = CaptureEditablePlotboekTextFields();
                foreach (var (label, attemptedValue) in attemptedValues)
                {
                    if (!string.Equals(attemptedValue, currentValues.GetValueOrDefault(label), StringComparison.Ordinal)
                        && !string.IsNullOrEmpty(attemptedValue))
                    {
                        AttemptedSettingsChanges[label] = attemptedValue;
                    }
                }

                return Page();
            }

            // Refresh the data to show the updated values
            await LoadPlotboekSettings();
            await LoadPlotboekPaginas();
        }

        return Page();
    }

    private async Task<Result> SavePlotboek()
    {
        logger.LogInformation("Saving plotboek {PlotboekId} with title: {Title}, StartDatumString: {StartDatumString}, EindDatumString: {EindDatumString}, StartDatum: {StartDatum}, EindDatum: {EindDatum}, SubmittedPlotboekVersion: {PlotboekVersion}", PlotboekId, EditablePlotboekTitel, EditablePlotboekStartDatumString, EditablePlotboekEindDatumString, EditablePlotboekStartDatum, EditablePlotboekEindDatum, PlotboekVersion);

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            logger.LogWarning("No user found in session while trying to save plotboek {PlotboekId}", PlotboekId);
            return Result.OK;
        }

        var plotboek = await plotboekRepository.GetByIdAsync(PlotboekId, CancellationToken.None).ConfigureAwait(false);
        if (!plotboek.IsValid)
        {
            logger.LogWarning("Plotboek {PlotboekId} not found", PlotboekId);
            return Result.OK;
        }

        if (!CanEditCurrentPlotboek())
        {
            if (EditablePlotboekIsVolledigKlaar != plotboek.Value.IsVolledigKlaar)
            {
                return Result.WithMessages(ValidationMessage.Error(PlotboekUi.AlleenAdminsPlotboekVolledigKlaar));
            }

            return Result.WithMessages(ValidationMessage.Error(PlotboekUi.PlotboekVergrendeld));
        }

        var nietKlarePaginas = GetNietKlarePlotboekPaginaNames();
        if (IsAdmin && EditablePlotboekIsVolledigKlaar && nietKlarePaginas.Count > 0)
        {
            PlotboekNietKlaarWarnings = [.. nietKlarePaginas];
            return Result.WithMessages(ValidationMessage.Error(string.Format(CultureInfo.CurrentCulture, PlotboekUi.PlotboekNogNietKlaarMet, string.Join(", ", nietKlarePaginas))));
        }

        if (PlotboekVersion > 0 && PlotboekSettings?.EventVersion > 0 && PlotboekSettings.EventVersion != PlotboekVersion)
        {
            logger.LogWarning("Version mismatch for plotboek {PlotboekId}: form submitted version {SubmittedVersion}, ExportData projection version {ProjectionVersion}, aggregate stream version {AggregateVersion}",
                PlotboekId, PlotboekVersion, PlotboekSettings.EventVersion, plotboek.Value.Version);
            return Result.WithMessages(ValidationMessage.Error(PlotboekUi.IemandAndersAangepast));
        }

        // Apply basis data changes (titel, start/eind dag)
        plotboek = plotboek
            | (p => p.Aanpassen(BuildPlotboekBasisData(), user))
            | (p =>
            {
                // Apply kalender data changes (start/eind datum) if dates are provided
                if (EditablePlotboekStartDatum.HasValue && EditablePlotboekEindDatum.HasValue)
                    return p.Aanpassen(BuildPlotboekKalenderData(), user);
                if (!EditablePlotboekStartDatum.HasValue && !EditablePlotboekEindDatum.HasValue)
                    return p.Aanpassen(new Plotboek.KalenderData { StartDatum = null, EindDatum = null }, user);
                return Result.For(p);
            })
            // Apply samenvatting algemeen plot changes
            | (p => p.Aanpassen(
                new Plotboek.SamenvattingAlgemeenPlotData
                {
                    SamenvattingAlgemeenPlot = EditableSamenvattingAlgemeenPlot ?? string.Empty,
                },
                user))
            // Apply Google Drive folder URL changes
            | (p => p.Aanpassen(
                new Plotboek.GoogleDriveFolderUrlData
                {
                    GoogleDriveFolderUrl = EditableGoogleDriveFolderUrl ?? string.Empty,
                },
                user));

        if (IsAdmin)
        {
            plotboek |= p => p.Aanpassen(
                new Plotboek.VolledigKlaarData
                {
                    IsVolledigKlaar = EditablePlotboekIsVolledigKlaar,
                },
                user);
        }

        // Apply NPCs per day settings
        plotboek |= p => p.Aanpassen(BuildNpcsDagTotalenData(), user);

        // Apply bijlagen changes
        plotboek |= p => p.Aanpassen(BuildBijlagenData(), user);

        if (!plotboek.IsValid)
        {
            return Result.WithMessages(plotboek.Messages);
        }

        try
        {
            await plotboekRepository.SaveAsync(plotboek.Value, CancellationToken.None).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to save plotboek {PlotboekId}", PlotboekId);
            return Result.WithMessages(ValidationMessage.Error(ex.Message));
        }

        session.Store(plotboek.Value.GetData());

        // Handle voorblad image upload or removal
        if (VerwijderVoorblad)
        {
            session.Delete<PlotboekVoorblad>(PlotboekId.ToString());
        }
        else if (VoorbladAfbeelding is { Length: > 0 })
        {
            using var ms = new System.IO.MemoryStream();
            await VoorbladAfbeelding.CopyToAsync(ms);
            session.Store(new PlotboekVoorblad
            {
                Id = PlotboekId.ToString(),
                Afbeelding = ms.ToArray(),
                ContentType = VoorbladAfbeelding.ContentType,
            });
        }

        await session.SaveChangesAsync(CancellationToken.None);
        return Result.OK;
    }

    internal Plotboek.BasisData BuildPlotboekBasisData()
    {
        return new Plotboek.BasisData
        {
            Titel = EditablePlotboekTitel ?? string.Empty,
            StartDag = EditablePlotboekStartDag,
            EindDag = EditablePlotboekEindDag,
        };
    }

    internal Plotboek.KalenderData BuildPlotboekKalenderData()
    {
        return new Plotboek.KalenderData
        {
            StartDatum = EditablePlotboekStartDatum ?? default,
            EindDatum = EditablePlotboekEindDatum ?? default,
        };
    }

    public sealed record PlotboekSettingsInfo
    {
        public long EventVersion { get; init; }

        public string? Titel { get; init; }

        public Dag StartDag { get; init; }

        public Dag EindDag { get; init; }

        public DateOnly? StartDatum { get; init; }

        public DateOnly? EindDatum { get; init; }

        public string? SamenvattingAlgemeenPlot { get; init; }

        public string? GoogleDriveFolderUrl { get; init; }

        public string? GoogleDriveFileUrl { get; init; }

        public DateTime? GoogleDriveLastSyncedAt { get; init; }

        public IReadOnlyDictionary<Dag, int> NpcsDagTotalen { get; init; } = new Dictionary<Dag, int>();

        public IReadOnlyList<BijlageData> Bijlagen { get; init; } = [];

        public bool IsVolledigKlaar { get; init; }

        public bool HasDatums => StartDatum.HasValue && EindDatum.HasValue;
    }

    public IReadOnlyList<string> GetNietKlarePlotboekPaginaNames()
    {
        return [.. PlotboekPaginas
            .Where(p => !p.IsVolledigKlaar)
            .Select(GetPlotboekPaginaDisplayName)];
    }

    private static string GetPlotboekPaginaDisplayName(PlotboekPaginaInfo pagina)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(pagina.DomainShorthand))
        {
            parts.Add(pagina.DomainShorthand);
        }

        if (!string.IsNullOrWhiteSpace(pagina.PlotlijnNummer))
        {
            parts.Add(pagina.PlotlijnNummer);
        }

        if (!string.IsNullOrWhiteSpace(pagina.Title))
        {
            parts.Add(pagina.Title);
        }

        return parts.Count > 0 ? string.Join(" ", parts) : pagina.Id.ToString();
    }

    internal Plotboek.NpcsDagTotalenData BuildNpcsDagTotalenData()
    {
        var dict = new Dictionary<Dag, int>();

        if (EditableNpcsDonderdag is > 0)
        {
            dict[Dag.Donderdag] = EditableNpcsDonderdag.Value;
        }

        if (EditableNpcsVrijdag is > 0)
        {
            dict[Dag.Vrijdag] = EditableNpcsVrijdag.Value;
        }

        if (EditableNpcsZaterdag is > 0)
        {
            dict[Dag.Zaterdag] = EditableNpcsZaterdag.Value;
        }

        if (EditableNpcsZondag is > 0)
        {
            dict[Dag.Zondag] = EditableNpcsZondag.Value;
        }

        return new Plotboek.NpcsDagTotalenData { NpcsDagTotalen = dict };
    }

    internal Plotboek.BijlagenData BuildBijlagenData()
    {
        // Drop only genuinely empty rows — no name AND no content. A row that has content but no name
        // is the user's typing, not junk; ValidateEditableBijlagen rejects the save before we get here
        // so it can never be silently discarded. (The aggregate still refuses nameless bijlagen, so
        // such a row cannot leak into the plotboek either.)
        var bijlagen = (EditableBijlagen ?? [])
            .Where(b => !string.IsNullOrWhiteSpace(b.Naam) || HasBijlageInhoud(b.InhoudHtml ?? string.Empty))
            .Select((b, i) => BijlageData.Create(
                !string.IsNullOrWhiteSpace(b.Id) && BijlageId.TryParse(b.Id, out var id) ? id : BijlageId.Next(),
                b.Naam ?? string.Empty,
                b.InhoudHtml ?? string.Empty,
                i))
            .ToList();

        return new Plotboek.BijlagenData { Bijlagen = bijlagen };
    }

    /// <summary>
    /// Rejects bijlagen that carry rich-text content but no name. Such a row would be dropped by the
    /// aggregate (a bijlage is meaningful only once it has a title), so without this gate the user's
    /// typing disappears on the redirect with nothing said about it.
    /// </summary>
    /// <returns>
    /// <see cref="Result.OK"/> when every row with content also has a name; otherwise an invalid
    /// <see cref="Result"/> naming the offending rows by their position in the form.
    /// </returns>
    internal Result ValidateEditableBijlagen()
    {
        var naamloos = (EditableBijlagen ?? [])
            .Select((b, i) => (Nummer: i + 1, b.Naam, b.InhoudHtml))
            .Where(b => string.IsNullOrWhiteSpace(b.Naam) && HasBijlageInhoud(b.InhoudHtml ?? string.Empty))
            .Select(b => b.Nummer.ToString(CultureInfo.InvariantCulture))
            .ToList();

        if (naamloos.Count == 0)
        {
            return Result.OK;
        }

        var melding = string.Format(
            CultureInfo.CurrentCulture,
            naamloos.Count == 1 ? PlotboekUi.BijlageZonderNaamOne : PlotboekUi.BijlageZonderNaamMany,
            Opsomming(naamloos));

        return Result.WithMessages(ValidationMessage.Error(melding));
    }

    /// <summary>
    /// Whether a bijlage's rich-text field holds anything the user would recognise as content. Quill
    /// posts back wrapper markup such as <c>&lt;p&gt;&lt;br&gt;&lt;/p&gt;</c> for an editor that was
    /// opened but never typed in, and that must not count.
    /// </summary>
    /// <param name="inhoudHtml">The posted rich-text HTML, possibly empty.</param>
    /// <returns><c>true</c> when the markup carries text or an embedded element.</returns>
    public static bool HasBijlageInhoud(string inhoudHtml)
    {
        // Normalize first: it strips the empty Quill wrappers and trailing empty blocks, which is
        // exactly the "opened but never typed in" shape.
        var genormaliseerd = TextNormalization.Normalize(inhoudHtml ?? string.Empty);
        if (genormaliseerd.Length == 0)
        {
            return false;
        }

        // An image or a table is content even though it contributes no text.
        if (EmbeddedBijlageContentRegex().IsMatch(genormaliseerd))
        {
            return true;
        }

        // WebUtility.HtmlDecode turns &nbsp; into U+00A0, which IsNullOrWhiteSpace treats as blank.
        var tekst = WebUtility.HtmlDecode(BijlageHtmlTagRegex().Replace(genormaliseerd, string.Empty));
        return !string.IsNullOrWhiteSpace(tekst);
    }

    /// <summary>Joins row numbers the way a sentence does: "2, 3 en 5", "2, 3 and 5".</summary>
    private static string Opsomming(IReadOnlyList<string> onderdelen)
        => onderdelen.Count == 1
            ? onderdelen[0]
            : string.Format(CultureInfo.CurrentCulture, PlotboekUi.OpsommingLaatste, string.Join(", ", onderdelen.Take(onderdelen.Count - 1)), onderdelen[^1]);

    [GeneratedRegex("<[^>]+>", RegexOptions.None, "nl-NL")]
    private static partial Regex BijlageHtmlTagRegex();

    [GeneratedRegex("(?i)<(?:img|table|hr|iframe|video)\\b", RegexOptions.None, "nl-NL")]
    private static partial Regex EmbeddedBijlageContentRegex();

    /// <summary>Form input model for a single bijlage row in the settings form.</summary>
    public sealed record BijlageInput
    {
        public string? Id { get; init; }

        public string? Naam { get; init; }

        public string? InhoudHtml { get; init; }
    }

    public async Task<IActionResult> OnGetVoorbladAfbeelding()
    {
        if (!PlotboekId.HasValue)
            return NotFound();

        using var session = documentStore.OpenSession();
        var voorblad = await session.LoadAsync<PlotboekVoorblad>(PlotboekId.ToString());
        if (voorblad?.Afbeelding is not { Length: > 0 })
            return NotFound();

        return File(voorblad.Afbeelding, voorblad.ContentType);
    }
}
