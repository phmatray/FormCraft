using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Default implementation of field dependency that executes a callback when a dependent field changes.
/// Supports both synchronous and asynchronous callbacks.
/// </summary>
/// <typeparam name="TModel">The model type that contains the dependent fields.</typeparam>
/// <typeparam name="TDependsOn">The type of the field value that this dependency depends on.</typeparam>
public class FieldDependency<TModel, TDependsOn> : IFieldDependency<TModel>
{
    private readonly Expression<Func<TModel, TDependsOn>> _dependsOnExpression;
    private readonly Action<TModel, TDependsOn>? _onChanged;
    private readonly Func<TModel, TDependsOn, Task>? _onChangedAsync;
    private Func<TModel, TDependsOn>? _compiledAccessor;

    /// <inheritdoc />
    public string DependentFieldName { get; }

    /// <summary>
    /// Initializes a new instance of the FieldDependency class with a synchronous callback.
    /// </summary>
    /// <param name="dependsOnExpression">A lambda expression identifying the field that this dependency depends on.</param>
    /// <param name="onChanged">The callback to execute when the dependent field changes, receiving the model and the new value.</param>
    /// <exception cref="ArgumentException">Thrown when the expression does not represent a valid property access.</exception>
    public FieldDependency(
        Expression<Func<TModel, TDependsOn>> dependsOnExpression,
        Action<TModel, TDependsOn> onChanged)
        : this(dependsOnExpression)
    {
        _onChanged = onChanged;
    }

    /// <summary>
    /// Initializes a new instance of the FieldDependency class with an asynchronous callback.
    /// </summary>
    /// <param name="dependsOnExpression">A lambda expression identifying the field that this dependency depends on.</param>
    /// <param name="onChangedAsync">The async callback to execute when the dependent field changes, receiving the model and the new value.</param>
    /// <exception cref="ArgumentException">Thrown when the expression does not represent a valid property access.</exception>
    public FieldDependency(
        Expression<Func<TModel, TDependsOn>> dependsOnExpression,
        Func<TModel, TDependsOn, Task> onChangedAsync)
        : this(dependsOnExpression)
    {
        _onChangedAsync = onChangedAsync;
    }

    private FieldDependency(Expression<Func<TModel, TDependsOn>> dependsOnExpression)
    {
        _dependsOnExpression = dependsOnExpression;

        var memberExpression = dependsOnExpression.Body as MemberExpression;
        DependentFieldName = memberExpression?.Member.Name ?? throw new ArgumentException("Invalid expression");
    }

    /// <inheritdoc />
    /// <remarks>
    /// When this dependency was configured with an asynchronous callback, this synchronous
    /// entry point blocks until the callback completes. Prefer <see cref="OnDependencyChangedAsync"/>
    /// when async callbacks may be involved.
    /// </remarks>
    public void OnDependencyChanged(TModel model)
    {
        var value = GetDependentValue(model);
        if (_onChanged != null)
        {
            _onChanged(model, value);
        }
        else if (_onChangedAsync != null)
        {
            _onChangedAsync(model, value).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public Task OnDependencyChangedAsync(TModel model)
    {
        var value = GetDependentValue(model);
        if (_onChangedAsync != null)
        {
            return _onChangedAsync(model, value);
        }

        _onChanged?.Invoke(model, value);
        return Task.CompletedTask;
    }

    private TDependsOn GetDependentValue(TModel model)
    {
        _compiledAccessor ??= _dependsOnExpression.Compile();
        return _compiledAccessor(model);
    }
}
