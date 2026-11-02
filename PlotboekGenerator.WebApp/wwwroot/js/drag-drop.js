// Drag and drop variables
let draggedElement = null;
let draggedPaginaId = null;

// Track if we're in a drag operation
let isDragging = false;

function handleDragStart(event) {
    isDragging = true;

    // Get the anchor element (might be the target or its parent)
    const anchor = event.target.closest('a[draggable="true"]') || event.target;

    draggedElement = anchor;
    draggedPaginaId = anchor.dataset.paginaId;
    anchor.classList.add('dragging');
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', draggedPaginaId);

    // Show all dropzones
    document.querySelectorAll('.pagina-dropzone').forEach(zone => {
        zone.classList.add('drop-active');
    });
}

function handleDragEnd(event) {
    const anchor = event.target.closest('a[draggable="true"]') || event.target;
    if (anchor) anchor.classList.remove('dragging');
    draggedElement = null;
    draggedPaginaId = null;

    // Reset drag flag after a short delay to allow click event to check it
    setTimeout(() => { isDragging = false; }, 100);

    // Hide all dropzones
    document.querySelectorAll('.pagina-dropzone').forEach(zone => {
        zone.classList.remove('drop-active', 'drop-hover');
    });
}

// Prevent navigation when dropping (not clicking)
document.addEventListener('click', function(event) {
    const anchor = event.target.closest('a[draggable="true"]');
    if (anchor && isDragging) {
        event.preventDefault();
    }
}, true);

function handleDragOver(event) {
    // Ignore drags that aren't a pagina reorder (e.g. the "Weergave" dropdown rows, which share the
    // native HTML5 drag channel). Without this, any drag would mark dropzones as valid targets.
    if (!draggedElement) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
    event.currentTarget.classList.add('drop-hover');
}

function handleDragLeave(event) {
    event.currentTarget.classList.remove('drop-hover');
}

function handleDrop(event) {
    // Only a pagina drag sets draggedElement; ignore stray drops (e.g. a "Weergave" dropdown row
    // released over a dropzone) so we never submit a reorder with a non-pagina payload.
    if (!draggedElement) return;
    event.preventDefault();
    event.currentTarget.classList.remove('drop-hover');

    const paginaId = event.dataTransfer.getData('text/plain');
    const dropzone = event.currentTarget;
    const newDag = dropzone.dataset.dag;
    const newStarttijd = dropzone.dataset.starttijd;

    // Calculate new order based on drop position
    const items = dropzone.querySelectorAll('[data-pagina-id]');
    let newOrder = 0;

    if (items.length > 0) {
        // Find the position to insert
        const dropY = event.clientY;
        let insertIndex = items.length;

        for (let i = 0; i < items.length; i++) {
            const rect = items[i].getBoundingClientRect();
            const itemMiddle = rect.top + rect.height / 2;

            if (dropY < itemMiddle) {
                insertIndex = i;
                break;
            }
        }

        // Calculate order value
        if (insertIndex === 0) {
            // First position
            const firstOrder = parseInt(items[0].dataset.paginaOrder) || 0;
            newOrder = firstOrder - 10;
        } else if (insertIndex >= items.length) {
            // Last position
            const lastOrder = parseInt(items[items.length - 1].dataset.paginaOrder) || 0;
            newOrder = lastOrder + 10;
        } else {
            // Middle position - average of surrounding items
            const prevOrder = parseInt(items[insertIndex - 1].dataset.paginaOrder) || 0;
            const nextOrder = parseInt(items[insertIndex].dataset.paginaOrder) || 0;
            newOrder = Math.floor((prevOrder + nextOrder) / 2);
        }
    } else {
        newOrder = 0;
    }

    // Submit the reorder form
    const reorderPaginaId = document.getElementById('ReorderPaginaId');
    const reorderDag = document.getElementById('ReorderDag');
    const reorderStarttijd = document.getElementById('ReorderStarttijd');
    const reorderNewOrder = document.getElementById('ReorderNewOrder');
    const reorderForm = document.getElementById('reorderForm');

    if (reorderPaginaId) reorderPaginaId.value = paginaId;
    if (reorderDag) reorderDag.value = newDag;
    if (reorderStarttijd) reorderStarttijd.value = newStarttijd;
    if (reorderNewOrder) reorderNewOrder.value = newOrder;
    if (reorderForm) reorderForm.submit();
}
