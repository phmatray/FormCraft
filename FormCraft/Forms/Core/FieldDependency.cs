using System.Linq.Expressions;

namespace FormCraft;

/// <summary>
/// Default implementation of field dependency that executes a callback when a dependent field changes.
/// </summary>
/// <typeparam name="TModel">The model type that contains the dependent fields.</typeparam>
/// <typeparam name="TDependsOn">The type of the field value that this dependency depends on.</typeparam>
public class FieldDependency<TModel, TDependsOn> : IFieldDependency<TModel>
{
    private readonly Expression<Func<TModel, TDependsOn>> _dependsOnExpression;
    private readonly Action<TModel, TDependsOn> _onChanged;

    /// <inheritdoc />
    public string DependentFieldName { get; }

    /// <summary>
    /// Initializes a new instance of the FieldDependency class.
    /// </summary>
    /// <param name="dependsOnExpression">A lambda expression identifying the field that this dependency depends on.</param>
    /// <param name="onChanged">The callback to execute when the dependent field changes, receiving the model and the new value.</param>
    /// <exception cref="ArgumentException">Thrown when the expression does not represent a valid property access.</exception>
    public FieldDependency(
        Expression<Func<TModel, TDependsOn>> dependsOnExpression,
        Action<TModel, TDependsOn> onChanged)
    {
        _dependsOnExpression = dependsOnExpression;
        _onChanged = onChanged;

        var memberExpression = dependsOnExpression.Body as MemberExpression;
        DependentFieldName = memberExpression?.Member.Name ?? throw new ArgumentException("Invalid expression");
    }

    /// <inheritdoc />
    public void OnDependencyChanged(TModel model)
    {
        var func = _dependsOnExpression.Compile();
        var value = func(model);
        _onChanged(model, value);
    }
}