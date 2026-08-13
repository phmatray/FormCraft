# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

### Building the Project
```bash
# Restore dependencies and build
dotnet restore
dotnet build

# Build in Release mode
dotnet build --configuration Release

# Build with warnings as errors (for CI/CD validation)
dotnet build /p:TreatWarningsAsErrors=true

# Create local NuGet package
./pack-local.sh  # macOS/Linux - Creates packages in ./nupkg/
./pack-local.ps1 # Windows
```

### Running Tests

The test projects are **Microsoft.Testing.Platform** hosts, not VSTest. That changes how you filter —
see the warning below the commands, which is the part that costs time if you skip it.

```bash
# Run everything — ~1,550 tests across the three test projects (approximate on purpose: the
# exact figure drifts with every merge, and a stale precise number reads as authoritative)
dotnet test -c Release

# Run one test project. `-c Release` throughout: CI runs Release, and the host path
# below is a Release path — mixing configurations is how you end up running one build
# and inspecting another.
dotnet test FormCraft.UnitTests/FormCraft.UnitTests.csproj -c Release
dotnet test FormCraft.ForMudBlazor.UnitTests/FormCraft.ForMudBlazor.UnitTests.csproj -c Release
dotnet test FormCraft.ForFluentUI.UnitTests/FormCraft.ForFluentUI.UnitTests.csproj -c Release

# Run one class — everything after `--` is forwarded to the test host.
# Always name the .csproj: run solution-wide, the filter is applied to all three
# assemblies and the two that match nothing report Failed!, exiting 1.
dotnet test FormCraft.UnitTests/FormCraft.UnitTests.csproj -c Release \
  -- --filter-class FormCraft.UnitTests.Ci.GitignoreTests

# Same filter without the build step (~200ms). Run `dotnet build -c Release` first,
# or you are testing a stale binary. On Windows the host is `...\FormCraft.UnitTests.exe`.
FormCraft.UnitTests/bin/Release/net10.0/FormCraft.UnitTests \
  --filter-class FormCraft.UnitTests.Ci.GitignoreTests
```

Filters: `--filter-class`, `--filter-method`, `--filter-namespace`, `--filter-trait`, `--filter-uid`,
`--filter-query`, plus a `--filter-not-*` counterpart for class/method/namespace/trait. `*` wildcards
work at either end, and the simple filters cannot be combined with `--filter-query`.
`--filter-method` wants the **fully-qualified** name (`<namespace>.<class>.<method>`) — a bare method
name matches nothing. `--filter-trait` is useless here: no test in this repo carries a `[Trait]`.

⛔ **The VSTest spellings are silently ignored here.** Passing `--filter` to `dotnet test` (rather
than after `--`) forwards it as an MSBuild property that Microsoft.Testing.Platform discards with a
lone `MTP0001` warning — **the whole suite runs** while the command looks filtered, and exits `0`.
The same applies to `--collect:"XPlat Code Coverage"`, which additionally writes no coverage file at
all; coverage is not currently wired up for these projects (no MTP coverage extension is referenced),
so there is no working substitute to reach for. Filtering by `Category=…` never worked either: no
test in this repo carries a `[Trait]`. The MSBuild-property spellings (`-p:VSTestTestCaseFilter=…`,
`-p:VSTestCollect=…`) are inert for the same reason. `FormCraft.UnitTests/Ci/ClaudeMdTestCommandsTests`
fails — it is a unit test, so `dotnet test` catches this, **not** `dotnet build` — if any of these
return to this file.

⚠️ **A green-looking run may have run nothing, and the two paths fail differently.**

- **Summary lines are printed *per assembly*, with no aggregate.** A solution-wide filtered run
  prints `Passed! … Total: 6` for the assembly that matched and `Failed! … Total: 0` for the two
  that did not — exit `1`. "I saw a `Passed!` line" therefore proves nothing on its own: confirm
  **every** assembly reported, or read the exit code of an **unpiped** run.
- **`$?` is only meaningful unpiped.** `| tail` / `| grep` replaces it with the pipe's `0`.
- **Mistyped flag, direct host** → `Unknown option '--…'` plus the full `--help`, nothing runs, and
  **no summary line at all** (so `grep 'Failed!'` reads it as green); exit `5`.
- **Mistyped flag, `dotnet test`** → the diagnostic never reaches stdout. You get only
  `error run failed: Tests failed: '<path>/TestResults/<assembly>_net10.0_arm64.log'` and exit `1` —
  wording that blames the tests for what is an argument error. **Read that log before debugging any
  source.**
- **Filter matching nothing** → `Zero tests ran` (direct host, exit `8`) or `Failed! … Total: 0`
  (`dotnet test`, exit `1`). Check for `Total: 0` before hunting a phantom regression.

**This block is a summary.** The authoritative version — the full flag list, both failure paths, and
the measurements behind every claim — is [`.claude/skills/repo-profile.md`](.claude/skills/repo-profile.md)
→ *Build & test* → *Single-suite filter*. Where the two disagree, **the profile wins and this block is
the stale one**; keep corrections there and re-summarise here rather than growing a second copy.

### Running the Demo Application
```bash
cd FormCraft.DemoBlazorApp
dotnet run
# Navigate to https://localhost:5001 (or http://localhost:5000)
```

### NUKE Build System
The project uses NUKE for sophisticated build automation:
```bash
# Run full build pipeline (macOS/Linux)
./build.sh

# Run full build pipeline (Windows)
./build.ps1

# Available NUKE targets:
# - Clean: Cleans build outputs
# - Restore: Restores NuGet packages
# - Compile: Builds the solution
# - Test: Runs all unit tests
# - Pack: Creates NuGet packages
# - Continuous: Test + Pack, then PublishIfNeeded (publishes only when a version tag is checked out)
#
# There is no changelog target: CHANGELOG.md is generated and owned by release-please.
```

## High-Level Architecture

### Solution Structure
```
FormCraft/                      # Core library (framework-agnostic)
├── Builders/                   # Fluent API builders
│   ├── FormBuilder.cs         # Main entry point
│   ├── FieldBuilder.cs        # Individual field configuration
│   └── FieldGroupBuilder.cs   # Field grouping and layout
├── Configuration/              # Configuration models
├── Rendering/                  # Rendering pipeline
│   ├── IFieldRenderer.cs      # Renderer contract
│   └── FieldRendererService.cs # Renderer registry
├── Validation/                 # Validation system
│   └── IFieldValidator.cs     # Validator contract
├── Security/                   # Security features (v2.0.0+)
│   ├── IEncryptionService.cs  # Field encryption
│   └── ICsrfTokenService.cs   # CSRF protection
└── Extensions/                 # Extension methods

FormCraft.ForMudBlazor/         # MudBlazor UI implementation
├── Renderers/                  # MudBlazor-specific renderers
└── Services/                   # UI framework services

FormCraft.DemoBlazorApp/        # Interactive demo application
FormCraft.UnitTests/            # Core library test suite (560+ tests)
FormCraft.ForMudBlazor.UnitTests/ # MudBlazor integration tests (47 tests)
build/                          # NUKE build automation
```

### Target Frameworks
- **net8.0** and **net10.0** — multi-targeting for .NET 8 and .NET 10. All three shipping projects
  (`FormCraft`, `FormCraft.ForMudBlazor`, `FormCraft.ForFluentUI`) declare
  `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`; the demo app is `net10.0` only.
  ⛔ There is **no net9.0 target** — this file claimed one until #285. The csproj files are the
  authority here, so verify against them before re-adding a framework to this list; a demo page
  advertising `net8.0 · net10.0` is correct, not stale.

### Core Design Patterns

#### 1. Fluent Builder Pattern (Primary Architecture)
The entire API is built around method chaining with immutable configuration:
```csharp
FormBuilder<TModel>.Create()
    .AddField(x => x.Property, field => field.ConfigureField())
    .AddFieldGroup(group => group.ConfigureGroup())
    .WithLayout(FormLayout.Grid)
    .WithSecurity(security => security.ConfigureSecurity())
    .Build() // Returns immutable IFormConfiguration<TModel>
```

**Key Builder Classes:**
- `FormBuilder<TModel>` - Root builder, entry point via `.Create()`
- `FieldBuilder<TModel, TValue>` - Configures individual fields
- `FieldGroupBuilder<TModel>` - Groups fields with layout options
- `SecurityBuilder<TModel>` - Security features configuration (encryption, CSRF, rate limiting)

#### 2. Strategy Pattern (Field Rendering)
Pluggable rendering system with type-based renderer selection:
```csharp
public interface IFieldRenderer
{
    bool CanRender(Type fieldType, IFieldConfiguration<object, object> field);
    RenderFragment Render<TModel>(IFieldRenderContext<TModel> context);
}
```

**Renderer Registration:**
- Default renderers registered in DI container
- Custom renderers via `.WithCustomRenderer()`
- Priority-based selection when multiple renderers match

#### 3. Command Pattern (Validation)
Async validation with command pattern:
```csharp
public interface IFieldValidator<TModel, TValue>
{
    Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services);
}
```

**Built-in Validators:**
- `RequiredValidator<TModel, TValue>`
- `CustomValidator<TModel, TValue>`
- `AsyncValidator<TModel, TValue>`
- FluentValidation integration via `DynamicFormValidator`

#### 4. Observer Pattern (Field Dependencies)
Reactive field updates based on dependencies:
```csharp
.AddField(x => x.TotalPrice)
    .DependsOn(x => x.Quantity, x => x.Price)
    .WithValueProvider((model, services) => model.Quantity * model.Price)
    .WithVisibilityProvider(model => model.Quantity > 0)
```

**Dependency Types:**
- Value dependencies - Auto-calculate field values
- Visibility dependencies - Show/hide fields conditionally
- Validation dependencies - Conditional validation rules

#### 5. Adapter Pattern (UI Framework Integration)
Framework-agnostic core with UI-specific adapters. **The seam is `FieldRendererBase` plus
precedence-ordered DI registration — there is no adapter *interface*.** An adapter is just an
assembly that registers its own `IFieldRenderer`s; two of them ship (`FormCraft.ForMudBlazor`,
`FormCraft.ForFluentUI`) and both were built on exactly this.

⛔ There used to be an `IUIFrameworkAdapter` documented here with a `RenderField`/`RenderForm` shape.
That interface had neither method and — across 8 reference sites — **not one consumer**; it was
deleted in #279 along with `FrameworkAgnosticFieldRenderer` and `UIFrameworkConfiguration`. Do not
reintroduce it: a contributor who builds against it is building against something nothing calls.

A renderer names the component to render and says which fields it claims:
```csharp
public class MyTextFieldRenderer : FieldRendererBase
{
    protected override Type ComponentType => typeof(MyTextFieldComponent<,>);   // closed over TModel/TValue

    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => fieldType == typeof(string);
}
```

**Registration order is the precedence rule**, because `IFieldRendererService` picks the *first*
renderer whose `CanRender` matches. Configuration-driven renderers (select, LOV, lookup,
autocomplete, file upload) must therefore be registered *before* the generic type-based ones, or a
string field carrying options ends up in the plain text renderer. `AddFormCraft<Framework>()` also
strips core's built-in renderers — only those from the core assembly, so an application's own custom
renderers survive and keep precedence.

Components derive from **`FieldComponentBase<TModel, TValue>`** (core), which supplies `Context`,
`Value`/`ValueChanged`, `Label`, `IsRequired` and the rest; each adapter adds its own presentation
layer on top (`MudBlazorFieldComponentBase`, `FluentUIFieldComponentBase`). Shared, UI-agnostic
adapter machinery lives in core so both adapters read one implementation — `NativeRequired.Resolve`,
`.WithNativeRequired(...)`, `DynamicFormValidator<TModel>` and
`AdapterRegistration.EnsureSingleAdapter` (#279).

**One adapter per container.** Both `AddFormCraftMudBlazor()` and `AddFormCraftFluentUI()` call
`AdapterRegistration.EnsureSingleAdapter`, so whichever runs second throws — the rule is symmetric
because it lives in core rather than in one of the two packages that need it.

### Key Abstractions and Extension Points

#### Configuration Abstractions
- `IFormConfiguration<TModel>` - Complete immutable form configuration
- `IFieldConfiguration<TModel, TValue>` - Individual field settings
- `IFieldGroupConfiguration<TModel>` - Group layout and settings
- `IFormSecurity` - Security configuration

#### Rendering Pipeline
1. `IFieldRendererService` - Central rendering coordinator
2. `IFieldRenderContext<TModel>` - Rendering context with model and callbacks
3. `ICustomFieldRenderer<TValue>` - Base for custom renderers
4. `CustomFieldRendererBase<T>` - Simplified custom renderer base class

#### Validation System
- `IFieldValidator<TModel, TValue>` - Core validation contract
- `ValidationResult` - Validation outcome (IsValid, ErrorMessage)
- `DynamicFormValidator` - FluentValidation integration component
- Validators can be sync or async

#### Security Features (v2.0.0+)
```csharp
.WithSecurity(security => security
    .EncryptField(x => x.SSN, algorithm: "AES256")
    .EncryptField(x => x.CreditCard)
    .EnableCsrfProtection()
    .WithRateLimit(maxRequests: 5, window: TimeSpan.FromMinutes(1))
    .EnableAuditLogging(logger => logger.LogToDatabase()))
```

### Important Conventions

#### Fluent API Design Rules
- All builder methods return `this` for chaining
- Configuration is immutable after `.Build()`
- Method naming: `Add*` (add items), `With*` (configure), `Enable*` (features)
- No side effects in builder methods

#### Type Safety and Expression Trees
- Heavy use of generics for compile-time safety
- Expression trees for property binding: `x => x.Property`
- Strong typing throughout: `FieldBuilder<TModel, TValue>`
- No magic strings for property names

#### Validation Behavior
- **The convention governs browser *constraint validation*, not accessibility annotations** (#199).
  Those are different things, and conflating them is what left required fields silent to screen
  readers. `Required()` still routes validation server-side and the browser still runs none — but a
  required field must be *identified*, which is WCAG 2.1 3.3.2 (Level A)
- `Required()` therefore DOES set MudBlazor's `Required` on both render paths, which emits
  `aria-required="true"`, the `*` asterisk, and the HTML5 `required` attribute. MudBlazor derives all
  three from one flag and they cannot be separated: `MudInput` splats `UserAttributes` and then writes
  its own `required`/`aria-required` afterwards, so a caller-supplied value is always overwritten
  (measured on 9.8.0). The HTML5 attribute is inert here because the form renders `novalidate`.
  ⛔ Do not "restore the convention" by dropping this — that is #190, which #199 reversed, and it
  reintroduces a Level A accessibility failure. `.WithNativeRequired(false)` is the per-field opt-out
- Browser validation disabled via a `novalidate` attribute **rendered on the form** by
  `FormCraftComponent` (#206). It is a real attribute in the markup, so it applies during
  prerender/SSR, targets this component's own form rather than the first one on the page, and needs
  no JavaScript. ⛔ Do not reintroduce the `JSRuntime.InvokeVoidAsync("eval", …)` version: it marked
  `document.querySelector('form')` — the wrong form on any page with another form above it — never
  ran on the server pass, failed silently, and was blocked outright by a strict CSP
- All validation through FluentValidation
- Validation messages from server, not browser
- MudBlazor components DO set `Required` for a `.Required(...)` field since #199 (see above), on
  **every** field type that can carry it: text, numeric, date, select, multi-select, autocomplete,
  lookup, LOV and boolean, on both render paths. ⛔ Keep it uniform when adding a field type. Once
  required fields carry an asterisk, absence stops meaning "not annotated" and starts meaning
  "optional", so a new renderer that skips this actively mis-signals rather than merely omitting
- **Checkboxes take a different route.** `MudCheckBox`/`MudSwitch` emit no `aria-required`, so
  FormCraft passes it via `UserAttributes` — which lands there because nothing downstream re-emits
  it. Do **not** copy that trick to `MudInput`-based fields: there MudBlazor's own later write always
  wins, which is the whole reason `Required` had to be the mechanism (see `EffectiveNativeRequired`)
- **File upload is covered too, but NOT via `Required` on `MudFileUpload`** (#262). Its
  `<input type="file">` carries `tabindex="-1"` at `opacity-0` behind a custom drop zone, so
  annotating that input satisfies a DOM assertion while reaching no user who navigates by focus.
  Both upload components mark the requirement on two reachable channels instead: a visible `*` in
  the field's own `<MudText>` label, and `aria-describedby` on the **Browse** `MudButton` pointing
  at a `mud-sr-only` description. The rule lives in `MudBlazorFileUploadComponentBase` and the
  markup in `FileUploadRequiredMarker`/`FileUploadRequiredHint`, so the single- and multiple-file
  components cannot drift — centralising only the values still leaves two copies of the markup
- ⛔ **Do not "also bind `Required`" on `MudFileUpload` as belt-and-braces.** It was tried under #262
  and reverted with a measurement: `MudFormComponent` raises its own `RequiredError` once the flag is
  set, and with no cascaded `EditContext` — a standalone `IFieldRendererService.RenderField`, which
  this suite exercises — clearing a required upload rendered `mud-input-error` plus helper text
  reading `"Required"`, MudBlazor's wording rather than the developer's. Pinned by
  `AriaRequiredTests.Clearing_A_Standalone_Required_Upload_Should_Not_Surface_MudBlazors_Own_Error`.
  Note `MudFileUpload.Error`/`ErrorText` stay clean because FormCraft passes `ErrorText=""` as a
  *parameter* while MudBlazor writes its own internal state — so assert the **rendered DOM**, not
  those properties, or the test proves nothing
- **The upload asterisk is a text node, not MudBlazor's CSS `::after`.** MudBlazor's only rule is
  `.mud-input-control.mud-input-required > .mud-input-control-input-container > .mud-input-label::after`,
  which never matches FormCraft's `span`. So the marker carries its own
  `formcraft-required-marker` class — select and restyle **that**, and don't tell users to restyle
  `.mud-input-required` for upload fields
- **The hint id must stay unique per rendered instance.** `formcraft-{FieldName}-required-{guid8}`:
  the field name alone is not unique in a document, because item fields render through these very
  components since #203 (one hint per row), two forms over one model collide the same way, and two
  nested fields can share a member name. A test using two *different* fields cannot catch this —
  different names never collide; the real case is the same field rendered twice
- **A control that unmounts *or disables* itself on activation must move focus deliberately** —
  otherwise the element the keyboard user is standing on stops being focusable, focus falls to
  `<body>`, and the next <kbd>Tab</kbd> restarts from the top of the document (WCAG 2.1 **2.4.3
  Focus Order**, Level A). Both variants count: an `@if` over the value the handler mutates
  (upload **Clear**/chip close, collection **delete**/**Add**), and a `Disabled` binding the handler
  can make true (collection **move up/down** at the ends — browsers drop focus from a
  newly-disabled element). Every one of them routes through **`FocusRestore.FocusSafelyAsync`**
  (#281, #318). Targets: Clear and chip-close → the field's **Browse** button, because it carries
  #262's `aria-describedby` so the requirement is announced exactly when removal makes the field
  unsatisfied; row delete → the delete button taking the vacated slot, else the previous row's, else
  **Add**, else the collection header; **Add** → the new row's header, *not* its delete button
  (<kbd>Enter</kbd> there would undo the add) and not a field (they render through
  `IFieldRendererService` and expose no reference); a move → the same row's still-enabled move
  button, so focus follows the *item* rather than sitting on an index that now controls a different
  one. ⚠️ **Only the helper is shared** — each `@ref` and call site is written per component, and
  the null guard makes a dropped `@ref` silent, so a new control needs its own focus test.
  ⛔ **Don't narrow the catch list**: the action has already succeeded, so a failed focus must stay a
  no-op. `JSException` is the likely one — `domWrapper.focus` raises it for an element that has left
  the DOM, which `OnValueChanged` can cause between the mutation and the awaited interop call.
  Losing that catch escapes the click handler and tears down a Server circuit;
  `A_Failing_Focus_Call_Should_Not_Break_A_*_Clear` and `FocusRestoreTests` pin it
- ⛔ **A `@ref` on a *component* is captured once, when that component is created — it is NOT re-run
  on later renders.** So a per-index reference store must **not** be cleared each render to prune
  stale entries: doing so permanently loses the references for rows that were merely retained, and
  every subsequent focus falls through to the next target in the chain (measured under #318 —
  removals silently focused **Add**). Let the entries outlive their rows and decide from what is
  rendered *now* instead: bounds-check the index against `Items.Count` **and** re-evaluate the
  markup's own gate (`CanRemove && !HasReachedMin`), since reaching `MinItems` unmounts every row's
  delete button at once. Element references (`@ref` on plain HTML) *are* re-captured each render —
  the two behave differently, which is why the row header fallback is an `ElementReference`
- **Move focus from `OnAfterRenderAsync`, not from the handler.** The row you are aiming at may not
  exist, or may not be at that index, until the next render batch is applied — reading the captures
  inside the handler hands back the pre-action state. Set a pending-index field, act on it after the
  render (`_focusAfterRemovalFrom` / `_focusRowAfterRender` in `CollectionFieldComponent`)
- **Asserting focus in bUnit: assert the interop call, not DOM state.** bUnit models no real focus.
  `MudButton.FocusAsync()` records `Blazor._internal.domWrapper.focus` with the target
  `ElementReference` as `Arguments[0]` (measured on bUnit 2.9.0 / MudBlazor 9.8.0). MudButton exposes
  **no public** `ElementReference` — it lives in a private `MudBaseButton._elementReference` — and
  bUnit renders `blazor:elementReference` **empty**, so to say *which* button was focused, learn its
  id through the public API: call `FocusAsync()` on the candidate and read the id back off the
  recording (the id survives the clear re-render). ⛔ Don't reflect into MudBlazor's private field;
  it breaks on any patch release. The helpers live once on **`FocusAssertingTestBase`**
  (`TestSupport/`) — `FocusCount()`, `LastFocusedElementId()`, `LearnElementIdAsync(...)` (takes
  `MudBaseButton`, so it covers `MudIconButton` too) and `FailTheFocusInterop()`. See
  `FileUploadClearFocusTests`, `CollectionFocusTests`, `FocusRestoreTests`
- **`Loose` JSInterop makes focus always succeed, so a "does not throw" test proves nothing** unless
  it makes the call fail — use `FailTheFocusInterop()`. Without it the catch block has zero coverage,
  which is exactly how a missing `catch (JSException)` shipped under #281

#### Testing Patterns
```csharp
// Arrange-Act-Assert pattern with Shouldly assertions
[Fact]
public void MethodName_Should_ExpectedBehavior_When_Condition()
{
    // Arrange
    var builder = FormBuilder<TestModel>.Create();

    // Act
    var result = builder.AddField(x => x.Name);

    // Assert
    result.ShouldBeSameAs(builder);
}
```

#### MudBlazor Component Testing (bUnit)
```csharp
// Use MudBlazorTestBase for component tests
public class MyTests : MudBlazorTestBase  // Inherits from BunitContext
{
    [Fact]
    public void Component_Should_Render_Field()
    {
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        component.FindComponent<MudTextField<string>>().ShouldNotBeNull();
    }
}
```

### Common Development Patterns

#### Adding a Custom Field Renderer
```csharp
// 1. Create renderer class
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    protected override RenderFragment RenderField(IFieldRenderContext<string> context)
    {
        return builder => {
            builder.OpenComponent<MudColorPicker>(0);
            builder.AddAttribute(1, "Value", context.Value);
            builder.AddAttribute(2, "ValueChanged", context.ValueChanged);
            builder.CloseComponent();
        };
    }
}

// 2. Register globally in DI
services.AddFormCraft(options => {
    options.RegisterRenderer(new ColorPickerRenderer());
});

// 3. Or use inline
.WithCustomRenderer(new ColorPickerRenderer())
```

#### Creating Reusable Field Configurations
```csharp
public static class FormExtensions
{
    public static FormBuilder<TModel> AddEmailField<TModel>(
        this FormBuilder<TModel> builder,
        Expression<Func<TModel, string>> propertyExpression)
        where TModel : new()
    {
        return builder.AddField(propertyExpression, field => field
            .WithLabel("Email Address")
            .WithPlaceholder("user@example.com")
            .WithInputType("email")
            .Required("Email is required")
            .WithValidator(new EmailValidator<TModel>()));
    }
}
```

#### Implementing Field Dependencies
```csharp
// Conditional visibility
.AddField(x => x.State)
    .DependsOn(x => x.Country)
    .WithVisibilityProvider(model => model.Country == "USA")

// Calculated values
.AddField(x => x.Total)
    .DependsOn(x => x.Quantity, x => x.Price, x => x.TaxRate)
    .WithValueProvider((model, _) => 
        model.Quantity * model.Price * (1 + model.TaxRate))
    .ReadOnly()
```

#### Form Templates
```csharp
// Use predefined templates
var form = FormTemplates.CreateLoginForm<LoginModel>();
var form = FormTemplates.CreateRegistrationForm<UserModel>();

// Create custom template
public static class MyTemplates
{
    public static FormBuilder<T> CreateWizardForm<T>() where T : new()
    {
        return FormBuilder<T>.Create()
            .WithLayout(FormLayout.Wizard)
            .WithNavigation(nav => nav.EnableStepIndicator());
    }
}
```

### Advanced Features

#### Security Configuration
```csharp
.WithSecurity(security => security
    // Field-level encryption
    .EncryptField(x => x.SSN)
    .EncryptField(x => x.CreditCard, algorithm: "AES256")
    
    // CSRF protection
    .EnableCsrfProtection()
    .WithCsrfTokenProvider(customProvider)
    
    // Rate limiting
    .WithRateLimit(5, TimeSpan.FromMinutes(1))
    
    // Audit logging
    .EnableAuditLogging()
    .WithAuditLogger(customLogger))
```

#### Field Groups with Layouts
```csharp
.AddFieldGroup(group => group
    .WithGroupName("Contact Information")
    .WithColumns(2)
    .ShowInCard(elevation: 2)
    .Collapsible(defaultExpanded: true)
    .AddField(x => x.Email)
    .AddField(x => x.Phone)
    .AddField(x => x.Address, field => field.FullWidth()))
```

#### Async Operations
```csharp
// Async validation
.WithAsyncValidator(async (value, services) => {
    var api = services.GetRequiredService<IApiService>();
    var isUnique = await api.CheckUniqueAsync(value);
    return isUnique 
        ? ValidationResult.Success()
        : ValidationResult.Error("Value must be unique");
})

// Async value provider
.WithAsyncValueProvider(async (model, services) => {
    var api = services.GetRequiredService<IApiService>();
    return await api.GetDefaultValueAsync(model.Id);
})
```

### Versioning and Release Process
- **Versioning**: MinVer derives the version from the git tag. No file records it — do **not** add a
  `<Version>` element, MinVer's targets override it.
- **Release**: owned by [release-please](https://github.com/googleapis/release-please). It keeps a
  release PR open against `dev`; merging that PR tags `vX.Y.Z`, creates the GitHub Release, and
  publishes both packages in the same workflow run. **Never tag by hand** — no workflow is triggered
  by `on: push: tags` any more, so a hand-pushed tag silently produces nothing.
- **Changelog**: `CHANGELOG.md` is generated by release-please. **Never hand-edit it**, and never add
  a generator for it — a second writer would rewrite the file out from under the open release PR.
  (git-cliff and `cliff.toml` were removed for exactly this reason.)
- **Commits**: Follow conventional commits (feat:, fix:, docs:, test:, refactor:). PRs are
  squash-merged, so **the PR title is the commit release-please parses** — `pr-title-lint.yml`
  enforces it.
- **CI/CD**: GitHub Actions. `ci.yml` and `continuous.yml` build/test/pack only;
  `release-please.yml` is the only workflow that publishes, via NuGet Trusted Publishing (OIDC).