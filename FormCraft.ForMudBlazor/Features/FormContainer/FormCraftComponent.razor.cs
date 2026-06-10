using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;

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
            var property = typeof(TModel).GetProperty(field.FieldName);
            if (property == null) return;

            var fieldType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            var value = property.GetValue(Model);

            // Custom templates take precedence over every built-in renderer
            if (field.CustomTemplate != null && _editContext != null)
            {
                var templateContext = new FieldContext<TModel, object>(
                    Model,
                    field,
                    _editContext,
                    () => property.GetValue(Model)!,
                    newValue => _ = UpdateFieldValue(field.FieldName, newValue),
                    EventCallback.Factory.Create<object>(this, newValue => UpdateFieldValue(field.FieldName, newValue)));
                builder.AddContent(0, field.CustomTemplate(templateContext));
            }
            // Check for fields with options (select/dropdown)
            else if (field.AdditionalAttributes.TryGetValue("Options", out var optionsObj))
            {
                RenderSelectField(builder, field, value, optionsObj);
            }
            // Check for custom renderer
            else if (field.CustomRendererType != null)
            {
                RenderCustomField(builder, field, fieldType, value);
            }
            // Fields configured with a specialized renderer (LOV, lookup, autocomplete)
            // must not fall through to the generic type-based branches below
            else if (field.AdditionalAttributes.ContainsKey("LovConfiguration") ||
                     field.AdditionalAttributes.ContainsKey("LookupDataProvider") ||
                     field.AdditionalAttributes.ContainsKey("AutocompleteSearchFunc") ||
                     field.AdditionalAttributes.ContainsKey("AutocompleteOptionProvider"))
            {
                RenderCustomField(builder, field, fieldType, value);
            }
            // Render based on field type
            else if (fieldType == typeof(string))
            {
                RenderTextField(builder, field, value as string);
            }
            else if (underlyingType == typeof(int))
            {
                RenderNumericField(builder, field, (int)(value ?? 0));
            }
            else if (underlyingType == typeof(decimal))
            {
                RenderNumericField(builder, field, (decimal)(value ?? 0m));
            }
            else if (underlyingType == typeof(double))
            {
                RenderNumericField(builder, field, (double)(value ?? 0.0));
            }
            else if (underlyingType == typeof(float))
            {
                RenderNumericField(builder, field, (float)(value ?? 0f));
            }
            else if (underlyingType == typeof(long))
            {
                RenderNumericField(builder, field, (long)(value ?? 0L));
            }
            else if (underlyingType == typeof(short))
            {
                RenderNumericField(builder, field, (short)(value ?? (short)0));
            }
            else if (underlyingType == typeof(byte))
            {
                RenderNumericField(builder, field, (byte)(value ?? (byte)0));
            }
            else if (underlyingType == typeof(bool))
            {
                RenderBooleanField(builder, field, value ?? false);
            }
            else if (underlyingType == typeof(DateTime))
            {
                RenderDateTimeField(builder, field, value as DateTime?);
            }
            else if (underlyingType == typeof(DateOnly) || underlyingType == typeof(TimeOnly))
            {
                RenderCustomField(builder, field, fieldType, value);
            }
            else if (fieldType == typeof(IBrowserFile) || fieldType == typeof(IReadOnlyList<IBrowserFile>))
            {
                // Delegate to the renderer-service file upload components; the previous
                // hand-rolled MudFileUpload passed a plain RenderFragment to
                // CustomContent (typed RenderFragment<MudFileUpload<T>>) and crashed.
                RenderCustomField(builder, field, fieldType, value);
            }
        };
    }

    private void RenderSelectField(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, object? value, object optionsObj)
    {
        var property = typeof(TModel).GetProperty(field.FieldName);
        var valueType = property?.PropertyType ?? typeof(string);
        var underlyingType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        // Use reflection to call the generic helper method with the correct TValue type
        var method = typeof(FormCraftComponent<TModel>)
            .GetMethod(nameof(RenderSelectFieldGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(underlyingType);

        method.Invoke(this, new object?[] { builder, field, value, optionsObj });
    }

    private void RenderSelectFieldGeneric<TValue>(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, object? value, object optionsObj)
    {
        var typedValue = value is TValue tv ? tv : default;

        builder.OpenComponent<MudSelect<TValue>>(0);
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Value", typedValue);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<TValue>(this,
                newValue => UpdateFieldValue(field.FieldName, newValue)));
        builder.AddAttribute(11, "ChildContent", RenderSelectOptions<TValue>(optionsObj));
        builder.CloseComponent();
    }

    private RenderFragment RenderSelectOptions<TValue>(object optionsObj)
    {
        return builder =>
        {
            var sequence = 0;
            if (optionsObj is IEnumerable<SelectOption<TValue>> typedOptions)
            {
                foreach (var option in typedOptions)
                {
                    builder.OpenComponent<MudSelectItem<TValue>>(sequence++);
                    builder.AddAttribute(sequence++, "Value", option.Value);
                    builder.AddAttribute(sequence++, "ChildContent",
                        (RenderFragment)(itemBuilder => itemBuilder.AddContent(0, option.Label)));
                    builder.CloseComponent();
                }
            }
            else if (optionsObj is System.Collections.IEnumerable options)
            {
                foreach (var option in options)
                {
                    var optionType = option.GetType();
                    var valueProperty = optionType.GetProperty("Value");
                    var labelProperty = optionType.GetProperty("Label");

                    if (valueProperty != null && labelProperty != null)
                    {
                        var rawValue = valueProperty.GetValue(option);
                        var optionValue = rawValue is TValue tv ? tv : default;
                        var optionLabel = labelProperty.GetValue(option)?.ToString() ?? "";

                        builder.OpenComponent<MudSelectItem<TValue>>(sequence++);
                        builder.AddAttribute(sequence++, "Value", optionValue);
                        builder.AddAttribute(sequence++, "ChildContent",
                            (RenderFragment)(itemBuilder => itemBuilder.AddContent(0, optionLabel)));
                        builder.CloseComponent();
                    }
                }
            }
        };
    }

    private void RenderTextField(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, string? value)
    {
        // Create a proper FieldRenderContext to pass to MudBlazorTextFieldComponent
        var context = new FieldRenderContext<TModel>
        {
            Model = Model,
            Field = field,
            ActualFieldType = typeof(string),
            CurrentValue = value,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, val => UpdateFieldValue(field.FieldName, val)),
            OnDependencyChanged = EventCallback.Factory.Create(this, () => HandleFieldDependencyChanged(field.FieldName))
        };

        // Render MudBlazorTextFieldComponent instead of MudTextField directly
        builder.OpenComponent<MudBlazorTextFieldComponent<TModel>>(0);
        builder.AddAttribute(1, "Context", context);
        builder.CloseComponent();
    }

    private void RenderNumericField<T>(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, T value)
        where T : struct
    {
        builder.OpenComponent(0, typeof(MudNumericField<>).MakeGenericType(typeof(T)));
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<T>(this,
                newValue => UpdateFieldValue(field.FieldName, newValue)));
        builder.AddAttribute(4, "Immediate", true);

        // MudBlazor appends '*' to Pattern before emitting the HTML attribute, so a
        // fully-anchored regex here becomes invalid (e.g. "...?*"). The component's
        // default pattern already handles decimal input; only Culture is needed.
        builder.AddAttribute(5, "Culture", System.Globalization.CultureInfo.InvariantCulture);

        builder.CloseComponent();
    }

    private void RenderBooleanField(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, object value)
    {
        builder.OpenComponent<MudCheckBox<bool>>(0);
        builder.AddAttribute(1, "Label", field.Label);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<bool>(this,
                newValue => UpdateFieldValue(field.FieldName, newValue)));
        builder.AddAttribute(4, "ReadOnly", field.IsReadOnly);
        builder.AddAttribute(5, "Disabled", field.IsDisabled);
        builder.CloseComponent();
    }

    private void RenderDateTimeField(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, DateTime? value)
    {
        builder.OpenComponent<MudDatePicker>(0);
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Date", value);
        builder.AddAttribute(3, "DateChanged",
            EventCallback.Factory.Create<DateTime?>(this,
                newValue => UpdateFieldValue(field.FieldName, newValue)));
        if (field.AdditionalAttributes.TryGetValue("MinDate", out var minDate) && minDate is DateTime min)
        {
            builder.AddAttribute(4, "MinDate", (DateTime?)min);
        }
        if (field.AdditionalAttributes.TryGetValue("MaxDate", out var maxDate) && maxDate is DateTime max)
        {
            builder.AddAttribute(5, "MaxDate", (DateTime?)max);
        }
        builder.CloseComponent();
    }

    private void RenderCustomField(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, Type fieldType, object? value)
    {
        var context = new FieldRenderContext<TModel>
        {
            Model = Model,
            Field = field,
            ActualFieldType = fieldType,
            CurrentValue = value,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, val => UpdateFieldValue(field.FieldName, val)),
            OnDependencyChanged = EventCallback.Factory.Create(this, () => HandleFieldDependencyChanged(field.FieldName))
        };
        builder.AddContent(0,
            FieldRendererService.RenderField(Model, field, context.OnValueChanged, context.OnDependencyChanged));
    }

    private void AddCommonFieldAttributes(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, int startIndex)
    {
        builder.AddAttribute(startIndex++, "Label", field.Label);
        builder.AddAttribute(startIndex++, "Placeholder", field.Placeholder);
        builder.AddAttribute(startIndex++, "HelperText", field.HelpText);
        builder.AddAttribute(startIndex++, "Required", field.IsRequired);
        builder.AddAttribute(startIndex++, "ReadOnly", field.IsReadOnly);
        builder.AddAttribute(startIndex++, "Disabled", field.IsDisabled);
        builder.AddAttribute(startIndex++, "Variant", Variant.Outlined);
        builder.AddAttribute(startIndex++, "Margin", Margin.Dense);
        builder.AddAttribute(startIndex, "ShrinkLabel", true);
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

    private async Task HandleFileUpload(string fieldName, InputFileChangeEventArgs args)
    {
        var property = typeof(TModel).GetProperty(fieldName);
        if (property != null)
        {
            if (property.PropertyType == typeof(IBrowserFile))
            {
                await UpdateFieldValue(fieldName, args.File);
            }
            else if (property.PropertyType == typeof(IReadOnlyList<IBrowserFile>))
            {
                await UpdateFieldValue(fieldName, args.GetMultipleFiles());
            }
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