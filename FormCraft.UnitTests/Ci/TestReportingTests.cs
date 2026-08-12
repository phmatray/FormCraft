using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the build's test-reporting contract (#231). Every claim here was inert before that issue:
/// <c>Build.cs</c> asked for a results directory and two loggers through <see cref="DotNetTest" />'s
/// VSTest surface, <c>dotnet test</c> forwarded them as the MSBuild properties
/// <c>VSTestResultsDirectory</c>/<c>VSTestLogger</c>, and Microsoft.Testing.Platform ignored both
/// outright (warning <c>MTP0001</c>). So <c>test-results/</c> was never created, no trx or html
/// report was ever produced, and the <c>Test</c> target's <c>.Produces(...)</c> lines described files
/// that did not exist.
/// </summary>
/// <remarks>
/// Nothing about that failure was visible: it broke no build and reddened no run — the wiring simply
/// *read* correct and produced nothing, which is why it survived several CI reworks (#200, #225,
/// #230). A regression here would be equally quiet, so it is asserted on the build script's text
/// rather than left to be noticed.
/// </remarks>
public class TestReportingTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    /// <summary>
    /// Maps an artifact extension to the runner option that has to be present for the build to
    /// actually emit it. Deliberately lists every reporter xunit.v3's Microsoft.Testing.Platform
    /// runner offers, not only the two in use, so switching format stays a one-line change here
    /// rather than a puzzle: the point of the table is to reject an extension backed by *nothing*.
    /// </summary>
    private static readonly Dictionary<string, string> ReporterForExtension = new(StringComparer.Ordinal)
    {
        ["trx"] = "--report-xunit-trx",
        ["html"] = "--report-xunit-html",
        ["xunit"] = "--report-xunit",
        ["junit"] = "--report-junit",
        ["nunit"] = "--report-nunit",
        ["ctrf"] = "--report-ctrf",
        // Not a reporter: Microsoft.Testing.Platform writes one per-assembly diagnostic log — the
        // artifact #225 exists to preserve — into whatever --results-directory names.
        ["log"] = "--results-directory",
    };

    private static string LocateRepoRoot()
    {
        // dir.Parent is DirectoryInfo?, so the variable has to be too — inferring DirectoryInfo
        // from the initializer would fail the build under TreatWarningsAsErrors.
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FormCraft.sln")))
        {
            dir = dir.Parent;
        }

        dir.ShouldNotBeNull("could not locate FormCraft.sln above the test output directory");
        return dir.FullName;
    }

    /// <summary>
    /// The one step name all three workflows use for this upload. Asserted as a literal because it
    /// is also how a human finds the step in a run's log.
    /// </summary>
    private const string UploadStepName = "Publish: test-results";

    private static string ReadBuildScript() =>
        File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs"));

    private static string WorkflowsDirectory => Path.Combine(RepoRoot, ".github", "workflows");

    private static string ReadWorkflow(string fileName) =>
        File.ReadAllText(Path.Combine(WorkflowsDirectory, fileName));

    /// <summary>
    /// Every workflow that reaches Nuke's <c>Test</c> target, discovered rather than listed: `Pack`
    /// and `Continuous` both `DependsOn(Test)`, so all three run the suite and all three therefore
    /// owe the artifact. Deriving the set means a fourth workflow added later is held to the same
    /// contract on the day it lands, which is the failure mode this issue is about —
    /// <c>release-please.yml</c> was left behind by #225 and nobody noticed for two releases.
    /// </summary>
    private static List<string> WorkflowsThatRunTests() =>
        Directory
            .EnumerateFiles(WorkflowsDirectory, "*.*")
            .Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .Where(f => Regex.IsMatch(
                WithoutComments(File.ReadAllText(f), "#"),
                @"run:\s*\./build\.cmd\s+(Test|Pack|Continuous)\b"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// A single step of a workflow, so an assertion about its <c>if:</c> or <c>path:</c> cannot be
    /// satisfied by the same text sitting on some other step. Runs from the <c>- name:</c> line to
    /// the start of the next list item, which covers the step's <c>if:</c>, <c>uses:</c> and
    /// <c>with:</c>.
    /// </summary>
    private static string StepNamed(string workflowFile, string stepName)
    {
        var lines = ReadWorkflow(workflowFile).Split('\n');
        var start = Array.FindIndex(
            lines,
            l => l.TrimStart().StartsWith($"- name: '{stepName}'", StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(0, $"{workflowFile} no longer has a step named '{stepName}'");

        var end = Array.FindIndex(lines, start + 1, l => l.TrimStart().StartsWith("- ", StringComparison.Ordinal));
        if (end < 0)
        {
            end = lines.Length;
        }

        return string.Join('\n', lines[start..end]);
    }

    /// <summary>
    /// Drops whole-line comments so a "not referenced" assertion fires on wiring rather than on
    /// prose — <c>Build.cs</c> carries long explanatory blocks about this very wiring, and without
    /// this a documentation-only edit would turn the suite red for a reason unrelated to the build.
    /// Only leading-<paramref name="marker" /> lines are stripped: a trailing-comment strip would
    /// also mangle the "https://" in a URL.
    /// </summary>
    private static string WithoutComments(string text, string marker) =>
        string.Join(
            '\n',
            text.Split('\n').Where(line => !line.TrimStart().StartsWith(marker, StringComparison.Ordinal)));

    [Fact]
    public void BuildScript_Should_Not_Set_The_VSTest_Properties_That_Mtp_Ignores()
    {
        // Both of these reach dotnet test as VSTest-only MSBuild properties. Under
        // Microsoft.Testing.Platform they are not merely ineffective, they are announced as
        // ignored (MTP0001) on every single run — warning noise in a repo whose entire build runs
        // under TreatWarningsAsErrors, which is exactly how readers get trained past warnings.
        var build = WithoutComments(ReadBuildScript(), "//");

        build.ShouldNotContain("SetResultsDirectory");
        build.ShouldNotContain("SetLoggers");
    }

    [Fact]
    public void BuildScript_Should_Emit_Reports_Through_The_Testing_Platform()
    {
        // The native equivalents, which the runner does honour. --results-directory carries the
        // most weight of the three: it is what puts the reports *and* MTP's per-assembly diagnostic
        // log under test-results/, which is the single path all three workflows upload.
        var build = WithoutComments(ReadBuildScript(), "//");

        build.ShouldContain("--results-directory");
        build.ShouldContain("--report-xunit-trx");
        build.ShouldContain("--report-xunit-html");
    }

    [Fact]
    public void Test_Target_Should_Only_Promise_Artifacts_The_Runner_Is_Asked_To_Write()
    {
        // The defect this pins is not "the wrong glob", it is a .Produces(...) contract that named
        // files nothing emitted — `*.xml` was never written by anything, in either the VSTest or the
        // MTP world. Cross-checking the promise against the flags means the assertion cannot be
        // satisfied by copying a literal list, and a future format switch that updates one half
        // without the other turns red here.
        var build = WithoutComments(ReadBuildScript(), "//");

        var promised = Regex
            .Matches(build, """\.Produces\(TestResultsDirectory\s*/\s*"\*\.(?<ext>[A-Za-z]+)"\)""")
            .Select(match => match.Groups["ext"].Value)
            .ToList();

        promised.ShouldNotBeEmpty("the Test target promises no test-results artifact at all");

        // Shouldly prints the collection on failure, so this names the offending extension.
        var unbacked = promised
            .Where(ext => !ReporterForExtension.TryGetValue(ext, out var option)
                          || !build.Contains(option, StringComparison.Ordinal))
            .ToList();

        unbacked.ShouldBeEmpty();
    }

    [Fact]
    public void Every_Workflow_That_Runs_Tests_Should_Upload_The_TestResults_Artifact()
    {
        var workflows = WorkflowsThatRunTests();

        workflows.ShouldNotBeEmpty(
            "no workflow invokes ./build.cmd Test|Pack|Continuous — every assertion below would pass vacuously");

        var missing = workflows
            .Where(w => !ReadWorkflow(w).Contains($"- name: '{UploadStepName}'", StringComparison.Ordinal))
            .ToList();

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void Every_TestResults_Upload_Should_Run_On_Failure()
    {
        // `if: always()` is the entire point of the artifact (#225): the failure path is the only
        // one worth preserving, and it is exactly the path a bare step skips. Microsoft.Testing.
        // Platform prints only a summary line to stdout, so without this a red CI run leaves no
        // record of *which* assertion failed — a cost that was paid for real during #200.
        var offenders = WorkflowsThatRunTests()
            .Where(w => !StepNamed(w, UploadStepName).Contains("if: always()", StringComparison.Ordinal))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Every_TestResults_Upload_Should_Point_At_The_Directory_The_Build_Fills()
    {
        // Scoped to a path *line* rather than a substring search: `name: test-results` names the
        // artifact and would otherwise satisfy an assertion about where it is read from.
        var offenders = WorkflowsThatRunTests()
            .Where(w => !StepNamed(w, UploadStepName)
                .Split('\n')
                .Select(line => line.Trim())
                .Any(line => line is "path: test-results" or "test-results"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void No_TestResults_Upload_Should_Point_At_A_Path_The_Build_No_Longer_Fills()
    {
        // Until #231 the MTP logs landed in <project>/bin/<cfg>/<tfm>/TestResults/, and ci.yml and
        // continuous.yml globbed for them there because Nuke's test-results/ was never created.
        // --results-directory moved them, so that glob now matches nothing on every run — the same
        // declared-and-inert shape this issue exists to remove, just one file over. Every path in
        // the build reaches the Test target, so there is no run in which it could match again.
        var offenders = WorkflowsThatRunTests()
            .Where(w => StepNamed(w, UploadStepName).Contains("**/TestResults/", StringComparison.Ordinal))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Every_TestResults_Upload_Should_Tolerate_A_Missing_Path()
    {
        // These steps run under `if: always()`, which includes runs that failed in Compile — before
        // any test executed and therefore before test-results/ existed. Without this, upload-artifact
        // warns on a condition that is entirely expected, and a warning nobody can act on is how a
        // real one gets missed.
        var offenders = WorkflowsThatRunTests()
            .Where(w => !StepNamed(w, UploadStepName)
                .Contains("if-no-files-found: ignore", StringComparison.Ordinal))
            .ToList();

        offenders.ShouldBeEmpty();
    }
}
