// Convert UTC timestamps to the user's local time zone
function convertUtcTimestamps() {
    document.querySelectorAll('[data-utc]').forEach(el => {
        const utc = new Date(el.dataset.utc);
        if (isNaN(utc)) return;
        const dd = String(utc.getDate()).padStart(2, '0');
        const mm = String(utc.getMonth() + 1).padStart(2, '0');
        const yyyy = utc.getFullYear();
        const hh = String(utc.getHours()).padStart(2, '0');
        const min = String(utc.getMinutes()).padStart(2, '0');
        el.textContent = `${dd}-${mm}-${yyyy} ${hh}:${min}`;
    });
}

// Run as soon as the DOM is ready
document.addEventListener('DOMContentLoaded', convertUtcTimestamps);
