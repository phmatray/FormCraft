using Microsoft.AspNetCore.Components;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the rating field renderer.
/// Can be used as a custom renderer for int fields to provide star rating.
/// </summary>
public class MudBlazorRatingRenderer : CustomFieldRendererBase<int>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(MudBlazorRatingComponent<>).MakeGenericType(context.Model.GetType()));
            builder.AddAttribute(1, "Context", context);
            builder.CloseComponent();
        };
    }
}

/// <summary>
/// Alternative rating renderer that extends FieldRendererBase for automatic registration.
/// </summary>
public class MudBlazorRatingFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorRatingComponent<>);
    
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(int) && 
               field.AdditionalAttributes.ContainsKey("rating");
    }
}