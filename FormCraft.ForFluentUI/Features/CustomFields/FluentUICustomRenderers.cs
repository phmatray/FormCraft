using Microsoft.AspNetCore.Components;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a numeric field as a Fluent UI slider.
/// </summary>
/// <remarks>
/// A <b>custom</b> renderer, used via <c>.WithCustomRenderer(new FluentUISliderRenderer())</c>. It is
/// deliberately not an <c>IFieldRenderer</c> registered by <c>AddFormCraftFluentUI()</c>: registering
/// it would make every <c>double</c> field in every form a slider.
/// </remarks>
public class FluentUISliderRenderer : CustomFieldRendererBase<double>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(FluentUISliderComponent<>).MakeGenericType(context.Model.GetType()));
            builder.AddAttribute(1, "Context", context);
            builder.CloseComponent();
        };
    }
}

/// <summary>
/// Renders an integer field as a row of rating controls.
/// </summary>
/// <remarks>
/// A custom renderer, for the same reason as <see cref="FluentUISliderRenderer"/>: not every
/// <c>int</c> is a score.
/// </remarks>
public class FluentUIRatingRenderer : CustomFieldRendererBase<int>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(FluentUIRatingComponent<>).MakeGenericType(context.Model.GetType()));
            builder.AddAttribute(1, "Context", context);
            builder.CloseComponent();
        };
    }
}

/// <summary>
/// Renders a string field holding a hex colour as a Fluent UI colour input.
/// </summary>
/// <remarks>
/// A custom renderer: registering it would turn every <c>string</c> field into a colour picker.
/// </remarks>
public class FluentUIColorPickerRenderer : CustomFieldRendererBase<string>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(FluentUIColorPickerComponent<>).MakeGenericType(context.Model.GetType()));
            builder.AddAttribute(1, "Context", context);
            builder.CloseComponent();
        };
    }
}
