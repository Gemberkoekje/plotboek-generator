// Audit trail expand/collapse details
document.addEventListener('click', function (e) {
    const btn = e.target.closest('.audit-details-toggle');
    if (!btn) return;

    const details = btn.nextElementSibling;
    if (!details) return;

    const icon = btn.querySelector('i');
    const isVisible = details.style.display !== 'none';
    details.style.display = isVisible ? 'none' : 'block';
    if (icon) {
        icon.classList.toggle('fa-chevron-down', isVisible);
        icon.classList.toggle('fa-chevron-up', !isVisible);
    }
    btn.title = isVisible ? pbT('AuditDetailsTonen') : pbT('AuditDetailsVerbergen');
});

document.addEventListener('click', function (e) {
    const btn = e.target.closest('.audit-debug-toggle');
    if (!btn) return;

    const details = btn.nextElementSibling;
    if (!details) return;

    const isVisible = details.style.display !== 'none';
    details.style.display = isVisible ? 'none' : 'block';
    btn.textContent = isVisible ? pbT('AuditDebugTonen') : pbT('AuditDebugVerbergen');
});
