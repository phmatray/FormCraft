namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the rating field renderer.
/// </summary>
public class MudBlazorRatingRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorRatingComponent<>);
    
    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(int) && 
               field.AdditionalAttributes.ContainsKey("rating");
    }
}