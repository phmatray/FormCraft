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
/// The workflow assertions are scoped to the <b>job</b> that runs the build (#255), not to the file.
/// Each job gets a fresh workspace, so a build in one job and an upload in another uploads nothing
/// at all — silently, since <c>if-no-files-found: ignore</c> is pinned on every upload. Job splitting
/// is done by indentation in <see cref="WorkflowSource.JobsOf" /> rather than with a YAML parser this
/// project deliberately takes no dependency on.
/// </para>
/// </remarks>
public class TestReportingTests
{
    private static readonly IReadOnlyList<TestRunningJob> TestRunningJobs = DiscoverJobsThatRunTests();

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

    /// <summary>
    /// The wrapper invocation that means a job runs the suite. Matched anywhere in the job's
    /// comment-stripped text rather than on a <c>run:</c> prefix: a <c>run: |</c> block puts the
    /// command on a later line, and the repo documents <c>./build.sh</c> as the macOS/Linux entry
    /// point alongside <c>./build.cmd</c>.
    /// </summary>
    private static string InvocationPattern => $@"\./build\.(cmd|sh|ps1)\s+({TargetsThatRunTests})\b";

    /// <summary>One job of one workflow — the scope every workflow claim below is held against.</summary>
    private sealed record TestRunningJob(string Workflow, string Job, string Text)
    {
        /// <summary>Names the offending pair when Shouldly prints the collection on failure.</summary>
        public override string ToString() => $"{Workflow} / {Job}";
    }

    /// <summary>
    /// Every (workflow, job) pair that reaches Nuke's <c>Test</c> target, discovered rather than
    /// listed, so a workflow — or a job — added later is held to the same contract on the day it
    /// lands, which is the failure mode this guard is about: <c>release-please.yml</c> was left
    /// behind by #225 and nobody noticed.
    /// </summary>
    /// <remarks>
    /// Scoped to the <b>job</b> rather than the file (#255). Every job gets a fresh workspace, so a
    /// build in job A leaves nothing for an upload in job B to find — and with
    /// <c>if-no-files-found: ignore</c> pinned on every upload (#252), that combination uploads
    /// nothing while satisfying a file-scoped assertion completely silently. Files are narrowed
    /// first only as a cheap pre-filter: a job that matches implies a file that matches, so it
    /// cannot drop a pair.
    /// </remarks>
    private static IReadOnlyList<TestRunningJob> DiscoverJobsThatRunTests() =>
        WorkflowSource
            .Matching(InvocationPattern)
            .SelectMany(workflow => WorkflowSource
                .JobsOf(workflow)
                .OrderBy(job => job.Key, StringComparer.Ordinal)
                .Where(job => Regex.IsMatch(job.Value, InvocationPattern))
                .Select(job => new TestRunningJob(workflow, job.Key, job.Value)))
            .ToList();

    private static IReadOnlyList<TestRunningJob> JobsThatRunTests()
    {
        // The vacuity guard lives here rather than in one caller: every assertion below is of the
        // form "no job in this set offends", which an empty set satisfies trivially. A rename, a
        // reformatted invocation or a switch to .yaml would empty it and turn the whole class green
        // while checking nothing.
        //
        // It is deliberately only half the protection — it fires when the set is *entirely* empty,
        // and cannot see one workflow dropping out. That case is covered by the test below.
        TestRunningJobs.ShouldNotBeEmpty(
            $"no job invokes ./build.[cmd|sh|ps1] ({TargetsThatRunTests}) — every assertion over this set would pass vacuously");

        return TestRunningJobs;
    }

    [Fact]
    public void Every_Workflow_That_Runs_Tests_Should_Contribute_A_Job()
    {
        // Job scoping is strictly stronger than the file scoping it replaced — except in one way,
        // which this closes. File scoping could not lose a workflow: the file either matched or it
        // did not. Job scoping can, because a file that matches still contributes nothing when
        // JobsOf fails to split it (an unrecognised `jobs:` layout, a quoted or differently indented
        // job key). The whole-set vacuity guard cannot see that — the set is still non-empty from
        // the other workflows — so the assertions would simply stop covering that file, green.
        //
        // Asserted as an equality against the file-level discovery, which is the one thing known to
        // be complete: every workflow whose text invokes the build must appear in the pair set.
        var covered = JobsThatRunTests()
            .Select(j => j.Workflow)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        covered.ShouldBe(WorkflowSource.Matching(InvocationPattern));
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
    /// The upload step as it appears <em>within that job</em> — so an assertion about it cannot be
    /// answered by an identically-named step sitting in a sibling job.
    /// </summary>
    private static string UploadStep(TestRunningJob job) =>
        WorkflowSource.StepNamed(job.Text, UploadStepName, job.ToString());

    [Fact]
    public void BuildScript_Should_Not_Set_The_VSTest_Properties_That_Mtp_Ignores()
    {
        // Both of these reach dotnet test as VSTest-only MSBuild properties. Under
        // Microsoft.Testing.Platform they are not merely ineffective, they are announced as
        // ignored (MTP0001) on every single run — warning noise in a repo whose entire build runs
        // under TreatWarningsAsErrors, which is exactly how readers get trained past warnings.
        var build = WorkflowSource.BuildScript;

        build.ShouldNotContain("SetResultsDirectory");
        build.ShouldNotContain("SetLoggers");
    }

    [Fact]
    public void BuildScript_Should_Emit_Reports_Through_The_Testing_Platform()
    {
        // The native equivalents, which the runner does honour. --results-directory carries the
        // most weight of the three: it is what puts the reports *and* the per-assembly diagnostic
        // log under test-results/, which is the single path all three workflows upload.
        var build = WorkflowSource.BuildScript;

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
        var build = WorkflowSource.BuildScript;

        build.ShouldContain("Assert.NotEmpty");
        build.ShouldMatch(@"GlobFiles\(""\*\.trx""\)");
    }

    [Fact]
    public void Test_Target_Should_Only_Promise_Artifacts_The_Runner_Is_Asked_To_Write()
    {
        // The defect this pins is not "the wrong glob", it is a .Produces(...) contract that named
        // files nothing emitted — `*.xml` was never written by anything, in either the VSTest or the
        // MTP world. Cross-checking the promise against the flags means the assertion cannot be
        // satisfied by copying a literal list, and a future format switch that updates one half
        // without the other turns red here.
        var build = WorkflowSource.BuildScript;

        var promised = Regex
            .Matches(build, """\.Produces\(TestResultsDirectory\s*/\s*"\*\.(?<ext>[A-Za-z]+)"\)""")
            .Select(match => match.Groups["ext"].Value)
            .ToList();

        promised.ShouldNotBeEmpty("the Test target promises no test-results artifact at all");

        // Shouldly prints the collection on failure, so this names the offending extension.
        var unbacked = promised
            .Where(ext => !ReporterForExtension.TryGetValue(ext, out var option) || !Enables(build, option))
            .ToList();

        unbacked.ShouldBeEmpty();
    }

    [Fact]
    public void Every_Job_That_Runs_Tests_Should_Upload_The_TestResults_Artifact()
    {
        // In that same job, not merely somewhere in the file: the artifact is uploaded from the
        // workspace the build filled, and a sibling job's workspace is a different one.
        var missing = JobsThatRunTests()
            .Where(j => !j.Text.Contains($"- name: '{UploadStepName}'", StringComparison.Ordinal))
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
        var offenders = JobsThatRunTests()
            .Where(j => !UploadStep(j).Contains("if: always()", StringComparison.Ordinal))
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
        var offenders = JobsThatRunTests()
            .Where(j => !UploadStep(j)
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
        var offenders = JobsThatRunTests()
            .Where(j => UploadStep(j).Contains("**/TestResults/", StringComparison.Ordinal))
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
        var offenders = JobsThatRunTests()
            .Where(j => !UploadStep(j)
                .Contains("if-no-files-found: ignore", StringComparison.Ordinal))
            .ToList();

        offenders.ShouldBeEmpty();
    }
}
