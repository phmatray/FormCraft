using System.Linq.Expressions;
using DynamicFormBlazor.Forms.Core;
using DynamicFormBlazor.Forms.Validators;
using Microsoft.AspNetCore.Components;

namespace DynamicFormBlazor.Forms.Builders;

public class FieldBuilder<TModel, TValue> where TModel : new()
{
    private readonly FormBuilder<TModel> _formBuilder;
    private readonly FieldConfiguration<TModel, TValue> _fieldConfiguration;
    
    internal FieldBuilder(FormBuilder<TModel> formBuilder, FieldConfiguration<TModel, TValue> fieldConfiguration)
    {
        _formBuilder = formBuilder;
        _fieldConfiguration = fieldConfiguration;
    }
    
    public FieldBuilder<TModel, TValue> WithLabel(string label)
    {
        _fieldConfiguration.Label = label;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithPlaceholder(string placeholder)
    {
        _fieldConfiguration.Placeholder = placeholder;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithHelpText(string helpText)
    {
        _fieldConfiguration.HelpText = helpText;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithCssClass(string cssClass)
    {
        _fieldConfiguration.CssClass = cssClass;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> Required(string? errorMessage = null)
    {
        _fieldConfiguration.IsRequired = true;
        _fieldConfiguration.Validators.Add(new RequiredValidator<TModel, TValue>(errorMessage));
        return this;
    }
    
    public FieldBuilder<TModel, TValue> Disabled(bool disabled = true)
    {
        _fieldConfiguration.IsDisabled = disabled;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> ReadOnly(bool readOnly = true)
    {
        _fieldConfiguration.IsReadOnly = readOnly;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> VisibleWhen(Func<TModel, bool> condition)
    {
        _fieldConfiguration.VisibilityCondition = condition;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> DisabledWhen(Func<TModel, bool> condition)
    {
        _fieldConfiguration.DisabledCondition = condition;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithAttribute(string name, object value)
    {
        _fieldConfiguration.AdditionalAttributes[name] = value;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithAttributes(Dictionary<string, object> attributes)
    {
        foreach (var attr in attributes)
        {
            _fieldConfiguration.AdditionalAttributes[attr.Key] = attr.Value;
        }
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithValidator(IFieldValidator<TModel, TValue> validator)
    {
        _fieldConfiguration.Validators.Add(validator);
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithValidator(Func<TValue, bool> validation, string errorMessage)
    {
        _fieldConfiguration.Validators.Add(new CustomValidator<TModel, TValue>(validation, errorMessage));
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithAsyncValidator(Func<TValue, Task<bool>> validation, string errorMessage)
    {
        _fieldConfiguration.Validators.Add(new AsyncValidator<TModel, TValue>(validation, errorMessage));
        return this;
    }
    
    public FieldBuilder<TModel, TValue> DependsOn<TDependsOn>(
        Expression<Func<TModel, TDependsOn>> dependsOnExpression,
        Action<TModel, TDependsOn> onChanged)
    {
        var dependency = new FieldDependency<TModel, TDependsOn>(dependsOnExpression, onChanged);
        _fieldConfiguration.Dependencies.Add(dependency);
        _formBuilder.AddFieldDependency(_fieldConfiguration.FieldName, dependency);
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithCustomTemplate(RenderFragment<IFieldContext<TModel, TValue>> template)
    {
        _fieldConfiguration.CustomTemplate = template;
        return this;
    }
    
    public FieldBuilder<TModel, TValue> WithOrder(int order)
    {
        _fieldConfiguration.Order = order;
        return this;
    }
    
    // Return to FormBuilder to add more fields
    public FieldBuilder<TModel, TNewValue> AddField<TNewValue>(Expression<Func<TModel, TNewValue>> expression)
    {
        return _formBuilder.AddField(expression);
    }
    
    public IFormConfiguration<TModel> Build()
    {
        return _formBuilder.Build();
    }
}