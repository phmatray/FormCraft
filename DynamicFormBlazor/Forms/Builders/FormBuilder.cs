using System.Linq.Expressions;
using DynamicFormBlazor.Forms.Core;
using Microsoft.AspNetCore.Components;

namespace DynamicFormBlazor.Forms.Builders;

public class FormBuilder<TModel> where TModel : new()
{
    private readonly FormConfiguration<TModel> _configuration = new();
    private int _fieldOrder = 0;
    
    public static FormBuilder<TModel> Create() => new();
    
    public FieldBuilder<TModel, TValue> AddField<TValue>(Expression<Func<TModel, TValue>> expression)
    {
        var fieldConfig = new FieldConfiguration<TModel, TValue>(expression)
        {
            Order = _fieldOrder++
        };
        
        // Cast to object version for storage
        var objectConfig = new FieldConfigurationWrapper<TModel, TValue>(fieldConfig);
        _configuration.Fields.Add(objectConfig);
        
        return new FieldBuilder<TModel, TValue>(this, fieldConfig);
    }
    
    public FormBuilder<TModel> WithLayout(FormLayout layout)
    {
        _configuration.Layout = layout;
        return this;
    }
    
    public FormBuilder<TModel> WithCssClass(string cssClass)
    {
        _configuration.CssClass = cssClass;
        return this;
    }
    
    public FormBuilder<TModel> ShowValidationSummary(bool show = true)
    {
        _configuration.ShowValidationSummary = show;
        return this;
    }
    
    public FormBuilder<TModel> ShowRequiredIndicator(bool show = true, string indicator = "*")
    {
        _configuration.ShowRequiredIndicator = show;
        _configuration.RequiredIndicator = indicator;
        return this;
    }
    
    public IFormConfiguration<TModel> Build() => _configuration;
    
    internal void AddFieldDependency(string fieldName, IFieldDependency<TModel> dependency)
    {
        if (!_configuration.FieldDependencies.ContainsKey(fieldName))
        {
            _configuration.FieldDependencies[fieldName] = new List<IFieldDependency<TModel>>();
        }
        
        _configuration.FieldDependencies[fieldName].Add(dependency);
    }
}

// Wrapper to handle the generic to object casting
public class FieldConfigurationWrapper<TModel, TValue> : IFieldConfiguration<TModel, object>
{
    private readonly IFieldConfiguration<TModel, TValue> _inner;
    
    public FieldConfigurationWrapper(IFieldConfiguration<TModel, TValue> inner)
    {
        _inner = inner;
    }
    
    public string FieldName => _inner.FieldName;
    public Expression<Func<TModel, object>> ValueExpression => 
        Expression.Lambda<Func<TModel, object>>(
            Expression.Convert(_inner.ValueExpression.Body, typeof(object)), 
            _inner.ValueExpression.Parameters);
    public string Label { get => _inner.Label; set => _inner.Label = value; }
    public string? Placeholder { get => _inner.Placeholder; set => _inner.Placeholder = value; }
    public string? HelpText { get => _inner.HelpText; set => _inner.HelpText = value; }
    public string? CssClass { get => _inner.CssClass; set => _inner.CssClass = value; }
    public bool IsRequired { get => _inner.IsRequired; set => _inner.IsRequired = value; }
    public bool IsVisible { get => _inner.IsVisible; set => _inner.IsVisible = value; }
    public bool IsDisabled { get => _inner.IsDisabled; set => _inner.IsDisabled = value; }
    public bool IsReadOnly { get => _inner.IsReadOnly; set => _inner.IsReadOnly = value; }
    public int Order { get => _inner.Order; set => _inner.Order = value; }
    
    public Dictionary<string, object> AdditionalAttributes => _inner.AdditionalAttributes;
    public List<IFieldValidator<TModel, object>> Validators => 
        _inner.Validators.Select<IFieldValidator<TModel, TValue>, IFieldValidator<TModel, object>>(v => new ValidatorWrapper<TModel, TValue>(v)).ToList();
    public List<IFieldDependency<TModel>> Dependencies => _inner.Dependencies;
    
    public Func<TModel, bool>? VisibilityCondition 
    { 
        get => _inner.VisibilityCondition; 
        set => _inner.VisibilityCondition = value; 
    }
    public Func<TModel, bool>? DisabledCondition 
    { 
        get => _inner.DisabledCondition; 
        set => _inner.DisabledCondition = value; 
    }
    
    public RenderFragment<IFieldContext<TModel, object>>? CustomTemplate 
    { 
        get => null; // Simplified for now
        set { } // Simplified for now
    }
    
    // Access to the original typed configuration
    public IFieldConfiguration<TModel, TValue> TypedConfiguration => _inner;
    
    // Helper method to get the actual field type
    public Type GetActualFieldType() => typeof(TValue);
}

// Wrapper to handle validator type conversion
public class ValidatorWrapper<TModel, TValue> : IFieldValidator<TModel, object>
{
    private readonly IFieldValidator<TModel, TValue> _inner;
    
    public ValidatorWrapper(IFieldValidator<TModel, TValue> inner)
    {
        _inner = inner;
    }
    
    public string? ErrorMessage 
    { 
        get => _inner.ErrorMessage; 
        set => _inner.ErrorMessage = value; 
    }
    
    public async Task<ValidationResult> ValidateAsync(TModel model, object value, IServiceProvider services)
    {
        // Convert object back to TValue for the inner validator
        TValue typedValue;
        try
        {
            typedValue = (TValue)(value ?? default(TValue)!);
        }
        catch
        {
            typedValue = default(TValue)!;
        }
        
        return await _inner.ValidateAsync(model, typedValue, services);
    }
}