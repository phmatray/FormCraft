using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Test stub implementation of an integer field renderer.
/// This is used for unit testing and should not be used in production.
/// </summary>
/// <remarks>
/// For production use, use the MudBlazor implementation from FormCraft.ForMudBlazor.
/// </remarks>
public class IntFieldRenderer : FieldRendererBase<int>
{
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        // Only handle non-nullable int
        return fieldType == typeof(int);
    }
    
    /// <inheritdoc />
    protected override Type ComponentType => typeof(TestStubComponent<>);
    
    /// <summary>
    /// Simple test component that renders a numeric input-like representation.
    /// </summary>
    private class TestStubComponent<TModel> : ComponentBase
    {
        [Parameter] public IFieldRenderContext<TModel>? Context { get; set; }
        
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (Context == null) return;
            
            var sequence = 0;
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "test-int-field");
            
            builder.OpenElement(sequence++, "label");
            builder.AddContent(sequence++, Context.Field.Label);
            builder.CloseElement();
            
            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "type", "number");
            builder.AddAttribute(sequence++, "value", Context.CurrentValue);
            builder.AddAttribute(sequence++, "placeholder", Context.Field.Placeholder);
            builder.AddAttribute(sequence++, "disabled", Context.Field.IsDisabled);
            builder.CloseElement();
            
            if (!string.IsNullOrEmpty(Context.Field.HelpText))
            {
                builder.OpenElement(sequence++, "div");
                builder.AddAttribute(sequence++, "class", "help-text");
                builder.AddContent(sequence++, Context.Field.HelpText);
                builder.CloseElement();
            }
            
            builder.CloseElement();
        }
    }
}