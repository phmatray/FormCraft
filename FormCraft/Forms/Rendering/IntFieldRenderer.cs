using Microsoft.AspNetCore.Components;
using FormCraft.Forms.Abstractions;
using FormCraft.Forms.Core;
using MudBlazor;

namespace FormCraft.Forms.Rendering;

/// <summary>
/// Field renderer for integer values, rendering numeric input controls.
/// </summary>
public class IntFieldRenderer : IFieldRenderer
{
    /// <inheritdoc />
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(int);
    }

    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            builder.OpenComponent<MudNumericField<int>>(0);
            builder.AddAttribute(1, "Label", context.Field.Label);
            builder.AddAttribute(2, "Value", (int)(context.CurrentValue ?? 0));
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<int>(this, value =>
            {
                context.OnValueChanged.InvokeAsync(value);
            }));
            
            if (!string.IsNullOrEmpty(context.Field.HelpText))
                builder.AddAttribute(4, "HelperText", context.Field.HelpText);
            
            builder.AddAttribute(5, "Required", context.Field.IsRequired);
            builder.AddAttribute(6, "Disabled", context.Field.IsDisabled);
            
            builder.CloseComponent();
        };
    }
}