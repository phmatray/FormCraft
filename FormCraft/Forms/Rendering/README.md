# FormCraft Rendering Architecture

## Overview

FormCraft's core is UI-framework-agnostic. A **UI adapter** is an assembly that registers its own
`IFieldRenderer` implementations; core selects between them and renders whatever component each one
names. Two adapters ship — `FormCraft.ForMudBlazor` and `FormCraft.ForFluentUI` — and both are built
on exactly the seam described here.

> ⛔ **There is no adapter *interface*.** An `IUIFrameworkAdapter` used to be documented as the
> contract, together with `FrameworkAgnosticFieldRenderer<TComponent>` and a
> `UIFrameworkConfiguration` registry offering `UseFramework("MudBlazor")` / `RegisterFramework(...)`.
> None of it had a single consumer, the interface did not have the methods it was documented with,
> and `AddFormCraft()` has no options overload for any of those calls. All three types were deleted
> in #279. Do not reintroduce them: a contributor building against that description was building
> against something nothing calls.

## The seam

### 1. `IFieldRenderer` — which renderer claims a field

```csharp
public interface IFieldRenderer
{
    bool CanRender(Type fieldType, IFieldConfiguration<object, object> field);
    RenderFragment Render<TModel>(IFieldRenderContext<TModel> context);
}
```

`IFieldRendererService` picks the **first** registered renderer whose `CanRender` returns true.

### 2. `FieldRendererBase` — the base every shipped renderer uses

It implements `Render` for you: name the Razor component, and the base closes its generic arguments
over `TModel` (and the field's actual value type for a two-argument component), then passes the
`IFieldRenderContext<TModel>` in as `Context`.

```csharp
public class MyTextFieldRenderer : FieldRendererBase
{
    protected override Type ComponentType => typeof(MyTextFieldComponent<,>);

    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => fieldType == typeof(string);
}
```

Override `ResolveComponentType` for a component with more generic arguments, and
`AddComponentParameters` to pass extra parameters to it.

### 3. `FieldComponentBase<TModel, TValue>` — the component base

Supplies `Context`, `Value`/`ValueChanged`, `Label`, `HelpText`, `IsRequired` and the rest, and
implements `IFieldComponent<TModel>`. Each adapter layers its own presentation concerns on top —
`MudBlazorFieldComponentBase`, `FluentUIFieldComponentBase`.

### 4. Shared, UI-agnostic machinery in core

So that two adapters cannot drift apart re-implementing the same rule (#279):

- **`NativeRequired.Resolve(...)`** — whether a field carries the native required decoration, with
  an explicit `.WithNativeRequired(...)` winning over `IsRequired` in *both* directions.
- **`FieldBuilderExtensions.WithNativeRequired(...)`** — the typed builder that writes that attribute.
- **`DynamicFormValidator<TModel>`** — runs the configured validators against the `EditContext`. Set
  `ValidateCollections="false"` in an adapter that renders no collection/item-form UI.
- **`AdapterRegistration.EnsureSingleAdapter(...)`** — refuses a second adapter in one container.

## Registration order is the precedence rule

Because selection is first-match-wins, **configuration-driven renderers must be registered before
the generic type-based ones**. A string field carrying `.WithOptions(...)` would otherwise be
claimed by the plain text renderer, which matches on `typeof(string)` alone.

```csharp
public static IServiceCollection AddFormCraftMyFramework(this IServiceCollection services)
{
    // Refuse a container that already has a different adapter - selection is first-match-wins, so
    // two adapters render a form that is partly one framework and partly the other, silently.
    AdapterRegistration.EnsureSingleAdapter(services, "FormCraft.ForMyFramework");

    // Drop core's built-in renderers so this adapter's take precedence. Filter on the CORE
    // assembly: an application's own custom renderers must survive and keep their precedence.
    var coreAssembly = typeof(IFieldRenderer).Assembly;
    foreach (var descriptor in services
                 .Where(s => s.ServiceType == typeof(IFieldRenderer) &&
                             s.ImplementationType?.Assembly == coreAssembly)
                 .ToList())
    {
        services.Remove(descriptor);
    }

    services.AddScoped<IFieldRenderer, MySelectFieldRenderer>();   // configuration-driven first
    services.AddScoped<IFieldRenderer, MyTextFieldRenderer>();     // then type-based
    return services;
}
```

Consumers then call `AddFormCraft()` followed by the adapter's method:

```csharp
builder.Services.AddFormCraft();
builder.Services.AddFormCraftMudBlazor();   // or AddFormCraftFluentUI() - exactly one
```

`AddFormCraft()` registers core's built-in renderers only when no adapter has claimed the container
(`AdapterRegistration.IsAdapterRegistered`), so the two orders converge on the same result.

## Architecture benefits

1. **Separation of concerns**: UI framework code is isolated from core form logic.
2. **Extensibility**: a new adapter is a new assembly — no core changes.
3. **Type safety**: strong typing throughout the rendering pipeline.
4. **Testability**: renderers and components are testable independently.
5. **One implementation per rule**: shared behaviour lives in core rather than once per adapter.

## Custom renderers in an application

An application can register its own `IFieldRenderer` without writing a whole adapter. Register it
**before** calling the adapter's `AddFormCraft<Framework>()`: adapters only strip renderers from the
*core* assembly, so yours survives and keeps precedence over the adapter's.
