using System.Reflection;

namespace FormCraft.TestSupport;

/// <summary>
/// One <see cref="CollectionItemFixture"/> item-form builder that does not conform to its
/// trailing-<c>configureCollection</c> contract, and why.
/// </summary>
/// <param name="Member">The offending builder.</param>
/// <param name="Reason">A message naming what is wrong.</param>
public sealed record BuilderSurfaceOffence(MethodInfo Member, string Reason)
{
    public override string ToString() => $"{Member.Name}: {Reason}";
}

/// <summary>
/// The rule every <see cref="CollectionItemFixture"/> item-form builder is supposed to hold —
/// <i>a trailing, optional <c>configureCollection</c> callback that actually reaches the
/// collection, last</i> — expressed as something the build can check (#350).
/// </summary>
/// <remarks>
/// <para>
/// #282 added the parameter to <c>MultiFieldItemForm</c> alone; the other six builders went without
/// it until #300 noticed and fixed all seven by hand, backed only by named self-tests that enumerate
/// today's builders one by one. Neither catches an eighth builder added tomorrow — this guard is the
/// build-time version of the same check <see cref="CollectionItemShapeGuard"/> is for the fixture's
/// models, mirroring its shape.
/// </para>
/// <para>
/// <b>Two independent things are checked.</b> The <i>shape</i> — a last, optional
/// <c>Action&lt;CollectionFieldBuilder&lt;TModel, TItem&gt;&gt;</c> whose <c>TModel</c> matches the
/// member's own <c>IFormConfiguration&lt;TModel&gt;</c> — only proves the parameter compiles. A
/// builder can accept it and never invoke it, or invoke it before its own <c>WithLabel</c>/
/// <c>WithItemForm</c> so the caller's override is silently overwritten; both compile and both pass
/// a signature-only check. So the <i>behaviour</i> half invokes the member reflectively with a
/// callback that sets a synthetic sentinel label and asserts it survives onto the built
/// configuration's collection field.
/// </para>
/// <para>
/// <b>Not every builder here takes the parameter, and that is not an oversight to fix.</b>
/// <c>TwoCollectionItemForm</c> configures two collections through one form, and a single
/// <c>configureCollection</c> callback has no way to say which one it targets — see the
/// allowlisted exception where this guard runs over the real fixture.
/// </para>
/// </remarks>
public static class CollectionItemBuilderSurfaceGuard
{
    private const string SentinelLabel = "__configureCollection_sentinel__";

    /// <summary>
    /// Every static member of <paramref name="fixture"/> that returns <c>IFormConfiguration&lt;&gt;</c>
    /// — the item-form builders this guard polices.
    /// </summary>
    /// <remarks>
    /// Filtered on the return type rather than the name: <see cref="CollectionItemFixture"/> also
    /// declares model factories (<c>NewOrder</c>, <c>NewBasket</c>, …) that return plain models, not
    /// form configurations, and a name-based filter would need to keep a roster of those in sync by
    /// hand — the exact drift this whole guard exists to remove one level up.
    /// </remarks>
    public static IEnumerable<MethodInfo> ItemFormBuilders(Type fixture) =>
        fixture
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType.IsGenericType
                && m.ReturnType.GetGenericTypeDefinition() == typeof(IFormConfiguration<>));

    /// <summary>Every builder among <paramref name="members"/> that fails the shape or behaviour check.</summary>
    /// <param name="members">The builders to check — see <see cref="ItemFormBuilders"/>.</param>
    /// <param name="allowed">
    /// Builders that may keep a non-conforming surface despite matching, each justified where it is
    /// declared. Empty by default: an exception should be a deliberate act at the call site, not a
    /// standing grant.
    /// </param>
    public static IReadOnlyList<BuilderSurfaceOffence> FindOffenders(
        IEnumerable<MethodInfo> members,
        IReadOnlySet<MethodInfo>? allowed = null)
    {
        var offences = new List<BuilderSurfaceOffence>();

        foreach (var member in members)
        {
            if (allowed?.Contains(member) == true)
            {
                continue;
            }

            if (!ShapeConforms(member, out var shapeReason))
            {
                offences.Add(new BuilderSurfaceOffence(member, shapeReason!));
                continue;
            }

            if (!ReachesCollectionLast(member, out var behaviourReason))
            {
                offences.Add(new BuilderSurfaceOffence(member, behaviourReason!));
            }
        }

        return offences.OrderBy(o => o.Member.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Whether <paramref name="member"/> declares a last, optional
    /// <c>Action&lt;CollectionFieldBuilder&lt;TModel, TItem&gt;&gt;</c> parameter whose
    /// <c>TModel</c> matches <paramref name="member"/>'s own <c>IFormConfiguration&lt;TModel&gt;</c>
    /// return type.
    /// </summary>
    private static bool ShapeConforms(MethodInfo member, out string? reason)
    {
        var modelType = member.ReturnType.GetGenericArguments()[0];
        var parameters = member.GetParameters();
        var shapeIndex = Array.FindIndex(parameters, IsCollectionCallbackShape);

        if (shapeIndex < 0)
        {
            reason = "declares no Action<CollectionFieldBuilder<TModel, TItem>> configureCollection parameter";
            return false;
        }

        var builderType = parameters[shapeIndex].ParameterType.GetGenericArguments()[0];
        var closedModelType = builderType.GetGenericArguments()[0];
        if (closedModelType != modelType)
        {
            reason = $"configureCollection targets CollectionFieldBuilder<{closedModelType.Name}, ...> "
                + $"but the member returns IFormConfiguration<{modelType.Name}>";
            return false;
        }

        if (shapeIndex != parameters.Length - 1)
        {
            reason = "configureCollection must be the last parameter (found at position "
                + $"{shapeIndex} of {parameters.Length})";
            return false;
        }

        if (!parameters[shapeIndex].IsOptional)
        {
            reason = "configureCollection must be optional (declare a default value)";
            return false;
        }

        reason = null;
        return true;
    }

    private static bool IsCollectionCallbackShape(ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Action<>))
        {
            return false;
        }

        var argument = type.GetGenericArguments()[0];
        return argument.IsGenericType && argument.GetGenericTypeDefinition() == typeof(CollectionFieldBuilder<,>);
    }

    /// <summary>
    /// Invokes <paramref name="member"/> reflectively with a <c>configureCollection</c> that sets a
    /// synthetic sentinel label, and checks it survived onto the built configuration's collection
    /// field — proving the callback both runs and runs <b>after</b> the builder's own
    /// <c>WithLabel</c>. Every other parameter is passed <c>null</c>, which every builder here already
    /// treats as optional.
    /// </summary>
    private static bool ReachesCollectionLast(MethodInfo member, out string? reason)
    {
        var parameters = member.GetParameters();
        var callbackParameter = parameters[^1];
        var builderTypeArguments = callbackParameter.ParameterType.GetGenericArguments()[0].GetGenericArguments();

        var setSentinelLabel = typeof(CollectionItemBuilderSurfaceGuard)
            .GetMethod(nameof(SetSentinelLabel), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(builderTypeArguments);
        var callback = setSentinelLabel.CreateDelegate(callbackParameter.ParameterType);

        var arguments = new object?[parameters.Length];
        arguments[^1] = callback;

        object? configuration;
        try
        {
            configuration = member.Invoke(obj: null, arguments);
        }
        catch (TargetInvocationException ex)
        {
            reason = "threw when invoked to check its configureCollection callback: "
                + (ex.InnerException?.Message ?? ex.Message);
            return false;
        }

        if (configuration is null || !HasSentinelLabel(configuration))
        {
            reason = "configureCollection did not take effect - it is either never invoked, or invoked "
                + "before the builder's own WithLabel/WithItemForm calls and overwritten by them";
            return false;
        }

        reason = null;
        return true;
    }

    private static void SetSentinelLabel<TModel, TItem>(CollectionFieldBuilder<TModel, TItem> builder)
        where TModel : new()
        where TItem : new() =>
        builder.WithLabel(SentinelLabel);

    private static bool HasSentinelLabel(object configuration)
    {
        if (configuration.GetType().GetProperty("CollectionFields")?.GetValue(configuration)
            is not System.Collections.IEnumerable collectionFields)
        {
            return false;
        }

        foreach (var field in collectionFields)
        {
            if (field is ICollectionFieldConfigurationBase { Label: SentinelLabel })
            {
                return true;
            }
        }

        return false;
    }
}
