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

## Not yet covered

- Lookup and LOV dialogs, autocomplete, multi-select, file upload, and custom renderers.

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
[#199](https://github.com/phmatray/FormCraft/issues/199). Suppress the decoration per-field with
`.WithAttribute("Required", false)`.

FormCraft validates server-side: the form renders `novalidate`, so the browser runs no constraint
validation of its own.

## License

MIT
