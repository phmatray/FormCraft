using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using DynamicFormBlazor.Forms.Core;
using MudBlazor;

namespace DynamicFormBlazor.Forms.Rendering;

public class IntFieldRenderer : IFieldRenderer
{
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(int);
    }

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