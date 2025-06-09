using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Minimal implementation of IFieldConfiguration for renderer checks.
/// </summary>
internal class MinimalFieldConfiguration : IFieldConfiguration<object, object>
{
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
    public string? GroupName { get; set; }
    public string? CssClass { get; set; }
    public Dictionary<string, object> AdditionalAttributes { get; set; } = new();
    public List<IFieldValidator<object, object>> Validators { get; set; } = new();
    public List<IFieldDependency<object>> Dependencies { get; set; } = new();
    public Expression<Func<object, object>> ValueExpression { get; set; } = obj => obj;
    public Type? CustomRendererType { get; set; }
    public ICustomFieldRenderer? CustomRenderer { get; set; }
    public Func<object, Dictionary<string, object>>? DynamicAttributes { get; set; }
    public FileUploadConfiguration? FileUploadConfig { get; set; }
    public List<SelectOption<object>>? SelectOptions { get; set; }
    public Func<object, bool>? VisibilityCondition { get; set; }
    public Func<object, bool>? DisabledCondition { get; set; }
    public RenderFragment<IFieldContext<object, object>>? CustomTemplate { get; set; }
}