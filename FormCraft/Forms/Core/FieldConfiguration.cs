using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Default implementation of field configuration that stores all field metadata, attributes, and behavior.
/// </summary>
/// <typeparam name="TModel">The model type that contains the field.</typeparam>
/// <typeparam name="TValue">The type of the field value.</typeparam>
public class FieldConfiguration<TModel, TValue> : IFieldConfiguration<TModel, TValue>
{
    /// <inheritdoc />
    public string FieldName { get; }

    /// <inheritdoc />
    public Expression<Func<TModel, TValue>> ValueExpression { get; }

    /// <inheritdoc />
    public string Label { get; set; }

    /// <inheritdoc />
    public string? Placeholder { get; set; }

    /// <inheritdoc />
    public string? HelpText { get; set; }

    /// <inheritdoc />
    public string? CssClass { get; set; }

    /// <inheritdoc />
    public bool IsRequired { get; set; }

    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;

    /// <inheritdoc />
    public bool IsDisabled { get; set; }

    /// <inheritdoc />
    public bool IsReadOnly { get; set; }

    /// <inheritdoc />
    public int Order { get; set; }

    /// <inheritdoc />
    public Dictionary<string, object> AdditionalAttributes { get; } = new();

    /// <inheritdoc />
    public List<IFieldValidator<TModel, TValue>> Validators { get; } = new();

    /// <inheritdoc />
    public List<IFieldDependency<TModel>> Dependencies { get; } = new();

    /// <inheritdoc />
    public Func<TModel, bool>? VisibilityCondition { get; set; }

    /// <inheritdoc />
    public Func<TModel, bool>? DisabledCondition { get; set; }

    /// <inheritdoc />
    public RenderFragment<IFieldContext<TModel, TValue>>? CustomTemplate { get; set; }

    /// <summary>
    /// Gets or sets the custom field renderer type for this field.
    /// When specified, this renderer is used instead of the default field renderer.
    /// </summary>
    public Type? CustomRendererType { get; set; }

    /// <summary>
    /// Initializes a new instance of the FieldConfiguration class.
    /// </summary>
    /// <param name="valueExpression">A lambda expression that identifies the property on the model this field represents.</param>
    /// <exception cref="ArgumentException">Thrown when the expression does not represent a valid property access.</exception>
    public FieldConfiguration(Expression<Func<TModel, TValue>> valueExpression)
    {
        ValueExpression = valueExpression;

        var memberExpression = valueExpression.Body as MemberExpression;
        FieldName = memberExpression?.Member.Name ?? throw new ArgumentException("Invalid expression");
        Label = FieldName;
    }
}