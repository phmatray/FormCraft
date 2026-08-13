using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI renderer for single-file <c>IBrowserFile</c> fields.
/// </summary>
/// <remarks>
/// Must be registered <b>after</b> <see cref="FluentUIMultipleFileUploadRenderer"/>: a
/// <c>List&lt;IBrowserFile&gt;</c> field satisfies both predicates, and first-match-wins would give
/// a multiple-file field the single-file component.
/// </remarks>
public class FluentUIFileUploadFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIFileUploadFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => typeof(IBrowserFile).IsAssignableFrom(fieldType);
}
