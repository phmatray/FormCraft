using System.Linq.Expressions;

namespace FormCraft.Forms.Core;

/// <summary>
/// Defines the contract for field dependencies that allow one field to react to changes in another field.
/// </summary>
/// <typeparam name="TModel">The model type that contains the dependent fields.</typeparam>
/// <example>
/// <code>
/// // Example: Show/hide a field based on another field's value
/// .AddField(x => x.HasSpouse)
///     .WithLabel("Are you married?");
/// 
/// .AddField(x => x.SpouseName)
///     .WithLabel("Spouse Name")
///     .DependsOn(x => x.HasSpouse, (model, hasSpouse) => 
///     {
///         // This field is only visible when HasSpouse is true
///         model.SpouseName = hasSpouse ? model.SpouseName : null;
///     })
///     .VisibleWhen(x => x.HasSpouse);
/// </code>
/// </example>
public interface IFieldDependency<TModel>
{
    /// <summary>
    /// Gets the name of the field that this dependency depends on.
    /// </summary>
    string DependentFieldName { get; }
    
    /// <summary>
    /// Called when the dependent field's value changes, allowing this field to react accordingly.
    /// </summary>
    /// <param name="model">The complete model instance containing both fields.</param>
    void OnDependencyChanged(TModel model);
}

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