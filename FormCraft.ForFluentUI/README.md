# FormCraft for Fluent UI Blazor

Fluent UI Blazor **v5** implementation for [FormCraft](https://github.com/phmatray/FormCraft) dynamic
forms. Renders your `FormBuilder<TModel>` configuration with Microsoft's Fluent design language.

## Installation

```bash
dotnet add package FormCraft.ForFluentUI
```

## Setup

```csharp
// Program.cs
builder.Services.AddFormCraft();
builder.Services.AddFormCraftFluentUI();
builder.Services.AddFluentUIComponents();
```

```razor
@* _Imports.razor *@
@using FormCraft.ForFluentUI
```

Then render a form exactly as you would with any other FormCraft adapter:

```razor
<FormCraftComponent TModel="ContactModel"
                    Model="@_model"
                    Configuration="@_configuration"
                    OnValidSubmit="@HandleSubmit" />
```

## Supported field types

| Model type | Rendered with |
|---|---|
| `string` | `FluentTextInput` (or `FluentTextArea` when `Lines > 1`) |
| `string` with `.WithOptions(...)` | `FluentSelect` |
| `int`, `long`, `short`, `decimal`, `double`, `float` (and nullable forms) | `FluentNumberInput<T>` |
| `bool`, `bool?` | `FluentCheckbox` (or `FluentSwitch` via `.WithAttribute("Switch", true)`) |
| `DateTime`, `DateOnly` (and nullable forms) | `FluentDatePicker<T>` |
| `TimeOnly`, `TimeOnly?` | `FluentTimePicker<T>` |

## Field groups

`.AddFieldGroup(...)` renders with its card and column layout (#278): `ShowInCard()` produces a
`FluentCard`, `WithColumns(n)` lays the group's fields out across Fluent's 12-column grid, and any
field left out of every group still renders after them.

One deliberate difference from the MudBlazor adapter: `ShowInCard(elevation: n)` takes an integer on
MudBlazor's scale, and Fluent has only five shadow buckets, so the elevation is mapped onto the
nearest one rather than ignored. For an exact shadow, style the group's `WithCssClass(...)` instead.

## Security

`.WithSecurity(...)` is **enforced** — rate limiting, CSRF protection, audit logging and field
encryption all behave as they do in the MudBlazor adapter (#278). A blocked submission never reaches
your `OnValidSubmit` handler, and the reason is shown in a `FluentMessageBar`.

```csharp
var config = FormBuilder<ContactModel>.Create()
    .AddField(x => x.Email, f => f.WithLabel("Email"))
    .WithSecurity(s => s
        .WithRateLimit(5, TimeSpan.FromMinutes(1))
        .EnableCsrfProtection()
        .EnableAuditLogging())
    .Build();
```

Set `SecurityContextId` on the component to a per-user or per-session value, or rate limits are
shared across every user of the form (it defaults to the model type name).

## Lookup and LOV pickers are inline, not modal

`.AsLookup(...)` and `.AsLov(...)` render a read-only display plus a **Browse** button that reveals
the candidate rows in an inline panel. The MudBlazor adapter opens a modal dialog instead, and the
difference is deliberate: Fluent UI v5's dialog service renders nothing unless the host application
places a `FluentDialogProvider` in its layout, and a field component cannot check that. A Browse
button that silently did nothing on an app which had not added the provider is the kind of quiet
failure this library avoids elsewhere, so the picker is rendered where it cannot fail to appear.

⚠️ **`.AsLookup(...)` for Fluent lives in `FormCraft.ForFluentUI.Extensions`**, not in namespace
`FormCraft`, because the MudBlazor package already publishes a method of that name there and a
project referencing both would get `CS0121` on every call. Add
`using FormCraft.ForFluentUI.Extensions;` and call it as usual. Both write the same attributes, so
either package's `.AsLookup(...)` renders correctly under either adapter.

## Custom renderers

Three ship with the package, used via `.WithCustomRenderer(...)` rather than registered globally —
registering them would turn every `double` into a slider and every `string` into a colour picker:

```csharp
.AddField(x => x.Volume, f => f.WithCustomRenderer(typeof(FluentUISliderRenderer)))
.AddField(x => x.Score,  f => f.WithCustomRenderer(typeof(FluentUIRatingRenderer)))
.AddField(x => x.Colour, f => f.WithCustomRenderer(typeof(FluentUIColorPickerRenderer)))
```

The rating renders a row of focusable, labelled buttons rather than Fluent's `FluentRatingDisplay`.
Measured against `5.0.0-rc.5`: that component exposes `Value` but no `ValueChanged` and no click
handling — it is a read-only display, so a field bound to it would show the score and silently refuse
every edit.

## Running the showcase

```bash
cd FormCraft.DemoFluentApp && dotnet run
```

A **separate** app from `FormCraft.DemoBlazorApp` on purpose: the two adapters are mutually exclusive
in one DI container, and Blazor has no per-subtree service provider, so a Fluent page cannot live
inside the MudBlazor demo whatever it injects.

## Not yet covered

Nothing outstanding against the MudBlazor adapter's feature set.

See the follow-ups on [#260](https://github.com/phmatray/FormCraft/issues/260).

## One adapter at a time

`AddFormCraftFluentUI()` and `AddFormCraftMudBlazor()` are **mutually exclusive**. Renderer selection
is first-match-wins, so a container holding both renders a form that is partly Material and partly
Fluent, with no error to point at.

`AddFormCraftFluentUI()` throws when the MudBlazor adapter is **already** registered:

```csharp
builder.Services.AddFormCraft();
builder.Services.AddFormCraftMudBlazor();
builder.Services.AddFormCraftFluentUI();   // throws InvalidOperationException
```

⚠️ **The guard is one-directional.** It inspects the container at the moment it runs, so the reverse
order slips through and produces exactly the mixed container it exists to prevent:

```csharp
builder.Services.AddFormCraft();
builder.Services.AddFormCraftFluentUI();
builder.Services.AddFormCraftMudBlazor();  // does NOT throw — mixed renderers
```

Closing that needs a matching check on the MudBlazor side, which belongs to that package. Until then,
register exactly one adapter and prefer putting `AddFormCraftFluentUI()` last if you are unsure.

## Accessibility

A `.Required(...)` field is announced to assistive technology with `aria-required="true"`, matching
the guarantee the MudBlazor adapter carries since
[#199](https://github.com/phmatray/FormCraft/issues/199).

Control the decoration independently of the validator with the typed `.WithNativeRequired(...)`
(add `using FormCraft.ForFluentUI.Extensions;`):

```csharp
.AddField(x => x.Email, f => f
    .Required("Email is required")   // the validation
    .WithNativeRequired())           // the decoration

.AddField(x => x.Nickname, f => f
    .Required("Nickname is required")
    .WithNativeRequired(false))      // validated, but not announced
```

It wins over the validator in **both** directions, and replaces the raw
`.WithAttribute("Required", …)` form this README used to document — that still works and writes the
same attribute, but it is a magic string one typo away from silently doing nothing.

**File uploads are the exception:** a required upload is marked with a visible `*` on its label and
an `aria-describedby` hint on its Browse button, not `aria-required` on the hidden file input, which
no keyboard user ever reaches.

FormCraft validates server-side: the form renders `novalidate`, so the browser runs no constraint
validation of its own.

## License

MIT
