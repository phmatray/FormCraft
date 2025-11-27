using System.Linq.Expressions;
using System.Reflection;

namespace FormCraft;

/// <summary>
/// Typed implementation of field mapping with compile-time safety.
/// Maps a property from the LOV item to a property on the model.
/// </summary>
/// <typeparam name="TItem">The type of the LOV item.</typeparam>
/// <typeparam name="TModel">The type of the model.</typeparam>
/// <typeparam name="TValue">The type of the mapped value.</typeparam>
public class LovFieldMapping<TItem, TModel, TValue> : ILovFieldMapping
{
    private readonly Func<TItem, TValue> _sourceSelector;
    private readonly Action<TModel, TValue> _targetSetter;

    /// <inheritdoc />
    public string SourceProperty { get; }

    /// <inheritdoc />
    public string TargetProperty { get; }

    /// <inheritdoc />
    public bool IsAsync => false;

    /// <summary>
    /// Initializes a new instance of the LovFieldMapping class.
    /// </summary>
    /// <param name="sourceExpression">Expression to select the source value from the item.</param>
    /// <param name="targetExpression">Expression to identify the target property on the model.</param>
    public LovFieldMapping(
        Expression<Func<TItem, TValue>> sourceExpression,
        Expression<Func<TModel, TValue>> targetExpression)
    {
        SourceProperty = GetPropertyName(sourceExpression);
        TargetProperty = GetPropertyName(targetExpression);
        _sourceSelector = sourceExpression.Compile();
        _targetSetter = CreateSetter(targetExpression);
    }

    /// <inheritdoc />
    public void Apply(object item, object model)
    {
        if (item is TItem typedItem && model is TModel typedModel)
        {
            var value = _sourceSelector(typedItem);
            _targetSetter(typedModel, value);
        }
    }

    private static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operandMember)
        {
            return operandMember.Member.Name;
        }

        throw new ArgumentException(
            "Expression must be a simple property access expression (e.g., x => x.Property)",
            nameof(expression));
    }

    private static Action<TModel, TValue> CreateSetter(Expression<Func<TModel, TValue>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression &&
            memberExpression.Member is PropertyInfo propertyInfo)
        {
            return (model, value) => propertyInfo.SetValue(model, value);
        }

        if (propertyExpression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operandMember &&
            operandMember.Member is PropertyInfo unaryPropertyInfo)
        {
            return (model, value) => unaryPropertyInfo.SetValue(model, value);
        }

        throw new ArgumentException(
            "Expression must be a simple property access expression (e.g., x => x.Property)",
            nameof(propertyExpression));
    }
}

/// <summary>
/// Async field mapping that can perform additional operations during mapping.
/// </summary>
/// <typeparam name="TItem">The type of the LOV item.</typeparam>
/// <typeparam name="TModel">The type of the model.</typeparam>
/// <typeparam name="TValue">The type of the mapped value.</typeparam>
public class AsyncLovFieldMapping<TItem, TModel, TValue> : IAsyncLovFieldMapping
{
    private readonly Func<TItem, TValue> _sourceSelector;
    private readonly Action<TModel, TValue> _targetSetter;
    private readonly Func<TItem, TModel, IServiceProvider, CancellationToken, Task>? _asyncAction;

    /// <inheritdoc />
    public string SourceProperty { get; }

    /// <inheritdoc />
    public string TargetProperty { get; }

    /// <inheritdoc />
    public bool IsAsync => true;

    /// <summary>
    /// Initializes a new instance of the AsyncLovFieldMapping class.
    /// </summary>
    /// <param name="sourceExpression">Expression to select the source value from the item.</param>
    /// <param name="targetExpression">Expression to identify the target property on the model.</param>
    /// <param name="asyncAction">Optional async action to perform additional operations.</param>
    public AsyncLovFieldMapping(
        Expression<Func<TItem, TValue>> sourceExpression,
        Expression<Func<TModel, TValue>> targetExpression,
        Func<TItem, TModel, IServiceProvider, CancellationToken, Task>? asyncAction = null)
    {
        SourceProperty = GetPropertyName(sourceExpression);
        TargetProperty = GetPropertyName(targetExpression);
        _sourceSelector = sourceExpression.Compile();
        _targetSetter = CreateSetter(targetExpression);
        _asyncAction = asyncAction;
    }

    /// <inheritdoc />
    public void Apply(object item, object model)
    {
        if (item is TItem typedItem && model is TModel typedModel)
        {
            var value = _sourceSelector(typedItem);
            _targetSetter(typedModel, value);
        }
    }

    /// <inheritdoc />
    public async Task ApplyAsync(
        object item,
        object model,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        if (item is TItem typedItem && model is TModel typedModel)
        {
            // First apply the basic mapping
            var value = _sourceSelector(typedItem);
            _targetSetter(typedModel, value);

            // Then execute async action if provided
            if (_asyncAction != null)
            {
                await _asyncAction(typedItem, typedModel, services, cancellationToken);
            }
        }
    }

    private static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operandMember)
        {
            return operandMember.Member.Name;
        }

        throw new ArgumentException(
            "Expression must be a simple property access expression",
            nameof(expression));
    }

    private static Action<TModel, TValue> CreateSetter(Expression<Func<TModel, TValue>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression &&
            memberExpression.Member is PropertyInfo propertyInfo)
        {
            return (model, value) => propertyInfo.SetValue(model, value);
        }

        if (propertyExpression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression operandMember &&
            operandMember.Member is PropertyInfo unaryPropertyInfo)
        {
            return (model, value) => unaryPropertyInfo.SetValue(model, value);
        }

        throw new ArgumentException(
            "Expression must be a simple property access expression",
            nameof(propertyExpression));
    }
}
