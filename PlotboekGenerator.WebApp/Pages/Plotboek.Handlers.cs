using System.Linq;
using PlotboekGenerator.infrastructure.Generator;

namespace PlotboekGenerator.WebApp.Pages;

public sealed partial class PlotboekModel
{
    private async Task<IActionResult> HandleMovePagina()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        if (!PaginaId.HasValue || !MoveToPlotboekId.HasValue || !PlotboekId.HasValue)
        {
            return Page();
        }

        if (MoveToPlotboekId.Equals(PlotboekId))
        {
            return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId });
        }

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            logger.LogWarning("No user found in session while trying to move plotboek pagina {PaginaId}", PaginaId);
            return Page();
        }

        if (await paginaRepository.LoadAsync(PaginaId) is not { } plotboekPagina || !plotboekPagina.IsValid)
        {
            logger.LogWarning("Plotboek pagina {PaginaId} not found for move", PaginaId);
            return Page();
        }

        var targetPlotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == MoveToPlotboekId.ToString());
        if (targetPlotboek == null)
        {
            var allPlotboeken = await session.QueryAllAsync<Plotboek.ExportData>();
            var targetInfo = AvailablePlotboeken.FirstOrDefault(p => p.Id == MoveToPlotboekId);
            if (targetInfo == null)
            {
                logger.LogWarning("Target plotboek {PlotboekId} not found in available options while moving pagina {PaginaId}", MoveToPlotboekId, PaginaId);
                return Page();
            }

            var (startDag, eindDag) = await GetEventDagenAsync(targetInfo.EventId);
            var newTargetPlotboek = Plotboek.Create(targetInfo.Jaar, targetInfo.EventId, startDag, eindDag, user).Value;
            if (allPlotboeken.Count > 0)
            {
                var sourcePlotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == PlotboekId.ToString());
                if (sourcePlotboek != null)
                {
                    newTargetPlotboek = newTargetPlotboek.Aanpassen(
                        new Plotboek.BasisData
                        {
                            StartDag = sourcePlotboek.StartDag,
                            EindDag = sourcePlotboek.EindDag,
                            Titel = sourcePlotboek.Titel,
                        },
                        user).Value;

                    newTargetPlotboek = newTargetPlotboek.Aanpassen(
                        new Plotboek.KalenderData
                        {
                            StartDatum = sourcePlotboek.StartDatum,
                            EindDatum = sourcePlotboek.EindDatum,
                        },
                        user).Value;

                    newTargetPlotboek = newTargetPlotboek.Aanpassen(
                        new Plotboek.SamenvattingAlgemeenPlotData
                        {
                            SamenvattingAlgemeenPlot = sourcePlotboek.SamenvattingAlgemeenPlot,
                        },
                        user).Value;

                    newTargetPlotboek = newTargetPlotboek.Aanpassen(
                        new Plotboek.NpcsDagTotalenData
                        {
                            NpcsDagTotalen = sourcePlotboek.NpcsDagTotalen ?? new Dictionary<Dag, int>(),
                        },
                        user).Value;
                }
            }

            try
            {
                await plotboekRepository.SaveAsync(newTargetPlotboek, CancellationToken.None).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Failed to persist target plotboek {PlotboekId} while moving pagina {PaginaId}", MoveToPlotboekId, PaginaId);
                ConcurrencyError = ex.Message;
                return Page();
            }

            targetPlotboek = newTargetPlotboek.GetData();
            session.Store(targetPlotboek);
            await session.SaveChangesAsync(CancellationToken.None);
        }

        var movedPagina = plotboekPagina.Value.AanPlotboekToevoegen(MoveToPlotboekId, targetPlotboek.DisplayName, user);
        if (!movedPagina.IsValid)
        {
            logger.LogWarning("Failed to move plotboek pagina {PaginaId}: {Errors}", PaginaId, string.Join(", ", movedPagina.Messages.Select(m => m.Message)));
            return Page();
        }

        var saveResult = await paginaRepository.SaveAsync(movedPagina.Value, CancellationToken.None);
        if (!saveResult.IsValid)
        {
            logger.LogWarning("Concurrency conflict while moving plotboek pagina {PaginaId}", PaginaId);
            ConcurrencyError = string.Join(" ", saveResult.Messages.Select(m => m.Message));
            return Page();
        }

        session.Store(movedPagina.Value.GetData());
        await session.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("User {User} moved plotboek pagina {PaginaId} from {FromPlotboekId} to {ToPlotboekId}", user.Id, PaginaId, PlotboekId, MoveToPlotboekId);

        var targetInfoForRedirect = AvailablePlotboeken.FirstOrDefault(p => p.Id == MoveToPlotboekId);
        if (targetInfoForRedirect != null)
        {
            return RedirectToPage("/Plotboek", new { targetInfoForRedirect.Jaar, Event = targetInfoForRedirect.EventId, PaginaId });
        }

        return RedirectToPage("/Plotboek");
    }

    private async Task<IActionResult> HandleGenereerElevatorPitch()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        if (!PaginaId.HasValue)
        {
            return Page();
        }

        // Ensure the pagina exists and rehydrate UI state
        if (await paginaRepository.LoadAsync(PaginaId) is not { } plotboekPagina || !plotboekPagina.IsValid)
        {
            logger.LogWarning("Plotboek pagina {PaginaId} not found while generating elevator pitch", PaginaId);
            return Page();
        }
        await LoadPlotboekPaginas();
        LoadSelectedPlotboekPagina();

        // Clear any previous error and preview before starting a new request
        ElevatorPitchError = null;
        var ct = CancellationToken.None;
        try
        {
            if (geminiSummaryService is not { IsConfigured: true })
            {
                logger.LogWarning("GeminiSummaryService not configured; cannot generate elevator pitch");
                GeneratedElevatorPitchPreview = string.Empty;
            }
            else
            {
                // Build contextual fields to pass to the summary service.
                var npcParts = new List<string>();
                if (EditableAlleNpcs)
                {
                    npcParts.Add("ALLE NPCs");
                }
                if (EditableNpcRollen != null)
                {
                    foreach (var r in EditableNpcRollen)
                    {
                        if (string.IsNullOrWhiteSpace(r.Rol))
                        {
                            continue;
                        }

                        string roleText;
                        if (r.MaximumAantal.HasValue)
                        {
                            if (r.MaximumAantal.Value == r.MinimumAantal)
                            {
                                roleText = $"{r.MinimumAantal}x {r.Rol}";
                            }
                            else
                            {
                                roleText = $"{r.MinimumAantal}-{r.MaximumAantal} {r.Rol}";
                            }
                        }
                        else
                        {
                            roleText = $"{r.MinimumAantal}x {r.Rol}";
                        }

                        npcParts.Add(roleText);
                    }
                }
                var npcSummary = npcParts.Count > 0 ? string.Join(", ", npcParts) : string.Empty;

                var encounterParts = new List<string>();
                if (EditableEncounterTypesMajor != null && EditableEncounterTypesMajor.Any())
                {
                    encounterParts.Add("Hoofd: " + string.Join(", ", EditableEncounterTypesMajor));
                }

                if (EditableEncounterTypesMinor != null && EditableEncounterTypesMinor.Any())
                {
                    encounterParts.Add("Mogelijk: " + string.Join(", ", EditableEncounterTypesMinor));
                }

                if (EditableLocatieEncounter != default)
                {
                    // Normalize through the domain exactly like BuildPaginaData does: the picker keeps
                    // its value when the user switches away from Questveld, so reading the posted id
                    // straight would staple a leftover questlocatie onto another encounter type.
                    var locatieData = LocatieEncounterData.Create(
                        EditableLocatieEncounter,
                        EditableLocatieEncounterSpecificatie,
                        ResolveEditableQuestLocatieId());

                    // Prefer the picked questlocatie's display name; fall back to the free-text field.
                    var locatieDetail = locatieData.HasQuestLocatieKeuze
                        ? AvailableQuestLocatieConfigs.GetDisplayName(locatieData.LocatieId)
                        : locatieData.Specificatie;
                    encounterParts.Add($"Locatie: {EditableLocatieEncounter}" + (string.IsNullOrWhiteSpace(locatieDetail) ? string.Empty : $" ({locatieDetail})"));
                }
                var encounterSummary = encounterParts.Count > 0 ? string.Join("; ", encounterParts) : string.Empty;

                var propsList = (EditableProps ?? []).Where(p => !string.IsNullOrWhiteSpace(p.Omschrijving)).Select(p => p.Omschrijving!.Trim()).ToList();
                var lootList = (EditableLoot ?? []).Where(l => !string.IsNullOrWhiteSpace(l.Omschrijving)).Select(l => l.Omschrijving!.Trim()).ToList();
                var propsAndLoot = string.Join("; ", new[] { propsList.Any() ? "Props: " + string.Join(", ", propsList) : string.Empty, lootList.Any() ? "Loot: " + string.Join(", ", lootList) : string.Empty }.Where(s => !string.IsNullOrEmpty(s)));

                var techniekParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(EditableInstructiePentagon))
                {
                    techniekParts.Add(EditableInstructiePentagon!.Trim());
                }

                if (EditableMuziekVanuitTechniek)
                {
                    techniekParts.Add(Terminologie.Huidig.MuziekVanuitTechniek);
                }

                if (EditableLichtOfRookVanuitTechniek)
                {
                    techniekParts.Add(Terminologie.Huidig.LichtOfRookVanuitTechniek);
                }
                var techniekInstructions = techniekParts.Count > 0 ? string.Join("; ", techniekParts) : string.Empty;

                var extraInformation = EditableMeerInformatie ?? string.Empty;
                var debrief = EditableDebriefGewenst ? string.Join("; ", EditableDebriefVragen ?? []) : string.Empty;

                GeneratedElevatorPitchPreview = await geminiSummaryService.GenereerElevatorPitchAsync(
                    EditableTitle,
                    EditableSamenvattingPlot,
                    npcSummary,
                    encounterSummary,
                    propsAndLoot,
                    techniekInstructions,
                    extraInformation,
                    debrief,
                    ct);
            }
        }
        catch (OperationCanceledException ex)
        {
            // Timeout or explicit cancellation
            logger.LogWarning(ex, "Elevator pitch generation cancelled/timeout");
            GeneratedElevatorPitchPreview = string.Empty;
            ElevatorPitchError = "Het genereren van de elevator pitch is geannuleerd of nam te veel tijd in beslag. Probeer het opnieuw.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate elevator pitch via Gemini");
            GeneratedElevatorPitchPreview = string.Empty;
            ElevatorPitchError = "Het genereren van de elevator pitch is mislukt. Probeer het opnieuw.";
        }

        // Ensure UI state is present
        await LoadAvailablePlotlijnen();
        await LoadPlotboekSettings(initializeEditableFields: false);
        if (PlotboekSettings == null)
        {
            LoadAvailableStarttijden();
        }
        else
        {
            AvailableStarttijden = GetAvailableStarttijden(
                EditableDay,
                PlotboekSettings.StartDag,
                PlotboekSettings.EindDag,
                AvailableStarttijdConfigs,
                EditableStarttijd);
        }

        // Keep posted editable fields intact; do not reinitialize from persisted pagina data.

        // Do not modify editable or hidden fields yet; wait for user confirmation
        return Page();
    }

    private async Task<IActionResult> HandleVervangElevatorPitch()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        if (!PaginaId.HasValue)
        {
            return Page();
        }

        // Re-populate UI state and return the page; do NOT persist to the domain yet.
        await LoadPlotboekPaginas();
        await LoadAvailablePlotlijnen();
        await LoadPlotboekSettings(initializeEditableFields: false);
        LoadSelectedPlotboekPagina();
        LoadAvailableStarttijden();
        InitializePaginaEditableFields();

        // After initializing editable fields from the selected pagina, overwrite the elevator pitch
        // with the generated value so the user sees it in the form and can press Save to persist.
        EditableElevatorPitch = GeneratedElevatorPitchHidden ?? string.Empty;
        // Clear the preview after confirming replacement
        GeneratedElevatorPitchPreview = string.Empty;

        return Page();
    }

    private async Task<IActionResult> HandleNewPagina()
    {
        if (!CanEditCurrentPlotboek())
        {
            return ReadOnlyMutationResult();
        }

        if (PlotboekId.HasValue)
        {
            var titel = string.IsNullOrWhiteSpace(NewPaginaTitel) ? "Nieuwe Pagina" : NewPaginaTitel.Trim();
            var result = await CreateNewPlotboekPagina(Jaar, EventId, titel);
            if (!result.IsValid)
            {
                logger.LogError("Failed to create new plotboek pagina for {Jaar}-{EventId} {Errors}", Jaar, EventId, string.Join(", ", result.Messages.Select(m => m.Message)));
                return Page();
            }
            var newPaginaId = result.Value;
            // Redirect to the new pagina with GET parameters
            return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId = newPaginaId });
        }

        return Page();
    }

    private async Task<IActionResult> HandleDuplicatePagina()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        if (!PaginaId.HasValue || !PlotboekId.HasValue)
        {
            return Page();
        }

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            logger.LogWarning("No user found in session while trying to duplicate plotboek pagina {PaginaId}", PaginaId);
            return Page();
        }

        if (await paginaRepository.LoadAsync(PaginaId) is not { } sourcePagina || !sourcePagina.IsValid)
        {
            logger.LogWarning("Plotboek pagina {PaginaId} not found for duplication", PaginaId);
            return Page();
        }

        var plotboek = await session.FirstOrDefaultAsync<Plotboek.ExportData>(p => p.Id == PlotboekId.ToString());
        if (plotboek == null)
        {
            logger.LogWarning("Plotboek {PlotboekId} not found while duplicating pagina {PaginaId}", PlotboekId, PaginaId);
            return Page();
        }

        var source = sourcePagina.Value.GetData();

        // The copy lands at the end of the same block (same day/starttijd), just after the original;
        // the user drags it to another day/time afterwards.
        var existingPages = await session.QueryAsync<PlotboekPagina.ExportData>(p => p.PlotboekIdQueryable == PlotboekId.ToString() && !p.IsVerwijderd);
        var nextOrder = existingPages.Count > 0 ? existingPages.Max(p => p.Order) + 1 : 1;

        var verantwoordelijkeSlName = AvailableSls.FirstOrDefault(s => s.Id == source.VerantwoordelijkeSl)?.DisplayName;

        var copy = new PlotboekPagina();
        copy = copy.AanPlotboekToevoegen(PlotboekId, plotboek.DisplayName, user).Value;

        var duplicated = copy.Aanpassen(
            new PlotboekPagina.Data
            {
                Titel = $"{source.Titel} (kopie)",
                DomainId = source.DomainId,
                DomainShorthand = source.DomainShorthand,
                DomainDescription = source.DomainDescription,
                Dag = source.Dag,
                // Copy the source values verbatim; the copy lands in the same block as the original.
                StarttijdId = source.StarttijdId,
                Order = nextOrder,
                VerantwoordelijkeSlId = source.VerantwoordelijkeSl,
                VerantwoordelijkeSlName = verantwoordelijkeSlName,
                ExtraSlIds = [.. source.ExtraSlIds ?? []],
                ExtraSlNames = [.. (source.ExtraSlIds ?? []).Select(id => AvailableSls.FirstOrDefault(s => s.Id == id)?.DisplayName ?? id)],
                AantalBlokkenId = source.AantalBlokkenId,
                EncounterTypeIds = source.EncounterTypeIds,
                LocatieEncounter = source.LocatieEncounter,
                AantalNpcs = source.AantalNpcs,
                ElevatorPitch = source.ElevatorPitch,
                SamenvattingPlot = source.SamenvattingPlot,
                MeerInformatie = source.MeerInformatie,
                // The copy references the same shared bijlagen as the source (it lands in the same plotboek).
                BijlageIds = [.. source.BijlageIds ?? []],
                // A copy is a standalone encounter: it is not auto-added to the source's plotlijn.
                PlotlijnId = PlotlijnId.Empty,
                // Reset completion so the user can fill in the "minimale verschillen" before marking it done.
                IsVolledigKlaar = false,
                IsGedraaid = false,
                IsSpelerGedreven = source.IsSpelerGedreven,
                IsFlexibel = source.IsFlexibel,
                IsFlexibelNachtPlot = source.IsFlexibelNachtPlot,
                InstructiePentagon = source.InstructiePentagon,
                Iconen = source.Iconen,
                PropsEnLoot = source.PropsEnLoot,
                Opmerkingen = source.Opmerkingen,
                Debrief = source.Debrief,
            }, user);

        if (!duplicated.IsValid)
        {
            logger.LogWarning("Failed to build duplicate of plotboek pagina {PaginaId}: {Errors}", PaginaId, string.Join(", ", duplicated.Messages.Select(m => m.Message)));
            return Page();
        }

        var saveResult = await paginaRepository.SaveAsync(duplicated.Value, CancellationToken.None);
        if (!saveResult.IsValid)
        {
            logger.LogWarning("Failed to persist duplicate of plotboek pagina {PaginaId}", PaginaId);
            ConcurrencyError = string.Join(" ", saveResult.Messages.Select(m => m.Message));
            return Page();
        }

        session.Store(duplicated.Value.GetData());
        await session.SaveChangesAsync(CancellationToken.None);

        var newPaginaId = duplicated.Value.Id;
        logger.LogInformation("User {User} duplicated plotboek pagina {SourcePaginaId} to {NewPaginaId}", user.Id, PaginaId, newPaginaId);

        return RedirectToPage("/Plotboek", new { Jaar, Event = EventId, PaginaId = newPaginaId });
    }

    private async Task<IActionResult> HandleDeletePagina()
    {
        if (!CanEditCurrentPagina())
        {
            return ReadOnlyMutationResult();
        }

        if (!PaginaId.HasValue)
        {
            return Page();
        }

        using var session = documentStore.OpenSession();
        if (await User.GetUserAsync(session) is not PlotboekUser user)
        {
            logger.LogWarning("No user found in session while trying to delete plotboek pagina {PaginaId}", PaginaId);
            return Page();
        }

        if (await paginaRepository.LoadAsync(PaginaId) is not { } plotboekPagina || !plotboekPagina.IsValid)
        {
            logger.LogWarning("Plotboek pagina {PaginaId} not found for deletion", PaginaId);
            return Page();
        }

        plotboekPagina = plotboekPagina.Value.SetVerwijderd(true, user);
        if (!plotboekPagina.IsValid)
        {
            logger.LogWarning("Failed to mark plotboek pagina {PaginaId} as deleted: {Errors}", PaginaId, string.Join(", ", plotboekPagina.Messages.Select(m => m.Message)));
            return Page();
        }

        var result = await paginaRepository.SaveAsync(plotboekPagina.Value, CancellationToken.None);
        if (!result.IsValid)
        {
            logger.LogWarning("Concurrency conflict while deleting plotboek pagina {PaginaId}", PaginaId);
            ConcurrencyError = string.Join(" ", result.Messages.Select(m => m.Message));
            return Page();
        }

        session.Store(plotboekPagina.Value.GetData());
        await session.SaveChangesAsync(CancellationToken.None);

        logger.LogInformation("User {User} soft-deleted plotboek pagina {PaginaId}", user.Id, PaginaId);

        return RedirectToPage("/Plotboek", new { Jaar, Event = EventId });
    }

    private async Task<IActionResult> HandleExportDocx()
    {
        if (!PlotboekId.HasValue)
        {
            return Page();
        }

        const string? contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        var downloadFileName = $"Plotboek-{Jaar}-{EventId}.docx";

        try
        {
            var result = await exportPlotboekHandler.HandleAsync(new ExportPlotboekCommand(PlotboekId), HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                logger.LogWarning("Failed to export DOCX for {Jaar}-{EventId}: {Errors}", Jaar, EventId, string.Join(", ", result.Messages.Select(message => message.Message)));
                return Page();
            }

            return File(result.Value, contentType, downloadFileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to export DOCX for {Jaar}-{EventId}", Jaar, EventId);
            return Page();
        }
    }

    // Google Drive upload handler moved to Plotboek.GoogleDrive.cs
}
