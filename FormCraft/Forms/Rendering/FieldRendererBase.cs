using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FormCraft;

/// <summary>
/// Base class for field renderers that provides common functionality.
/// </summary>
public abstract class FieldRendererBase : IFieldRenderer
{
    /// <summary>
    /// Gets the type of the Razor component that renders this field.
    /// </summary>
    protected abstract Type ComponentType { get; }
    
    /// <inheritdoc />
    public abstract bool CanRender(Type fieldType, IFieldConfiguration<object, object> field);
    
    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            var sequence = 0;
            
            // If ComponentType is a generic type definition, make it concrete
            var componentType = ComponentType;
            if (componentType.IsGenericTypeDefinition)
            {
                var genericArgs = componentType.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    // Single generic argument - just TModel
                    componentType = componentType.MakeGenericType(typeof(TModel));
                }
                else if (genericArgs.Length == 2)
                {
                    // Two generic arguments - TModel and TValue
                    // Get the actual field type from the context
                    var valueType = context.ActualFieldType;
                    
                    // For nullable types, we need to use the underlying type for components with struct constraints
                    var underlyingType = Nullable.GetUnderlyingType(valueType);
                    if (underlyingType != null && HasStructConstraint(componentType, 1))
                    {
                        valueType = underlyingType;
                    }
                    
                    componentType = componentType.MakeGenericType(typeof(TModel), valueType);
                }
                else
                {
                    throw new NotSupportedException($"Component type {ComponentType} has {genericArgs.Length} generic arguments, which is not supported.");
                }
            }
            
            builder.OpenComponent(sequence++, componentType);
            builder.AddAttribute(sequence++, "Context", context);
            
            // Add any additional parameters specific to the renderer
            AddComponentParameters(builder, ref sequence, context);
            
            builder.CloseComponent();
        };
    }
    
    /// <summary>
    /// Override to add additional parameters to the component.
    /// </summary>
    protected virtual void AddComponentParameters<TModel>(RenderTreeBuilder builder, ref int sequence, IFieldRenderContext<TModel> context)
    {
        // Base implementation does nothing
    }
    
    /// <summary>
    /// Checks if a generic type parameter has a struct constraint.
    /// </summary>
    private static bool HasStructConstraint(Type genericType, int parameterIndex)
    {
        var genericArgs = genericType.GetGenericArguments();
        if (parameterIndex >= genericArgs.Length)
            return false;
            
        var attributes = genericArgs[parameterIndex].GenericParameterAttributes;
        
        // Check for the NotNullableValueTypeConstraint flag which indicates struct constraint
        return (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
    }
}

/// <summary>
/// Base class for typed field renderers.
/// </summary>
/// <typeparam name="TValue">The type of value this renderer handles.</typeparam>
public abstract class FieldRendererBase<TValue> : FieldRendererBase
{
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(TValue) || 
               (typeof(TValue).IsAssignableFrom(fieldType) && CanRenderDerivedType(fieldType));
    }
    
    /// <summary>
    /// Override to allow rendering of derived types.
    /// </summary>
    protected virtual bool CanRenderDerivedType(Type fieldType) => false;
}