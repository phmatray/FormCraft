namespace FormCraft.ForFluentUI;

/// <summary>
/// Fluent UI implementation of the LOV (List of Values) field renderer.
/// </summary>
public class FluentUILovFieldRenderer : FieldRendererBase
{
    /// <inheritdoc />
    protected override Type ComponentType => typeof(FluentUILovFieldComponent<,,>);

    /// <inheritdoc />
    public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
        => field.AdditionalAttributes.ContainsKey("LovConfiguration");

    /// <inheritdoc />
    protected override Type ResolveComponentType<TModel>(IFieldRenderContext<TModel> context)
    {
        // The component takes <TModel, TValue, TItem>; TItem exists only on the stored
        // ILovConfiguration<TItem, TValue>, so close the generic over the configuration's own
        // type arguments.
        if (!context.Field.AdditionalAttributes.TryGetValue("LovConfiguration", out var lovConfig) || lovConfig is null)
        {
            throw new InvalidOperationException(
                $"Field '{context.Field.FieldName}' is missing its LovConfiguration. Configure the field with .AsLov().");
        }

        var configInterface = lovConfig.GetType()
            .GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILovConfiguration<,>));
        var args = configInterface.GetGenericArguments(); // [TItem, TValue]

        return typeof(FluentUILovFieldComponent<,,>).MakeGenericType(typeof(TModel), args[1], args[0]);
    }
}
