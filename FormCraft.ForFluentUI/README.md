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

## Not yet covered

- Collection/item-form fields, lookup and LOV dialogs, autocomplete, multi-select, file upload, and
  custom renderers.
- **Field groups** — `.AddFieldGroup(...)` fields render, but ungrouped and without the card/column
  layout the MudBlazor adapter gives them.
- **`.WithSecurity(...)`** — rate limiting, CSRF protection, audit logging and field encryption are
  *not* enforced. Rather than drop them silently, a form configured with security **throws** at
  render time. Use `FormCraft.ForMudBlazor` for forms that need them.

See the follow-ups on [#260](https://github.com/phmatray/FormCraft/issues/260).

## One adapter at a time

`AddFormCraftFluentUI()` and `AddFormCraftMudBlazor()` are **mutually exclusive**. Renderer selection
is first-match-wins, so a container holding both renders a form that is partly Material and partly
Fluent, with no error to point at.

Whichever registration runs second throws, so **both orders fail identically**:

```csharp
builder.Services.AddFormCraft();
builder.Services.AddFormCraftMudBlazor();
builder.Services.AddFormCraftFluentUI();   // throws InvalidOperationException

builder.Services.AddFormCraft();
builder.Services.AddFormCraftFluentUI();
builder.Services.AddFormCraftMudBlazor();  // throws too, since #279
```

The guard used to live in this package alone, which meant it only caught the first order — the
second slipped through and produced exactly the mixed container it exists to prevent. The rule now
lives in `FormCraft` core (`AdapterRegistration.EnsureSingleAdapter`) and both adapters call it, so
it is symmetric by construction rather than by two packages remembering to agree.

Registering the *same* adapter twice is not a conflict and stays legal.

## Accessibility

A `.Required(...)` field is announced to assistive technology with `aria-required="true"`, matching
the guarantee the MudBlazor adapter carries since
[#199](https://github.com/phmatray/FormCraft/issues/199). Suppress the decoration per-field with
`.WithAttribute("Required", false)`.

FormCraft validates server-side: the form renders `novalidate`, so the browser runs no constraint
validation of its own.

## License

MIT
