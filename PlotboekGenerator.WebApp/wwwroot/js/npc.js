// NPC Role Management
var npcRolIndex = typeof npcRolIndex !== 'undefined' ? npcRolIndex : 0;

function initializeNpcRolIndex(count) {
    npcRolIndex = count;
}

// Escapes a value for a double-quoted attribute in the role template below; a quote in a costume
// description would otherwise end the attribute early.
function escapeNpcAttribuut(value) {
    return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

// Adds an empty role row, or one filled in from a standaard-NPC when `prefill` is given
// ({ rol, kledingEnSchmink, castingInstructie }). The count always starts at its default: it differs per plot.
function addNpcRol(prefill) {
    const container = document.getElementById('npc-rollen-container');
    const i = npcRolIndex++;
    const isAlleNpcs = document.getElementById('editable-alle-npcs')?.checked || false;
    const defaultMin = isAlleNpcs ? '0' : '1';
    const rol = escapeNpcAttribuut(prefill?.rol);
    const kleding = escapeNpcAttribuut(prefill?.kledingEnSchmink);
    const casting = escapeNpcAttribuut(prefill?.castingInstructie);

    const rolHtml = `
        <div class="npc-rol-item border rounded p-3 mb-2" data-index="${i}">
            <div class="row g-2 mb-2">
                <div class="col-md-6">
                    <label class="form-label small text-muted">Rol/Type NPC</label>
                    <input type="text"
                           name="EditableNpcRollen[${i}].Rol"
                           value="${rol}"
                           class="form-control form-control-sm">
                </div>
                <div class="col-md-3">
                    <label class="form-label small text-muted npc-count-label">Minimum</label>
                    <input type="number"
                           name="EditableNpcRollen[${i}].MinimumAantal"
                           value="${defaultMin}"
                           class="form-control form-control-sm npc-min-input"
                           min="0"
                           placeholder="Min">
                </div>
                <div class="col-md-3">
                    <label class="form-label small text-muted npc-count-label">Maximum</label>
                    <input type="number"
                           name="EditableNpcRollen[${i}].MaximumAantal"
                           class="form-control form-control-sm npc-max-input"
                           min="0"
                           placeholder="Max (optioneel)">
                </div>
            </div>
            <div class="row g-2 mb-2">
                <div class="col-md-12">
                    <label class="form-label small text-muted">Kleding & Schmink</label>
                    <input type="text"
                           name="EditableNpcRollen[${i}].KledingEnSchmink"
                           value="${kleding}"
                           class="form-control form-control-sm">
                </div>
            </div>
            <div class="row g-2 mb-2">
                <div class="col-md-12">
                    <label class="form-label small text-muted">Casting &mdash; wat voor figurant zoeken we?</label>
                    <input type="text"
                           name="EditableNpcRollen[${i}].CastingInstructie"
                           value="${casting}"
                           class="form-control form-control-sm"
                           placeholder="bv. vrouwelijk uiterlijk, goede roleplayer, stevig postuur">
                </div>
            </div>
            <div class="row g-2 mb-2">
                <div class="col-md-12">
                    <div class="form-check">
                        <input class="form-check-input npc-afstemming-check"
                               type="checkbox"
                               name="EditableNpcRollen[${i}].VereistAfstemming"
                               value="true"
                               id="npc-afstemming-${i}"
                               data-target="npc-afstemming-toelichting-${i}"
                               onchange="toggleNpcTextField(this)">
                        <label class="form-check-label small" for="npc-afstemming-${i}">
                            &#x1F91D; Vereist afstemming vooraf (bv. marteling, intimiteit, heftige angstinhoud)
                        </label>
                        <small class="text-muted d-block">De MSL stemt dit vooraf af met de figurant.</small>
                    </div>
                    <input type="text"
                           name="EditableNpcRollen[${i}].AfstemmingToelichting"
                           id="npc-afstemming-toelichting-${i}"
                           class="form-control form-control-sm mt-1"
                           placeholder="Waarover moet afgestemd worden?"
                           style="display: none;">
                </div>
            </div>
            <div class="row g-2 mb-2">
                <div class="col-md-4">
                    <div class="form-check">
                        <input class="form-check-input"
                               type="checkbox"
                               name="EditableNpcRollen[${i}].IsTerukerend"
                               value="true"
                               id="npc-terugkerend-${i}">
                        <label class="form-check-label small" for="npc-terugkerend-${i}">
                            Terugkerend na dit plot
                        </label>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="form-check">
                        <input class="form-check-input npc-eerder-gespeeld-check"
                               type="checkbox"
                               name="EditableNpcRollen[${i}].IsEerderGespeeld"
                               value="true"
                               id="npc-eerder-${i}"
                               data-target="npc-eerder-door-${i}"
                               onchange="toggleNpcTextField(this, 'npc-eerder-door-${i}')">
                        <label class="form-check-label small" for="npc-eerder-${i}">
                            Eerder gespeeld
                        </label>
                    </div>
                    <input type="text"
                           name="EditableNpcRollen[${i}].GespeeldDoor"
                           id="npc-eerder-door-${i}"
                           class="form-control form-control-sm mt-1"
                           placeholder="Door wie?"
                           style="display: none;">
                </div>
                <div class="col-md-4">
                    <div class="form-check">
                        <input class="form-check-input npc-gekozen-check"
                               type="checkbox"
                               name="EditableNpcRollen[${i}].IsAlGekozen"
                               value="true"
                               id="npc-gekozen-${i}"
                               data-target="npc-gekozen-persoon-${i}"
                               data-warning="npc-gekozen-warning-${i}"
                               onchange="toggleNpcGekozen(this)">
                        <label class="form-check-label small" for="npc-gekozen-${i}">
                            Al gekozen/gebriefd
                        </label>
                    </div>
                    <input type="text"
                           name="EditableNpcRollen[${i}].GekozenPersoon"
                           id="npc-gekozen-persoon-${i}"
                           class="form-control form-control-sm mt-1"
                           placeholder="Wie?"
                           style="display: none;">
                </div>
            </div>
            <div id="npc-gekozen-warning-${i}"
                 class="alert alert-warning py-2 small mb-2"
                 style="display: none;">
                <i class="fas fa-triangle-exclamation me-1"></i><strong>LET OP:</strong> De MSL kiezen de NPC's. Gebruik deze optie alleen voor rollen waarbij een hele lange briefing nodig is die niet op het event zelf gegeven kan worden.
            </div>
            <button type="button" class="btn btn-outline-danger btn-sm" onclick="removeNpcRol(this)">
                <i class="fas fa-trash me-1"></i>Verwijderen
            </button>
            <button type="button" class="btn btn-outline-secondary btn-sm ms-1"
                    title="Slaat het plot op en bewaart deze rol (naam, kleding &amp; schmink, casting) als standaard-NPC."
                    onclick="bewaarNpcRolAlsStandaard(this)">
                <i class="fas fa-bookmark me-1"></i>Bewaar als standaard
            </button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', rolHtml);

    // Apply the "Alle NPCs" styling if needed
    if (isAlleNpcs) {
        toggleAlleNpcsMode();
    }
}

function removeNpcRol(button) {
    const rolItem = button.closest('.npc-rol-item');
    if (rolItem) {
        rolItem.remove();
    }
}

// onchange of the "Standaard-NPC toevoegen" dropdown: adds a role filled in from the chosen standaard-NPC.
// It is a copy; the plot never points back to the standaard-NPC. The dropdown goes back to its
// placeholder so the same one can be picked again.
function addStandaardNpcRol(select) {
    const option = select.selectedOptions[0];
    if (!option || !option.value) return;

    addNpcRol({
        rol: option.dataset.rol,
        kledingEnSchmink: option.dataset.kleding,
        castingInstructie: option.dataset.casting,
    });
    select.value = '';
}

// The same key StandaardNpc.IdVoor derives on the server (lower-cased, diacritics and apostrophes dropped,
// every word capitalised and glued), so the browser can tell whether saving would overwrite one.
function standaardNpcId(rol) {
    const id = (rol || '').trim().toLowerCase()
        .normalize('NFD').replace(/\p{Mn}/gu, '').normalize('NFC')
        .replace(/['’]/g, '')
        .split(/[^\p{L}\p{N}]+/u)
        .filter(woord => woord.length > 0)
        .map(woord => woord.charAt(0).toUpperCase() + woord.slice(1))
        .join('');
    return /^\p{Nd}/u.test(id) ? 'N' + id : id;
}

// "Bewaar als standaard" on a role row: saves the plot and keeps this role as a standaard-NPC. Rides on
// the regular pagina form (Action = bewaarStandaardNpc) so no unsaved edit is lost. The values go in
// hidden fields rather than as a row index: indexes have gaps once roles are removed.
function bewaarNpcRolAlsStandaard(button) {
    const rij = button.closest('.npc-rol-item');
    const form = document.getElementById('pagina-details-form');
    if (!rij || !form) return;

    const waarde = veld => rij.querySelector(`[name$=".${veld}"]`)?.value?.trim() || '';
    const rol = waarde('Rol');
    const id = standaardNpcId(rol);
    if (!id) {
        alert('Vul eerst een naam voor de rol in.');
        return;
    }

    const bestaand = Array.from(document.querySelectorAll('#standaard-npc-select option'))
        .find(option => option.value === id);
    if (bestaand && !confirm(`Er bestaat al een standaard-NPC '${bestaand.dataset.rol}'. Overschrijven?`)) {
        return;
    }

    form.querySelector('input[name="StandaardNpcRol"]').value = rol;
    form.querySelector('input[name="StandaardNpcKledingEnSchmink"]').value = waarde('KledingEnSchmink');
    form.querySelector('input[name="StandaardNpcCastingInstructie"]').value = waarde('CastingInstructie');
    form.querySelector('input[name="Action"]').value = 'bewaarStandaardNpc';

    // requestSubmit fires the submit listeners, which sync the Quill editors, re-enable disabled fields
    // and switch off the unsaved-changes warning, exactly as the regular Save button does.
    if (typeof form.requestSubmit === 'function') {
        form.requestSubmit();
    } else {
        form.submit();
    }
}

function toggleNpcTextField(checkbox, targetId) {
    const textField = document.getElementById(targetId || checkbox.dataset.target);
    if (textField) {
        textField.style.display = checkbox.checked ? 'block' : 'none';
        if (!checkbox.checked) {
            textField.value = '';
        }
    }
}

// Shows/hides the "the MSL pick the NPCs" warning that belongs to an
// "Al gekozen/gebriefd" checkbox. Purely informational: it never blocks saving.
function toggleNpcGekozenWarning(checkbox) {
    const warning = document.getElementById(checkbox.dataset.warning);
    if (warning) {
        warning.style.display = checkbox.checked ? 'block' : 'none';
    }
}

// onchange handler for "Al gekozen/gebriefd": toggles both the "Wie?" field and
// the warning. Both targets come from data-* attributes on the checkbox.
function toggleNpcGekozen(checkbox) {
    toggleNpcTextField(checkbox, checkbox.dataset.target);
    toggleNpcGekozenWarning(checkbox);
}

// Enables/disables the "ALLE NPCs" checkbox, the role list, the "add role" button and the standaard-NPC
// dropdown depending on whether "Geen NPC's nodig" is active (they are mutually exclusive).
function setGeenNpcsNodigLock(locked) {
    const alleNpcs = document.getElementById('editable-alle-npcs');
    const container = document.getElementById('npc-rollen-container');
    const addBtn = document.querySelector('button[onclick="addNpcRol()"]');
    const standaardSelect = document.getElementById('standaard-npc-select');
    if (alleNpcs) {
        alleNpcs.disabled = locked;
    }
    if (addBtn) {
        addBtn.disabled = locked;
    }
    if (standaardSelect) {
        standaardSelect.disabled = locked;
    }
    if (container) {
        container.style.display = locked ? 'none' : '';
    }
}

function toggleGeenNpcsNodig() {
    const geenNpcs = document.getElementById('editable-geen-npcs');
    const isGeen = geenNpcs?.checked || false;

    if (isGeen) {
        // Mutually exclusive: clear "ALLE NPCs" and remove any roles.
        const alleNpcs = document.getElementById('editable-alle-npcs');
        if (alleNpcs) {
            alleNpcs.checked = false;
        }
        const container = document.getElementById('npc-rollen-container');
        if (container) {
            container.innerHTML = '';
        }
        npcRolIndex = 0;
    }

    setGeenNpcsNodigLock(isGeen);

    if (typeof updateEventBreedState === 'function') {
        updateEventBreedState();
    }
}

function initGeenNpcsNodigState() {
    const geenNpcs = document.getElementById('editable-geen-npcs');
    if (geenNpcs && geenNpcs.checked) {
        setGeenNpcsNodigLock(true);
    }
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initGeenNpcsNodigState);
} else {
    initGeenNpcsNodigState();
}

function toggleAlleNpcsMode() {
    const isAlleNpcs = document.getElementById('editable-alle-npcs')?.checked || false;

    // "ALLE NPCs" and "Geen NPC's nodig" are mutually exclusive.
    if (isAlleNpcs) {
        const geenNpcs = document.getElementById('editable-geen-npcs');
        if (geenNpcs && geenNpcs.checked) {
            geenNpcs.checked = false;
        }
        setGeenNpcsNodigLock(false);
    }

    const container = document.getElementById('npc-rollen-container');
    const countLabels = container?.querySelectorAll('.npc-count-label') || [];
    const minInputs = container?.querySelectorAll('.npc-min-input') || [];
    const maxInputs = container?.querySelectorAll('.npc-max-input') || [];

    updateEventBreedState();

    if (isAlleNpcs) {
        // Add visual indication that counts are optional
        countLabels.forEach(label => {
            if (!label.textContent.includes('(optioneel)')) {
                label.textContent = label.textContent.replace('Minimum', 'Minimum (optioneel)');
                label.textContent = label.textContent.replace('Maximum', 'Maximum (optioneel)');
            }
        });
        minInputs.forEach(input => {
            input.placeholder = 'Optioneel';
        });
        maxInputs.forEach(input => {
            input.placeholder = 'Optioneel';
        });
    } else {
        // Restore normal labels
        countLabels.forEach(label => {
            label.textContent = label.textContent.replace(' (optioneel)', '');
        });
        minInputs.forEach(input => {
            input.placeholder = 'Min';
        });
        maxInputs.forEach(input => {
            input.placeholder = 'Max (optioneel)';
        });
    }
}
