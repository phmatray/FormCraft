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
    /// Every glob the <c>Test</c> target promises under <c>TestResultsDirectory</c>, as written —
    /// e.g. <c>**/*.trx</c>. Shared so the "is recursive" and "is backed by a reporter" assertions
    /// read one list: a glob only one of them could see would let the pair disagree about what the
    /// target actually promises.
    /// </summary>
    /// <remarks>
    /// The vacuity guard lives here rather than in each caller, for the reason
    /// <see cref="JobsThatRunTests" /> holds its own: every assertion over this list is of the form
    /// "no promised glob offends", which an empty list satisfies trivially. A reformatted
    /// <c>.Produces</c> line would empty it and turn both callers green while checking nothing, and
    /// a third caller added later would have to remember to repeat the check.
    /// </remarks>
    private static List<string> PromisedGlobs(string build)
    {
        var promised = Regex
            .Matches(build, """\.Produces\(TestResultsDirectory\s*/\s*"(?<glob>[^"]+)"\)""")
            .Select(match => match.Groups["glob"].Value)
            .ToList();

        promised.ShouldNotBeEmpty("the Test target promises no test-results artifact at all");

        return promised;
    }

    /// <summary>
    /// The extension a promised glob resolves to — <c>trx</c> for both <c>*.trx</c> and
    /// <c>**/*.trx</c>, so the reporter cross-check reads the same answer either side of #256's
    /// move to per-project subdirectories.
    /// </summary>
    private static string ExtensionOf(string glob) => glob[(glob.LastIndexOf('.') + 1)..];

    /// <summary>
    /// The upload step as it appears <em>within that job</em> — so an assertion about it cannot be
    /// answered by an identically-named step sitting in a sibling job — or <c>null</c> when that job
    /// has no such step.
    /// </summary>
    /// <remarks>
    /// Non-asserting since #267. The asserting form threw from in here, so deleting one upload step
    /// reddened five tests: the one whose actual subject is "every such job uploads" named the
    /// offending job cleanly, and the other four died inside this helper, presented as a fault in the
    /// shared reader rather than in the workflow.
    /// <para>
    /// What changed is the <em>message</em>, not the count. Measured on <c>ci.yml</c>: five tests were
    /// red before and five are red after — but where four used to read
    /// <c>Array.FindIndex(…) should be greater than or equal to 0 but was -1</c>, all five now name
    /// <c>ci.yml / build-and-test</c> as an offender. The count cannot fall; every one of these claims
    /// is genuinely unsatisfied by a job with no upload step.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// The <c>job.ToString()</c> is not decoration. Absence comes back as <c>null</c> and is reported by
    /// the caller — but an <em>ambiguous</em> name (this job carrying two identically-named upload steps,
    /// which is legal YAML) still throws from inside the reader, and without a scope description that
    /// failure reads "the searched text has 2 steps named …" across all five assertions: precisely the
    /// undifferentiated shared-reader failure #267 set out to remove, reintroduced by omission.
    /// </remarks>
    private static string? UploadStep(TestRunningJob job) =>
        WorkflowSource.TryStepNamed(job.Text, UploadStepName, job.ToString());

    /// <summary>
    /// Whether <paramref name="job" />'s upload step fails <paramref name="claim" /> — including by
    /// not existing at all.
    /// </summary>
    /// <remarks>
    /// The absence rule lives here, once, rather than inline at each assertion below. Restating
    /// <c>is not { } step ||</c> per call site puts a vacuity hole one forgotten clause away: written
    /// as <c>is { } step &amp;&amp;</c>, a missing step reads as *satisfying* the claim, which is the
    /// silence this file guards against everywhere else (see <see cref="JobsThatRunTests" />). Absence
    /// offends every claim here — a step that is not there has no <c>if: always()</c>, and equally
    /// does not "avoid the stale glob" in any sense worth being green about.
    /// <para>
    /// That <c>&amp;&amp;</c> inversion is no longer a hazard a reader has to hold in mind (#303):
    /// <see cref="Every_Upload_Claim_Should_Report_A_Job_That_Has_No_Upload_Step" /> drives every claim
    /// over a synthetic job that lacks the step and fails if any of them lets it pass. Before that, the
    /// branch was reachable only by hand — #267 verified it by deleting the step from <c>ci.yml</c> and
    /// reverting, which proved the behaviour once and could not prove it again.
    /// </para>
    /// </remarks>
    private static bool UploadStepFails(TestRunningJob job, Func<string, bool> claim) =>
        UploadStep(job) is not { } step || !claim(step);

    /// <summary>
    /// The jobs in <paramref name="jobs" /> whose upload step fails <paramref name="claim" /> —
    /// including by not existing.
    /// </summary>
    /// <remarks>
    /// Takes the job set rather than reading <see cref="JobsThatRunTests" /> itself (#303), and that
    /// parameter is the whole point: every real test-running job has the upload step, so a test that
    /// cannot supply its own jobs cannot reach the "the step is missing" branch at all. The five
    /// assertions below pass <see cref="JobsThatRunTests" />; the coverage test passes a synthetic pair.
    /// <para>
    /// The vacuity guard stays in <see cref="JobsThatRunTests" /> rather than moving here — a hand-built
    /// list cannot be accidentally empty, and asserting non-emptiness of the *caller's* argument would
    /// make the synthetic case impossible to express.
    /// </para>
    /// </remarks>
    private static List<TestRunningJob> UploadOffenders(
        IReadOnlyList<TestRunningJob> jobs,
        Func<string, bool> claim) =>
        jobs.Where(j => UploadStepFails(j, claim)).ToList();

    /// <summary>
    /// The jobs with no upload step at all — <see cref="UploadOffenders" /> under a claim every present
    /// step satisfies.
    /// </summary>
    /// <remarks>
    /// Named rather than spelled `_ =&gt; true` at the call site: "jobs that fail an always-true claim"
    /// is a double negative the reader has to unfold before it reads as "jobs with no step".
    /// </remarks>
    private static List<TestRunningJob> UploadMissing(IReadOnlyList<TestRunningJob> jobs) =>
        UploadOffenders(jobs, _ => true);

    // The four content claims, each defined ONCE. The `[Fact]` that holds a claim against the real
    // workflows and the coverage `[Theory]` that proves the claim reports an offender both reference
    // the same delegate — a dictionary of re-typed copies would let the two drift apart silently, which
    // is the failure this file exists to prevent rather than to commit.

    /// <summary>
    /// `if: always()` is the entire point of the artifact (#225): the failure path is the only one worth
    /// preserving, and it is exactly the path a bare step skips.
    /// </summary>
    private static readonly Func<string, bool> RunsOnFailure =
        s => s.Contains("if: always()", StringComparison.Ordinal);

    /// <summary>
    /// Asserted as the whole <c>path:</c> value, not as a substring: <c>name: test-results</c> names the
    /// artifact and would otherwise satisfy a claim about where it is read from.
    /// </summary>
    private static readonly Func<string, bool> PointsAtTheDirectoryTheBuildFills =
        s => s.Split('\n').Any(line => line.Trim() == "path: test-results");

    /// <summary>
    /// Until #231 the per-assembly logs landed under <c>**/TestResults/</c>; <c>--results-directory</c>
    /// moved them, so that glob now matches nothing on every run.
    /// </summary>
    private static readonly Func<string, bool> AvoidsThePathTheBuildNoLongerFills =
        s => !s.Contains("**/TestResults/", StringComparison.Ordinal);

    /// <summary>
    /// These steps run under <c>if: always()</c>, which includes runs that failed before any test
    /// executed and therefore before <c>test-results/</c> existed.
    /// </summary>
    private static readonly Func<string, bool> ToleratesAMissingPath =
        s => s.Contains("if-no-files-found: ignore", StringComparison.Ordinal);

    /// <summary>The four content claims by name, for the coverage theories below.</summary>
    private static readonly IReadOnlyDictionary<string, Func<string, bool>> UploadClaims =
        new Dictionary<string, Func<string, bool>>(StringComparer.Ordinal)
        {
            ["if: always()"] = RunsOnFailure,
            ["path: test-results"] = PointsAtTheDirectoryTheBuildFills,
            ["avoids **/TestResults/"] = AvoidsThePathTheBuildNoLongerFills,
            ["if-no-files-found: ignore"] = ToleratesAMissingPath,
        };

    /// <summary>
    /// The upload step as all three workflows really write it — copied from <c>ci.yml</c> rather than
    /// simplified.
    /// </summary>
    /// <remarks>
    /// The <c>name: test-results</c> line matters: it is the exact key
    /// <see cref="PointsAtTheDirectoryTheBuildFills" /> exists to tell apart from <c>path:</c>, so a
    /// fixture that dropped it could not exercise the one discrimination that claim makes.
    /// </remarks>
    private const string ConformingUploadStep = """
                                                  - name: 'Publish: test-results'
                                                    if: always()
                                                    uses: actions/upload-artifact@v7
                                                    with:
                                                      name: test-results
                                                      path: test-results
                                                      if-no-files-found: ignore
                                                """;

    private static TestRunningJob Job(string name, string steps) =>
        new("fixture.yml", name, $"steps:\n{steps}");

    /// <summary>A step satisfying every content claim except the named one.</summary>
    public static TheoryData<string, string> StepsViolatingOneClaim() => new()
    {
        { "if: always()", WithoutLine(ConformingUploadStep, "if: always()") },
        { "path: test-results", ConformingUploadStep.Replace("path: test-results", "path: test-results/**", StringComparison.Ordinal) },
        // Adds the stale glob rather than replacing the good path, so this violates that claim ALONE.
        { "avoids **/TestResults/", ConformingUploadStep.Replace("      path: test-results\n", "      path: test-results\n      extra: '**/TestResults/'\n", StringComparison.Ordinal) },
        { "if-no-files-found: ignore", WithoutLine(ConformingUploadStep, "if-no-files-found: ignore") },
    };

    private static string WithoutLine(string step, string marker) =>
        string.Join('\n', step.Split('\n').Where(l => !l.Contains(marker, StringComparison.Ordinal)));

    public static TheoryData<string> UploadClaimNames() => [.. UploadClaims.Keys];

    [Theory]
    [MemberData(nameof(UploadClaimNames))]
    public void Every_Upload_Claim_Should_Report_A_Job_With_No_Upload_Step(string claimName)
    {
        // The branch this pins (#267, PR #287) shipped verified only by hand — deleting the step from
        // ci.yml, watching the tests name the job, then reverting. It fires exactly when this file does
        // its job, but it cannot fire in a *green* run, because every real test-running job has the
        // step. So writing `is { } step &&` instead of `is not { } step ||` would ship green, and a
        // missing upload would read as *satisfying* every claim.
        //
        // The conforming job is in the same call on purpose: passed one job at a time, this would pass
        // just as happily against a helper that returned the whole list whenever any member offended —
        // which in production (three jobs) would destroy the "names the offending job" property.
        var missing = Job("build", "  - name: 'Run: Test'\n    run: ./build.sh Test");

        UploadOffenders([Job("good", ConformingUploadStep), missing], UploadClaims[claimName])
            .ShouldBe([missing]);
    }

    [Theory]
    [MemberData(nameof(StepsViolatingOneClaim))]
    public void Every_Upload_Claim_Should_Report_A_Step_That_Violates_It(string claimName, string violatingStep)
    {
        // The other half, and the one the absence coverage cannot reach: a step that is PRESENT and
        // wrong. Without it, a `UploadStepFails` that ignored `claim` entirely — reducing all four
        // content assertions to "the step exists" — would pass every test in this file.
        var violating = Job("violates", violatingStep);

        UploadOffenders([Job("good", ConformingUploadStep), violating], UploadClaims[claimName])
            .ShouldBe([violating]);
    }

    [Fact]
    public void A_Conforming_Upload_Step_Should_Offend_No_Claim()
    {
        // The fixture the two theories above lean on has to be conforming, or both of them prove
        // nothing: "reports the offender" is trivially satisfied by a helper that reports everyone.
        var conforming = Job("good", ConformingUploadStep);

        UploadMissing([conforming]).ShouldBeEmpty();

        foreach (var claim in UploadClaims)
        {
            UploadOffenders([conforming], claim.Value)
                .ShouldBeEmpty($"the '{claim.Key}' claim reports a conforming upload step as an offender");
        }
    }

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
        var build = WorkflowSource.BuildScript;

        // The behaviour this whole issue turns on, and the one thing every other assertion here
        // survives the loss of: the runner is pointed at a directory derived from the project. Drop
        // it and both suites write into one directory under names that differ only by microsecond —
        // #256 undone, with this file still green.
        build.ShouldMatch(
            @"TestResultsDirectory\s*/\s*project\.Name",
            "the results directory is not derived per project, so the two suites' reports collide again");

        build.ShouldNotMatch(
            @"""--results-directory"",\s*TestResultsDirectory\s*,",
            "the runner is handed the shared results directory rather than the project's own");

        // Narrow on purpose. A future target may have perfectly good reason to glob the whole
        // results directory; what must not come back is the *guard* doing so, since a single
        // reporting suite is enough to satisfy it while the other emits nothing.
        build.ShouldNotMatch(
            @"TestResultsDirectory\.GlobFiles\(""\*\*/\*\.trx""\)",
            "the report guard globs the whole results directory again, so one silent suite passes it");

        // The trx is looked for in the project's own directory, not the shared one.
        build.ShouldMatch(
            @"ResultsDirectoryFor\(\w+\)\.GlobFiles\(""\*\.trx""\)",
            "the report guard does not look for the trx in the project's own results directory");

        // Matched on the crafted message rather than on the loop variable's identifier: renaming
        // `project` or hoisting the string into a local is a refactor this test has no business
        // reddening on. `\w+\.Name` still requires each entry to lead with the offending project,
        // and the directory to follow it — the identity #256 exists to make recoverable.
        build.ShouldMatch(
            @"\{\w+\.Name\} \(nothing in \{ResultsDirectoryFor",
            "the missing-report message does not name the project and directory that came up empty");
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

        // Shouldly prints the collection on failure, so this names the offending extension.
        var unbacked = PromisedGlobs(build)
            .Select(ExtensionOf)
            .Where(ext => !ReporterForExtension.TryGetValue(ext, out var option) || !Enables(build, option))
            .ToList();

        unbacked.ShouldBeEmpty();
    }

    [Fact]
    public void Test_Target_Should_Promise_Its_Artifacts_Recursively()
    {
        // Each test project writes into its own test-results/<project>/ subdirectory since #256, so
        // the reports sit one level below where a flat `*.trx` glob looks.
        //
        // Nothing resolves these globs at runtime — AutoGenerate is false, so no workflow is
        // generated from them, and all three workflows upload `path: test-results` wholesale. What
        // a .Produces line does here is describe the shape on disk, and that is the entire reason
        // to keep it honest: a promise still describing a flat layout the build no longer writes is
        // the declared-but-untrue shape this file exists to catch, one file over from #231's.
        var build = WorkflowSource.BuildScript;

        // Shouldly prints the collection on failure, so this names the offending glob.
        var flat = PromisedGlobs(build)
            .Where(glob => !glob.StartsWith("**/", StringComparison.Ordinal))
            .ToList();

        flat.ShouldBeEmpty();
    }

    [Fact]
    public void Every_Job_That_Runs_Tests_Should_Upload_The_TestResults_Artifact()
    {
        // In that same job, not merely somewhere in the file: the artifact is uploaded from the
        // workspace the build filled, and a sibling job's workspace is a different one.
        //
        // Asked through the same primitive as the four tests below rather than by a raw substring
        // search, so "present" means the one thing here and there — a step the scan can actually
        // isolate, not merely the text of a `- name:` line appearing somewhere in the job.
        var missing = UploadMissing(JobsThatRunTests());

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void Every_TestResults_Upload_Should_Run_On_Failure()
    {
        // `if: always()` is the entire point of the artifact (#225): the failure path is the only
        // one worth preserving, and it is exactly the path a bare step skips. Microsoft.Testing.
        // Platform prints only a summary line to stdout, so without this a red CI run leaves no
        // record of *which* assertion failed — a cost that was paid for real during #200.
        var offenders = UploadOffenders(JobsThatRunTests(), RunsOnFailure);

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
        var offenders = UploadOffenders(JobsThatRunTests(), PointsAtTheDirectoryTheBuildFills);

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
        // The claim is inverted, so it reads "the step exists AND avoids the stale glob". Absence
        // offends it, even though a step that is not there points at nothing: a missing step makes
        // this test red today by throwing, so letting it read as vacuously satisfied would trade a
        // loud failure for a quieter suite — the opposite of the point.
        var offenders = UploadOffenders(JobsThatRunTests(), AvoidsThePathTheBuildNoLongerFills);

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
        var offenders = UploadOffenders(JobsThatRunTests(), ToleratesAMissingPath);

        offenders.ShouldBeEmpty();
    }
}
