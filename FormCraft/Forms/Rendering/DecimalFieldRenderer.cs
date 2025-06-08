using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft;

/// <summary>
/// Field renderer for decimal values, rendering numeric input controls with decimal support.
/// </summary>
public class DecimalFieldRenderer : IFieldRenderer
{
    /// <inheritdoc />
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(decimal) || fieldType == typeof(decimal?);
    }

    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            var isNullable = context.ActualFieldType == typeof(decimal?);

            if (isNullable)
            {
                builder.OpenComponent<MudNumericField<decimal?>>(0);
                builder.AddAttribute(1, "Label", context.Field.Label);
                builder.AddAttribute(2, "Value", (decimal?)context.CurrentValue);
                builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<decimal?>(this, value =>
                {
                    context.OnValueChanged.InvokeAsync(value);
                }));
            }
            else
            {
                builder.OpenComponent<MudNumericField<decimal>>(0);
                builder.AddAttribute(1, "Label", context.Field.Label);
                builder.AddAttribute(2, "Value", (decimal)(context.CurrentValue ?? 0m));
                builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<decimal>(this, value =>
                {
                    context.OnValueChanged.InvokeAsync(value);
                }));
            }

            if (!string.IsNullOrEmpty(context.Field.Placeholder))
                builder.AddAttribute(4, "Placeholder", context.Field.Placeholder);

            if (!string.IsNullOrEmpty(context.Field.HelpText))
                builder.AddAttribute(5, "HelperText", context.Field.HelpText);

            builder.AddAttribute(6, "Required", context.Field.IsRequired);
            builder.AddAttribute(7, "Disabled", context.Field.IsDisabled);

            builder.CloseComponent();
        };
    }
}