using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the build's test-reporting contract (#231). Every claim here was inert before that issue:
/// <c>Build.cs</c> asked for a results directory and two loggers through <c>DotNetTest</c>'s VSTest
/// surface, <c>dotnet test</c> forwarded them as the MSBuild properties
/// <c>VSTestResultsDirectory</c>/<c>VSTestLogger</c>, and Microsoft.Testing.Platform ignored both
/// outright (warning <c>MTP0001</c>). So <c>test-results/</c> was never created, no trx or html
/// report was ever produced, and the <c>Test</c> target's <c>.Produces(...)</c> lines described files
/// that did not exist.
/// </summary>
/// <remarks>
/// <para>
/// Nothing about that failure was visible: it broke no build and reddened no run — the wiring simply
/// *read* correct and produced nothing, which is why it survived several CI reworks (#200, #225,
/// #230). A regression here would be equally quiet, so it is asserted on the build script's text
/// rather than left to be noticed.
/// </para>
/// <para>
/// ⚠️ Known limitation: the workflow assertions are file-scoped, not job-scoped. A workflow that ran
/// the build in one job and uploaded the artifact from another would satisfy them while uploading
/// nothing, because the second job gets a fresh workspace. Catching that needs a real YAML parse,
/// which this project has no dependency for; the shape is noted in the PR that added these tests.
/// </para>
/// </remarks>
public class TestReportingTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    /// <summary>Every workflow file, read once — these tests are pure text assertions over them.</summary>
    private static readonly Dictionary<string, string> WorkflowText = ReadAllWorkflows();

    private static readonly List<string> TestRunningWorkflows = DiscoverWorkflowsThatRunTests();

    /// <summary>
    /// The one step name all three workflows use for this upload. Asserted as a literal because it
    /// is also how a human finds the step in a run's log.
    /// </summary>
    private const string UploadStepName = "Publish: test-results";

    /// <summary>
    /// Every Nuke target that reaches <c>Test</c>, directly or through <c>DependsOn</c>: <c>Pack</c>,
    /// <c>Publish</c>, <c>PublishIfNeeded</c>, <c>Continuous</c> and <c>Release</c> all run the suite.
    /// A workflow invoking any of them owes the artifact.
    /// </summary>
    private const string TargetsThatRunTests = "Test|Pack|PublishIfNeeded|Publish|Continuous|Release";

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
        // Not a reporter. `dotnet test`'s MSBuild integration writes one per-assembly `.log` — the
        // artifact #225 exists to preserve — into whatever --results-directory names, which is what
        // moved them out of <project>/bin/<cfg>/<tfm>/TestResults/. Distinct from MTP's --diagnostic
        // log, which is opt-in and named log_<timestamp>.diag.
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

    private static string WorkflowsDirectory => Path.Combine(RepoRoot, ".github", "workflows");

    private static Dictionary<string, string> ReadAllWorkflows() =>
        Directory
            .EnumerateFiles(WorkflowsDirectory, "*.*")
            .Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(f => Path.GetFileName(f), f => File.ReadAllText(f), StringComparer.Ordinal);

    private static string ReadWorkflow(string fileName) => WorkflowText[fileName];

    private static string ReadBuildScript() =>
        File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs"));

    /// <summary>
    /// Every workflow that reaches Nuke's <c>Test</c> target, discovered rather than listed, so a
    /// fourth workflow added later is held to the same contract on the day it lands — which is the
    /// failure mode this issue is about: <c>release-please.yml</c> was left behind by #225 and
    /// nobody noticed.
    /// </summary>
    /// <remarks>
    /// Matched on the wrapper invocation anywhere in the comment-stripped file rather than on a
    /// <c>run:</c> prefix: a <c>run: |</c> block puts the command on a later line, and the repo
    /// documents <c>./build.sh</c> as the macOS/Linux entry point alongside <c>./build.cmd</c>.
    /// </remarks>
    private static List<string> DiscoverWorkflowsThatRunTests() =>
        WorkflowText
            .Where(entry => Regex.IsMatch(
                WithoutComments(entry.Value, "#"),
                $@"\./build\.(cmd|sh|ps1)\s+({TargetsThatRunTests})\b"))
            .Select(entry => entry.Key)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static List<string> WorkflowsThatRunTests()
    {
        // The vacuity guard lives here rather than in one caller: every assertion below is of the
        // form "no workflow in this set offends", which an empty set satisfies trivially. A rename,
        // a reformatted invocation or a switch to .yaml would empty it and turn the whole class
        // green while checking nothing.
        TestRunningWorkflows.ShouldNotBeEmpty(
            $"no workflow invokes ./build.[cmd|sh|ps1] ({TargetsThatRunTests}) — every assertion over this set would pass vacuously");

        return TestRunningWorkflows;
    }

    /// <summary>
    /// Whether the build script enables <paramref name="option" /> itself, as opposed to merely
    /// mentioning a longer option that starts with it. Load-bearing: these option names are prefixes
    /// of one another — <c>--report-xunit</c> of <c>--report-xunit-trx</c>, and
    /// <c>--report-xunit-html</c> of <c>--report-xunit-html-filename</c>, which names a file without
    /// enabling the reporter. A plain substring test would call an extension "backed" by an option
    /// that emits nothing, which is precisely the defect class this file exists to catch.
    /// </summary>
    private static bool Enables(string build, string option) =>
        Regex.IsMatch(build, Regex.Escape(option) + "(?![-A-Za-z])");

    /// <summary>
    /// Every glob the <c>Test</c> target promises under <c>TestResultsDirectory</c>, as written —
    /// e.g. <c>**/*.trx</c>. Shared so the "is recursive" and "is backed by a reporter" assertions
    /// read one list: a glob only one of them could see would let the pair disagree about what the
    /// target actually promises.
    /// </summary>
    private static List<string> PromisedGlobs(string build) =>
        Regex
            .Matches(build, """\.Produces\(TestResultsDirectory\s*/\s*"(?<glob>[^"]+)"\)""")
            .Select(match => match.Groups["glob"].Value)
            .ToList();

    /// <summary>
    /// The extension a promised glob resolves to — <c>trx</c> for both <c>*.trx</c> and
    /// <c>**/*.trx</c>, so the reporter cross-check reads the same answer either side of #256's
    /// move to per-project subdirectories.
    /// </summary>
    private static string ExtensionOf(string glob) => glob[(glob.LastIndexOf('.') + 1)..];

    /// <summary>
    /// Drops whole-line comments so a "not referenced" assertion fires on wiring rather than on
    /// prose — these files carry long explanatory blocks about this very wiring, and without this a
    /// documentation-only edit would turn the suite red for a reason unrelated to the build. Only
    /// leading-<paramref name="marker" /> lines are stripped: a trailing-comment strip would also
    /// mangle the "https://" in a URL.
    /// </summary>
    private static string WithoutComments(string text, string marker) =>
        string.Join(
            '\n',
            text.Split('\n').Where(line => !line.TrimStart().StartsWith(marker, StringComparison.Ordinal)));

    /// <summary>
    /// A single step of a workflow, so an assertion about its <c>if:</c> or <c>path:</c> cannot be
    /// satisfied by the same text sitting on some other step. Runs from the <c>- name:</c> line to
    /// the start of the next list item, which covers the step's <c>if:</c>, <c>uses:</c> and
    /// <c>with:</c>.
    /// </summary>
    private static string StepNamed(string workflowFile, string stepName)
    {
        var lines = WithoutComments(ReadWorkflow(workflowFile), "#").Split('\n');
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

    private static bool HasUploadStep(string workflowFile) =>
        WithoutComments(ReadWorkflow(workflowFile), "#")
            .Contains($"- name: '{UploadStepName}'", StringComparison.Ordinal);

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
        // most weight of the three: it is what puts the reports *and* the per-assembly diagnostic
        // log under test-results/, which is the single path all three workflows upload.
        var build = WithoutComments(ReadBuildScript(), "//");

        Enables(build, "--results-directory").ShouldBeTrue();
        Enables(build, "--report-xunit-trx").ShouldBeTrue();
        Enables(build, "--report-xunit-html").ShouldBeTrue();
    }

    [Fact]
    public void BuildScript_Should_Fail_The_Build_When_No_Report_Is_Emitted()
    {
        // The options above are wiring; this is the only assertion that survives the wiring being
        // honoured today and silently dropped tomorrow, exactly as VSTestResultsDirectory was. #231
        // existed because nothing anywhere noticed months of empty output, so the build itself has
        // to notice.
        var build = WithoutComments(ReadBuildScript(), "//");

        build.ShouldContain("Assert.NotEmpty");

        // `**/` optional: the reports moved into per-project subdirectories in #256, so the guard's
        // own glob had to recurse to keep finding them. What is pinned is that the guard still
        // counts *.trx files — not the depth it counts them at.
        build.ShouldMatch(@"GlobFiles\(""(\*\*/)?\*\.trx""\)");
    }

    [Fact]
    public void BuildScript_Should_Assert_A_Report_Per_Test_Project()
    {
        // "Some project emitted a trx" is a strictly weaker claim than "each did", and the gap is
        // not hypothetical: with one suite reporting and the other silent, a whole-directory glob
        // stays green while half the artifact is missing — the same nothing-happens silence #231
        // was filed about, just at half scale. Since #256 each project owns a subdirectory, so the
        // guard can and must be asked per project.
        var build = WithoutComments(ReadBuildScript(), "//");

        build.ShouldNotMatch(
            @"TestResultsDirectory\.GlobFiles\(",
            "the report guard still globs the whole results directory, so one silent suite passes it");

        // Naming the project is the point of the exercise: an artifact reader who is told only
        // "no trx was produced" is no better off than before, because the directory that should
        // have held one is exactly what they are trying to identify.
        build.ShouldMatch(
            @"Assert\.NotEmpty\([^;]*\{project\.Name\}",
            "the per-project report assertion does not name the project that emitted nothing");
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

        var promised = PromisedGlobs(build);

        promised.ShouldNotBeEmpty("the Test target promises no test-results artifact at all");

        // Shouldly prints the collection on failure, so this names the offending extension.
        var unbacked = promised
            .Select(ExtensionOf)
            .Where(ext => !ReporterForExtension.TryGetValue(ext, out var option) || !Enables(build, option))
            .ToList();

        unbacked.ShouldBeEmpty();
    }

    [Fact]
    public void Test_Target_Should_Promise_Its_Artifacts_Recursively()
    {
        // Each test project now writes into its own test-results/<project>/ subdirectory (#256), so
        // the reports sit one level below where a flat `*.trx` glob looks. Nuke resolves .Produces
        // globs against the filesystem when it collects artifacts, so a non-recursive glob here
        // matches nothing on every run while still reading like a promise — the declared-and-inert
        // shape #231 was filed about, reintroduced by the very change that fixed the naming.
        var build = WithoutComments(ReadBuildScript(), "//");

        var promised = PromisedGlobs(build);

        promised.ShouldNotBeEmpty("the Test target promises no test-results artifact at all");

        // Shouldly prints the collection on failure, so this names the offending glob.
        var flat = promised
            .Where(glob => !glob.StartsWith("**/", StringComparison.Ordinal))
            .ToList();

        flat.ShouldBeEmpty();
    }

    [Fact]
    public void Every_Workflow_That_Runs_Tests_Should_Upload_The_TestResults_Artifact()
    {
        var missing = WorkflowsThatRunTests()
            .Where(w => !HasUploadStep(w))
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
        // Asserted as the whole `path:` value, not as a substring: `name: test-results` names the
        // artifact and would otherwise satisfy a claim about where it is read from, and a `path: |`
        // block listing test-results among other globs would satisfy a laxer line-wise check while
        // re-introducing the very globs the next test rejects. All three steps upload exactly one
        // directory, so that is what is pinned.
        var offenders = WorkflowsThatRunTests()
            .Where(w => !StepNamed(w, UploadStepName)
                .Split('\n')
                .Any(line => line.Trim() == "path: test-results"))
            .ToList();

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void No_TestResults_Upload_Should_Point_At_A_Path_The_Build_No_Longer_Fills()
    {
        // Until #231 the per-assembly logs landed in <project>/bin/<cfg>/<tfm>/TestResults/, and
        // ci.yml and continuous.yml globbed for them there because Nuke's test-results/ was never
        // created. --results-directory moved them, so that glob now matches nothing on every run —
        // the same declared-and-inert shape this issue exists to remove, just one file over. Every
        // target that tests routes through Test, so there is no run in which it could match again.
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
        // real one gets missed. The opposite risk — an *empty* directory passing unremarked — is
        // covered in the build rather than here, by the Assert.NotEmpty on the trx.
        var offenders = WorkflowsThatRunTests()
            .Where(w => !StepNamed(w, UploadStepName)
                .Contains("if-no-files-found: ignore", StringComparison.Ordinal))
            .ToList();

        offenders.ShouldBeEmpty();
    }
}
