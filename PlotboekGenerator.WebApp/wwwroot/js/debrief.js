// Debrief Management
let debriefVraagIndex = 0;

function initializeDebriefVraagIndex(count) {
    debriefVraagIndex = count;
}

function toggleDebriefVragen() {
    const checkbox = document.getElementById('editable-debrief-gewenst');
    const section = document.getElementById('debrief-vragen-section');
    if (!checkbox || !section) return;

    section.style.display = checkbox.checked ? 'block' : 'none';
}

function addDebriefVraag() {
    const container = document.getElementById('debrief-vragen-container');
    if (!container) return;

    const i = debriefVraagIndex++;

    const itemHtml = `
        <div class="debrief-vraag-item d-flex align-items-center mb-2" data-index="${i}">
            <input type="text"
                   name="EditableDebriefVragen[${i}]"
                   class="form-control form-control-sm me-2"
                   placeholder="Wat wil je weten?">
            <button type="button" class="btn btn-outline-danger btn-sm" onclick="removeDebriefVraag(this)">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', itemHtml);
}

function removeDebriefVraag(button) {
    const item = button.closest('.debrief-vraag-item');
    if (item) {
        item.remove();
    }
}
