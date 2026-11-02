/*
 * The light/dark theme of the product's own layout.
 *
 * The choice is "system", "light" or "dark". "light" and "dark" live in a cookie whose name the
 * layout puts on <html> as data-theme-cookie (and, to share one choice across sites, a domain as
 * data-theme-domain). No cookie means: follow the system setting. The effective theme lands on
 * <html> as data-bs-theme, where Bootstrap picks it up; the choice lands as data-theme-choice.
 *
 * Every element with data-theme-toggle cycles system -> light -> dark on click; its aria-label and
 * title come from its data-label-system / -light / -dark attributes. Load this script in <head>
 * without defer, so the theme is set before the first paint.
 */
(function () {
    'use strict';

    var root = document.documentElement;
    var cookieName = root.getAttribute('data-theme-cookie') || 'plotboek-theme';
    var sharedDomain = root.getAttribute('data-theme-domain') || '';
    var maxAge = 34560000; // 400 days: the longest a browser keeps a cookie.
    var media = window.matchMedia ? window.matchMedia('(prefers-color-scheme: dark)') : null;

    function readCookie() {
        var prefix = cookieName + '=';
        var parts = document.cookie.split(';');
        for (var i = 0; i < parts.length; i++) {
            var part = parts[i].trim();
            if (part.indexOf(prefix) === 0) {
                var value = part.substring(prefix.length);
                return value === 'light' || value === 'dark' ? value : 'system';
            }
        }
        return 'system';
    }

    function writeCookie(value) {
        var host = window.location.hostname;
        var shared = sharedDomain !== '' && (host === sharedDomain || host.endsWith('.' + sharedDomain));
        var parts = [cookieName + '=' + (value === 'system' ? '' : value), 'path=/', 'SameSite=Lax'];
        parts.push(value === 'system' ? 'max-age=0' : 'max-age=' + maxAge);
        // A host that does not belong to the shared domain gets a host-only cookie: a Domain it does
        // not belong to would make the browser drop the cookie altogether.
        if (shared) { parts.push('domain=' + sharedDomain); }
        if (window.location.protocol === 'https:') { parts.push('Secure'); }
        document.cookie = parts.join('; ');
    }

    var choice = readCookie();

    function effectiveTheme() {
        if (choice === 'light' || choice === 'dark') { return choice; }
        return media && media.matches ? 'dark' : 'light';
    }

    function labelToggles() {
        var toggles = document.querySelectorAll('[data-theme-toggle]');
        for (var i = 0; i < toggles.length; i++) {
            var label = toggles[i].getAttribute('data-label-' + choice);
            toggles[i].hidden = false;
            if (label) {
                toggles[i].setAttribute('aria-label', label);
                toggles[i].setAttribute('title', label);
            }
        }
    }

    function apply() {
        root.setAttribute('data-bs-theme', effectiveTheme());
        root.setAttribute('data-theme-choice', choice);
        labelToggles();
    }

    var next = { system: 'light', light: 'dark', dark: 'system' };

    document.addEventListener('click', function (event) {
        var toggle = event.target && event.target.closest ? event.target.closest('[data-theme-toggle]') : null;
        if (!toggle) { return; }
        choice = next[choice] || 'system';
        writeCookie(choice);
        apply();
    });

    if (media && media.addEventListener) {
        media.addEventListener('change', function () {
            if (choice === 'system') { apply(); }
        });
    }

    // Another tab may have changed the cookie meanwhile.
    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible' && readCookie() !== choice) {
            choice = readCookie();
            apply();
        }
    });

    apply();
    document.addEventListener('DOMContentLoaded', labelToggles);
})();
