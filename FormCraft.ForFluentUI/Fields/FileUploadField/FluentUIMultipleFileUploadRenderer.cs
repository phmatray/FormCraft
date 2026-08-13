using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI renderer for multiple-file fields - a <c>List&lt;T&gt;</c> of <c>IBrowserFile</c>.
/// </summary>
public class FluentUIMultipleFileUploadRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIMultipleFileUploadComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => fieldType.IsGenericType &&
           fieldType.GetGenericTypeDefinition() == typeof(List<>) &&
           typeof(IBrowserFile).IsAssignableFrom(fieldType.GetGenericArguments()[0]);
}
