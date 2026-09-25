// Hide the loading screen once Blazor has painted the shell.
window.addEventListener('DOMContentLoaded', () => {
    const checkBlazorReady = setInterval(() => {
        const loadingElement = document.getElementById('app-loading');
        if (loadingElement && document.querySelector('.fc-shell')) {
            loadingElement.style.transition = 'opacity 0.3s ease-out';
            loadingElement.style.opacity = '0';
            setTimeout(() => loadingElement.remove(), 300);
            clearInterval(checkBlazorReady);
        }
    }, 100);
});

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

// ---------------------------------------------------------------------------
// Adapter preference (MudBlazor | Fluent UI), read by AdapterPreference.cs.
// Storage can throw (private mode, blocked site data), so every access is guarded;
// the attribute still switches for the session when storage is unavailable.
// ---------------------------------------------------------------------------
window.fcAdapter = {
    // index.html resolved ?adapter= and storage into the attribute before boot; report that.
    get: function () {
        return document.documentElement.dataset.adapter === 'fluentui' ? 'fluentui' : 'mudblazor';
    },
    set: function (value) {
        if (value === 'fluentui') document.documentElement.dataset.adapter = 'fluentui';
        else delete document.documentElement.dataset.adapter;
        try { localStorage.setItem('fc-adapter', value === 'fluentui' ? 'fluentui' : 'mudblazor'); } catch (e) { }
    }
};

// Asked once by the home hero before it starts its sequence.
window.formcraftPrefersReducedMotion = () => matchMedia('(prefers-reduced-motion: reduce)').matches;

window.formcraftCopy = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (e) {
        return false;
    }
};

// ---------------------------------------------------------------------------
// Gallery previews on the home page move while hovered or focused.
// Delegated from the document, so it survives Blazor re-rendering the tiles.
// ---------------------------------------------------------------------------
(function () {
    const usd = n => '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const flash = el => { el.classList.remove('is-flash'); void el.offsetWidth; el.classList.add('is-flash'); };
    const wait = ms => new Promise(r => setTimeout(r, ms));
    const combos = [['Books', 'Technical guide', 49.99, 3], ['Clothing', 'Jacket', 89.99, 1], ['Electronics', 'Laptop', 999.99, 2]];
    const strength = ['Weak', 'Fair', 'Good', 'Strong'];

    // Each loop is one frame of the preview's story; the tile keeps its own frame counter.
    const loops = {
        async deps(t, i) {
            const [c, p, price, q] = combos[i % combos.length];
            const set = (k, v) => { const f = t.querySelector(`[data-k="${k}"]`); f.querySelector('[data-v]').textContent = v; flash(f); };
            set('cat', c); await wait(380);
            set('prod', `${p} · ${usd(price)}`); await wait(380);
            set('qty', q); t.querySelector('[data-total]').textContent = usd(price * q);
        },
        steps(t, i) {
            const n = i % 3;
            t.querySelectorAll('.fc-steps__s').forEach((s, k) => s.classList.toggle('is-on', k <= n));
            t.querySelectorAll('.fc-steps__bar').forEach((b, k) => b.style.setProperty('--p', k < n ? 1 : 0));
        },
        upload(t) {
            const bar = t.querySelector('.fc-prog i');
            bar.style.transition = 'none'; bar.style.transform = 'scaleX(0)'; void bar.offsetWidth;
            bar.style.transition = ''; bar.style.transform = 'scaleX(1)';
        },
        password(t, i) {
            const s = (i % 4) + 1;
            t.querySelector('.fc-meter').dataset.s = s;
            t.querySelector('[data-l]').textContent = strength[s - 1];
            t.querySelector('[data-v]').textContent = '•'.repeat(s * 3);
        },
        rows(t) {
            const body = t.querySelector('tbody');
            if (body.children.length > 2) { body.lastElementChild.remove(); return; }
            const tr = document.createElement('tr');
            tr.className = 'is-new';
            tr.innerHTML = '<td>27" monitor</td><td>1</td><td>$329.00</td>';
            body.append(tr);
        }
    };
    const every = { deps: 2000, steps: 1100, upload: 1900, password: 700, rows: 1300 };
    const running = new WeakMap();

    function start(tile) {
        const kind = tile.dataset.loop;
        if (!loops[kind] || running.has(tile) || matchMedia('(prefers-reduced-motion: reduce)').matches) return;
        let i = 0;
        const tick = () => loops[kind](tile, i++);
        tick();
        running.set(tile, setInterval(tick, every[kind]));
    }
    function stop(tile) {
        clearInterval(running.get(tile));
        running.delete(tile);
    }
    const tileOf = e => e.target.closest && e.target.closest('[data-loop]');
    document.addEventListener('mouseover', e => { const t = tileOf(e); if (t) start(t); });
    document.addEventListener('mouseout', e => { const t = tileOf(e); if (t && !t.contains(e.relatedTarget)) stop(t); });
    document.addEventListener('focusin', e => { const t = tileOf(e); if (t) start(t); });
    document.addEventListener('focusout', e => { const t = tileOf(e); if (t) stop(t); });
})();
