using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace FormCraft;

/// <summary>
/// Default implementation of the field renderer service that coordinates multiple field renderers.
/// </summary>
public class FieldRendererService : IFieldRendererService
{
    private readonly IEnumerable<IFieldRenderer> _renderers;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the FieldRendererService class.
    /// </summary>
    /// <param name="renderers">Collection of field renderers available for rendering different field types.</param>
    /// <param name="serviceProvider">Service provider for resolving custom renderers.</param>
    public FieldRendererService(IEnumerable<IFieldRenderer> renderers, IServiceProvider serviceProvider)
    {
        _renderers = renderers;
        _serviceProvider = serviceProvider;
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

        // Check for custom renderer first
        if (field.CustomRendererType != null)
        {
            var customRenderer = TryResolveCustomRenderer(field.CustomRendererType, fieldType);
            if (customRenderer != null)
            {
                return customRenderer.Render(context);
            }
        }

        // Fall back to standard renderers
        var renderer = _renderers.FirstOrDefault(r => r.CanRender(fieldType, null!));
        if (renderer != null)
        {
            return renderer.Render(context);
        }

        return builder => builder.AddContent(0, $"Unsupported field type: {fieldType.Name} for field: {field.FieldName}");
    }

    private ICustomFieldRenderer? TryResolveCustomRenderer(Type rendererType, Type fieldType)
    {
        try
        {
            // First try to resolve from service provider
            var renderer = _serviceProvider.GetService(rendererType);
            if (renderer is ICustomFieldRenderer customRenderer && IsValidForFieldType(customRenderer, fieldType))
            {
                return customRenderer;
            }

            // If not registered, try to create an instance
            if (rendererType.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(rendererType);
                if (instance is ICustomFieldRenderer createdRenderer && IsValidForFieldType(createdRenderer, fieldType))
                {
                    return createdRenderer;
                }
            }
        }
        catch
        {
            // Log error or handle appropriately
        }

        return null;
    }

    private static bool IsValidForFieldType(ICustomFieldRenderer renderer, Type fieldType)
    {
        return renderer.ValueType.IsAssignableFrom(fieldType);
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