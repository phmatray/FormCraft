namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the NuGet Trusted Publishing wiring (#173, #198). The publish path runs only when a
/// release-please release PR is merged — a handful of times a year — so a regression here is
/// invisible until a release breaks.
/// </summary>
/// <remarks>
/// Since #197 the OIDC exchange lives in <c>release-please.yml</c>, not <c>continuous.yml</c>:
/// release-please creates the tag with <c>GITHUB_TOKEN</c>, and GitHub does not fire
/// <c>on: push: tags</c> for that token, so publishing has to happen inside release-please's own
/// run. These tests therefore assert two complementary things — that release-please still performs
/// the exchange, and that continuous.yml still performs no publishing at all.
/// </remarks>
public class TrustedPublishingWorkflowTests
{
    /// <summary>
    /// A single step of a workflow, so an assertion about its <c>if:</c> cannot be satisfied by the
    /// same expression sitting on some other step.
    /// </summary>
    /// <remarks>
    /// Deliberately reads the workflow *unstripped*. The scan runs from the step's own <c>- </c> list
    /// item to the next one, so the slice carries whatever explanatory comments sit between the step
    /// and its successor — harmless here, because every claim below is about a line these files spell
    /// as wiring (<c>if:</c>, <c>uses:</c>) rather than as prose.
    /// </remarks>
    private static string StepWithId(string workflowFile, string stepId) =>
        WorkflowSource.StepWithId(WorkflowSource.Read(workflowFile), stepId, workflowFile);

    /// <summary>
    /// The <c>if:</c> expression of a single step, so a claim about what a step is *gated on* cannot
    /// be satisfied — or contradicted — by the same name appearing in its <c>with:</c> block. #226
    /// turns entirely on that distinction: the login step legitimately *reads* <c>NUGET_USER</c>,
    /// and only *gating* on it is the defect.
    /// </summary>
    private static string StepCondition(string workflowFile, string stepId)
    {
        var condition = StepWithId(workflowFile, stepId)
            .Split('\n')
            .FirstOrDefault(l => l.TrimStart().StartsWith("if:", StringComparison.Ordinal));

        condition.ShouldNotBeNull($"the `{stepId}` step of {workflowFile} no longer has an `if:` condition");
        return condition;
    }

    [Fact]
    public void ReleaseWorkflow_Should_Request_The_OidcToken()
    {
        WorkflowSource.Stripped("release-please.yml").ShouldContain("id-token: write");
    }

    [Fact]
    public void ReleaseWorkflow_Should_Exchange_The_OidcToken_For_A_ShortLived_Key()
    {
        var step = StepWithId("release-please.yml", "login");

        // Pinned by digest, never by a moving tag — asserted as a shape rather than as one literal
        // SHA, so a legitimate Renovate digest bump does not turn this suite red for a reason that
        // has nothing to do with the wiring. `NuGet/login@v1` still fails.
        step.ShouldMatch(@"uses:\s*NuGet/login@[0-9a-f]{40}\b");
        step.ShouldContain("NUGET_USER");

        // The minted key has to actually reach the build; this lives on the later `Run` step.
        WorkflowSource.Stripped("release-please.yml").ShouldContain("steps.login.outputs.NUGET_API_KEY");
    }

    [Fact]
    public void ReleaseWorkflow_Should_Gate_The_Login_On_A_Created_Release()
    {
        // Scoped to the login step: the same condition on an unrelated step would leave a real
        // nuget.org key minted on runs that publish nothing.
        StepWithId("release-please.yml", "login").ShouldContain("release_created");
    }

    [Fact]
    public void ReleaseWorkflow_Should_Not_Gate_The_Login_On_The_NuGetUser_Secret()
    {
        // #226. A guard on the *trigger* and a guard on a *secret* fail in opposite directions.
        // `release_created == false` means nothing was meant to publish, so a skip is correct.
        // An empty NUGET_USER means a release *was* meant to publish and could not — and skipping
        // the login there leaves steps.login.outputs.NUGET_API_KEY empty, which gates Nuke's
        // Publish off while release-please has already cut the tag, the changelog and the GitHub
        // Release. The run is then GREEN on a version that never reached nuget.org, on a path this
        // repo exercises a handful of times a year. So the secret must fail the job, not skip it.
        //
        // Asserted on the `if:` alone: `with: user: ${{ env.NUGET_USER }}` is the legitimate read.
        var condition = StepCondition("release-please.yml", "login");

        condition.ShouldContain("release_created");
        condition.ShouldNotContain("NUGET_USER");
    }

    [Fact]
    public void Continuous_Workflow_Should_Not_Publish_Anything()
    {
        // The counterpart invariant to the three above, and the reason the tag trigger was removed
        // in #197: release-please creates the tag with GITHUB_TOKEN, which never fires
        // `on: push: tags`, so a publish path here could not run — but it could still mint a key.
        // Two workflows able to push the same version is the failure this prevents.
        var continuous = WorkflowSource.Stripped("continuous.yml");

        continuous.ShouldNotContain("NuGet/login@");
        continuous.ShouldNotContain("id-token");
        continuous.ShouldNotContain("tags:");
    }

    [Fact]
    public void No_Workflow_Should_Reference_The_LongLived_NuGetApiKey_Secret()
    {
        // Every workflow, not just the publishing one: any workflow that added
        // ${{ secrets.NUGET_API_KEY }} would re-arm exactly the credential #198 exists to retire,
        // with release-please.yml still perfectly clean.
        var workflows = WorkflowSource.All.Keys;

        workflows.ShouldNotBeEmpty("no workflow files found — the scan would pass vacuously");

        var offenders = workflows
            .Where(fileName => WorkflowSource.Stripped(fileName)
                .Contains("secrets.NUGET_API_KEY", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        // Shouldly prints the collection on failure, so this names the workflow that re-armed it.
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void BuildScript_Should_Not_Import_The_LongLived_NuGetApiKey_Secret()
    {
        // The key arrives as a step output now, not as an imported secret.
        WorkflowSource.BuildScript.ShouldNotContain("ImportSecrets");
    }

    [Fact]
    public void BuildScript_Should_Keep_The_Continuous_Workflow_HandMaintained()
    {
        // The [GitHubActions] attribute is the only in-repo declaration of continuous.yml's shape.
        // Flipping this to true regenerates the file — and a stale attribute would reinstate the
        // tag-triggered publish path that #197 deliberately removed. The wipe lands on the next
        // ./build.cmd, long after the edit, so assert the flag itself rather than trusting the
        // tests to run after a regeneration.
        WorkflowSource.BuildScript.ShouldContain("AutoGenerate = false");
    }

    [Fact]
    public void BuildScript_Should_Gate_Publishing_On_A_Version_Tag()
    {
        // release-please.yml leaves NUGET_API_KEY empty on a dry run, but a skipped step yields an
        // empty string rather than an unset variable — so this static gate, not the emptiness of
        // the key, is what keeps a non-release run from reaching DotNetNuGetPush.
        var build = WorkflowSource.BuildScript;
        build.ShouldMatch(@"OnlyWhenStatic\(\(\) => IsOnVersionTag\(\)\)");
        build.ShouldMatch(@"OnlyWhenStatic\(\(\) => IsServerBuild\)");
    }
}
