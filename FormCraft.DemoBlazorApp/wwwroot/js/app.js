// Hide the loading screen once Blazor has painted the layout.
window.addEventListener('DOMContentLoaded', () => {
    const checkBlazorReady = setInterval(() => {
        const loadingElement = document.getElementById('app-loading');
        if (loadingElement && document.querySelector('.mud-layout')) {
            loadingElement.style.transition = 'opacity 0.3s ease-out';
            loadingElement.style.opacity = '0';
            setTimeout(() => loadingElement.remove(), 300);
            clearInterval(checkBlazorReady);
        }
    }, 100);
});

// ---------------------------------------------------------------------------
// Theme
// A returning visitor keeps the theme they chose; a first-time visitor gets the
// one their OS asks for. index.html resolves the same value before first paint,
// so there is no flash of the wrong theme.
// ---------------------------------------------------------------------------
window.formcraftTheme = {
    STORAGE_KEY: 'formcraft-theme',

    // Returns true for dark. Falls back to the system preference when the
    // visitor has never chosen, and to light when storage is unavailable.
    resolve: function () {
        try {
            const stored = localStorage.getItem(this.STORAGE_KEY);
            if (stored === 'dark') return true;
            if (stored === 'light') return false;
        } catch (e) { /* private mode */ }
        return window.matchMedia('(prefers-color-scheme: dark)').matches;
    },

    persist: function (isDark) {
        try {
            localStorage.setItem(this.STORAGE_KEY, isDark ? 'dark' : 'light');
        } catch (e) { /* private mode: the choice lasts for this session only */ }
    },

    // Mirror the theme onto <body> so plain markup can read the tokens too,
    // rather than depending on MudBlazor's own class name.
    apply: function (isDark) {
        document.body.classList.toggle('fc-dark', isDark);
        document.documentElement.classList.toggle('fc-boot-dark', isDark);
    }
};

// ---------------------------------------------------------------------------
// Command palette shortcut
// ---------------------------------------------------------------------------
window.formcraftShortcuts = {
    _handler: null,

    register: function (dotNetRef) {
        this.unregister();
        this._handler = (e) => {
            const key = e.key ? e.key.toLowerCase() : '';
            if ((e.metaKey || e.ctrlKey) && key === 'k') {
                e.preventDefault();
                dotNetRef.invokeMethodAsync('OpenPalette');
                return;
            }
            // "/" opens search too, but not while the visitor is typing.
            if (key === '/' && !e.metaKey && !e.ctrlKey && !e.altKey) {
                const el = document.activeElement;
                const tag = el ? el.tagName : '';
                if (tag === 'INPUT' || tag === 'TEXTAREA' || (el && el.isContentEditable)) return;
                e.preventDefault();
                dotNetRef.invokeMethodAsync('OpenPalette');
            }
        };
        document.addEventListener('keydown', this._handler);
    },

    unregister: function () {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler);
            this._handler = null;
        }
    },

    // True on Apple platforms, so the palette hint can show ⌘K rather than Ctrl K.
    isApple: function () {
        return /Mac|iPhone|iPad|iPod/.test(navigator.platform || navigator.userAgent || '');
    }
};

window.formcraftCopy = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (e) {
        return false;
    }
};
