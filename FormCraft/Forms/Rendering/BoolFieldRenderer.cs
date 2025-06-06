using Microsoft.AspNetCore.Components;
using FormCraft.Forms.Abstractions;
using FormCraft.Forms.Core;
using MudBlazor;

namespace FormCraft.Forms.Rendering;

/// <summary>
/// Field renderer for boolean values, rendering checkbox controls.
/// </summary>
public class BoolFieldRenderer : IFieldRenderer
{
    /// <inheritdoc />
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(bool);
    }

    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            builder.OpenComponent<MudCheckBox<bool>>(0);
            builder.AddAttribute(1, "Label", context.Field.Label);
            builder.AddAttribute(2, "Checked", (bool)(context.CurrentValue ?? false));
            builder.AddAttribute(3, "CheckedChanged", EventCallback.Factory.Create<bool>(this, value =>
            {
                context.OnValueChanged.InvokeAsync(value);
            }));
            
            if (!string.IsNullOrEmpty(context.Field.HelpText))
                builder.AddAttribute(4, "HelperText", context.Field.HelpText);
            
            builder.AddAttribute(5, "Disabled", context.Field.IsDisabled);
            
            builder.CloseComponent();
        };
    }
}