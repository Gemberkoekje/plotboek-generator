// Form state and orchestration
var initialFormState = typeof initialFormState !== 'undefined' ? initialFormState : null;
var initialSettingsFormState = typeof initialSettingsFormState !== 'undefined' ? initialSettingsFormState : null;
var isFormSubmitting = typeof isFormSubmitting !== 'undefined' ? isFormSubmitting : false;
var __bypassBeforeUnload = typeof __bypassBeforeUnload !== 'undefined' ? __bypassBeforeUnload : false;

// var, not const: this file shares global scope with the other page scripts and follows their
// re-declaration-safe idiom.
var SETTINGS_FORM_ID = typeof SETTINGS_FORM_ID !== 'undefined' ? SETTINGS_FORM_ID : 'plotboek-settings-form';

function captureFormState(form) {
    const data = new FormData(form);
    // Include unchecked checkboxes as "off" so toggling them is detected
    form.querySelectorAll('input[type="checkbox"]').forEach(cb => {
        if (!cb.checked) {
            data.set(cb.name, 'off');
        }
    });
    const entries = [];
    for (const [key, value] of data.entries()) {
        // A File stringifies to "[object File]" whatever it holds, which would hide a picked upload.
        entries.push(key + '=' + (value instanceof File ? value.name + ':' + value.size : value));
    }
    return entries.sort().join('&');
}

function hasUnsavedChanges() {
    const form = document.getElementById('pagina-details-form');
    if (!form || initialFormState === null) return false;
    // Temporarily re-enable disabled fields so their values are captured
    const fieldset = document.getElementById('pagina-fields');
    const wasDisabled = fieldset?.hasAttribute('disabled');
    if (wasDisabled) fieldset.removeAttribute('disabled');
    const eventBreed = document.getElementById('editable-event-breed');
    const eventBreedWasDisabled = eventBreed?.hasAttribute('disabled');
    if (eventBreedWasDisabled) eventBreed.removeAttribute('disabled');
    const dagSelect = document.getElementById('editable-day');
    const dagWasDisabled = dagSelect?.hasAttribute('disabled');
    if (dagWasDisabled) dagSelect.removeAttribute('disabled');
    const starttijdSelect = document.getElementById('editable-starttijd');
    const starttijdWasDisabled = starttijdSelect?.hasAttribute('disabled');
    if (starttijdWasDisabled) starttijdSelect.removeAttribute('disabled');

    const currentState = captureFormState(form);

    if (wasDisabled) fieldset.setAttribute('disabled', '');
    if (eventBreedWasDisabled) eventBreed.setAttribute('disabled', '');
    if (dagWasDisabled) dagSelect.setAttribute('disabled', '');
    if (starttijdWasDisabled) starttijdSelect.setAttribute('disabled', '');

    return currentState !== initialFormState;
}

// The plotboek settings form (titel, samenvatting, NPC-aantallen, bijlagen) is a second editable form
// on the same page. It needs no re-enabling dance: its only disabled state is the read-only fieldset,
// which is disabled at snapshot time and at diff time alike, so it drops out of both.
//
// Bijlage content lives in Quill editors, which contribute to FormData only through their hidden
// inputs — _PlotboekBijlagen.cshtml syncs those on every user edit precisely so this diff sees them.
function hasUnsavedSettingsChanges() {
    const form = document.getElementById(SETTINGS_FORM_ID);
    if (!form || initialSettingsFormState === null) return false;

    return captureFormState(form) !== initialSettingsFormState;
}

// A Quill editor writes into its own DOM only; the hidden input the form actually posts is refreshed
// on submit, which is too late for a diff that has to answer "are there unsaved changes?". Mirror
// every already-mounted settings editor into its hidden input on each user edit. Bijlage editors
// mount lazily on expand and wire the same sync themselves in _PlotboekBijlagen.cshtml.
function wireSettingsQuillSync(form) {
    if (!window.Quill || typeof Quill.find !== 'function') return;

    form.querySelectorAll('[data-target]').forEach(editorEl => {
        const hidden = document.getElementById(editorEl.dataset.target);
        const quill = Quill.find(editorEl);
        if (!hidden || !quill || editorEl.dataset.unsavedWired === 'true') return;

        editorEl.dataset.unsavedWired = 'true';
        const getHtml = () => typeof getSanitizedQuillSemanticHtml === 'function'
            ? getSanitizedQuillSemanticHtml(quill)
            : quill.root.innerHTML;

        // Only 'user' edits: the editors normalize their content through the API on init, and posting
        // that re-serialization would register as a change on every unrelated settings save.
        const initialHtml = getHtml();
        quill.on('text-change', (delta, oldDelta, source) => {
            if (source !== 'user') return;
            const html = getHtml();
            if (html !== initialHtml) {
                hidden.value = html;
            }
        });
    });
}

window.addEventListener('beforeunload', function(e) {
    if (isFormSubmitting) return;
    if (__bypassBeforeUnload) return;
    if (hasUnsavedChanges() || hasUnsavedSettingsChanges()) {
        // Customize the message shown in some browsers via returnValue
        var message = 'Je hebt nog niet opgeslagen wijzigingen. Weet je zeker dat je deze pagina wilt verlaten?';
        (e || window.event).returnValue = message;
        return message;
    }
});

// Quill instances inside the pagina-fields fieldset, populated during initialization
var paginaQuillInstances = [];

function toggleVolledigKlaar() {
    const checkbox = document.getElementById('editable-is-volledig-klaar');
    const fieldset = document.getElementById('pagina-fields');
    if (!checkbox || !fieldset) return;

    if (checkbox.checked) {
        fieldset.setAttribute('disabled', '');
    } else {
        fieldset.removeAttribute('disabled');
    }

    // Quill editors use contenteditable divs that are not affected by fieldset disabled
    paginaQuillInstances.forEach(q => q.enable(!checkbox.checked));

    updateVolledigKlaarHerinnering(checkbox.checked);
}

// The "alleen het vinkje nog" reminder is rendered server-side; once the switch is ticked (even before
// saving) it has done its job, so hide it and drop the yellow block colour. Unticking brings both back.
function updateVolledigKlaarHerinnering(isChecked) {
    const herinnering = document.getElementById('volledig-klaar-herinnering');
    if (!herinnering) return;

    herinnering.classList.toggle('d-none', isChecked);
    const blok = herinnering.closest('.volledig-klaar-blok');
    if (blok) {
        ['bg-warning', 'bg-opacity-10', 'border-warning'].forEach(c => blok.classList.toggle(c, !isChecked));
        blok.classList.toggle('bg-body-tertiary', isChecked);
    }
}

function initializeVolledigKlaar() {
    const checkbox = document.getElementById('editable-is-volledig-klaar');
    if (!checkbox) return;

    toggleVolledigKlaar();
}

function toggleSpelerGedreven() {
    const checkbox = document.getElementById('editable-is-speler-gedreven');
    const dagSelect = document.getElementById('editable-day');
    const starttijdSelect = document.getElementById('editable-starttijd');
    if (!checkbox) return;

    if (checkbox.checked) {
        if (dagSelect) {
            dagSelect.value = 'None';
            dagSelect.setAttribute('disabled', '');
        }
        if (starttijdSelect) {
            starttijdSelect.value = 'None';
            starttijdSelect.setAttribute('disabled', '');
        }
    } else {
        if (dagSelect) dagSelect.removeAttribute('disabled');
        if (starttijdSelect) starttijdSelect.removeAttribute('disabled');
    }
}

function toggleDagFields() {
    const startDatum = document.getElementById('EditablePlotboekStartDatumString');
    const eindDatum = document.getElementById('EditablePlotboekEindDatumString');
    const startDag = document.getElementById('EditablePlotboekStartDag');
    const eindDag = document.getElementById('EditablePlotboekEindDag');

    if (startDatum && eindDatum && startDag && eindDag) {
        const hasDatums = startDatum.value && eindDatum.value;
        startDag.disabled = hasDatums;
        eindDag.disabled = hasDatums;
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    toggleDagFields();
    updateEncounterPreview();
    updateLocatieEncounterPreview();
    toggleAlleNpcsMode();
    updateEventBreedState();
    updatePlotlijnOptions();
    initializeVolledigKlaar();

    // Re-enable the fieldset before form submission so disabled fields are included
    const form = document.getElementById('pagina-details-form');
    if (form) {
        form.addEventListener('submit', function() {
            isFormSubmitting = true;
            const fieldset = document.getElementById('pagina-fields');
            if (fieldset) {
                fieldset.removeAttribute('disabled');
            }
            // Re-enable individually disabled fields
            const eventBreed = document.getElementById('editable-event-breed');
            if (eventBreed) {
                eventBreed.removeAttribute('disabled');
            }
            const dagSelect = document.getElementById('editable-day');
            if (dagSelect) dagSelect.removeAttribute('disabled');
            const starttijdSelect = document.getElementById('editable-starttijd');
            if (starttijdSelect) starttijdSelect.removeAttribute('disabled');
        });

        // Capture initial state after all initialization is complete
        const fieldset = document.getElementById('pagina-fields');
        const wasDisabled = fieldset?.hasAttribute('disabled');
        if (wasDisabled) fieldset.removeAttribute('disabled');
        const eventBreed = document.getElementById('editable-event-breed');
        const eventBreedWasDisabled = eventBreed?.hasAttribute('disabled');
        if (eventBreedWasDisabled) eventBreed.removeAttribute('disabled');
        const dagSelect = document.getElementById('editable-day');
        const dagWasDisabled = dagSelect?.hasAttribute('disabled');
        if (dagWasDisabled) dagSelect.removeAttribute('disabled');
        const starttijdSelect = document.getElementById('editable-starttijd');
        const starttijdWasDisabled = starttijdSelect?.hasAttribute('disabled');
        if (starttijdWasDisabled) starttijdSelect.removeAttribute('disabled');

        initialFormState = captureFormState(form);

        if (wasDisabled) fieldset.setAttribute('disabled', '');
        if (eventBreedWasDisabled) eventBreed.setAttribute('disabled', '');
        if (dagWasDisabled) dagSelect.setAttribute('disabled', '');
        if (starttijdWasDisabled) starttijdSelect.setAttribute('disabled', '');
    }

    const settingsForm = document.getElementById(SETTINGS_FORM_ID);
    if (settingsForm) {
        settingsForm.addEventListener('submit', function() {
            isFormSubmitting = true;
        });

        // Wire the sync before snapshotting: the sync writes nothing by itself, so the snapshot below
        // still records the values the server rendered.
        wireSettingsQuillSync(settingsForm);
        initialSettingsFormState = captureFormState(settingsForm);
    }
});
