using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a FormCraft configuration as a Fluent UI form.
/// </summary>
/// <remarks>
/// Deliberately shares its type name with <c>FormCraft.ForMudBlazor.FormCraftComponent&lt;TModel&gt;</c>
/// so switching adapters is a namespace change and a DI call, and nothing else. The two packages are
/// mutually exclusive - <c>AddFormCraftFluentUI()</c> refuses to register alongside the MudBlazor
/// adapter - so the shared name can never be ambiguous in a working configuration.
/// </remarks>
/// <typeparam name="TModel">The form's model type.</typeparam>
public partial class FormCraftComponent<TModel> where TModel : new()
{
    /// <summary>The model instance the form edits.</summary>
    [Parameter]
    public TModel Model { get; set; } = new();

    /// <summary>The form configuration produced by <c>FormBuilder&lt;TModel&gt;.Build()</c>.</summary>
    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    /// <summary>Invoked with the model once validation passes on submit.</summary>
    [Parameter]
    public EventCallback<TModel> OnValidSubmit { get; set; }

    /// <summary>Invoked whenever a field's value changes, with the field name and new value.</summary>
    [Parameter]
    public EventCallback<(string fieldName, object? value)> OnFieldChanged { get; set; }

    /// <summary>Whether the built-in submit button is rendered. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool ShowSubmitButton { get; set; } = true;

    /// <summary>The submit button's label.</summary>
    [Parameter]
    public string SubmitButtonText { get; set; } = "Submit";

    /// <summary>The submit button's label while <see cref="IsSubmitting"/> is set.</summary>
    [Parameter]
    public string SubmittingText { get; set; } = "Submitting...";

    /// <summary>Disables the submit button and shows <see cref="SubmittingText"/>.</summary>
    [Parameter]
    public bool IsSubmitting { get; set; }

    /// <summary>CSS class applied to the submit button.</summary>
    [Parameter]
    public string? SubmitButtonClass { get; set; }

    /// <summary>Content rendered above the form element.</summary>
    [Parameter]
    public RenderFragment? BeforeForm { get; set; }

    /// <summary>Content rendered below the form element.</summary>
    [Parameter]
    public RenderFragment? AfterForm { get; set; }

    /// <summary>Invoked with the <see cref="EditContext"/> once it has been created.</summary>
    [Parameter]
    public EventCallback<EditContext> OnEditContextCreated { get; set; }

    private EditContext? _editContext;
    private DynamicFormValidator<TModel>? _validator;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// Thrown when the configuration carries <c>.WithSecurity(...)</c> settings, which this adapter
    /// does not yet enforce.
    /// </exception>
    protected override async Task OnInitializedAsync()
    {
        // ⛔ Fail closed. The MudBlazor container enforces WithSecurity(...) - rate limiting, CSRF
        // generation and validation, audit logging - in HandleSubmit before the submit handler
        // runs. This adapter implements none of it yet, and rendering the form anyway would accept
        // unlimited submissions with no CSRF check and no audit trail, silently: no exception, no
        // warning, and a form that LOOKS protected because the configuration says so.
        //
        // A security feature that quietly does nothing is worse than one that is absent, so an
        // unsupported configuration is refused outright rather than ignored. Removing this guard
        // without implementing enforcement re-opens exactly that hole.
        if (Configuration?.Security is not null)
        {
            throw new NotSupportedException(
                "FormCraft.ForFluentUI does not yet enforce .WithSecurity(...) (rate limiting, CSRF " +
                "protection, audit logging, field encryption). Rendering this form would silently " +
                "drop those protections. Use FormCraft.ForMudBlazor for forms that need them, or " +
                "remove the security configuration. Tracked on issue #260.");
        }

        if (Model is not null)
        {
            _editContext = new EditContext(Model);
            if (OnEditContextCreated.HasDelegate)
            {
                await OnEditContextCreated.InvokeAsync(_editContext);
            }
        }

        await base.OnInitializedAsync();
    }

    /// <summary>Validates the form synchronously. Prefer <see cref="ValidateAsync"/>.</summary>
    public bool Validate() => _editContext?.Validate() ?? false;

    /// <summary>
    /// Validates the form, awaiting any asynchronous validators before returning.
    /// </summary>
    public async Task<bool> ValidateAsync()
    {
        if (_validator is not null)
        {
            return await _validator.ValidateModelAsync();
        }

        return _editContext?.Validate() ?? false;
    }

    /// <summary>The form's <see cref="EditContext"/>, or null before the first render.</summary>
    public EditContext? GetEditContext() => _editContext;

    private bool ShouldShowField(IFieldConfiguration<TModel, object> field) =>
        field.VisibilityCondition?.Invoke(Model) ?? field.IsVisible;

    private RenderFragment RenderField(IFieldConfiguration<TModel, object> field)
    {
        return builder =>
        {
            // A custom template takes precedence over every registered renderer.
            if (field.CustomTemplate != null && _editContext != null)
            {
                var property = typeof(TModel).GetProperty(field.FieldName);
                if (property == null)
                {
                    return;
                }

                var templateContext = new FieldContext<TModel, object>(
                    Model,
                    field,
                    _editContext,
                    () => property.GetValue(Model)!,
                    newValue => _ = UpdateFieldValue(field.FieldName, newValue),
                    EventCallback.Factory.Create<object>(this, newValue => UpdateFieldValue(field.FieldName, newValue)));

                builder.AddContent(0, field.CustomTemplate(templateContext));
                return;
            }

            // Single rendering path: every field is dispatched through the IFieldRendererService
            // registry, which resolves the Fluent renderers registered by AddFormCraftFluentUI().
            builder.AddContent(0, FieldRendererService.RenderField(
                Model,
                field,
                EventCallback.Factory.Create<object?>(this, val => UpdateFieldValue(field.FieldName, val)),
                EventCallback.Factory.Create(this, () => HandleFieldDependencyChanged(field.FieldName))));
        };
    }

    private async Task UpdateFieldValue(string fieldName, object? value)
    {
        var property = typeof(TModel).GetProperty(fieldName);
        if (property is null)
        {
            return;
        }

        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var convertedValue = value;

        if (value != null && value.GetType() != targetType)
        {
            try
            {
                convertedValue = Convert.ChangeType(value, targetType);
            }
            catch
            {
                // Conversion failed - hand the value over as-is and let validation report it.
            }
        }

        property.SetValue(Model, convertedValue);

        // Notify the EditContext so field-level validation runs and stale errors clear as soon as
        // the user corrects the value.
        _editContext?.NotifyFieldChanged(_editContext.Field(fieldName));

        if (OnFieldChanged.HasDelegate)
        {
            await OnFieldChanged.InvokeAsync((fieldName, convertedValue));
        }

        await HandleFieldDependencyChanged(fieldName);

        StateHasChanged();
    }

    private async Task HandleFieldDependencyChanged(string fieldName)
    {
        if (!Configuration.FieldDependencies.TryGetValue(fieldName, out var dependencies))
        {
            return;
        }

        foreach (var dependency in dependencies)
        {
            await dependency.OnDependencyChangedAsync(Model);
        }

        // Re-render once the async callbacks settle, so cascaded model mutations reach the UI
        // without the caller having to force it.
        StateHasChanged();
    }

    private async Task HandleSubmit()
    {
        // EditForm's OnValidSubmit relies on the synchronous EditContext.Validate(), which returns
        // before async validators finish. Await the full pass explicitly so they can block submit.
        var isValid = _validator is not null
            ? await _validator.ValidateModelAsync()
            : _editContext?.Validate() ?? false;

        if (isValid && OnValidSubmit.HasDelegate)
        {
            await OnValidSubmit.InvokeAsync(Model);
        }
    }
}
