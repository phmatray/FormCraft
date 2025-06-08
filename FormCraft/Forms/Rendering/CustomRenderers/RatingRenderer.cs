using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft;

/// <summary>
/// Custom renderer for rating fields using MudBlazor's MudRating component.
/// </summary>
public class RatingRenderer : CustomFieldRendererBase<int>
{
    /// <inheritdoc />
    public override RenderFragment Render(IFieldRenderContext context)
    {
        return builder =>
        {
            var value = GetValue(context);
            var fieldConfig = context.FieldConfiguration;
            var label = GetFieldLabel(fieldConfig);
            var isDisabled = GetIsDisabled(fieldConfig);
            var isReadOnly = GetIsReadOnly(fieldConfig);
            var maxValue = GetMaxValue(fieldConfig);

            // Container div
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "mud-form-control mud-form-control-default mud-form-control-margin-normal");

            // Label
            if (!string.IsNullOrEmpty(label))
            {
                builder.OpenElement(2, "label");
                builder.AddAttribute(3, "class", "mud-input-label");
                builder.AddContent(4, label);
                builder.CloseElement();
            }

            // MudRating component
            builder.OpenComponent<MudRating>(5);
            builder.AddAttribute(6, "SelectedValue", value);
            builder.AddAttribute(7, "MaxValue", maxValue);
            builder.AddAttribute(8, "Disabled", isDisabled);
            builder.AddAttribute(9, "ReadOnly", isReadOnly);
            builder.AddAttribute(10, "Color", Color.Primary);
            builder.AddAttribute(11, "Size", Size.Medium);

            // Handle value change
            builder.AddAttribute(12, "SelectedValueChanged", EventCallback.Factory.Create<int>(
                this,
                async (newValue) => await SetValue(context, newValue)));

            builder.CloseComponent();

            // Help text
            var helpText = GetHelpText(fieldConfig);
            if (!string.IsNullOrEmpty(helpText))
            {
                builder.OpenElement(13, "div");
                builder.AddAttribute(14, "class", "mud-input-helper-text");
                builder.AddContent(15, helpText);
                builder.CloseElement();
            }

            builder.CloseElement(); // container div
        };
    }

    private string GetFieldLabel(object fieldConfig)
    {
        return fieldConfig.GetType().GetProperty("Label")?.GetValue(fieldConfig) as string ?? "";
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

    private int GetMaxValue(object fieldConfig)
    {
        // Check if max value is specified in additional attributes
        var additionalAttributes = fieldConfig.GetType()
            .GetProperty("AdditionalAttributes")?
            .GetValue(fieldConfig) as Dictionary<string, object>;

        if (additionalAttributes?.TryGetValue("MaxRating", out var maxValue) == true)
        {
            if (maxValue is int intValue)
                return intValue;
        }

        return 5; // Default max rating
    }
}