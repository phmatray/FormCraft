namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the LOV (List of Values) field renderer.
/// Handles fields configured with the .AsLov() extension method.
/// </summary>
public class MudBlazorLovFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorLovFieldComponent<,,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        // This renderer handles fields with LovConfiguration in AdditionalAttributes
        return field.AdditionalAttributes.ContainsKey("LovConfiguration");
    }
}
