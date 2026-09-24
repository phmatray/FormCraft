namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the LOV (List of Values) field renderer.
/// </summary>
public class FluentUILovFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUILovFieldComponent<,,,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => field.AdditionalAttributes.ContainsKey("LovConfiguration");

    /// <inheritdoc />
    protected override Type ResolveComponentType<TModel>(IFieldRenderContext<TModel> context)
    {
        // The component takes <TModel, TValue, TKey, TItem>. TValue is the field's bound type
        // (IEnumerable<TKey> for AsMultiSelectLov), while TKey and TItem exist only on the stored
        // ILovConfiguration<TItem, TKey> — so they come from the configuration (#480).
        if (!context.Field.AdditionalAttributes.TryGetValue("LovConfiguration", out var lovConfig) || lovConfig is null)
        {
            throw new InvalidOperationException(
                $"Field '{context.Field.FieldName}' is missing its LovConfiguration. Configure the field with .AsLov().");
        }

        var configInterface = lovConfig.GetType()
            .GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILovConfiguration<,>));
        var args = configInterface.GetGenericArguments(); // [TItem, TKey]

        return typeof(FluentUILovFieldComponent<,,,>).MakeGenericType(typeof(TModel), context.ActualFieldType, args[1], args[0]);
    }
}
