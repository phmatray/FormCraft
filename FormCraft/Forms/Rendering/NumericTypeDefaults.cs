namespace FormCraft;

/// <summary>
/// The unconfigured-bound defaults for a numeric field's <c>Min</c>, <c>Max</c> and <c>Step</c> —
/// shared by every numeric field component in both UI adapters (#389).
/// </summary>
/// <remarks>
/// Before this type, <c>FormCraft.ForMudBlazor</c>'s <c>MudBlazorNumericFieldComponent</c> /
/// <c>MudBlazorNullableNumericFieldComponent</c> and <c>FormCraft.ForFluentUI</c>'s
/// <c>FluentUINumericFieldComponent</c> / <c>FluentUINullableNumericFieldComponent</c> each carried
/// their own private copy of this same reflection lookup — flagged by #348's code review as the
/// drift pattern this library has already paid for repeatedly (#146, #177, #184, #190, #203, #279).
/// It is public, mirroring <see cref="NativeRequired"/>, because both adapter assemblies consume it.
/// <para>
/// <c>Step</c> unifies onto MudBlazor's existing per-type defaults
/// (<c>0.01</c>/decimal, <c>0.1</c>/double, <c>0.1</c>/float, <c>1</c> otherwise) rather than
/// Fluent's flat <c>1</c>-for-everything: MudBlazor has shipped (v3.1.0 and earlier) with these
/// values, while <c>FormCraft.ForFluentUI</c> has not shipped in any release yet (added for the
/// still-unreleased 4.0.0). Keeping the shipped adapter's behaviour makes this a true zero-behaviour
/// refactor rather than a change dressed as one.
/// </para>
/// </remarks>
/// <typeparam name="TValue">The field's numeric value type.</typeparam>
public static class NumericTypeDefaults<TValue>
    where TValue : struct
{
    /// <summary>The type's own <c>MinValue</c> static field, or <c>default</c> when it has none.</summary>
    public static readonly TValue Min = ResolveBound("MinValue");

    /// <summary>The type's own <c>MaxValue</c> static field, or <c>default</c> when it has none.</summary>
    public static readonly TValue Max = ResolveBound("MaxValue");

    /// <summary>The default step for an unconfigured field.</summary>
    public static readonly TValue Step = ResolveStep();

    private static TValue ResolveBound(string fieldName)
    {
        var field = typeof(TValue).GetField(fieldName);
        return field != null ? (TValue)field.GetValue(null)! : default;
    }

    private static TValue ResolveStep()
    {
        // Unbox-then-cast only succeeds when the boxed type matches TValue exactly, so each
        // floating type needs its own literal (0.1 boxed as double cannot be unboxed as float, and
        // 1 boxed as int cannot be unboxed as long/short/byte).
        if (typeof(TValue) == typeof(decimal))
        {
            return (TValue)(object)0.01m;
        }

        if (typeof(TValue) == typeof(double))
        {
            return (TValue)(object)0.1d;
        }

        if (typeof(TValue) == typeof(float))
        {
            return (TValue)(object)0.1f;
        }

        return (TValue)Convert.ChangeType(1, typeof(TValue));
    }
}
