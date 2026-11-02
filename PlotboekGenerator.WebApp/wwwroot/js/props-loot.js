// Props & Loot Management
let propIndex = 0;
let lootIndex = 0;

function initializePropIndex(count) {
    propIndex = count;
}

function initializeLootIndex(count) {
    lootIndex = count;
}

function addPropItem() {
    const container = document.getElementById('props-container');
    if (!container) return;

    const i = propIndex++;

    const primarySelect = document.getElementById('editable-verantwoordelijke-sl');
    let optionsHtml = `<option value="">${escapeTekst(pbT('PropsSlKiezen'))}</option>`;
    if (primarySelect) {
        primarySelect.querySelectorAll('option').forEach(opt => {
            if (opt.value) {
                optionsHtml += `<option value="${opt.value}">${opt.textContent}</option>`;
            }
        });
    }

    const itemHtml = `
        <div class="prop-item d-flex align-items-center mb-2" data-index="${i}">
            <input type="text"
                   name="EditableProps[${i}].Omschrijving"
                   class="form-control form-control-sm me-2"
                   placeholder="${escapeTekst(pbT('PropOmschrijvingPlaceholder'))}">

            <select name="EditableProps[${i}].VerantwoordelijkeSource"
                    class="form-select form-select-sm me-2 prop-source-select" style="max-width: 160px;"
                    onchange="onPropSourceChanged(this)">
                <option value="SL">${escapeTekst(pbT('PropsBronSl'))}</option>
                <option value="SpelerOfNpc">${escapeTekst(pbT('PropsBronSpelerOfNpc'))}</option>
                <option value="Anders">${escapeTekst(pbT('PropsBronAnders'))}</option>
            </select>

            <select name="EditableProps[${i}].VerantwoordelijkeSlId"
                    class="form-select form-select-sm me-2 prop-sl-select" style="max-width: 200px;">
                ${optionsHtml}
            </select>

            <input type="text"
                   name="EditableProps[${i}].VerantwoordelijkeVrijeTekst"
                   class="form-control form-control-sm me-2 prop-vrije-tekst"
                   placeholder="${escapeTekst(pbT('PropsVrijeTekstPlaceholder'))}"
                   style="max-width: 200px; display: none;">

            <button type="button" class="btn btn-outline-danger btn-sm" onclick="removePropItem(this)">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', itemHtml);
}

function removePropItem(button) {
    const item = button.closest('.prop-item');
    if (item) {
        item.remove();
    }
}

function addLootItem() {
    const container = document.getElementById('loot-container');
    if (!container) return;

    const i = lootIndex++;

    const primarySelect = document.getElementById('editable-verantwoordelijke-sl');
    let optionsHtml = `<option value="">${escapeTekst(pbT('PropsSlKiezen'))}</option>`;
    if (primarySelect) {
        primarySelect.querySelectorAll('option').forEach(opt => {
            if (opt.value) {
                optionsHtml += `<option value="${opt.value}">${opt.textContent}</option>`;
            }
        });
    }

    const itemHtml = `
        <div class="loot-item d-flex align-items-center mb-2" data-index="${i}">
            <input type="text"
                   name="EditableLoot[${i}].Omschrijving"
                   class="form-control form-control-sm me-2"
                   placeholder="${escapeTekst(pbT('LootOmschrijvingPlaceholder'))}">

            <select name="EditableLoot[${i}].VerantwoordelijkeSource"
                    class="form-select form-select-sm me-2 loot-source-select" style="max-width: 160px;"
                    onchange="onLootSourceChanged(this)">
                <option value="SL">${escapeTekst(pbT('PropsBronSl'))}</option>
                <option value="SpelerOfNpc">${escapeTekst(pbT('PropsBronSpelerOfNpc'))}</option>
                <option value="Anders">${escapeTekst(pbT('PropsBronAnders'))}</option>
            </select>

            <select name="EditableLoot[${i}].VerantwoordelijkeSlId"
                    class="form-select form-select-sm me-2 loot-sl-select" style="max-width: 200px;">
                ${optionsHtml}
            </select>

            <input type="text"
                   name="EditableLoot[${i}].VerantwoordelijkeVrijeTekst"
                   class="form-control form-control-sm me-2 loot-vrije-tekst"
                   placeholder="${escapeTekst(pbT('PropsVrijeTekstPlaceholder'))}"
                   style="max-width: 200px; display: none;">

            <button type="button" class="btn btn-outline-danger btn-sm" onclick="removeLootItem(this)">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', itemHtml);
}

function removeLootItem(button) {
    const item = button.closest('.loot-item');
    if (item) {
        item.remove();
    }
}

function onPropSourceChanged(select) {
    const container = select.closest('.prop-item');
    if (!container) return;
    const source = select.value;
    const slSelect = container.querySelector('.prop-sl-select');
    const vrijeInput = container.querySelector('.prop-vrije-tekst');
    if (source === 'SL') {
        if (slSelect) slSelect.style.display = 'block';
        if (vrijeInput) vrijeInput.style.display = 'none';
    } else {
        if (slSelect) slSelect.style.display = 'none';
        if (vrijeInput) vrijeInput.style.display = 'block';
    }
}

function onLootSourceChanged(select) {
    const container = select.closest('.loot-item');
    if (!container) return;
    const source = select.value;
    const slSelect = container.querySelector('.loot-sl-select');
    const vrijeInput = container.querySelector('.loot-vrije-tekst');
    if (source === 'SL') {
        if (slSelect) slSelect.style.display = 'block';
        if (vrijeInput) vrijeInput.style.display = 'none';
    } else {
        if (slSelect) slSelect.style.display = 'none';
        if (vrijeInput) vrijeInput.style.display = 'block';
    }
}
