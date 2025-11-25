using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Provides a fluent API for building field groups within a form.
/// </summary>
/// <typeparam name="TModel">The model type that the form will bind to.</typeparam>
public class FieldGroupBuilder<TModel> where TModel : new()
{
    private readonly FormBuilder<TModel> _formBuilder;
    private readonly FieldGroup<TModel> _fieldGroup;

    /// <summary>
    /// Initializes a new instance of the FieldGroupBuilder class.
    /// </summary>
    /// <param name="formBuilder">The parent form builder.</param>
    /// <param name="fieldGroup">The field group being configured.</param>
    internal FieldGroupBuilder(FormBuilder<TModel> formBuilder, FieldGroup<TModel> fieldGroup)
    {
        _formBuilder = formBuilder;
        _fieldGroup = fieldGroup;
    }

    /// <summary>
    /// Adds a field to this group with configuration and returns the FieldGroupBuilder for method chaining.
    /// </summary>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <param name="expression">A lambda expression that identifies the property.</param>
    /// <param name="fieldConfig">A lambda expression to configure the field.</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> AddField<TValue>(
        Expression<Func<TModel, TValue>> expression,
        Action<FieldBuilder<TModel, TValue>>? fieldConfig = null)
    {
        // Add field through the form builder with configuration
        _formBuilder.AddField(expression, fieldConfig);

        // Get the field name from the expression
        if (expression.Body is MemberExpression memberExpression)
        {
            _fieldGroup.FieldNames.Add(memberExpression.Member.Name);
        }

        return this;
    }

    /// <summary>
    /// Sets the display name for this field group.
    /// </summary>
    /// <param name="name">The name to display for this group.</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> WithGroupName(string name)
    {
        _fieldGroup.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the number of columns for this group's grid layout.
    /// </summary>
    /// <param name="columns">The number of columns (1-6).</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> WithColumns(int columns)
    {
        _fieldGroup.Columns = columns;
        return this;
    }

    /// <summary>
    /// Sets the CSS class for this field group's container.
    /// </summary>
    /// <param name="cssClass">The CSS class to apply.</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> WithCssClass(string cssClass)
    {
        _fieldGroup.CssClass = cssClass;
        return this;
    }

    /// <summary>
    /// Configures the group to render its fields within a card/panel.
    /// </summary>
    /// <param name="elevation">The elevation for the card (0-24). Default is 1.</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> ShowInCard(int elevation = 1)
    {
        _fieldGroup.ShowCard = true;
        _fieldGroup.CardElevation = elevation;
        return this;
    }

    /// <summary>
    /// Sets the order in which this group should be rendered.
    /// </summary>
    /// <param name="order">The order value (lower values render first).</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> WithOrder(int order)
    {
        _fieldGroup.Order = order;
        return this;
    }

    /// <summary>
    /// Sets custom content to display to the right of the group name.
    /// This can be used for tooltips, info icons, or other UI elements.
    /// </summary>
    /// <param name="headerRightContent">The render fragment containing the custom content.</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> WithHeaderRightContent(RenderFragment headerRightContent)
    {
        _fieldGroup.HeaderRightContent = headerRightContent;
        return this;
    }

    /// <summary>
    /// Sets custom content to display to the right of the group name using a component type.
    /// This can be used for tooltips, info icons, or other UI elements.
    /// </summary>
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> WithHeaderRightContent<TComponent>() where TComponent : IComponent
    {
        _fieldGroup.HeaderRightContent = builder =>
        {
            builder.OpenComponent<TComponent>(0);
            builder.CloseComponent();
        };
        return this;
    }

    /// <summary>
    /// Sets custom content to display to the right of the group name using a component type with parameters.
    /// This can be used for tooltips, info icons, or other UI elements.
    /// </summary>
    /// <typeparam name="TComponent">The component type to render.</typeparam>
    /// <param name="parameters">Action to configure component parameters.</param>
    /// <returns>The FieldGroupBuilder instance for method chaining.</returns>
    public FieldGroupBuilder<TModel> WithHeaderRightContent<TComponent>(Action<Dictionary<string, object>> parameters) where TComponent : IComponent
    {
        _fieldGroup.HeaderRightContent = builder =>
        {
            builder.OpenComponent<TComponent>(0);

            var paramDict = new Dictionary<string, object>();
            parameters(paramDict);

            var sequence = 1;
            foreach (var param in paramDict)
            {
                builder.AddAttribute(sequence++, param.Key, param.Value);
            }

            builder.CloseComponent();
        };
        return this;
    }
}