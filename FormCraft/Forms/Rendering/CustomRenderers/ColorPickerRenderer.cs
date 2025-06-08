using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace FormCraft;

/// <summary>
/// Custom renderer for color picker fields.
/// </summary>
public class ColorPickerRenderer : CustomFieldRendererBase<string>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            var value = GetValue(context) ?? "#000000";
            var fieldConfig = context.FieldConfiguration;
            var label = GetFieldLabel(fieldConfig);
            var fieldName = GetFieldName(fieldConfig);
            var isDisabled = GetIsDisabled(fieldConfig);
            var isReadOnly = GetIsReadOnly(fieldConfig);

            // Container div
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "mud-form-control mud-form-control-default mud-form-control-margin-normal");

            // Label
            if (!string.IsNullOrEmpty(label))
            {
                builder.OpenElement(2, "label");
                builder.AddAttribute(3, "class", "mud-input-label mud-input-label-animated mud-input-label-text");
                builder.AddAttribute(4, "for", fieldName);
                builder.AddContent(5, label);
                builder.CloseElement();
            }

            // Color input wrapper
            builder.OpenElement(6, "div");
            builder.AddAttribute(7, "class", "mud-input mud-input-text mud-input-adorned-start");

            // Color preview
            builder.OpenElement(8, "div");
            builder.AddAttribute(9, "class", "mud-input-adornment mud-input-adornment-start");
            builder.OpenElement(10, "div");
            builder.AddAttribute(11, "style", $"width: 24px; height: 24px; background-color: {value}; border: 1px solid #ccc; border-radius: 4px;");
            builder.CloseElement();
            builder.CloseElement();

            // Input element
            builder.OpenElement(12, "input");
            builder.AddAttribute(13, "type", "color");
            builder.AddAttribute(14, "class", "mud-input-slot mud-input-root mud-input-root-text");
            builder.AddAttribute(15, "id", fieldName);
            builder.AddAttribute(16, "value", value);
            builder.AddAttribute(17, "disabled", isDisabled);
            builder.AddAttribute(18, "readonly", isReadOnly);

            // Handle value change
            builder.AddAttribute(19, "onchange", EventCallback.Factory.CreateBinder<string>(
                this,
                async (newValue) => await SetValue(context, newValue),
                value));

            builder.CloseElement(); // input

            // Text input for manual entry
            builder.OpenElement(20, "input");
            builder.AddAttribute(21, "type", "text");
            builder.AddAttribute(22, "class", "mud-input-slot mud-input-root mud-input-root-text");
            builder.AddAttribute(23, "value", value);
            builder.AddAttribute(24, "placeholder", "#RRGGBB");
            builder.AddAttribute(25, "style", "margin-left: 8px; width: 100px;");
            builder.AddAttribute(26, "disabled", isDisabled);
            builder.AddAttribute(27, "readonly", isReadOnly);

            builder.AddAttribute(28, "onchange", EventCallback.Factory.CreateBinder<string>(
                this,
                async (newValue) => await SetValue(context, newValue),
                value));

            builder.CloseElement(); // text input

            builder.CloseElement(); // wrapper div

            // Help text
            var helpText = GetHelpText(fieldConfig);
            if (!string.IsNullOrEmpty(helpText))
            {
                builder.OpenElement(29, "div");
                builder.AddAttribute(30, "class", "mud-input-helper-text");
                builder.AddContent(31, helpText);
                builder.CloseElement();
            }

            builder.CloseElement(); // container div
        };
    }

    private string GetFieldLabel(object fieldConfig)
    {
        return fieldConfig.GetType().GetProperty("Label")?.GetValue(fieldConfig) as string ?? "";
    }

    private string GetFieldName(object fieldConfig)
    {
        return fieldConfig.GetType().GetProperty("FieldName")?.GetValue(fieldConfig) as string ?? "";
    }

    private bool GetIsDisabled(object fieldConfig)
    {
        return fieldConfig.GetType().GetProperty("IsDisabled")?.GetValue(fieldConfig) as bool? ?? false;
    }

    private bool GetIsReadOnly(object fieldConfig)
    {
        return fieldConfig.GetType().GetProperty("IsReadOnly")?.GetValue(fieldConfig) as bool? ?? false;
    }

    private string? GetHelpText(object fieldConfig)
    {
        return fieldConfig.GetType().GetProperty("HelpText")?.GetValue(fieldConfig) as string;
    }
}