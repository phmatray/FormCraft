using System.Linq.Expressions;

namespace DynamicFormBlazor.Forms.Core;

public interface IFieldDependency<TModel>
{
    string DependentFieldName { get; }
    void OnDependencyChanged(TModel model);
}

public class FieldDependency<TModel, TDependsOn> : IFieldDependency<TModel>
{
    private readonly Expression<Func<TModel, TDependsOn>> _dependsOnExpression;
    private readonly Action<TModel, TDependsOn> _onChanged;
    
    public string DependentFieldName { get; }
    
    public FieldDependency(
        Expression<Func<TModel, TDependsOn>> dependsOnExpression,
        Action<TModel, TDependsOn> onChanged)
    {
        _dependsOnExpression = dependsOnExpression;
        _onChanged = onChanged;
        
        var memberExpression = dependsOnExpression.Body as MemberExpression;
        DependentFieldName = memberExpression?.Member.Name ?? throw new ArgumentException("Invalid expression");
    }
    
    public void OnDependencyChanged(TModel model)
    {
        var func = _dependsOnExpression.Compile();
        var value = func(model);
        _onChanged(model, value);
    }
}