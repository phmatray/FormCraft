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
    private TValue? _lastNotifiedValue;
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
    /// Sets the current value without triggering a notification.
    /// Use this when you've already notified the parent of the change.
    /// Also tracks this as the last notified value to prevent race conditions.
    /// </summary>
    protected void SetValueWithoutNotification(TValue? value)
    {
        _currentValue = value;
        _lastNotifiedValue = value;
    }

    /// <summary>
    /// Notifies that the value has changed.
    /// </summary>
    protected virtual async Task NotifyValueChangedAsync(TValue? value)
    {
        await Context.OnValueChanged.InvokeAsync(value);
        // The parent has now written the value to the model; record it so
        // ShouldReloadValue can tell our own edits apart from external changes.
        _lastNotifiedValue = value;
        StateHasChanged(); // Force re-render after value change
    }

    /// <summary>
    /// Tracks which field this instance's cached configuration was loaded from (#298, #335).
    /// </summary>
    private readonly FieldConfigurationTracker _fieldTracker = new();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        LoadValueFromModel();
        _isInitialized = true;

        // Before the derived component's own OnInitialized body runs — it calls base.OnInitialized()
        // first, so its configuration is loaded by the time the rest of that body looks at it.
        RefreshFieldConfigurationIfChanged();
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

        // Blazor reuses a component instance whenever the render-tree shape matches, so this is the
        // only place a component learns it has been handed a different field. Without it the instance
        // renders the previous field's settings indefinitely — silently, with plausible-looking
        // output (#298 for MudBlazor, #335 for Fluent UI).
        RefreshFieldConfigurationIfChanged();
    }

    /// <summary>
    /// Calls <see cref="OnFieldConfigurationChanged"/> when, and only when, the field changed.
    /// </summary>
    /// <remarks>
    /// The guard — see <see cref="FieldConfigurationTracker"/> — is what makes this affordable:
    /// <see cref="OnParametersSet"/> runs on every keystroke for an immediately-bound input, so the
    /// alternative is re-reading every attribute per character typed.
    /// </remarks>
    private void RefreshFieldConfigurationIfChanged()
    {
        if (_fieldTracker.HasChanged(Context?.Field))
        {
            OnFieldConfigurationChanged();
        }
    }

    /// <summary>
    /// Reads everything this component caches from <c>Context.Field</c>. Called once per field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Override this instead of loading configuration in <c>OnInitialized</c>. It runs on first render
    /// and again whenever a different field arrives, so a component that puts all of its
    /// <c>GetAttribute</c> calls here can never render a stale setting.
    /// </para>
    /// <para>
    /// ⛔ <b>Assign every cached property on every call, including back to its default.</b> The
    /// override is a reload, not a patch: a property left untouched because the new field does not
    /// declare that attribute keeps the <i>previous</i> field's value, which is the same bug in a
    /// smaller box. Watch for two shapes in particular, both of which shipped and had to be fixed —
    /// <c>X = GetAttribute(…) ?? X</c>, which reads as "keep the default" and means "keep the previous
    /// field's value" on a reload; and an assignment guarded by <c>if (value != null)</c>.
    /// </para>
    /// <para>
    /// State <i>derived</i> from the configuration counts too — display text, a selected-items list, a
    /// revealed-password flag — along with any per-instance diagnostic latch, since a new field
    /// deserves its own verdict.
    /// </para>
    /// </remarks>
    protected virtual void OnFieldConfigurationChanged()
    {
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
            // Note: when TValue is a nullable value type (e.g. int?), a null model
            // value falls through to default(TValue), which IS null - null is
            // preserved rather than coerced to zero (#150).
            _currentValue = value is TValue typedValue ? typedValue : default;
            _lastNotifiedValue = _currentValue;
        }
        else
        {
            // Fallback to context value
            _currentValue = Context.CurrentValue is TValue contextValue ? contextValue : default;
            _lastNotifiedValue = _currentValue;
        }
    }

    /// <summary>
    /// Determines if the value should be reloaded from the model.
    /// </summary>
    private bool ShouldReloadValue()
    {
        // While a notification is in flight (we changed the value but the parent
        // hasn't written it to the model yet), the model still holds the previous
        // value - reloading now would wipe the user's input.
        if (!EqualityComparer<TValue>.Default.Equals(_currentValue, _lastNotifiedValue))
        {
            return false;
        }

        // Settled state: reload whenever the model diverged from what we display.
        // This is how external mutations (dependency callbacks, programmatic
        // model changes) reach the UI.
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
