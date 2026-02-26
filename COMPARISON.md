# FormCraft Feature Comparison

This document provides a factual comparison of FormCraft with other popular Blazor form solutions. Each library has its strengths — this guide helps you choose the right tool for your project.

## Competitors Overview

| Library | Description | Approach |
|---------|-------------|----------|
| **Blazor EditForm** | Built-in Blazor form component from Microsoft | Manual markup with DataAnnotations |
| **Blazored.FluentValidation** | FluentValidation integration for Blazor EditForm | Validation-focused add-on for EditForm |
| **MudBlazor Forms** | MudBlazor's built-in form and input components | Component-based with Material Design |
| **FormCraft** | Type-safe fluent form builder for Blazor | Fluent API with automatic field rendering |

## Feature Comparison Matrix

| Feature | Blazor EditForm | Blazored.FluentValidation | MudBlazor Forms | FormCraft |
|---------|:-:|:-:|:-:|:-:|
| **Form Definition** | | | | |
| Fluent API configuration | - | - | - | Yes |
| Attribute-based form generation | - | - | - | Yes |
| Automatic field rendering | - | - | - | Yes |
| Manual markup required | Yes | Yes | Yes | Optional |
| **Type Safety** | | | | |
| Compile-time property binding | Partial (1) | Partial (1) | Partial (1) | Yes |
| Generic model constraints | - | - | - | Yes |
| Expression tree field references | - | - | - | Yes |
| IntelliSense for field config | - | - | Limited | Yes |
| **Validation** | | | | |
| DataAnnotations support | Yes | Yes | Yes | Yes |
| FluentValidation integration | - | Yes | - | Yes |
| Custom sync validators | Manual | Yes | Manual | Yes |
| Custom async validators | Manual | Yes | Manual | Yes |
| Cross-field validation | Manual | Yes | Manual | Yes |
| **Dynamic Forms** | | | | |
| Conditional field visibility | Manual | Manual | Manual | Built-in |
| Field dependencies | Manual | Manual | Manual | Built-in |
| Calculated field values | Manual | Manual | Manual | Built-in |
| Dynamic option loading | Manual | Manual | Manual | Built-in |
| **Layout & Rendering** | | | | |
| Multiple layout modes | Manual | N/A (2) | Manual | Built-in (4 modes) |
| Field groups with columns | Manual | N/A (2) | Manual | Built-in |
| Custom field renderers | N/A | N/A (2) | N/A | Yes |
| Material Design (MudBlazor) | - | - | Yes | Yes (3) |
| **Security** | | | | |
| Field-level encryption | - | - | - | Yes |
| CSRF protection | Manual | Manual | Manual | Built-in |
| Rate limiting | - | - | - | Built-in |
| Audit logging | - | - | - | Built-in |
| **Developer Experience** | | | | |
| Lines of code per form (4) | High | Medium | High | Low |
| Learning curve | Low | Low | Medium | Medium |
| Official documentation | Excellent | Good | Excellent | Good |
| Unit test coverage | N/A | Good | Excellent | 550+ tests |

**Notes:**
1. EditForm and MudBlazor use `@bind-Value` which provides some compile-time checking, but field configuration (labels, placeholders, validation messages) is done through markup strings, not typed expressions.
2. Blazored.FluentValidation is a validation library only — it does not handle layout or rendering.
3. FormCraft supports MudBlazor through the `FormCraft.ForMudBlazor` package.
4. "Lines of code per form" is relative — FormCraft's fluent API and automatic rendering typically require fewer lines for forms with many fields.

## Code Comparison

### Simple Registration Form

**Blazor EditForm (approx. 45 lines)**

```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="form-group">
        <label for="firstName">First Name</label>
        <InputText id="firstName" @bind-Value="model.FirstName" class="form-control" />
        <ValidationMessage For="@(() => model.FirstName)" />
    </div>

    <div class="form-group">
        <label for="lastName">Last Name</label>
        <InputText id="lastName" @bind-Value="model.LastName" class="form-control" />
        <ValidationMessage For="@(() => model.LastName)" />
    </div>

    <div class="form-group">
        <label for="email">Email</label>
        <InputText id="email" @bind-Value="model.Email" class="form-control" />
        <ValidationMessage For="@(() => model.Email)" />
    </div>

    <div class="form-group">
        <label for="age">Age</label>
        <InputNumber id="age" @bind-Value="model.Age" class="form-control" />
        <ValidationMessage For="@(() => model.Age)" />
    </div>

    <button type="submit" class="btn btn-primary">Register</button>
</EditForm>
```

**MudBlazor Forms (approx. 35 lines)**

```razor
<MudForm @ref="form" @bind-IsValid="@isValid">
    <MudTextField @bind-Value="model.FirstName" Label="First Name"
                  Required="true" RequiredError="First name is required" />
    <MudTextField @bind-Value="model.LastName" Label="Last Name"
                  Required="true" RequiredError="Last name is required" />
    <MudTextField @bind-Value="model.Email" Label="Email"
                  InputType="InputType.Email"
                  Required="true" RequiredError="Email is required" />
    <MudNumericField @bind-Value="model.Age" Label="Age"
                     Min="18" Max="120" />
    <MudButton OnClick="HandleSubmit" Disabled="@(!isValid)"
               Variant="Variant.Filled" Color="Color.Primary">
        Register
    </MudButton>
</MudForm>
```

**FormCraft Fluent API (approx. 15 lines)**

```csharp
var formConfig = FormBuilder<UserRegistration>.Create()
    .AddRequiredTextField(x => x.FirstName, "First Name")
    .AddRequiredTextField(x => x.LastName, "Last Name")
    .AddEmailField(x => x.Email)
    .AddNumericField(x => x.Age, "Age", min: 18, max: 120)
    .Build();
```

```razor
<FormCraftComponent TModel="UserRegistration"
                   Model="@model"
                   Configuration="@formConfig"
                   OnValidSubmit="@HandleSubmit"
                   ShowSubmitButton="true" />
```

**FormCraft Attribute-Based (approx. 5 lines of config)**

```csharp
// Model defined once with attributes — form generated automatically
var formConfig = FormBuilder<UserRegistration>.Create()
    .AddFieldsFromAttributes()
    .Build();
```

### Form with Dynamic Dependencies

This is where FormCraft's advantages become most apparent. With traditional approaches, managing field dependencies requires significant manual wiring.

**Traditional Approach (EditForm / MudBlazor)**

```csharp
// Requires manual event handlers, state management, and re-rendering logic
// for each dependent field — typically 50-100+ lines per dependency chain
```

**FormCraft**

```csharp
var formConfig = FormBuilder<OrderForm>.Create()
    .AddSelectField(x => x.ProductType, "Product Type", productOptions)
    .AddSelectField(x => x.ProductModel, "Model",
        dependsOn: x => x.ProductType,
        optionsProvider: (type) => GetModelsForType(type))
    .AddNumericField(x => x.Quantity, "Quantity", min: 1)
    .AddField(x => x.TotalPrice, "Total Price")
        .IsReadOnly()
        .DependsOn(x => x.ProductModel, x => x.Quantity)
        .WithValueProvider((model, _) => CalculatePrice(model))
    .Build();
```

## Key Differentiators

### 1. Type-Safe Fluent API

FormCraft uses expression trees (`x => x.Property`) throughout, which means:
- Refactoring-safe property references (rename a property and all form configs update)
- Full IntelliSense support in your IDE
- Compile-time errors for invalid field configurations
- No string-based property names that can silently break

### 2. Automatic Field Rendering

Unlike other solutions where you manually write markup for each field, FormCraft:
- Automatically selects the appropriate input component based on the property type
- Applies labels, placeholders, and validation from the configuration
- Handles layout and grouping without manual CSS/HTML
- Supports attribute-based generation for zero-config forms

### 3. Built-in Field Dependencies

FormCraft treats field dependencies as a first-class concept:
- Declarative dependency chains with `.DependsOn()`
- Automatic value recalculation via `.WithValueProvider()`
- Conditional visibility via `.VisibleWhen()`
- Dynamic option loading based on other field values

### 4. MudBlazor Integration

The `FormCraft.ForMudBlazor` package provides:
- Material Design components rendered automatically
- Consistent styling without manual component selection
- Access to MudBlazor's theming system
- Custom renderers that leverage MudBlazor components

### 5. Built-in Security Features

FormCraft includes security features that other form libraries leave to the developer:
- Field-level encryption for sensitive data (SSN, credit cards)
- CSRF protection with anti-forgery tokens
- Rate limiting to prevent form spam
- Audit logging for compliance requirements

## When to Use Each Solution

| Use Case | Recommended Solution |
|----------|---------------------|
| Simple forms (1-5 fields), no dependencies | **Blazor EditForm** — minimal setup, built into the framework |
| Complex validation rules with FluentValidation | **Blazored.FluentValidation** — purpose-built for this scenario |
| Material Design UI with manual form layout control | **MudBlazor Forms** — full control over component placement |
| Many forms with similar patterns across the app | **FormCraft** — reuse configurations, reduce boilerplate |
| Forms with dynamic field dependencies | **FormCraft** — built-in dependency management |
| Rapid prototyping with model-first approach | **FormCraft** — attribute-based generation |
| Security-sensitive forms (PII, financial data) | **FormCraft** — built-in encryption, CSRF, rate limiting |
| Large enterprise apps with dozens of forms | **FormCraft** — consistency, type safety, maintainability |

## Combining Solutions

FormCraft does not require an all-or-nothing adoption. You can:

- Use **Blazor EditForm** for simple, one-off forms
- Use **FormCraft** for complex or repeated form patterns
- Use **Blazored.FluentValidation** alongside FormCraft's own validation
- Use **MudBlazor components** directly within FormCraft custom renderers

## Summary

FormCraft is best suited for projects that:
- Have many forms with similar structures
- Need dynamic, data-driven form behavior
- Value type safety and refactoring support
- Want to reduce form-related boilerplate
- Require built-in security features

For simple forms or projects that prefer full manual control over markup, the built-in Blazor EditForm or MudBlazor's form components remain excellent choices.
