namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the numeric field renderer.
/// </summary>
public class FluentUINumericFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUINumericFieldComponent<,>);

    /// <summary>
    /// Nullable numeric fields (<c>int?</c>, <c>decimal?</c>, ...) bind to the nullable-aware
    /// component so null displays as an empty input and round-trips back to the model instead of
    /// being coerced to <c>default(TValue)</c> (#150).
    /// </summary>
    protected override Type ResolveComponentType<TModel>(IFieldRenderContext<TModel> context)
    {
        var underlyingType = Nullable.GetUnderlyingType(context.ActualFieldType);
        return underlyingType != null
            ? typeof(FluentUINullableNumericFieldComponent<,>).MakeGenericType(typeof(TModel), underlyingType)
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
