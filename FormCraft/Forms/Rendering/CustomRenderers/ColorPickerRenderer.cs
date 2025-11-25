using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Test stub implementation of a color picker renderer.
/// This is used for unit testing and should not be used in production.
/// </summary>
/// <remarks>
/// For production use, use the MudBlazor implementation from FormCraft.ForMudBlazor.
/// </remarks>
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "test-color-picker");

            // For testing, just use simple labels
            builder.OpenElement(sequence++, "label");
            builder.AddContent(sequence++, "Color");
            builder.CloseElement();

            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "type", "color");
            builder.AddAttribute(sequence, "value", GetValue(context) ?? "#000000");
            builder.CloseElement();

            builder.CloseElement();
        };
    }
}