using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Test stub implementation of a rating renderer.
/// This is used for unit testing and should not be used in production.
/// </summary>
/// <remarks>
/// For production use, use the MudBlazor implementation from FormCraft.ForMudBlazor.
/// </remarks>
public class RatingRenderer : CustomFieldRendererBase<int>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "test-rating");
            
            // For testing, just use simple labels
            builder.OpenElement(sequence++, "label");
            builder.AddContent(sequence++, "Rating");
            builder.CloseElement();
            
            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "type", "range");
            builder.AddAttribute(sequence++, "min", "0");
            builder.AddAttribute(sequence++, "max", "5");
            builder.AddAttribute(sequence, "value", GetValue(context));
            builder.CloseElement();
            
            builder.CloseElement();
        };
    }
}