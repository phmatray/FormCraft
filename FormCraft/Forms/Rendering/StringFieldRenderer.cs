using Microsoft.AspNetCore.Components;
using FormCraft.Forms.Abstractions;
using FormCraft.Forms.Core;
using FormCraft.Forms.Extensions;
using MudBlazor;

namespace FormCraft.Forms.Rendering;

/// <summary>
/// Field renderer for string values, supporting text inputs and dropdown selections.
/// </summary>
public class StringFieldRenderer : IFieldRenderer
{
    /// <inheritdoc />
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(string);
    }

    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        if (context.Field.AdditionalAttributes.ContainsKey("Options"))
        {
            return RenderSelectField(context);
        }
        
        return RenderTextField(context);
    }

    private RenderFragment RenderTextField<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            builder.OpenComponent<MudTextField<string>>(0);
            builder.AddAttribute(1, "Label", context.Field.Label);
            builder.AddAttribute(2, "Value", context.CurrentValue as string ?? "");
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string>(this, value =>
            {
                context.OnValueChanged.InvokeAsync(value);
            }));
            
            if (!string.IsNullOrEmpty(context.Field.Placeholder))
                builder.AddAttribute(4, "Placeholder", context.Field.Placeholder);
            
            if (!string.IsNullOrEmpty(context.Field.HelpText))
                builder.AddAttribute(5, "HelperText", context.Field.HelpText);
            
            builder.AddAttribute(6, "Required", context.Field.IsRequired);
            builder.AddAttribute(7, "Disabled", context.Field.IsDisabled);
            
            builder.CloseComponent();
        };
    }

    private RenderFragment RenderSelectField<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            builder.OpenComponent<MudSelect<string>>(0);
            builder.AddAttribute(1, "Label", context.Field.Label);
            builder.AddAttribute(2, "Value", context.CurrentValue as string ?? "");
            builder.AddAttribute(3, "ValueChanged", EventCallback.Factory.Create<string>(this, value =>
            {
                context.OnValueChanged.InvokeAsync(value);
                context.OnDependencyChanged.InvokeAsync();
            }));
            
            
            if (context.Field.AdditionalAttributes.TryGetValue("Options", out var optionsObj) &&
                optionsObj is IEnumerable<SelectOption<string>> options)
            {
                builder.AddAttribute(5, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    var index = 0;
                    foreach (var option in options)
                    {
                        childBuilder.OpenComponent<MudSelectItem<string>>(index);
                        childBuilder.AddAttribute(1, "Value", option.Value);
                        childBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, option.Label)));
                        childBuilder.CloseComponent();
                        index++;
                    }
                }));
            }
            
            builder.CloseComponent();
        };
    }
    
}