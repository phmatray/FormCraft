using Microsoft.AspNetCore.Components;
using FormCraft.Forms.Core;

namespace FormCraft.Forms.Rendering;

/// <summary>
/// Provides field rendering services by coordinating multiple field renderers to generate appropriate UI components.
/// </summary>
public interface IFieldRendererService
{
    /// <summary>
    /// Renders a form field by selecting the appropriate field renderer and generating the UI component.
    /// </summary>
    /// <typeparam name="TModel">The model type that contains the field.</typeparam>
    /// <param name="model">The model instance containing the field value.</param>
    /// <param name="field">The field configuration with metadata and validation rules.</param>
    /// <param name="onValueChanged">Callback to invoke when the field value changes.</param>
    /// <param name="onDependencyChanged">Callback to invoke when field dependencies change.</param>
    /// <returns>A RenderFragment that generates the appropriate UI component for the field.</returns>
    RenderFragment RenderField<TModel>(TModel model, IFieldConfiguration<TModel, object> field, 
        EventCallback<object?> onValueChanged, EventCallback onDependencyChanged);
}

/// <summary>
/// Default implementation of the field renderer service that coordinates multiple field renderers.
/// </summary>
public class FieldRendererService : IFieldRendererService
{
    private readonly IEnumerable<IFieldRenderer> _renderers;

    /// <summary>
    /// Initializes a new instance of the FieldRendererService class.
    /// </summary>
    /// <param name="renderers">Collection of field renderers available for rendering different field types.</param>
    public FieldRendererService(IEnumerable<IFieldRenderer> renderers)
    {
        _renderers = renderers;
    }

    /// <inheritdoc />
    public RenderFragment RenderField<TModel>(TModel model, IFieldConfiguration<TModel, object> field, 
        EventCallback<object?> onValueChanged, EventCallback onDependencyChanged)
    {
        var fieldType = GetActualFieldType(field);
        var currentValue = GetCurrentValue(model, field);
        
        var context = new FieldRenderContext<TModel>
        {
            Model = model,
            Field = field,
            ActualFieldType = fieldType,
            CurrentValue = currentValue,
            OnValueChanged = onValueChanged,
            OnDependencyChanged = onDependencyChanged,
        };

        var renderer = _renderers.FirstOrDefault(r => r.CanRender(fieldType, null!));
        if (renderer != null)
        {
            return renderer.Render(context);
        }

        return builder => builder.AddContent(0, $"Unsupported field type: {fieldType.Name} for field: {field.FieldName}");
    }

    private static Type GetActualFieldType<TModel>(IFieldConfiguration<TModel, object> field)
    {
        var wrapperType = field.GetType();
        if (wrapperType.IsGenericType && wrapperType.GetGenericTypeDefinition().Name.Contains("FieldConfigurationWrapper"))
        {
            var getActualFieldTypeMethod = wrapperType.GetMethod("GetActualFieldType");
            if (getActualFieldTypeMethod != null)
            {
                return (Type)getActualFieldTypeMethod.Invoke(field, null)!;
            }
            
            var property = typeof(TModel).GetProperty(field.FieldName);
            return property?.PropertyType ?? typeof(object);
        }
        
        return field.ValueExpression.Body.Type;
    }

    private static object? GetCurrentValue<TModel>(TModel model, IFieldConfiguration<TModel, object> field)
    {
        var getter = field.ValueExpression.Compile();
        return getter(model);
    }
}