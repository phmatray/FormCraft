using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft;

/// <summary>
/// Field renderer for DateTime values, rendering date picker controls.
/// </summary>
public class DateTimeFieldRenderer : IFieldRenderer
{
    /// <inheritdoc />
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(DateTime) || fieldType == typeof(DateTime?);
    }

    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            builder.OpenComponent<MudDatePicker>(0);
            builder.AddAttribute(1, "Label", context.Field.Label);
            
            var currentValue = context.CurrentValue as DateTime?;
            if (currentValue.HasValue)
            {
                builder.AddAttribute(2, "Date", currentValue.Value);
            }
            
            builder.AddAttribute(3, "DateChanged", EventCallback.Factory.Create<DateTime?>(this, value =>
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