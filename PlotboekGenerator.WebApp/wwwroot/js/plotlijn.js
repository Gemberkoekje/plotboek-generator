// Plotlijn Management
function updatePlotlijnOptions() {
    const domainSelect = document.getElementById('editable-domain');
    const plotlijnSelect = document.getElementById('editable-plotlijn');
    const plotlijnContainer = document.getElementById('plotlijn-container');

    if (!domainSelect || !plotlijnSelect || !plotlijnContainer) return;

    const selectedDomainId = domainSelect.value;

    // Always show the plotlijn container
    plotlijnContainer.style.display = 'block';

    // Filter options in optgroups to only show items with the same domain
    const options = plotlijnSelect.querySelectorAll('option');
    options.forEach(option => {
        if (option.value === '') {
            // Always show the empty option
            option.style.display = '';
        } else {
            const optionDomainId = option.dataset.domainId || '';
            const domainMatches = optionDomainId === selectedDomainId;
            option.style.display = domainMatches ? '' : 'none';
        }
    });

    // Hide optgroups that have no visible options
    const optgroups = plotlijnSelect.querySelectorAll('optgroup');
    optgroups.forEach(optgroup => {
        const visibleOptions = optgroup.querySelectorAll('option:not([style*="display: none"])');
        optgroup.style.display = visibleOptions.length > 0 ? '' : 'none';
    });

    // If current selection is now hidden, reset to empty
    const currentOption = plotlijnSelect.querySelector(`option[value="${plotlijnSelect.value}"]`);
    if (currentOption && currentOption.style.display === 'none') {
        plotlijnSelect.value = '';
    }
}
