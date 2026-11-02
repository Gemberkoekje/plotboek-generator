// The starttijd dropdown mirrors the server. Razor writes the options for every day into
// #starttijd-opties (see _PlotboekPaginaDetails.cshtml); this script only picks the list for the
// day that is selected. Availability, block numbers and block windows therefore live in the
// reference data alone - a slot added or switched off in Admin -> Starttijden shows up here
// without a code change.

function getStarttijdOptiesPerDag() {
    const holder = document.getElementById('starttijd-opties');
    if (!holder) {
        return null;
    }

    try {
        return JSON.parse(holder.textContent);
    } catch (error) {
        console.error('Could not read the start time options', error);
        return null;
    }
}

function updateStarttijdOptions() {
    const dagSelect = document.getElementById('editable-day');
    const starttijdSelect = document.getElementById('editable-starttijd');

    if (!dagSelect || !starttijdSelect) return;

    const optiesPerDag = getStarttijdOptiesPerDag();

    // No payload means we cannot know better than the server did: leave its options alone.
    if (!optiesPerDag) return;

    const opties = optiesPerDag[dagSelect.value] || optiesPerDag['None'] || [];
    const currentStarttijd = starttijdSelect.value;
    const heeftHuidige = opties.some(optie => optie.id === currentStarttijd);

    starttijdSelect.innerHTML = '';

    opties.forEach(optie => {
        const option = document.createElement('option');
        option.value = optie.id;
        option.textContent = optie.label;
        option.selected = heeftHuidige && optie.id === currentStarttijd;
        starttijdSelect.appendChild(option);
    });

    // The current slot does not exist on the newly picked day: fall back to None.
    if (!heeftHuidige) {
        starttijdSelect.value = 'None';
    }
}
