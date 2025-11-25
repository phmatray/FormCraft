namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the file upload field renderer.
/// </summary>
public class MudBlazorFileUploadFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorFileUploadFieldComponent<>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType.Name.Contains("IBrowserFile") ||
               fieldType.Name.Contains("IFormFile") ||
               (fieldType.IsGenericType &&
                fieldType.GetGenericTypeDefinition() == typeof(List<>) &&
                fieldType.GetGenericArguments()[0].Name.Contains("IBrowserFile"));
    }
}