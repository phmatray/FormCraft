namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the numeric field renderer.
/// </summary>
public class MudBlazorNumericFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(MudBlazorNumericFieldComponent<,>);

    /// <summary>
    /// Nullable numeric fields (int?, decimal?, ...) bind to the nullable-aware
    /// component so null displays as an empty input and round-trips back to the
    /// model instead of being coerced to default(TValue) (#150).
    /// </summary>
    protected override Type ResolveComponentType<TModel>(IFieldRenderContext<TModel> context)
    {
        var underlyingType = Nullable.GetUnderlyingType(context.ActualFieldType);
        return underlyingType != null
            ? typeof(MudBlazorNullableNumericFieldComponent<,>).MakeGenericType(typeof(TModel), underlyingType)
            : base.ResolveComponentType(context);
    }

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        return underlyingType == typeof(int) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte);
    }
}