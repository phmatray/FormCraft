using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForMudBlazor;

public partial class FormCraftComponent<TModel>
{
    [Parameter]
    public TModel Model { get; set; } = new();

    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    [Parameter]
    public EventCallback<TModel> OnValidSubmit { get; set; }

    [Parameter]
    public EventCallback<(string fieldName, object? value)> OnFieldChanged { get; set; }

    [Parameter]
    public bool ShowSubmitButton { get; set; } = true;

    [Parameter]
    public string SubmitButtonText { get; set; } = "Submit";

    [Parameter]
    public string SubmittingText { get; set; } = "Submitting...";

    [Parameter]
    public bool IsSubmitting { get; set; }

    [Parameter]
    public string? SubmitButtonClass { get; set; }

    [Parameter]
    public RenderFragment? BeforeForm { get; set; }

    [Parameter]
    public RenderFragment? AfterForm { get; set; }

    [Parameter]
    public EventCallback<EditContext> OnEditContextCreated { get; set; }

    private EditContext? _editContext;
    private DynamicFormValidator<TModel>? _validator;
    private IGroupedFormConfiguration<TModel>? GroupedConfiguration => Configuration as IGroupedFormConfiguration<TModel>;
    private ICollectionFormConfiguration<TModel>? CollectionConfiguration => Configuration as ICollectionFormConfiguration<TModel>;

    protected override async Task OnInitializedAsync()
    {
        if (Model != null)
        {
            _editContext = new EditContext(Model);
            if (OnEditContextCreated.HasDelegate)
            {
                await OnEditContextCreated.InvokeAsync(_editContext);
            }
        }
        await base.OnInitializedAsync();
    }

    public bool Validate()
    {
        return _editContext?.Validate() ?? false;
    }

    /// <summary>
    /// Validates the form, awaiting any asynchronous validators before returning.
    /// Prefer this over <see cref="Validate"/> when async validators are configured.
    /// </summary>
    public async Task<bool> ValidateAsync()
    {
        if (_validator != null)
        {
            return await _validator.ValidateModelAsync();
        }

        return _editContext?.Validate() ?? false;
    }

    public EditContext? GetEditContext()
    {
        return _editContext;
    }

    private RenderFragment RenderField(IFieldConfiguration<TModel, object> field)
    {
        return builder =>
        {
            // Custom templates take precedence over every renderer
            if (field.CustomTemplate != null && _editContext != null)
            {
                var property = typeof(TModel).GetProperty(field.FieldName);
                if (property == null) return;

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

            // Single rendering path: every field is dispatched through the
            // FieldRendererService registry (#148). Type- and configuration-based
            // selection (text, numeric, boolean, date, select, LOV, lookup,
            // autocomplete, file upload, custom renderers) lives in the
            // registered IFieldRenderer implementations.
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
        if (property != null)
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = value;

            // Convert value to the target type if necessary
            if (value != null && value.GetType() != targetType)
            {
                try
                {
                    convertedValue = Convert.ChangeType(value, targetType);
                }
                catch
                {
                    // If conversion fails, use the value as-is
                }
            }

            property.SetValue(Model, convertedValue);

            // Notify the EditContext so field-level validation runs and stale
            // error messages clear as soon as the user corrects the value.
            _editContext?.NotifyFieldChanged(_editContext.Field(fieldName));

            if (OnFieldChanged.HasDelegate)
            {
                await OnFieldChanged.InvokeAsync((fieldName, convertedValue));
            }

            // Handle dependencies
            await HandleFieldDependencyChanged(fieldName);

            StateHasChanged();
        }
    }

    private Task HandleFieldDependencyChanged(string fieldName)
    {
        if (Configuration.FieldDependencies.TryGetValue(fieldName, out var dependencies))
        {
            foreach (IFieldDependency<TModel> dependency in dependencies)
            {
                dependency.OnDependencyChanged(Model);
            }
        }

        return Task.CompletedTask;
    }

    private void HandleCollectionChanged()
    {
        _editContext?.NotifyValidationStateChanged();
        StateHasChanged();
    }

    private bool ShouldShowField(IFieldConfiguration<TModel, object> field)
    {
        if (field.VisibilityCondition != null)
        {
            return field.VisibilityCondition(Model);
        }

        return field.IsVisible;
    }

    private async Task HandleSubmit()
    {
        // EditForm's OnValidSubmit relies on the synchronous EditContext.Validate(),
        // which returns before async validators finish. Await the full validation
        // pass explicitly so async validators can block submission.
        var isValid = _validator != null
            ? await _validator.ValidateModelAsync()
            : _editContext?.Validate() ?? false;

        if (isValid && OnValidSubmit.HasDelegate)
        {
            await OnValidSubmit.InvokeAsync(Model);
        }
    }
}