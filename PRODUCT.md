# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Evaluating .NET developers: Blazor developers deciding whether to adopt FormCraft instead of
hand-writing `EditForm` markup or using another form library. They skim, compare, read code, and copy
it. Existing users looking something up are a secondary audience served by the same demo pages.

## Product Purpose

FormCraft is an open-source (MIT) .NET library that builds Blazor forms from a typed fluent builder
(`FormBuilder<TModel>`). The demo site (`FormCraft.DemoBlazorApp`, Blazor WebAssembly, deployed to
GitHub Pages at phmatray.github.io/FormCraft) shows it working. It succeeds when a visitor leaves
believing that a form they would spend ~150 lines of Razor on is ~10 lines of typed C#, and that it
comes accessible and validated out of the box.

## Positioning

The form is described once, in C#, with expression-bound fields (`x => x.Email`), and the library
renders it through a pluggable UI adapter. Validation runs server-side against the model, not in the
browser. Accessibility is part of the library itself (aria-required on every field type, focus
restore on controls that unmount), not left for the app to add.

## Operating Context

- Visitors arrive from NuGet, GitHub, or search. They usually check the install command, the code
  shape, and the test count and targets before anything else.
- Every demo page hosts a real, interactive `FormCraftComponent`, not a screenshot. That is non-negotiable.
- The site runs as a static WASM app with a GitHub Pages SPA redirect (`wwwroot/404.html`, `index.html`).

## Capabilities and Constraints

- Packages: `FormCraft` (core), `FormCraft.ForMudBlazor`, `FormCraft.ForFluentUI`. The demo uses MudBlazor.
- Targets: net8.0 and net10.0 (library); the demo is net10.0.
- ~20 demos ordered Beginner → Intermediate → Advanced by `DemoRegistry`, plus Markdown docs pages.
- Capabilities shown: simplified/fluent/attribute-based builders, field groups, dependencies,
  cross-field and FluentValidation, async value providers, custom renderers, file upload, LOV,
  master-detail collections, dialogs, stepper/tabbed layouts, form slots, security (encryption, CSRF,
  rate limiting).
- MudBlazor ≥ 9.9.0 is a hard floor (see CLAUDE.md).

## Brand Commitments

- Name: FormCraft. No logo mark beyond the wordmark and `favicon.png`.
- Voice: plain, specific, engineer-to-engineer; name the API that does the job.

## Evidence on Hand

- Test suite: ~1,550 tests across three projects (the figure drifts; quote a lower bound).
- MIT licence, public GitHub repo, NuGet packages.
- No testimonials, customer logos, benchmarks, or download figures are available to the site. Do not invent them.

## Product Principles

1. Show, don't claim: every capability links to a working demo with its source.
2. The code and the form it produces belong side by side. Hiding one behind a tab breaks the argument.
3. The site must meet the accessibility bar the library sets (WCAG 2.1 AA, keyboard-complete).
4. No generic SaaS aesthetics: no gradient blobs, glass cards, or stock icon grids.

## Accessibility & Inclusion

WCAG 2.1 AA. Keyboard-complete navigation with a visible focus ring, a skip link, support for
reduced motion, and light and dark themes that both pass contrast.
