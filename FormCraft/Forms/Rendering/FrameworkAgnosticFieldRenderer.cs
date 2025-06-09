using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace FormCraft;

/// <summary>
/// Abstract base class for framework-agnostic field renderers.
/// </summary>
/// <typeparam name="TComponent">The type of the component to render.</typeparam>
public abstract class FrameworkAgnosticFieldRenderer<TComponent> : IFieldRenderer
    where TComponent : IComponent
{
    /// <summary>
    /// Gets the UI framework adapter to use for rendering.
    /// </summary>
    protected IUIFrameworkAdapter UIFramework { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FrameworkAgnosticFieldRenderer{TComponent}"/> class.
    /// </summary>
    /// <param name="uiFramework">The UI framework adapter.</param>
    protected FrameworkAgnosticFieldRenderer(IUIFrameworkAdapter uiFramework)
    {
        UIFramework = uiFramework ?? throw new ArgumentNullException(nameof(uiFramework));
    }
    
    /// <inheritdoc />
    public virtual bool CanRender(Type fieldType, IFieldConfiguration<object, object> field) => GetSupportedTypes().Contains(fieldType);
    
    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            var sequence = 0;
            var componentType = typeof(TComponent);
            
            // Handle generic component types
            if (componentType.IsGenericTypeDefinition)
            {
                componentType = componentType.MakeGenericType(typeof(TModel));
            }
            
            builder.OpenComponent(sequence++, componentType);
            builder.AddAttribute(sequence++, "Context", context);
            
            // Add any additional attributes specific to this renderer
            AddAdditionalAttributes(builder, ref sequence, context);
            
            builder.CloseComponent();
        };
    }
    
    /// <summary>
    /// Gets the types supported by this renderer.
    /// </summary>
    /// <returns>A collection of supported types.</returns>
    protected abstract IEnumerable<Type> GetSupportedTypes();
    
    /// <summary>
    /// Adds additional attributes to the component being rendered.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="builder">The render tree builder.</param>
    /// <param name="sequence">The current sequence number.</param>
    /// <param name="context">The field render context.</param>
    protected virtual void AddAdditionalAttributes<TModel>(RenderTreeBuilder builder, ref int sequence, IFieldRenderContext<TModel> context)
    {
        // Default implementation does nothing
        // Derived classes can override to add specific attributes
    }
}