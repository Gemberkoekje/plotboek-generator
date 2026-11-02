// Extra SL Management
let extraSlIndex = 0;

function initializeExtraSlIndex(count) {
    extraSlIndex = count;
}

function addExtraSl() {
    const container = document.getElementById('extra-sl-container');
    if (!container) return;

    const i = extraSlIndex++;

    // Get available SLs from the primary dropdown
    const primarySelect = document.getElementById('editable-verantwoordelijke-sl');
    if (!primarySelect) return;

    let optionsHtml = '<option value="">-- Selecteer een SL --</option>';
    primarySelect.querySelectorAll('option').forEach(opt => {
        if (opt.value) {
            optionsHtml += `<option value="${opt.value}">${opt.textContent}</option>`;
        }
    });

    const itemHtml = `
        <div class="extra-sl-item d-flex align-items-center mb-2" data-index="${i}">
            <select name="EditableExtraSlIds[${i}]"
                    class="form-select form-select-sm me-2">
                ${optionsHtml}
            </select>
            <button type="button" class="btn btn-outline-danger btn-sm" onclick="removeExtraSl(this)">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', itemHtml);
}

function removeExtraSl(button) {
    const item = button.closest('.extra-sl-item');
    if (item) {
        item.remove();
    }
}
