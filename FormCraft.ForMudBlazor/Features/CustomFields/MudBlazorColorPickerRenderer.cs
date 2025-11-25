using Microsoft.AspNetCore.Components;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the color picker field renderer.
/// Can be used as a custom renderer for string fields to provide color selection.
/// </summary>
public class MudBlazorColorPickerRenderer : CustomFieldRendererBase<string>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(MudBlazorColorPickerComponent<>).MakeGenericType(context.Model.GetType()));
            builder.AddAttribute(1, "Context", context);
            builder.CloseComponent();
        };
    }
}

/// <summary>
/// Alternative color picker renderer that extends FieldRendererBase for automatic registration.
/// </summary>
public class MudBlazorColorPickerFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorColorPickerComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(string) &&
               field.AdditionalAttributes.ContainsKey("color-picker");
    }
}