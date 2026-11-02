// What the reader sees for an encounter type: its checkbox's label, not its stored id.
function encounterTypeNaam(checkbox) {
    const label = checkbox.id ? document.querySelector(`label[for="${CSS.escape(checkbox.id)}"]`) : null;
    return label?.textContent.trim() || checkbox.value;
}

function updateEncounterPreview() {
    const majorCheckboxes = document.querySelectorAll('input[name="EditableEncounterTypesMajor"]:checked');
    const minorCheckboxes = document.querySelectorAll('input[name="EditableEncounterTypesMinor"]:checked');
    const preview = document.getElementById('encounter-preview');
    const previewText = document.getElementById('encounter-preview-text');

    if (!preview || !previewText) return;

    const majorValues = Array.from(majorCheckboxes).map(encounterTypeNaam);
    const minorValues = Array.from(minorCheckboxes).map(encounterTypeNaam);

    if (majorValues.length === 0) {
        preview.style.display = 'none';
        return;
    }

    let formatted = majorValues.join(' / ');
    if (minorValues.length > 0) {
        formatted += ' ' + pbT('EncounterMogelijk', minorValues.join(', '));
    }

    previewText.textContent = formatted;
    preview.style.display = 'block';
}

// Value of the "Anders… (vul zelf in)" option; keep in sync with PlotboekModel.QuestLocatieAndersSentinel.
const QUEST_LOCATIE_ANDERS = 'Anders';

function getSelectedQuestLocatieOption() {
    const questSelect = document.getElementById('editable-questlocatie');
    if (!questSelect) return null;

    const option = questSelect.selectedOptions[0];
    if (!option || option.value === QUEST_LOCATIE_ANDERS) return null;

    return option;
}

function updateLocatieEncounterSpecificatie() {
    const locatieSelect = document.getElementById('editable-locatie-encounter');
    const specificatieContainer = document.getElementById('locatie-specificatie-container');
    const specificatieInput = document.getElementById('editable-locatie-specificatie');
    const questContainer = document.getElementById('questlocatie-container');

    if (!locatieSelect || !specificatieContainer) return;

    const selectedValue = locatieSelect.value;
    const isQuestveld = selectedValue === 'Questveld';
    const heeftQuestLocatie = isQuestveld && getSelectedQuestLocatieOption() !== null;

    // Groeperingsveld always types its own field; Questveld only in the "Anders…" branch.
    const requiresSpecificatie = selectedValue === 'Groeperingsveld' || (isQuestveld && !heeftQuestLocatie);

    if (questContainer) {
        questContainer.style.display = isQuestveld ? 'block' : 'none';
    }

    specificatieContainer.style.display = requiresSpecificatie ? 'block' : 'none';

    // Clear specificatie if not needed
    if (!requiresSpecificatie && specificatieInput) {
        specificatieInput.value = '';
    }

    // Update the contextual note under the picker
    updateQuestLocatieNotitie();

    // Update preview
    updateLocatieEncounterPreview();

    // Update the event-wide checkbox state based on new locatie
    updateEventBreedState();
}

function updateQuestLocatieNotitie() {
    const notitie = document.getElementById('questlocatie-notitie');
    const notitieText = document.getElementById('questlocatie-notitie-text');
    const locatieSelect = document.getElementById('editable-locatie-encounter');

    if (!notitie || !notitieText) return;

    const option = locatieSelect?.value === 'Questveld' ? getSelectedQuestLocatieOption() : null;
    const parts = [];
    if (option) {
        if (option.dataset.notitie) parts.push(option.dataset.notitie);
        if (option.dataset.locatieNotitie) parts.push(option.dataset.locatieNotitie);
    }

    if (parts.length === 0) {
        notitie.style.display = 'none';
        return;
    }

    notitie.classList.remove('alert-info', 'alert-warning');
    notitie.classList.add(option.dataset.ernst === 'Afgeraden' ? 'alert-warning' : 'alert-info');
    notitieText.textContent = parts.join(' ');
    notitie.style.display = 'block';
}

function updateLocatieEncounterPreview() {
    const locatieSelect = document.getElementById('editable-locatie-encounter');
    const specificatieInput = document.getElementById('editable-locatie-specificatie');
    const preview = document.getElementById('locatie-encounter-preview');
    const previewText = document.getElementById('locatie-encounter-preview-text');

    if (!locatieSelect || !preview || !previewText) return;

    const selectedValue = locatieSelect.value;
    const specificatieValue = specificatieInput?.value || '';

    if (!selectedValue || selectedValue === 'None') {
        preview.style.display = 'none';
        return;
    }

    // The option's text, not its value: the value is the stored (Dutch) name of the location.
    let formatted = locatieSelect.selectedOptions[0]?.textContent.trim() || selectedValue;
    const questOption = selectedValue === 'Questveld' ? getSelectedQuestLocatieOption() : null;
    if (questOption) {
        formatted += ' (' + questOption.textContent.trim() + ')';
    } else if ((selectedValue === 'Groeperingsveld' || selectedValue === 'Questveld') && specificatieValue) {
        formatted += ' (' + specificatieValue + ')';
    }

    previewText.textContent = formatted;
    preview.style.display = 'block';
}

function updateEventBreedState() {
    const eventBreedCheckbox = document.getElementById('editable-event-breed');
    if (!eventBreedCheckbox) return;

    const locatieSelect = document.getElementById('editable-locatie-encounter');
    const locatieValue = locatieSelect?.value || 'None';
    const isHoofdveld = locatieValue === 'Hoofdveld';
    const isNone = locatieValue === 'None';
    const isAlleNpcs = document.getElementById('editable-alle-npcs')?.checked || false;

    if (!isHoofdveld && !isNone) {
        // Questveld/Groeperingsveld: disabled, off
        eventBreedCheckbox.disabled = true;
        eventBreedCheckbox.checked = false;
    } else if (isAlleNpcs) {
        // Hoofdveld/None + Alle NPCs: forced on, read-only
        eventBreedCheckbox.disabled = true;
        eventBreedCheckbox.checked = true;
    } else {
        // Hoofdveld or None + not Alle NPCs: selectable
        eventBreedCheckbox.disabled = false;
    }
}

function onEventBreedChanged() {
    const eventBreedCheckbox = document.getElementById('editable-event-breed');
    const locatieSelect = document.getElementById('editable-locatie-encounter');
    if (!eventBreedCheckbox || !locatieSelect) return;

    // When checking EventBreed while locatie is not set, auto-assign Hoofdveld
    if (eventBreedCheckbox.checked && locatieSelect.value === 'None') {
        locatieSelect.value = 'Hoofdveld';
        updateLocatieEncounterSpecificatie();
    }
}
