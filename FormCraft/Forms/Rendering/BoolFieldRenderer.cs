using Microsoft.AspNetCore.Components;

namespace FormCraft;

/// <summary>
/// Test stub implementation of a boolean field renderer.
/// This is used for unit testing and should not be used in production.
/// </summary>
/// <remarks>
/// For production use, use the MudBlazor implementation from FormCraft.ForMudBlazor.
/// </remarks>
public class BoolFieldRenderer : FieldRendererBase<bool>
{
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        // Only handle non-nullable bool
        return fieldType == typeof(bool);
    }

    /// <inheritdoc />
    protected override Type ComponentType => typeof(TestStubComponent<>);

    /// <summary>
    /// Simple test component that renders a checkbox-like representation.
    /// </summary>
    private class TestStubComponent<TModel> : ComponentBase
    {
        [Parameter] public IFieldRenderContext<TModel>? Context { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (Context == null) return;

            var sequence = 0;
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "test-bool-field");

            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "type", "checkbox");
            builder.AddAttribute(sequence++, "checked", Context.CurrentValue);
            builder.AddAttribute(sequence++, "disabled", Context.Field.IsDisabled);
            builder.CloseElement();

            builder.OpenElement(sequence++, "label");
            builder.AddContent(sequence, Context.Field.Label);
            builder.CloseElement();

            builder.CloseElement();
        }
    }
}