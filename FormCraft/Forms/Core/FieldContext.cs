using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// Default implementation of <see cref="IFieldContext{TModel, TValue}"/> handed to
/// custom field templates configured via WithCustomTemplate().
/// </summary>
/// <typeparam name="TModel">The model type that the form binds to.</typeparam>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public class FieldContext<TModel, TValue> : IFieldContext<TModel, TValue>
{
    private readonly Func<TValue> _getValue;
    private readonly Action<TValue> _setValue;

    /// <summary>
    /// Initializes a new field context.
    /// </summary>
    /// <param name="model">The model instance containing the field.</param>
    /// <param name="configuration">The field configuration.</param>
    /// <param name="editContext">The EditContext of the enclosing form.</param>
    /// <param name="getValue">Reads the current field value from the model.</param>
    /// <param name="setValue">Writes a new field value to the model.</param>
    /// <param name="valueChanged">Callback invoked when the template changes the value.</param>
    public FieldContext(
        TModel model,
        IFieldConfiguration<TModel, TValue> configuration,
        EditContext editContext,
        Func<TValue> getValue,
        Action<TValue> setValue,
        EventCallback<TValue> valueChanged)
    {
        Model = model;
        Configuration = configuration;
        EditContext = editContext;
        _getValue = getValue;
        _setValue = setValue;
        ValueChanged = valueChanged;
        FieldIdentifier = new FieldIdentifier(model!, configuration.FieldName);
    }

    /// <inheritdoc />
    public TModel Model { get; }

    /// <inheritdoc />
    public IFieldConfiguration<TModel, TValue> Configuration { get; }

    /// <inheritdoc />
    public EditContext EditContext { get; }

    /// <inheritdoc />
    public TValue Value
    {
        get => _getValue();
        set => _setValue(value);
    }

    /// <inheritdoc />
    public EventCallback<TValue> ValueChanged { get; }

    /// <inheritdoc />
    public FieldIdentifier FieldIdentifier { get; }

    /// <inheritdoc />
    public IEnumerable<string> ValidationMessages => EditContext.GetValidationMessages(FieldIdentifier);

    /// <inheritdoc />
    public bool IsValid => !ValidationMessages.Any();

    /// <inheritdoc />
    public string FieldCssClass =>
        string.IsNullOrEmpty(Configuration.CssClass)
            ? EditContext.FieldCssClass(FieldIdentifier)
            : $"{Configuration.CssClass} {EditContext.FieldCssClass(FieldIdentifier)}";
}
