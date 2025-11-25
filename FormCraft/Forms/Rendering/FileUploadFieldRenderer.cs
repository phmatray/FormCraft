using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// Test stub implementation of a file upload field renderer.
/// This is used for unit testing and should not be used in production.
/// </summary>
/// <remarks>
/// For production use, use the MudBlazor implementation from FormCraft.ForMudBlazor.
/// </remarks>
public class FileUploadFieldRenderer : IFieldRenderer
{
    /// <inheritdoc />
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(IBrowserFile) ||
               fieldType == typeof(IReadOnlyList<IBrowserFile>) ||
               fieldType == typeof(IBrowserFile[]) ||
               fieldType == typeof(List<IBrowserFile>);
    }

    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            var sequence = 0;
            builder.OpenComponent(sequence++, typeof(TestStubComponent<TModel>));
            builder.AddAttribute(sequence, "Context", context);
            builder.CloseComponent();
        };
    }

    /// <summary>
    /// Simple test component that renders a file input-like representation.
    /// </summary>
    private class TestStubComponent<TModel> : ComponentBase
    {
        [Parameter] public IFieldRenderContext<TModel>? Context { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            if (Context == null) return;

            var sequence = 0;
            builder.OpenElement(sequence++, "div");
            builder.AddAttribute(sequence++, "class", "test-fileupload-field");

            builder.OpenElement(sequence++, "label");
            builder.AddContent(sequence++, Context.Field.Label);
            builder.CloseElement();

            builder.OpenElement(sequence++, "input");
            builder.AddAttribute(sequence++, "type", "file");
            builder.AddAttribute(sequence++, "disabled", Context.Field.IsDisabled);

            // Check if field type supports multiple files
            var isMultiple = Context.ActualFieldType == typeof(IReadOnlyList<IBrowserFile>) ||
                           Context.ActualFieldType == typeof(IBrowserFile[]) ||
                           Context.ActualFieldType == typeof(List<IBrowserFile>);

            if (isMultiple)
            {
                builder.AddAttribute(sequence++, "multiple", true);
            }

            // Check for accept attribute in additional attributes
            if (Context.Field.AdditionalAttributes.TryGetValue("accept", out var accept))
            {
                builder.AddAttribute(sequence++, "accept", accept);
            }

            builder.CloseElement();

            if (!string.IsNullOrEmpty(Context.Field.HelpText))
            {
                builder.OpenElement(sequence++, "div");
                builder.AddAttribute(sequence++, "class", "help-text");
                builder.AddContent(sequence, Context.Field.HelpText);
                builder.CloseElement();
            }

            builder.CloseElement();
        }
    }
}