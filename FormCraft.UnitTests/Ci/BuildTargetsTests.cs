using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the <c>Clean</c> target's claim that it cleans the repository (#275). It hand-listed the
/// directories it swept and reached two of the solution's eight projects, so the three most worth
/// removing — <c>FormCraft.ForMudBlazor.UnitTests</c>, <c>FormCraft.DemoBlazorApp</c> and
/// <c>build</c> — survived every <c>./build.sh Clean</c> untouched.
/// </summary>
/// <remarks>
/// <para>
/// The asymmetry is what made it quiet: an over-eager <c>Clean</c> announces itself on the next
/// build, an under-eager one announces nothing ever. It exits 0, prints nothing alarming, and hands
/// back a tree that is still dirty — so the run performed specifically to rule out a stale assembly
/// preserved it, and the result was then trusted more than it had earned.
/// </para>
/// <para>
/// This is the same defect class as #231 (<c>.Produces(...)</c> naming files nothing emitted) and
/// #256 (artifacts that could not be attributed): a build script's claims are unexecuted prose until
/// something checks them. It is asserted on <c>Build.cs</c>'s text, like every other guard in this
/// directory — this project deliberately takes no dependency on an MSBuild parser, and #259 records
/// what happens when one is reached for anyway (<c>Project.GetProperty</c> passes locally and throws
/// <c>InvalidProjectFileException</c> on every CI run).
/// </para>
/// <para>
/// Every assertion here is scoped to the <b>body of the <c>Clean</c> target</b> rather than to the
/// whole file, and that scoping is load-bearing rather than tidiness: <c>Solution.AllProjects</c>
/// already appears in <c>Test</c>, so a file-scoped "enumerates the solution" assertion passes
/// against the very hand-listed <c>Clean</c> this guard exists to reject.
/// </para>
/// </remarks>
public class BuildTargetsTests
{
    /// <summary>
    /// A sweep driven by a literal directory <em>property</em> — <c>SourceDirectory</c>,
    /// <c>TestsDirectory</c>, … — which is the shape that drifts: it names today's projects and
    /// silently keeps naming them after a new one lands. PascalCase is required so the enumeration's
    /// own lambda parameter is not mistaken for one.
    /// </summary>
    /// <remarks>
    /// Deliberately matched on <c>GlobDirectories</c> alone. <c>ArtifactsDirectory</c> and
    /// <c>TestResultsDirectory</c> are literal properties too and must stay literal — they name
    /// output directories rather than projects, and <c>Clean</c> recreates them with
    /// <c>CreateOrCleanDirectory</c>, which this pattern does not touch.
    /// </remarks>
    private static readonly Regex LiteralProjectSweep =
        new(@"\b[A-Z]\w*Directory\s*\.\s*GlobDirectories", RegexOptions.Compiled);

    [Fact]
    public void Clean_Should_Sweep_Every_Project_In_The_Solution()
    {
        // The property that the hand-list cannot have: a project added to FormCraft.sln is cleaned
        // on the day it lands. FormCraft.ForFluentUI and its test suite (#261) are the proof that
        // this is not hypothetical — they were added to the solution after the hand-list was
        // written, and Clean has never once removed their build output.
        var clean = TargetBody("Clean");

        clean.ShouldContain(
            "AllProjects",
            customMessage: "the Clean target does not enumerate the solution's projects");
    }

    [Fact]
    public void Clean_Should_Not_Name_Project_Directories_Literally()
    {
        var clean = TargetBody("Clean");

        // Shouldly prints the collection on failure, so this names the offending property rather
        // than only reporting that some literal sweep survived.
        var literal = LiteralProjectSweep
            .Matches(clean)
            .Select(match => match.Value)
            .ToList();

        literal.ShouldBeEmpty();
    }

    [Fact]
    public void Clean_Should_Still_Delete_Bin_And_Obj()
    {
        // The vacuity guard, and the reason the two assertions above cannot be satisfied by simply
        // deleting the sweep: "names no directory literally" is true of a Clean target that sweeps
        // nothing at all. DotNetClean alone does not close that gap — it removes an SDK-tracked
        // subset, which is why the glob sweep exists alongside it in the first place.
        var clean = TargetBody("Clean");

        clean.ShouldMatch(
            @"GlobDirectories\(\s*""\*\*/bin""\s*,\s*""\*\*/obj""\s*\)",
            customMessage: "the Clean target no longer globs **/bin and **/obj");
        clean.ShouldContain(
            "DeleteDirectory",
            customMessage: "the Clean target globs build output but deletes none of it");
    }

    [Fact]
    public void Clean_Should_Not_Sweep_From_The_Repository_Root()
    {
        // The blast radius, pinned. RootDirectory.GlobDirectories("**/bin", "**/obj") is the
        // shortest fix available and would satisfy every assertion above — while also deleting the
        // build output of every full checkout under .claude/worktrees/, which is where this repo
        // keeps other agents' in-flight work. Enumerating from the solution is what keeps the sweep
        // inside solution projects, so the alternative is rejected here rather than left to taste.
        var clean = TargetBody("Clean");

        clean.ShouldNotMatch(@"\bRootDirectory\s*\.\s*GlobDirectories");
    }

    [Fact]
    public void Clean_Should_Not_Filter_The_Projects_It_Sweeps()
    {
        // Enumerating the solution and then narrowing it is the hand-list rebuilt in a slower way:
        // every assertion above survives a `.Where(...)`, because the target still reads
        // "AllProjects", still globs bin/obj, and still names no directory literally. Every project
        // in the solution has build output worth removing — including _build itself — so there is
        // no predicate this target legitimately needs.
        //
        // It also blocks the specific predicate most likely to be reached for. #259 filtered
        // projects with Project.GetProperty(...), which evaluates the csproj with MSBuild in-process:
        // green locally, and InvalidProjectFileException ("could not load NuGet.Frameworks") on
        // every ubuntu CI run. Clean has no filtering to do, so the cheapest way to keep that out is
        // to admit no predicate at all.
        var clean = TargetBody("Clean");

        clean.ShouldNotContain(".Where(", customMessage: "the Clean target narrows the solution's projects");
        clean.ShouldNotContain("GetProperty", customMessage: "Clean evaluates csproj properties (see #259)");
    }

    /// <summary>
    /// The text of one Nuke target — from its <c>Target Name =&gt;</c> declaration up to the next
    /// one — so an assertion about this target cannot be answered by a sibling.
    /// </summary>
    /// <remarks>
    /// Reads <see cref="WorkflowSource.BuildScript" />, the shared comment-stripped
    /// <c>build/Build.cs</c> text (#255), so the prose in this file's own subject cannot turn it
    /// red: without that, a documentation-only edit fails the suite for a reason unrelated to the
    /// build, whose natural repair under time pressure is to delete the assertion.
    /// </remarks>
    private static string TargetBody(string name)
    {
        var body = Regex.Match(
            WorkflowSource.BuildScript,
            $@"^\s*Target\s+{Regex.Escape(name)}\s*=>.*?(?=^\s*Target\s+\w+\s*=>|\z)",
            RegexOptions.Singleline | RegexOptions.Multiline);

        // Not decoration: every assertion above reads this string, and a rename or a reformatted
        // declaration would hand them all the empty string. "Contains no literal sweep" is true of
        // nothing at all, so the whole class would go green while checking a target it never found.
        body.Success.ShouldBeTrue($"build/Build.cs declares no `Target {name}` to assert against");

        return body.Value;
    }
}
