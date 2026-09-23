// ---------------------------------------------------------------------------
// Code blocks, docs and the API reference.
//
// One small highlighter for C# and Razor replaces Prism: the site only ever shows those two, and
// owning the tokenizer lets every block render one element per line. That is what makes the
// hanging indent possible — a long line wraps and continues four columns past its own
// indentation — so no code block on the site needs a horizontal scrollbar.
// ---------------------------------------------------------------------------
(function () {
    const esc = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

    const KW = 'var|new|private|protected|public|internal|readonly|static|class|record|interface|string|bool|int|long|decimal|double|object|return|override|void|get|set|init|null|true|false|using|namespace|async|await|if|else|foreach|in|is|this|typeof|nameof|required|partial|const|throw';
    const csRe = new RegExp(`(\\/\\/.*$)|(\\$?@?"(?:[^"\\\\]|\\\\.)*")|\\b(${KW})\\b|(\\b[A-Za-z_]\\w*(?=\\s*(?:<[\\w<>, ?]*>)?\\())|(\\b[A-Z]\\w*\\b)|(\\b\\d+(?:\\.\\d+)?[mMdDfF]?\\b)|(=>|[{}()\\[\\];,.<>=*+?!&|:-])`, 'g');
    const csCls = [null, 'tk-com', 'tk-str', 'tk-kw', 'tk-fn', 'tk-ty', 'tk-num', 'tk-pun'];
    const rzRe = /(@\*.*?\*@)|(@(?:code|page|using|inject|inherits|implements|namespace|attribute|typeparam|layout|rendermode)\b)|("@[^"]*")|("(?:[^"\\]|\\.)*")|(<\/?[A-Za-z][\w.:]*|\/?>)|(@?\b[A-Za-z][\w:.-]*(?==))|([{}])/g;
    const rzCls = [null, 'tk-com', 'tk-dir', 'tk-dir', 'tk-str', 'tk-tag', 'tk-attr', 'tk-pun'];

    function tokens(line, mode) {
        const re = mode === 'cs' ? csRe : rzRe, cls = mode === 'cs' ? csCls : rzCls;
        const out = [];
        let last = 0, m;
        re.lastIndex = 0;
        while ((m = re.exec(line))) {
            if (m[0] === '') { re.lastIndex++; continue; }
            if (m.index > last) out.push(['', line.slice(last, m.index)]);
            out.push([cls[m.findIndex((v, i) => i > 0 && v !== undefined)], m[0]]);
            last = re.lastIndex;
        }
        if (last < line.length) out.push(['', line.slice(last)]);
        return out;
    }

    // A Razor file is markup until `@code {`, then C# until the closing brace at column 0.
    function tokenize(text, lang) {
        let mode = lang === 'razor' ? 'rz' : lang === 'cs' ? 'cs' : 'plain';
        return text.split('\n').map(line => {
            if (mode === 'plain') return [['', line]];
            if (lang === 'razor') {
                if (/^@code\s*\{/.test(line)) { mode = 'cs'; return [['tk-dir', '@code'], ['', ' '], ['tk-pun', '{']]; }
                if (mode === 'cs' && /^\}\s*$/.test(line)) { mode = 'rz'; return [['tk-pun', '}']]; }
            }
            return tokens(line, mode === 'cs' ? 'cs' : 'rz');
        });
    }

    const LANG = { csharp: 'cs', cs: 'cs', razor: 'razor', cshtml: 'razor', html: 'razor', markup: 'razor', xml: 'razor' };

    // Replaces a <code>'s text with one span per line, each carrying its indentation as --ind.
    function render(code, lang) {
        const text = code.textContent.replace(/\n$/, '');
        code.innerHTML = tokenize(text, lang).map(toks => {
            const line = toks.map(([c, t]) => c ? `<span class="${c}">${esc(t)}</span>` : esc(t)).join('');
            const ind = (toks.map(t => t[1]).join('').match(/^ */) || [''])[0].length;
            return `<span class="cl" style="--ind:${ind}">${line || ' '}</span>`;
        }).join('');
        code.dataset.done = '1';
    }

    const langOf = code => {
        const raw = ((code.className || '').match(/language-(\w+)/) || [])[1] || code.dataset.lang || '';
        return LANG[raw.toLowerCase()] || 'plain';
    };
    const LABEL = { cs: 'C#', razor: 'Razor', plain: 'Shell' };
    const COPY = '<svg class="fc-i" viewBox="0 0 24 24" aria-hidden="true"><rect x="8.5" y="8.5" width="11" height="11" rx="2"/><path d="M15.5 8.5v-3a1 1 0 0 0-1-1h-9a1 1 0 0 0-1 1v9a1 1 0 0 0 1 1h3"/></svg>';
    const CHECK = '<svg class="fc-i" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.5 4.5L19 7.5"/></svg>';

    const textOf = el => [...el.querySelectorAll('.cl')].map(l => l.textContent).join('\n');

    async function copyFrom(button, text) {
        try { await navigator.clipboard.writeText(text); } catch (e) { return; }
        const label = button.getAttribute('aria-label'), icon = button.innerHTML;
        button.classList.add('is-done');
        button.setAttribute('aria-label', 'Copied');
        button.innerHTML = CHECK;
        setTimeout(() => { button.classList.remove('is-done'); button.setAttribute('aria-label', label); button.innerHTML = icon; }, 2000);
    }

    // Any [data-copy-code] button copies the nearest code block, line by line.
    document.addEventListener('click', e => {
        const b = e.target.closest('[data-copy-code]');
        if (!b) return;
        const host = b.closest('[data-code-host]') || b.parentElement;
        const code = host && host.querySelector('code');
        if (code) copyFrom(b, textOf(code));
    });

    function highlightUnder(root) {
        if (!root) return;
        root.querySelectorAll('pre > code:not([data-done])').forEach(code => render(code, langOf(code)));
    }

    // Markdown output: every fence becomes a labelled block with a copy button.
    function enhanceBlocks(root) {
        root.querySelectorAll('pre > code:not([data-done])').forEach(code => {
            const lang = langOf(code);
            render(code, lang);
            const pre = code.parentElement;
            const block = document.createElement('div');
            block.className = 'fc-block';
            block.setAttribute('data-code-host', '');
            block.innerHTML = `<div class="fc-block__bar"><span>${LABEL[lang]}</span><button type="button" class="fc-copy" data-copy-code aria-label="Copy code">${COPY}</button></div>`;
            pre.replaceWith(block);
            pre.className = 'fc-code';
            block.append(pre);
        });
    }

    // Links between Markdown files become routes: getting-started.md -> docs/getting-started.
    function rewriteLinks(root) {
        root.querySelectorAll('a[href]').forEach(a => {
            const href = a.getAttribute('href');
            const m = href.match(/^(?:\.\/)?([\w-]+)\.md(#[\w-]*)?$/);
            if (m) a.setAttribute('href', `docs/${m[1]}${m[2] || ''}`);
            else if (/^https?:/.test(href)) { a.target = '_blank'; a.rel = 'noopener'; }
        });
    }

    let spy = null;
    function buildToc(article, toc) {
        if (!toc) return;
        const heads = [...article.querySelectorAll('h2[id], h3[id]')];
        toc.innerHTML = heads.map(h => `<a href="#${h.id}" class="lv${h.tagName[1]}">${esc(h.textContent)}</a>`).join('');
        if (spy) spy.disconnect();
        spy = new IntersectionObserver(entries => entries.forEach(en => {
            if (!en.isIntersecting) return;
            toc.querySelectorAll('a').forEach(a => a.classList.toggle('is-on', a.getAttribute('href') === '#' + en.target.id));
        }), { rootMargin: '-90px 0px -70% 0px' });
        heads.forEach(h => spy.observe(h));
    }

    // In-page anchors scroll without a route change: with <base href="/">, "#x" resolves to "/#x"
    // and Blazor's router would navigate home. Capture phase, so this runs before the router does.
    document.addEventListener('click', e => {
        const a = e.target.closest('a[href^="#"]');
        if (!a || a.getAttribute('href').length < 2) return;
        const target = document.getElementById(decodeURIComponent(a.getAttribute('href').slice(1)));
        if (!target) return;
        e.preventDefault();
        e.stopPropagation();
        const reduce = matchMedia('(prefers-reduced-motion: reduce)').matches;
        target.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'start' });
        history.replaceState(null, '', location.pathname + location.search + a.getAttribute('href'));
    }, true);

    // API reference: the Markdown is flat, so regroup it. An h2 opens a group; an h4 opens an
    // entry that owns everything up to the next heading. Then build the side list and the filter.
    function enhanceApi(article, nav, filter, empty) {
        // The page header already titles the reference; drop the Markdown's own h1 and intro line.
        article.querySelector('h1')?.remove();
        const lead = article.querySelector('p');
        if (lead && /Complete API documentation/i.test(lead.textContent)) lead.remove();
        const out = document.createElement('div');
        let group = null, entry = null;
        [...article.childNodes].forEach(n => {
            const tag = n.tagName;
            if (tag === 'H2' || !group) {
                group = document.createElement('section');
                group.className = 'fc-apigroup';
                out.append(group);
                entry = null;
                if (tag === 'H2') { group.append(n); return; }
            }
            if (tag === 'H3') { entry = null; group.append(n); return; }
            if (tag === 'H4') {
                entry = document.createElement('section');
                entry.className = 'fc-entry';
                entry.id = n.id;
                n.removeAttribute('id');
                const body = document.createElement('div');
                body.className = 'fc-entry__body';
                entry.append(n, body);
                group.append(entry);
                return;
            }
            (entry ? entry.querySelector('.fc-entry__body') : group).append(n);
        });
        article.replaceChildren(out);

        nav.innerHTML = [...out.querySelectorAll('.fc-apigroup')].map(g => {
            const h2 = g.querySelector(':scope > h2');
            if (!h2) return '';
            let html = `<p class="fc-side__label">${esc(h2.textContent)}</p>`;
            [...g.children].forEach(c => {
                if (c.tagName === 'H3') html += `<a class="fc-side__sub" href="#${c.id}" data-sub>${esc(c.textContent)}</a>`;
                if (c.classList.contains('fc-entry')) html += `<a href="#${c.id}" data-entry="${c.id}">${esc(c.querySelector('h4').textContent)}</a>`;
            });
            return `<div class="fc-side__group">${html}</div>`;
        }).join('');

        filter.oninput = () => {
            const q = filter.value.trim().toLowerCase();
            let shown = 0;
            out.querySelectorAll('.fc-entry').forEach(en => {
                const hit = !q || en.textContent.toLowerCase().includes(q);
                en.hidden = !hit;
                if (hit) shown++;
            });
            out.querySelectorAll('.fc-apigroup').forEach(g => {
                const any = [...g.querySelectorAll('.fc-entry')].some(en => !en.hidden);
                g.hidden = !!q && !any;
                [...g.children].forEach(c => { if (!c.classList.contains('fc-entry') && c.tagName !== 'H2') c.hidden = !!q; });
            });
            nav.querySelectorAll('a[data-entry]').forEach(a => { a.hidden = document.getElementById(a.dataset.entry)?.hidden; });
            nav.querySelectorAll('a[data-sub]').forEach(a => {
                let n = a.nextElementSibling, any = false;
                while (n && n.matches('a[data-entry]')) { any = any || !n.hidden; n = n.nextElementSibling; }
                a.hidden = !!q && !any && !a.textContent.toLowerCase().includes(q);
            });
            nav.querySelectorAll('.fc-side__group').forEach(g => { g.hidden = !!q && ![...g.querySelectorAll('a')].some(a => !a.hidden); });
            empty.hidden = shown > 0;
            empty.querySelector('b').textContent = filter.value;
        };
    }

    window.formcraftCode = {
        highlightUnder,

        // Called by DocumentationPage after each document renders.
        enhanceDoc(article, toc, apiNav, apiFilter, apiEmpty) {
            if (!article) return;
            rewriteLinks(article);
            enhanceBlocks(article);
            if (apiNav) enhanceApi(article, apiNav, apiFilter, apiEmpty);
            else buildToc(article, toc);
        },

        // Keeps the newest typed line of the home hero in view, inside the editor only.
        follow(pre, line) {
            const el = pre && pre.querySelectorAll('.cl')[line - 1];
            if (!el) return;
            const top = el.offsetTop - pre.clientHeight + el.offsetHeight + 24;
            if (top > pre.scrollTop) pre.scrollTop = top;
        },

        copy: copyFrom,
        textOf
    };
})();
