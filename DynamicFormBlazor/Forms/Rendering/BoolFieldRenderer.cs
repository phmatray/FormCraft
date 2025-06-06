using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using DynamicFormBlazor.Forms.Core;
using MudBlazor;

namespace DynamicFormBlazor.Forms.Rendering;

public class BoolFieldRenderer : IFieldRenderer
{
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(bool);
    }

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