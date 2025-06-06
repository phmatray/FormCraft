using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace DynamicFormBlazor.Forms.Core;

public class FieldConfiguration<TModel, TValue> : IFieldConfiguration<TModel, TValue>
{
    public string FieldName { get; }
    public Expression<Func<TModel, TValue>> ValueExpression { get; }
    public string Label { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string? CssClass { get; set; }
    public bool IsRequired { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsDisabled { get; set; }
    public bool IsReadOnly { get; set; }
    public int Order { get; set; }
    
    public Dictionary<string, object> AdditionalAttributes { get; } = new();
    public List<IFieldValidator<TModel, TValue>> Validators { get; } = new();
    public List<IFieldDependency<TModel>> Dependencies { get; } = new();
    
    public Func<TModel, bool>? VisibilityCondition { get; set; }
    public Func<TModel, bool>? DisabledCondition { get; set; }
    
    public RenderFragment<IFieldContext<TModel, TValue>>? CustomTemplate { get; set; }
    
    public FieldConfiguration(Expression<Func<TModel, TValue>> valueExpression)
    {
        ValueExpression = valueExpression;
        
        var memberExpression = valueExpression.Body as MemberExpression;
        FieldName = memberExpression?.Member.Name ?? throw new ArgumentException("Invalid expression");
        Label = FieldName;
    }
}