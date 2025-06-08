using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// A validation component that integrates Dynamic Form validation with Blazor's EditContext.
/// This component handles both form-level and field-level validation using the configured validators.
/// Add this component inside an EditForm to enable dynamic validation.
/// </summary>
/// <typeparam name="TModel"></typeparam>
public class DynamicFormValidator<TModel> : ComponentBase, IDisposable
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the form configuration containing field definitions and validation rules.
    /// </summary>
    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;

    protected override void OnInitialized()
    {
        var editContext = CascadedEditContext ?? throw new InvalidOperationException(
            $"{nameof(DynamicFormValidator<TModel>)} requires a cascading parameter of type {nameof(EditContext)}. " +
            $"For example, you can use {nameof(DynamicFormValidator<TModel>)} inside an {nameof(EditForm)}.");

        _editContext = editContext;
        _messageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += HandleValidationRequested;
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    [CascadingParameter] private EditContext CascadedEditContext { get; set; } = default!;

    private async void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        var model = (TModel)_editContext!.Model;

        // Clear all existing custom validation messages
        _messageStore!.Clear();

        foreach (var field in Configuration.Fields)
        {
            var getter = field.ValueExpression.Compile();
            var value = getter(model);

            foreach (var validator in field.Validators)
            {
                var result = await validator.ValidateAsync(model, value, ServiceProvider);
                if (!result.IsValid)
                {
                    _messageStore.Add(_editContext.Field(field.FieldName), result.ErrorMessage!);
                }
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private async void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        // Find the field configuration for the changed field
        var fieldConfig = Configuration.Fields.FirstOrDefault(f => f.FieldName == e.FieldIdentifier.FieldName);
        if (fieldConfig == null) return;

        var model = (TModel)_editContext!.Model;
        var getter = fieldConfig.ValueExpression.Compile();
        var value = getter(model);

        // Clear existing messages for this field only
        _messageStore!.Clear(e.FieldIdentifier);

        // Validate the specific field
        foreach (var validator in fieldConfig.Validators)
        {
            var result = await validator.ValidateAsync(model, value, ServiceProvider);
            if (!result.IsValid)
            {
                _messageStore.Add(e.FieldIdentifier, result.ErrorMessage!);
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        if (_editContext != null)
        {
            _editContext.OnValidationRequested -= HandleValidationRequested;
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }
    }
}