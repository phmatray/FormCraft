namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI renderer for multi-select fields: <c>IEnumerable&lt;T&gt;</c> fields carrying the
/// <c>MultiSelectOptions</c> attribute (written by <c>.AsMultiSelect(...)</c>).
/// </summary>
public class FluentUIMultiSelectFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUIMultiSelectFieldComponent<,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => field.AdditionalAttributes.ContainsKey("MultiSelectOptions") && GetItemType(fieldType) != null;

    /// <inheritdoc />
    protected override Type ResolveComponentType<TModel>(IFieldRenderContext<TModel> context)
    {
        var itemType = GetItemType(context.ActualFieldType)
            ?? throw new NotSupportedException(
                $"Field type {context.ActualFieldType} is not an IEnumerable<T> and cannot be rendered as a multi-select.");

        return typeof(FluentUIMultiSelectFieldComponent<,>).MakeGenericType(typeof(TModel), itemType);
    }

    private static Type? GetItemType(Type fieldType)
    {
        // string is IEnumerable<char>, but a string field is never a multi-select.
        if (fieldType == typeof(string))
        {
            return null;
        }

        if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return fieldType.GetGenericArguments()[0];
        }

        return fieldType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }
}
