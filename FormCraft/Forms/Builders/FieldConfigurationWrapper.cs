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

    /// <summary>
    /// Initializes a new instance of the FieldConfigurationWrapper class.
    /// </summary>
    /// <param name="inner">The strongly-typed field configuration to wrap.</param>
    public FieldConfigurationWrapper(IFieldConfiguration<TModel, TValue> inner)
    {
        TypedConfiguration = inner;
    }

    /// <inheritdoc />
    public string FieldName => TypedConfiguration.FieldName;

    /// <inheritdoc />
    /// <remarks>
    /// The projected lambda is built once per wrapper and then reused, rather than rebuilt on every
    /// access (#269). This requires the wrapped configuration to return a stable
    /// <c>ValueExpression</c> — which <see cref="FieldConfiguration{TModel, TValue}" /> provides by
    /// assigning it in its constructor, and which the fluent builder's immutable-after-<c>Build()</c>
    /// contract extends to anything built through it. An implementation that re-targeted its
    /// expression after construction would keep seeing this first projection.
    /// <para>
    /// The callers this actually saves are the ones that read it per <i>validation</i>:
    /// <c>CollectionFieldValidator</c> and <c>DynamicFormValidator</c> read it once per item per
    /// field. The renderer service reads it at most once per configuration, because it caches the
    /// compiled getter itself.
    /// </para>
    /// <para>
    /// The memo is deliberately not synchronized. Blazor renders on a single sync context, and two
    /// racing readers would each build an equivalent tree with one winning the field — a wasted
    /// allocation, never a wrong value.
    /// </para>
    /// </remarks>
    public Expression<Func<TModel, object>> ValueExpression =>
        field ??= Expression.Lambda<Func<TModel, object>>(
            Expression.Convert(TypedConfiguration.ValueExpression.Body, typeof(object)),
            TypedConfiguration.ValueExpression.Parameters);

    /// <inheritdoc />
    public string? Label { get => TypedConfiguration.Label; set => TypedConfiguration.Label = value; }

    /// <inheritdoc />
    public string? Placeholder { get => TypedConfiguration.Placeholder; set => TypedConfiguration.Placeholder = value; }

    /// <inheritdoc />
    public string? HelpText { get => TypedConfiguration.HelpText; set => TypedConfiguration.HelpText = value; }

    /// <inheritdoc />
    public string? CssClass { get => TypedConfiguration.CssClass; set => TypedConfiguration.CssClass = value; }

    /// <inheritdoc />
    public bool IsRequired { get => TypedConfiguration.IsRequired; set => TypedConfiguration.IsRequired = value; }

    /// <inheritdoc />
    public bool IsVisible { get => TypedConfiguration.IsVisible; set => TypedConfiguration.IsVisible = value; }

    /// <inheritdoc />
    public bool IsDisabled { get => TypedConfiguration.IsDisabled; set => TypedConfiguration.IsDisabled = value; }

    /// <inheritdoc />
    public bool IsReadOnly { get => TypedConfiguration.IsReadOnly; set => TypedConfiguration.IsReadOnly = value; }

    /// <inheritdoc />
    public int Order { get => TypedConfiguration.Order; set => TypedConfiguration.Order = value; }

    /// <inheritdoc />
    public Dictionary<string, object> AdditionalAttributes => TypedConfiguration.AdditionalAttributes;

    /// <inheritdoc />
    public string? InputType { get => TypedConfiguration.InputType; set => TypedConfiguration.InputType = value; }

    private List<IFieldValidator<TModel, object>>? _validators;
    private int _wrappedInnerValidatorCount;

    /// <summary>
    /// Gets the object-typed view of the field's validators.
    /// The list instance is cached: repeated reads return the same instance, so validators
    /// added to it (e.g. <c>config.Fields[0].Validators.Add(...)</c>) are retained and execute
    /// during validation instead of being silently dropped. Validators added through the typed
    /// builder API after the first read are appended to the cached view on the next read.
    /// Prefer <see cref="AddValidator"/> to keep the underlying typed configuration in sync.
    /// </summary>
    public IReadOnlyList<IFieldValidator<TModel, object>> Validators
    {
        get
        {
            if (_validators == null)
            {
                _validators = TypedConfiguration.Validators
                    .Select<IFieldValidator<TModel, TValue>, IFieldValidator<TModel, object>>(v => new ValidatorWrapper<TModel, TValue>(v))
                    .ToList();
                _wrappedInnerValidatorCount = TypedConfiguration.Validators.Count;
            }
            else
            {
                SyncNewInnerValidators();
            }

            return _validators;
        }
    }

    /// <summary>
    /// Adds a validator to the field, registering it against the underlying typed configuration
    /// so it is visible through both the typed and the object-typed validator views.
    /// </summary>
    /// <param name="validator">The validator to add.</param>
    public void AddValidator(IFieldValidator<TModel, object> validator)
    {
        // Materialize the cached object-typed view (and pull in any typed validators
        // added since the last read) so the original instance — not a re-wrapped
        // copy — is what callers observe through Validators afterwards.
        _ = Validators;

        // Unwrap validators that already wrap a typed validator; adapt arbitrary
        // object-typed validators so the inner typed list remains the source of truth.
        var typedValidator = validator is ValidatorWrapper<TModel, TValue> wrapper
            ? wrapper.Inner
            : new ObjectValidatorAdapter(validator);
        TypedConfiguration.AddValidator(typedValidator);

        // Appended to the backing list directly: the public view is IReadOnlyList since #155, and
        // the whole point of that change is that callers cannot do this. Adding the caller's own
        // instance (rather than re-wrapping the one just handed to _inner) keeps reference identity
        // through the object-typed view.
        _validators!.Add(validator);
        _wrappedInnerValidatorCount = TypedConfiguration.Validators.Count;
    }

    private void SyncNewInnerValidators()
    {
        for (var i = _wrappedInnerValidatorCount; i < TypedConfiguration.Validators.Count; i++)
        {
            _validators!.Add(new ValidatorWrapper<TModel, TValue>(TypedConfiguration.Validators[i]));
        }

        _wrappedInnerValidatorCount = TypedConfiguration.Validators.Count;
    }

    private sealed class ObjectValidatorAdapter : IFieldValidator<TModel, TValue>
    {
        private readonly IFieldValidator<TModel, object> _objectValidator;

        public ObjectValidatorAdapter(IFieldValidator<TModel, object> objectValidator)
        {
            _objectValidator = objectValidator;
        }

        public string? ErrorMessage
        {
            get => _objectValidator.ErrorMessage;
            set => _objectValidator.ErrorMessage = value;
        }

        public Task<ValidationResult> ValidateAsync(TModel model, TValue value, IServiceProvider services)
            => _objectValidator.ValidateAsync(model, value!, services);
    }

    /// <inheritdoc />
    public List<IFieldDependency<TModel>> Dependencies => TypedConfiguration.Dependencies;

    /// <inheritdoc />
    public Func<TModel, bool>? VisibilityCondition
    {
        get => TypedConfiguration.VisibilityCondition;
        set => TypedConfiguration.VisibilityCondition = value;
    }

    /// <inheritdoc />
    public Func<TModel, bool>? DisabledCondition
    {
        get => TypedConfiguration.DisabledCondition;
        set => TypedConfiguration.DisabledCondition = value;
    }

    /// <inheritdoc />
    public RenderFragment<IFieldContext<TModel, object>>? CustomTemplate
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            // Surface templates configured through the typed builder API by adapting
            // the object-typed render context to the typed one the template expects.
            var typedTemplate = TypedConfiguration.CustomTemplate;
            if (typedTemplate == null)
            {
                return null;
            }

            return objectContext => typedTemplate(new TypedFieldContextAdapter(objectContext, TypedConfiguration));
        }
        set;
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
        get => TypedConfiguration.CustomRendererType;
        set => TypedConfiguration.CustomRendererType = value;
    }

    /// <summary>
    /// Gets access to the original typed configuration.
    /// </summary>
    public IFieldConfiguration<TModel, TValue> TypedConfiguration { get; }

    /// <summary>
    /// Gets the actual runtime type of the field value.
    /// </summary>
    /// <returns>The Type of TValue, representing the actual field type.</returns>
    public Type GetActualFieldType() => typeof(TValue);
}
