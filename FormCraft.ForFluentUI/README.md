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

Collection/item-form fields, lookup and LOV dialogs, autocomplete, multi-select and file upload are
not yet implemented for Fluent — see the follow-ups on
[#260](https://github.com/phmatray/FormCraft/issues/260).

## One adapter at a time

`AddFormCraftFluentUI()` and `AddFormCraftMudBlazor()` are **mutually exclusive**. Renderer selection
is first-match-wins, so a container holding both would render a form that is partly Material and
partly Fluent with no error to point at. Registering both throws instead.

## Accessibility

A `.Required(...)` field is announced to assistive technology with `aria-required="true"`, matching
the guarantee the MudBlazor adapter carries since
[#199](https://github.com/phmatray/FormCraft/issues/199). Suppress the decoration per-field with
`.WithAttribute("Required", false)`.

FormCraft validates server-side: the form renders `novalidate`, so the browser runs no constraint
validation of its own.

## License

MIT
