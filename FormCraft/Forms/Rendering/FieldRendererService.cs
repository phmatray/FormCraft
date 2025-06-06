using Microsoft.AspNetCore.Components;
using FormCraft.Forms.Abstractions;
using FormCraft.Forms.Core;

namespace FormCraft.Forms.Rendering;

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