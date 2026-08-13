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
        //
        // Compared against an UNFILTERED listing on purpose. Re-stating the reader's own `.yml`/
        // `.yaml` predicate here would make the two sides move together — drop `.yaml` from the
        // reader and the copy drops it too, so the assertion stays green while the guard family
        // quietly stops seeing those files. That is the one regression this test exists to catch,
        // so the expectation has to come from somewhere the reader does not decide: the directory.
        var onDisk = Directory
            .EnumerateFiles(WorkflowSource.WorkflowsDirectory)
            .Select(f => Path.GetFileName(f))
            .Order(StringComparer.Ordinal)
            .ToList();

        onDisk.ShouldNotBeEmpty("no workflow files found — the comparison would pass vacuously");

        // Every file in .github/workflows is a workflow; if a non-YAML file is ever added there,
        // this is meant to go red so a human decides whether the reader should skip it.
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
    public void StepWithId_Should_Not_Bind_To_A_Step_Whose_Id_Merely_Starts_With_The_One_Asked_For()
    {
        // The defect #267 exists to remove: the scan matched `id: <stepId>` as an unanchored
        // substring, so a search for `login` also matched `id: login-legacy` — and it takes the
        // *first* hit, so an unrelated earlier step silently redefined the whole slice and every
        // claim about the login step's `if:` and `uses:` was answered by the wrong step. That is the
        // exact failure StepWithId exists to prevent, reintroduced one level down, on the one
        // primitive #226 depends on — where a wrong answer is a green run on a version that never
        // reached nuget.org.
        const string Steps = """
                             steps:
                               - name: legacy login
                                 id: login-legacy
                                 uses: NuGet/login-legacy@v0
                               - name: current login
                                 id: login
                                 uses: NuGet/login@v1
                             """;

        var step = WorkflowSource.StepWithId(Steps, "login");

        step.ShouldContain("uses: NuGet/login@v1");
        step.ShouldNotContain("login-legacy");
    }

    [Fact]
    public void StepWithId_Should_Fail_Loudly_When_Two_Steps_Claim_One_Id()
    {
        // Two steps sharing an id is invalid in GitHub Actions, so the only open question is what the
        // scan does when a workflow gets there anyway. Resolving to the first is how the old scan
        // returned the wrong step without saying so, and anchoring the match buys nothing if an
        // ambiguous one still quietly picks a winner: the caller would be told about *a* step, with
        // no way to know it was not the one it asked about.
        const string Steps = """
                             steps:
                               - name: first login
                                 id: login
                                 uses: NuGet/login@v1
                               - name: second login
                                 id: login
                                 uses: NuGet/login@v2
                             """;

        var error = Should.Throw<ShouldAssertException>(
            () => WorkflowSource.StepWithId(Steps, "login", "the fixture"));

        error.Message.ShouldContain("ambiguous");

        // The scan cannot infer what it was handed, so the scope description is the only thing that
        // tells a reader *which* file or job to go and look at.
        error.Message.ShouldContain("the fixture");
    }

    [Fact]
    public void StepWithId_Should_Match_An_Id_Carrying_Trailing_Space_Or_A_Comment()
    {
        // The tolerance an exact key match has to keep, and the reason it is spelled as a regex
        // rather than as string equality. `WithoutComments` only drops *whole-line* comments (so a
        // URL's "//" survives), which means a trailing `# …` reaches this scan intact — and callers
        // may hand over unstripped text anyway, as TrustedPublishingWorkflowTests does. An anchored
        // match that forgot either case would report a step that is plainly there as absent.
        const string Steps = """
                             steps:
                               - name: current login
                                 id: login   # minted per run
                                 uses: NuGet/login@v1
                             """;

        WorkflowSource.StepWithId(Steps, "login").ShouldContain("uses: NuGet/login@v1");

        // `-  id:` and `-<tab>id:` are the same YAML as `- id:`; requiring exactly one space reported
        // a step that is plainly present as absent.
        WorkflowSource.TryStepWithId("steps:\n  -  id: login\n     uses: NuGet/login@v1", "login")
            .ShouldNotBeNull()
            .ShouldContain("uses: NuGet/login@v1");
    }

    [Fact]
    public void StepWithId_Should_Return_The_Step_From_Its_Own_Header()
    {
        // The second half of #267: the scan started at the `id:` line, so the "step" it returned
        // excluded the step's own `- name:` — it could not be used to assert anything *about* the
        // name, and it silently differed in shape from what StepNamed returns for the very same step.
        // Two primitives on one reader answering the same question differently is how the looser of
        // them drifts unnoticed.
        // Only the header claim lives here. The upper bound on the same step is already owned by
        // StepWithId_Should_Return_Only_That_Steps_Own_Lines above; re-asserting it would leave two
        // near-identical bodies to be kept in step when release-please.yml's login step moves.
        WorkflowSource.StepWithId(WorkflowSource.Read("release-please.yml"), "login")
            .ShouldContain("- name: NuGet login");
    }

    [Fact]
    public void StepWithId_Should_Not_Stop_The_Walk_Back_On_A_Nested_List_Item()
    {
        // "The nearest dash above" is not the rule — containment is. A sequence nested inside the
        // step (here under `with:`, but a bullet in a `path: |` block behaves identically) sits
        // between the `id:` and its real header, and stopping on it returns a fragment of the step as
        // the step: every later claim about this step's `uses:` would then be answered by nothing.
        const string Steps = """
                             steps:
                               - name: current login
                                 with:
                                   args:
                                     - --verbose
                                 id: login
                                 uses: NuGet/login@v1
                             """;

        var step = WorkflowSource.StepWithId(Steps, "login");

        step.ShouldContain("- name: current login");
        step.ShouldContain("uses: NuGet/login@v1");
    }

    [Fact]
    public void StepWithId_Should_Recognise_A_Bare_Dash_Step_Header()
    {
        // `-` alone, with the item's keys starting on the next line, is legal YAML and the failure it
        // caused was the silent kind: `StartsWith("- ")` skipped this header and the walk-back carried
        // on into the PREVIOUS step, so a claim about `login` was answered by the checkout step.
        const string Steps = """
                             steps:
                               - name: previous step
                                 uses: actions/checkout@v4
                               -
                                 name: current login
                                 id: login
                                 uses: NuGet/login@v1
                             """;

        var step = WorkflowSource.StepWithId(Steps, "login");

        step.ShouldContain("name: current login");
        step.ShouldNotContain("previous step");
    }

    [Fact]
    public void StepWithId_Should_Not_Bind_To_An_Id_Whose_Hash_Is_Part_Of_The_Value()
    {
        // The trailing-comment tolerance has to require whitespace before the `#`. YAML only starts a
        // comment after whitespace, so `login#1` is one scalar naming a different step — and a
        // tolerance spelled `\s*#` swallowed the `#1`, letting a LONGER id back in through the very
        // clause added to keep longer ids out.
        const string Steps = """
                             steps:
                               - name: hash login
                                 id: login#1
                                 uses: NuGet/login@v1
                             """;

        WorkflowSource.TryStepWithId(Steps, "login").ShouldBeNull();
        WorkflowSource.TryStepWithId(Steps, "login#1").ShouldNotBeNull();
    }

    [Fact]
    public void StepWithId_Should_Treat_An_Id_Outside_Any_Step_As_Absent()
    {
        // Walking back has to be allowed to fail. An `id:` key that belongs to no list item is not a
        // step, and slicing from line 0 would hand the caller the file's preamble dressed up as one —
        // a wrong answer of exactly the kind the anchored match was added to stop, arriving by the
        // other door.
        //
        // The fixture carries a real step in an EARLIER job on purpose. Without one it proves nothing:
        // a scope containing no list item at all reaches the top of the file whatever the walk-back
        // does, so the test passed while the loose "nearest dash above" version happily crossed the
        // job boundary and returned `- name: real step` as the login step. What makes this absent is
        // that `env:` is indented shallower than the id — a container, reached without a header.
        const string Malformed = """
                                 jobs:
                                   build:
                                     steps:
                                       - name: real step
                                         uses: actions/checkout@v4
                                   publish:
                                     env:
                                       id: login
                                 """;

        var error = Should.Throw<ShouldAssertException>(
            () => WorkflowSource.StepWithId(Malformed, "login", "the fixture"));

        error.Message.ShouldContain("no longer has a step with `id: login`");
    }

    [Fact]
    public void StepWithId_Should_Find_An_Id_Written_On_The_List_Item_Line()
    {
        // `- id: login` is legal, common in the wild, and the one shape an anchored `^id:` match
        // loses that the old substring scan handled. Losing it fails in the opposite direction to
        // #267's bug — loudly, reporting a step that is plainly present as gone — which is a better
        // failure but still a wrong answer, and one no workflow in this repo would currently catch.
        const string Steps = """
                             steps:
                               - id: login
                                 uses: NuGet/login@v1
                             """;

        var step = WorkflowSource.StepWithId(Steps, "login");

        // The matched line *is* the header here, so the walk-back must stop on it rather than run
        // past it to `steps:`.
        step.ShouldContain("- id: login");
        step.ShouldContain("uses: NuGet/login@v1");
        step.ShouldNotContain("steps:");
    }

    [Fact]
    public void TryStepNamed_Should_Return_Null_Where_StepNamed_Asserts()
    {
        // Why absence needs a non-asserting form at all: StepNamed asserts from inside the shared
        // helper, so one missing step turns every test that reaches for it red — each of them
        // reporting the same single root cause as an apparent *helper* failure. TestReportingTests
        // had four tests doing that, next to one that reported the offender cleanly.
        const string Steps = """
                             steps:
                               - name: 'Publish: test-results'
                                 uses: actions/upload-artifact@v4
                             """;

        WorkflowSource.TryStepNamed(Steps, "nope").ShouldBeNull();

        // A present step still comes back whole, from its own header.
        WorkflowSource.TryStepNamed(Steps, "Publish: test-results")
            .ShouldNotBeNull()
            .ShouldContain("- name: 'Publish: test-results'");

        // And the asserting form is untouched — it is still the right choice for a caller with
        // nothing better to say about absence, because it names the scope the scan cannot infer.
        var error = Should.Throw<ShouldAssertException>(
            () => WorkflowSource.StepNamed(Steps, "nope", "the fixture"));

        error.Message.ShouldContain("the fixture");
        error.Message.ShouldContain("no longer has a step named 'nope'");
    }

    [Fact]
    public void TryStepWithId_Should_Return_Null_Where_StepWithId_Asserts()
    {
        const string Steps = """
                             steps:
                               - name: current login
                                 id: login
                                 uses: NuGet/login@v1
                             """;

        WorkflowSource.TryStepWithId(Steps, "nope").ShouldBeNull();

        WorkflowSource.TryStepWithId(Steps, "login")
            .ShouldNotBeNull()
            .ShouldContain("- name: current login");

        var error = Should.Throw<ShouldAssertException>(
            () => WorkflowSource.StepWithId(Steps, "nope", "the fixture"));

        error.Message.ShouldContain("the fixture");
        error.Message.ShouldContain("no longer has a step with `id: nope`");
    }

    [Fact]
    public void TryStepWithId_Should_Still_Fail_Loudly_On_An_Ambiguous_Id()
    {
        // The one thing the Try form must NOT soften. Absence and ambiguity are different answers:
        // returning null for two steps claiming one id would report a duplicated step as a missing
        // one, quietly undoing Task 1's whole point one call further out.
        const string Steps = """
                             steps:
                               - name: first login
                                 id: login
                                 uses: NuGet/login@v1
                               - name: second login
                                 id: login
                                 uses: NuGet/login@v2
                             """;

        Should.Throw<ShouldAssertException>(() => WorkflowSource.TryStepWithId(Steps, "login"))
            .Message.ShouldContain("ambiguous");
    }

    [Fact]
    public void Matching_Should_Find_The_Workflows_That_Invoke_The_Build()
    {
        // The discovery primitive the whole TestReportingTests family rests on: it decides which
        // workflows are held to the artifact contract, so a regression here does not redden
        // anything — it just quietly shrinks the set being checked.
        var matched = WorkflowSource.Matching(@"\./build\.(cmd|sh|ps1)\s+(Test|Pack|Continuous)\b");

        matched.ShouldBe(["ci.yml", "continuous.yml", "release-please.yml"]);
    }

    [Fact]
    public void Matching_Should_Not_Be_Satisfied_By_A_Commented_Out_Invocation()
    {
        // The other half of the same decision, and the reason Matching strips before it matches.
        // release-please.yml discusses `./build.cmd Continuous` in prose; ci.yml's comment block
        // names `**/TestResults/`, a glob its wiring deliberately no longer uses. Neither may count.
        WorkflowSource.Matching(@"\*\*/TestResults/").ShouldBeEmpty();
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
