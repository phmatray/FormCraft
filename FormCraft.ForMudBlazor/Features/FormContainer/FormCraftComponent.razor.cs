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
    private IGroupedFormConfiguration<TModel>? GroupedConfiguration => Configuration as IGroupedFormConfiguration<TModel>;
    
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

    private RenderFragment RenderField(IFieldConfiguration<TModel, object> field)
    {
        return builder =>
        {
            var property = typeof(TModel).GetProperty(field.FieldName);
            if (property == null) return;

            var fieldType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            var value = property.GetValue(Model);

            // Check for fields with options (select/dropdown)
            if (field.AdditionalAttributes.TryGetValue("Options", out var optionsObj) && optionsObj != null)
            {
                RenderSelectField(builder, field, value, optionsObj);
            }
            // Check for custom renderer
            else if (field.CustomRendererType != null)
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
                RenderNumericField<int>(builder, field, (int)(value ?? 0));
            }
            else if (underlyingType == typeof(decimal))
            {
                RenderNumericField<decimal>(builder, field, (decimal)(value ?? 0m));
            }
            else if (underlyingType == typeof(double))
            {
                RenderNumericField<double>(builder, field, (double)(value ?? 0.0));
            }
            else if (underlyingType == typeof(bool))
            {
                RenderBooleanField(builder, field, value ?? false);
            }
            else if (underlyingType == typeof(DateTime))
            {
                RenderDateTimeField(builder, field, value as DateTime?);
            }
            else if (fieldType == typeof(IBrowserFile) || fieldType == typeof(IReadOnlyList<IBrowserFile>))
            {
                RenderFileUploadField(builder, field);
            }
        };
    }

    private void RenderSelectField(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field, object? value, object optionsObj)
    {
        builder.OpenComponent<MudSelect<string>>(0);
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Value", value?.ToString() ?? string.Empty);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(this,
                newValue => UpdateFieldValue(field.FieldName, newValue)));
        builder.AddAttribute(11, "ChildContent", RenderSelectOptions(optionsObj));
        builder.CloseComponent();
    }

    private RenderFragment RenderSelectOptions(object optionsObj)
    {
        return builder =>
        {
            var sequence = 0;
            if (optionsObj is IEnumerable<SelectOption<string>> stringOptions)
            {
                foreach (var option in stringOptions)
                {
                    builder.OpenComponent<MudSelectItem<string>>(sequence++);
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
                        var optionValue = valueProperty.GetValue(option)?.ToString() ?? "";
                        var optionLabel = labelProperty.GetValue(option)?.ToString() ?? "";

                        builder.OpenComponent<MudSelectItem<string>>(sequence++);
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
        builder.OpenComponent<MudTextField<string>>(0);
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(this,
                newValue => UpdateFieldValue(field.FieldName, newValue)));
        builder.AddAttribute(11, "Immediate", true);
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
        builder.CloseComponent();
    }

    private void RenderFileUploadField(RenderTreeBuilder builder, IFieldConfiguration<TModel, object> field)
    {
        builder.OpenComponent<MudFileUpload<IBrowserFile>>(0);
        builder.AddAttribute(1, "T", typeof(IBrowserFile));
        builder.AddAttribute(2, "OnFilesChanged",
            EventCallback.Factory.Create<InputFileChangeEventArgs>(this,
                args => HandleFileUpload(field.FieldName, args)));
        builder.AddAttribute(3, "Accept",
            field.AdditionalAttributes.TryGetValue("Accept", out var accept) ? accept : "*/*");
        builder.AddAttribute(4, "Disabled", field.IsDisabled);
        builder.AddAttribute(5, "ChildContent", RenderFileUploadButton(field));
        builder.CloseComponent();
    }

    private RenderFragment RenderFileUploadButton(IFieldConfiguration<TModel, object> field)
    {
        return builder =>
        {
            builder.OpenComponent<MudButton>(0);
            builder.AddAttribute(1, "HtmlTag", "label");
            builder.AddAttribute(2, "Variant", Variant.Filled);
            builder.AddAttribute(3, "Color", Color.Primary);
            builder.AddAttribute(4, "StartIcon", Icons.Material.Filled.CloudUpload);
            builder.AddAttribute(5, "for", field.FieldName);
            builder.AddAttribute(6, "ChildContent",
                (RenderFragment)(buttonBuilder => buttonBuilder.AddContent(0, field.Label ?? "Upload File")));
            builder.CloseComponent();
        };
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
        builder.AddAttribute(startIndex, "Margin", Margin.Dense);
    }

    private async Task UpdateFieldValue(string fieldName, object? value)
    {
        var property = typeof(TModel).GetProperty(fieldName);
        if (property != null)
        {
            property.SetValue(Model, value);

            if (OnFieldChanged.HasDelegate)
            {
                await OnFieldChanged.InvokeAsync((fieldName, value));
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

    private bool ShouldShowField(IFieldConfiguration<TModel, object> field)
    {
        if (field.VisibilityCondition != null)
        {
            return field.VisibilityCondition(Model);
        }

        return field.IsVisible;
    }

    private Task OnSubmit()
    {
        if (OnValidSubmit.HasDelegate)
        {
            return OnValidSubmit.InvokeAsync(Model);
        }

        return Task.CompletedTask;
    }
}