using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Base class for all field components, providing common functionality regardless of UI framework.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public abstract class FieldComponentBase<TModel, TValue> : ComponentBase, IFieldComponent<TModel>
{
    private TValue? _currentValue;
    private bool _isInitialized;

    /// <summary>
    /// Gets or sets the field render context.
    /// </summary>
    [Parameter]
    public IFieldRenderContext<TModel> Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current value of the field.
    /// </summary>
    protected TValue? CurrentValue
    {
        get => _currentValue;
        set
        {
            if (!EqualityComparer<TValue>.Default.Equals(_currentValue, value))
            {
                _currentValue = value;
                _ = NotifyValueChangedAsync(value);
            }
        }
    }

    /// <summary>
    /// Notifies that the value has changed.
    /// </summary>
    protected virtual async Task NotifyValueChangedAsync(TValue? value)
    {
        await Context.OnValueChanged.InvokeAsync(value);
        StateHasChanged(); // Force re-render after value change
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadValueFromModel();
        _isInitialized = true;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Only reload value if the model reference has changed or on first load
        if (!_isInitialized || ShouldReloadValue())
        {
            LoadValueFromModel();
        }
    }

    /// <summary>
    /// Loads the current value from the model.
    /// </summary>
    private void LoadValueFromModel()
    {
        var property = Context.Model?.GetType().GetProperty(Context.Field.FieldName);
        if (property != null && Context.Model != null)
        {
            var value = property.GetValue(Context.Model);
            _currentValue = value is TValue typedValue ? typedValue : default;
        }
        else
        {
            // Fallback to context value
            _currentValue = Context.CurrentValue is TValue contextValue ? contextValue : default;
        }
    }

    /// <summary>
    /// Determines if the value should be reloaded from the model.
    /// </summary>
    private bool ShouldReloadValue()
    {
        // Check if the model value has changed externally
        var property = Context.Model?.GetType().GetProperty(Context.Field.FieldName);
        if (property != null && Context.Model != null)
        {
            var modelValue = property.GetValue(Context.Model);
            var typedModelValue = modelValue is TValue typed ? typed : default;
            return !EqualityComparer<TValue>.Default.Equals(_currentValue, typedModelValue);
        }
        return false;
    }

    /// <summary>
    /// Gets the label text for the field.
    /// </summary>
    protected string? Label => Context.Field.Label;

    /// <summary>
    /// Gets the placeholder text for the field.
    /// </summary>
    protected string? Placeholder => Context.Field.Placeholder;

    /// <summary>
    /// Gets the help text for the field.
    /// </summary>
    protected string? HelpText => Context.Field.HelpText;

    /// <summary>
    /// Gets whether the field is required.
    /// </summary>
    protected bool IsRequired => Context.Field.IsRequired;

    /// <summary>
    /// Gets whether the field is read-only.
    /// </summary>
    protected bool IsReadOnly => Context.Field.IsReadOnly;

    /// <summary>
    /// Gets whether the field is disabled.
    /// </summary>
    protected bool IsDisabled => Context.Field.IsDisabled;

    /// <summary>
    /// Gets an attribute value from the field's additional attributes.
    /// </summary>
    protected T? GetAttribute<T>(string key, T? defaultValue = default)
    {
        if (Context.Field.AdditionalAttributes.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
}