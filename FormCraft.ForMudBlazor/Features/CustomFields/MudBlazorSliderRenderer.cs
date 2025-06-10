using Microsoft.AspNetCore.Components;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Custom field renderer for numeric slider fields using MudBlazor MudSlider component.
/// Supports configurable min/max values, step size, tick marks, and value labels.
/// </summary>
public class MudBlazorSliderRenderer : CustomFieldRendererBase<double>
{
    /// <summary>
    /// Renders a MudSlider component for numeric input with visual slider control.
    /// </summary>
    /// <param name="context">The field render context containing field configuration and model information.</param>
    /// <returns>A RenderFragment that renders the MudSlider component.</returns>
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(MudBlazorSliderComponent<>).MakeGenericType(context.Model.GetType()));
            builder.AddAttribute(1, "Context", context);
            builder.CloseComponent();
        };
    }
}