using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Renderer for multiple file upload fields using MudBlazor components.
/// </summary>
public class MudBlazorMultipleFileUploadRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorMultipleFileUploadComponent<>);
    
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(IReadOnlyList<IBrowserFile>);
    }
}