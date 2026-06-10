using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Internal wrapper class that handles type conversion from strongly-typed field configurations to object-based configurations.
/// This allows the form builder to store different field types in a single collection while maintaining type safety.
/// </summary>
/// <typeparam name="TModel">The model type that the form binds to.</typeparam>
/// <typeparam name="TValue">The actual type of the field value.</typeparam>
public class FieldConfigurationWrapper<TModel, TValue> : IFieldConfiguration<TModel, object>
{
    private readonly IFieldConfiguration<TModel, TValue> _inner;

    /// <summary>
    /// Initializes a new instance of the FieldConfigurationWrapper class.
    /// </summary>
    /// <param name="inner">The strongly-typed field configuration to wrap.</param>
    public FieldConfigurationWrapper(IFieldConfiguration<TModel, TValue> inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public string FieldName => _inner.FieldName;

    /// <inheritdoc />
    public Expression<Func<TModel, object>> ValueExpression =>
        Expression.Lambda<Func<TModel, object>>(
            Expression.Convert(_inner.ValueExpression.Body, typeof(object)),
            _inner.ValueExpression.Parameters);

    /// <inheritdoc />
    public string? Label { get => _inner.Label; set => _inner.Label = value; }

    /// <inheritdoc />
    public string? Placeholder { get => _inner.Placeholder; set => _inner.Placeholder = value; }

    /// <inheritdoc />
    public string? HelpText { get => _inner.HelpText; set => _inner.HelpText = value; }

    /// <inheritdoc />
    public string? CssClass { get => _inner.CssClass; set => _inner.CssClass = value; }

    /// <inheritdoc />
    public bool IsRequired { get => _inner.IsRequired; set => _inner.IsRequired = value; }

    /// <inheritdoc />
    public bool IsVisible { get => _inner.IsVisible; set => _inner.IsVisible = value; }

    /// <inheritdoc />
    public bool IsDisabled { get => _inner.IsDisabled; set => _inner.IsDisabled = value; }

    /// <inheritdoc />
    public bool IsReadOnly { get => _inner.IsReadOnly; set => _inner.IsReadOnly = value; }

    /// <inheritdoc />
    public int Order { get => _inner.Order; set => _inner.Order = value; }

    /// <inheritdoc />
    public Dictionary<string, object> AdditionalAttributes => _inner.AdditionalAttributes;

    /// <inheritdoc />
    public string? InputType { get => _inner.InputType; set => _inner.InputType = value; }

    /// <inheritdoc />
    public List<IFieldValidator<TModel, object>> Validators =>
        _inner.Validators.Select<IFieldValidator<TModel, TValue>, IFieldValidator<TModel, object>>(v => new ValidatorWrapper<TModel, TValue>(v)).ToList();

    /// <inheritdoc />
    public List<IFieldDependency<TModel>> Dependencies => _inner.Dependencies;

    /// <inheritdoc />
    public Func<TModel, bool>? VisibilityCondition
    {
        get => _inner.VisibilityCondition;
        set => _inner.VisibilityCondition = value;
    }

    /// <inheritdoc />
    public Func<TModel, bool>? DisabledCondition
    {
        get => _inner.DisabledCondition;
        set => _inner.DisabledCondition = value;
    }

    private RenderFragment<IFieldContext<TModel, object>>? _customTemplate;

    /// <inheritdoc />
    public RenderFragment<IFieldContext<TModel, object>>? CustomTemplate
    {
        get
        {
            if (_customTemplate != null)
            {
                return _customTemplate;
            }

            // Surface templates configured through the typed builder API by adapting
            // the object-typed render context to the typed one the template expects.
            var typedTemplate = _inner.CustomTemplate;
            if (typedTemplate == null)
            {
                return null;
            }

            return objectContext => typedTemplate(new TypedFieldContextAdapter(objectContext, _inner));
        }
        set => _customTemplate = value;
    }

    private sealed class TypedFieldContextAdapter : IFieldContext<TModel, TValue>
    {
        private readonly IFieldContext<TModel, object> _objectContext;

        public TypedFieldContextAdapter(IFieldContext<TModel, object> objectContext, IFieldConfiguration<TModel, TValue> configuration)
        {
            _objectContext = objectContext;
            Configuration = configuration;
        }

        public TModel Model => _objectContext.Model;
        public IFieldConfiguration<TModel, TValue> Configuration { get; }
        public Microsoft.AspNetCore.Components.Forms.EditContext EditContext => _objectContext.EditContext;

        public TValue Value
        {
            get => _objectContext.Value is TValue typed ? typed : default!;
            set => _objectContext.Value = value!;
        }

        public EventCallback<TValue> ValueChanged =>
            EventCallback.Factory.Create<TValue>(this, value => _objectContext.ValueChanged.InvokeAsync(value));

        public Microsoft.AspNetCore.Components.Forms.FieldIdentifier FieldIdentifier => _objectContext.FieldIdentifier;
        public IEnumerable<string> ValidationMessages => _objectContext.ValidationMessages;
        public bool IsValid => _objectContext.IsValid;
        public string FieldCssClass => _objectContext.FieldCssClass;
    }

    /// <inheritdoc />
    public Type? CustomRendererType
    {
        get => _inner.CustomRendererType;
        set => _inner.CustomRendererType = value;
    }

    /// <summary>
    /// Gets access to the original typed configuration.
    /// </summary>
    public IFieldConfiguration<TModel, TValue> TypedConfiguration => _inner;

    /// <summary>
    /// Gets the actual runtime type of the field value.
    /// </summary>
    /// <returns>The Type of TValue, representing the actual field type.</returns>
    public Type GetActualFieldType() => typeof(TValue);
}