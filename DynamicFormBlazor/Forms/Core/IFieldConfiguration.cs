using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace DynamicFormBlazor.Forms.Core;

public interface IFieldConfiguration<TModel, TValue>
{
    string FieldName { get; }
    Expression<Func<TModel, TValue>> ValueExpression { get; }
    string Label { get; set; }
    string? Placeholder { get; set; }
    string? HelpText { get; set; }
    string? CssClass { get; set; }
    bool IsRequired { get; set; }
    bool IsVisible { get; set; }
    bool IsDisabled { get; set; }
    bool IsReadOnly { get; set; }
    int Order { get; set; }
    
    Dictionary<string, object> AdditionalAttributes { get; }
    List<IFieldValidator<TModel, TValue>> Validators { get; }
    List<IFieldDependency<TModel>> Dependencies { get; }
    
    Func<TModel, bool>? VisibilityCondition { get; set; }
    Func<TModel, bool>? DisabledCondition { get; set; }
    
    RenderFragment<IFieldContext<TModel, TValue>>? CustomTemplate { get; set; }
}