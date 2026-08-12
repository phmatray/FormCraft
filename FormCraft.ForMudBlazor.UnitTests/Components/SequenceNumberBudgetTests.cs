using System.Reflection;
using System.Text.RegularExpressions;

namespace FormCraft.ForMudBlazor.UnitTests.Components;

/// <summary>
/// Enforces <c>CollectionFieldComponent</c>'s <c>RenderTreeBuilder</c> sequence-number budget (#214).
/// </summary>
/// <remarks>
/// <para>
/// The budget was enforced <b>only by a comment</b>. <c>AddCommonFieldAttributes</c> grew from 8 to 13
/// attributes across #177/#181/#184/#190/#192, and <c>CallerAttributeStart</c> is 20 — at 20 the common
/// block would collide with the caller block and <b>corrupt render-tree diffing silently</b>: no
/// exception, no failed assertion, just attributes overwriting each other. The overflow had to be
/// re-verified by hand to know #193/#194/#195/#196 could land together.
/// </para>
/// <para>
/// Sequence numbers must be compile-time constants tied to a source position, so they cannot be
/// computed at runtime and a rendering test cannot observe them. This measures the <b>source</b>
/// instead: the block starts come from the real constants by reflection, and the widths from counting
/// the `AddAttribute` calls in each method. Both halves therefore track the code rather than a copy of
/// it — the failure mode this issue is about is a copied number going stale.
/// </para>
/// </remarks>
public class SequenceNumberBudgetTests
{
    private static readonly string Source = ReadComponentSource();

    [Fact]
    public void Common_Block_Should_Not_Reach_The_Caller_Block()
    {
        // The headline constraint. AddCommonFieldAttributes starts at CommonAttributeStart and
        // consumes one number per AddAttribute call; the last one it uses must stay strictly below
        // where the caller block begins.
        var start = ConstantValue("CommonAttributeStart");
        var used = CountSequentialAttributes("AddCommonFieldAttributes");
        var highest = start + used - 1;

        used.ShouldBeGreaterThan(0, "the attribute count could not be measured — the parser is broken, not the budget");
        highest.ShouldBeLessThan(
            ConstantValue("CallerAttributeStart"),
            $"the common block now reaches {highest}; raise CallerAttributeStart (and TextAttributeStart with it)");
    }

    [Fact]
    public void Caller_Block_Should_Not_Reach_The_Text_Block()
    {
        // Callers write their own attributes at CallerAttributeStart + N. #208 took the numeric
        // renderer's run from +3 to +5, which is exactly the kind of growth this guards.
        var start = ConstantValue("CallerAttributeStart");
        var highest = start + HighestOffsetOf("CallerAttributeStart");

        highest.ShouldBeLessThan(
            ConstantValue("TextAttributeStart"),
            $"the caller block now reaches {highest}; raise TextAttributeStart");
    }

    [Fact]
    public void Text_Block_Should_Start_Clear_Of_The_Caller_Block()
    {
        // The text block is last, so nothing sits above it — but it must still begin above the
        // caller block's highest number rather than merely above its start.
        var callerHighest = ConstantValue("CallerAttributeStart") + HighestOffsetOf("CallerAttributeStart");

        ConstantValue("TextAttributeStart").ShouldBeGreaterThan(callerHighest);
    }

    [Fact]
    public void Blocks_Should_Be_Declared_In_Ascending_Order()
    {
        // A cheap structural check that would catch someone reordering the constants without
        // reordering the code that depends on them.
        ConstantValue("CommonAttributeStart").ShouldBeLessThan(ConstantValue("CallerAttributeStart"));
        ConstantValue("CallerAttributeStart").ShouldBeLessThan(ConstantValue("TextAttributeStart"));
    }

    /// <summary>
    /// Reads one of the component's private sequence-number constants — the real value, so the test
    /// cannot drift from the code the way a copied literal would.
    /// </summary>
    private static int ConstantValue(string name)
    {
        var field = typeof(CollectionFieldComponent<,>)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField);

        field.ShouldNotBeNull($"CollectionFieldComponent no longer declares {name}");
        return (int)field.GetRawConstantValue()!;
    }

    /// <summary>
    /// Counts the <c>AddAttribute(startIndex…)</c> calls inside one method — how many sequence
    /// numbers that method consumes.
    /// </summary>
    /// <remarks>
    /// Scoped to a single method body on purpose: <c>AddTextInputAttributes</c> takes a parameter of
    /// the same name, so a whole-file count reports the two methods added together (17 rather than 13)
    /// and would pass a budget that is actually blown.
    /// </remarks>
    private static int CountSequentialAttributes(string methodName)
    {
        var body = MethodBody(methodName);
        return Regex.Matches(body, @"AddAttribute\(\s*startIndex").Count;
    }

    /// <summary>
    /// The largest <c>N</c> in <c>&lt;constant&gt; + N</c> across the file, or 0 when the constant is
    /// only ever used bare.
    /// </summary>
    private static int HighestOffsetOf(string constantName)
    {
        var offsets = Regex.Matches(Source, $@"{constantName}\s*\+\s*(\d+)")
            .Select(m => int.Parse(m.Groups[1].Value))
            .ToList();

        return offsets.Count == 0 ? 0 : offsets.Max();
    }

    /// <summary>
    /// Extracts a method body by brace matching from its <b>declaration</b>.
    /// </summary>
    /// <remarks>
    /// Anchored on an accessibility modifier on purpose. Matching the bare name finds a *call site*
    /// first — <c>AddCommonFieldAttributes(builder, …)</c> appears above its own declaration — and
    /// brace-matching from there reads the caller's body instead, which counted 0 attributes and
    /// would have reported a budget of zero as "plenty of room".
    /// </remarks>
    private static string MethodBody(string methodName)
    {
        var declaration = Regex.Match(
            Source,
            $@"(?:private|internal|protected|public)[^\n(]*\b{Regex.Escape(methodName)}\s*\(");

        declaration.Success.ShouldBeTrue($"CollectionFieldComponent no longer declares {methodName}");

        var open = Source.IndexOf('{', declaration.Index);
        open.ShouldBeGreaterThanOrEqualTo(0, $"could not find the body of {methodName}");

        var depth = 0;
        for (var i = open; i < Source.Length; i++)
        {
            if (Source[i] == '{')
            {
                depth++;
            }
            else if (Source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return Source[open..i];
                }
            }
        }

        throw new InvalidOperationException($"unbalanced braces while reading {methodName}");
    }

    private static string ReadComponentSource()
    {
        // dir.Parent is DirectoryInfo?, so the variable has to be too — inferring DirectoryInfo from
        // the initializer would fail the build under TreatWarningsAsErrors.
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FormCraft.sln")))
        {
            dir = dir.Parent;
        }

        dir.ShouldNotBeNull("could not locate FormCraft.sln above the test output directory");

        var path = Path.Combine(
            dir.FullName,
            "FormCraft.ForMudBlazor",
            "Features",
            "CollectionField",
            "CollectionFieldComponent.razor.cs");

        File.Exists(path).ShouldBeTrue($"CollectionFieldComponent.razor.cs not found at {path}");
        return File.ReadAllText(path);
    }
}
