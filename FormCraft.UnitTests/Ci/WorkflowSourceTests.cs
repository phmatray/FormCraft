namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Covers <see cref="WorkflowSource" />, the reader the <c>Ci/</c> guard suites share (#255).
/// </summary>
/// <remarks>
/// <para>
/// Before this type, <c>TrustedPublishingWorkflowTests</c> and <c>TestReportingTests</c> each carried
/// their own byte-identical copy of repo-root location, workflow enumeration, comment stripping and
/// the step scan — comments included. Nothing tested those copies directly; they were only ever
/// exercised through the assertions built on top of them, so a change to one had no way of
/// announcing that the other had drifted. #205 records what that costs: a copied test comment
/// propagated a factual error and hid a whole render path from coverage.
/// </para>
/// <para>
/// The step scan and the job split are deliberate approximations of YAML — the project has no parser
/// dependency and the assertions above them work on raw text on purpose (see
/// <see cref="WorkflowSource.WithoutComments" />). An approximation owned by nobody is the actual
/// hazard, so this file is where its edges are pinned.
/// </para>
/// </remarks>
public class WorkflowSourceTests
{
    [Fact]
    public void All_Should_Contain_Every_Workflow_File_On_Disk()
    {
        // The enumeration is the foundation every "no workflow offends" assertion rests on, and each
        // of those is satisfied trivially by a set that is missing the offender. A workflow added as
        // .yaml, or one this reader silently failed to pick up, would leave the whole family green.
        var onDisk = Directory
            .EnumerateFiles(WorkflowSource.WorkflowsDirectory, "*.*")
            .Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetFileName(f))
            .Order(StringComparer.Ordinal)
            .ToList();

        onDisk.ShouldNotBeEmpty("no workflow files found — the comparison would pass vacuously");
        WorkflowSource.All.Keys.Order(StringComparer.Ordinal).ShouldBe(onDisk);
    }

    [Fact]
    public void WithoutComments_Should_Drop_Whole_Line_Comments_But_Keep_A_Url()
    {
        // The two halves are one decision. Whole-line stripping is what stops a documentation-only
        // edit from reddening a "not referenced" assertion — these workflows carry comment blocks
        // longer than the wiring they describe. Stopping there, rather than also stripping trailing
        // comments, is what keeps the `//` in a URL intact.
        const string Text = """
                            # a leading comment
                              # an indented comment
                            uses: actions/checkout@v7 # see https://github.com/actions/checkout
                            """;

        var stripped = WorkflowSource.WithoutComments(Text, "#");

        stripped.ShouldNotContain("a leading comment");
        stripped.ShouldNotContain("an indented comment");
        stripped.ShouldContain("https://github.com/actions/checkout");
    }

    [Fact]
    public void WithoutComments_Should_Take_The_Marker_It_Is_Given()
    {
        // Both markers are in use: `#` over the workflows, `//` over Build.cs. The C# case is the
        // one where the URL clause above earns its keep twice, since there the marker and the thing
        // that must survive are the same two characters.
        const string Text = """
                            // a leading comment
                            var url = "https://nuget.org";
                            """;

        var stripped = WorkflowSource.WithoutComments(Text, "//");

        stripped.ShouldNotContain("a leading comment");
        stripped.ShouldContain("https://nuget.org");
    }

    [Fact]
    public void StepNamed_Should_Return_Only_That_Steps_Own_Lines()
    {
        // The whole reason the scan exists: an assertion about one step's `if:` or `path:` must not
        // be satisfiable by the same text on a neighbouring step. release-please.yml is the fixture
        // because its upload step is directly followed by another `- name:` step.
        var workflow = WorkflowSource.WithoutComments(WorkflowSource.Read("release-please.yml"), "#");

        var step = WorkflowSource.StepNamed(workflow, "Publish: test-results");

        step.ShouldContain("if-no-files-found: ignore");
        step.ShouldNotContain("Attach packages to the GitHub Release");
    }

    [Fact]
    public void StepWithId_Should_Return_Only_That_Steps_Own_Lines()
    {
        // Same boundary, matched on `id:` instead of `- name:` — the variant #226 depends on, where
        // a claim about the login step's gating must not be answerable by a later step's text.
        //
        // Read raw, not comment-stripped: the scope is the caller's to choose, and this path is
        // deliberately given the unstripped file (as #226's assertions always have). So the slice
        // does carry the explanatory block that follows the step — which is why the boundary is
        // asserted against the *next step's header*, the thing the scan actually has to exclude,
        // rather than against prose that legitimately sits inside the slice.
        var step = WorkflowSource.StepWithId(WorkflowSource.Read("release-please.yml"), "login");

        step.ShouldContain("uses: NuGet/login@");
        step.ShouldNotContain("- name: 'Run: Pack");
    }

    [Fact]
    public void JobsOf_Should_Split_A_Multi_Job_Workflow_At_Its_Job_Keys()
    {
        // release-please.yml is the fixture because it is a real two-job workflow already in the
        // repo — the shape that makes a file-scoped assertion a lie. `jobs:` is a top-level key and
        // each job is a two-space-indented key beneath it, which is the whole rule.
        var jobs = WorkflowSource.JobsOf("release-please.yml");

        jobs.Keys.Order(StringComparer.Ordinal).ShouldBe(["nupkg", "release-please"]);

        // The point of the split: the build invocation belongs to `nupkg` alone, so an assertion
        // about the upload can be held against that same job rather than against the whole file.
        jobs["nupkg"].ShouldContain("./build.cmd Pack");

        // And `release-please` reads as test-running only if comments survive the split — this file
        // *discusses* `./build.cmd Pack` in a comment block inside that job. Stripping before
        // splitting is what keeps a commented-out invocation from marking a job as running the build.
        jobs["release-please"].ShouldNotContain("./build.cmd");
    }

    [Fact]
    public void JobsOf_Should_Return_One_Job_For_A_SingleJob_Workflow()
    {
        // The other half of the rule: the split must not invent jobs out of the deeper keys that
        // make up a job's body (`runs-on:`, `steps:`, every `with:` entry), all of which are keys
        // too — just not at a job's indentation.
        WorkflowSource.JobsOf("ci.yml").Keys.ShouldBe(["build-and-test"]);
    }
}
