using System.IO;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the NuGet Trusted Publishing wiring (#173). The publish path runs only on a version
/// tag — a handful of times a year — so a regression here is invisible until a release breaks.
/// These tests fail if the OIDC exchange is removed from the workflow, or if build/Build.cs
/// starts declaring the long-lived key again (which a future AutoGenerate = true would bake
/// straight back into continuous.yml).
/// </summary>
public class TrustedPublishingWorkflowTests
{
    private static string RepoRoot()
    {
        // Nullable: dir.Parent is DirectoryInfo?, so the variable has to be too — inferring
        // DirectoryInfo from the initializer would fail the build under TreatWarningsAsErrors.
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FormCraft.sln")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("could not locate FormCraft.sln above the test output directory");
        return dir!.FullName;
    }

    private static string ReadWorkflow() =>
        File.ReadAllText(Path.Combine(RepoRoot(), ".github", "workflows", "continuous.yml"));

    private static string ReadBuildScript() =>
        File.ReadAllText(Path.Combine(RepoRoot(), "build", "Build.cs"));

    [Fact]
    public void Continuous_Workflow_Should_Request_The_OidcToken()
    {
        ReadWorkflow().ShouldContain("id-token: write");
    }

    [Fact]
    public void Continuous_Workflow_Should_Exchange_The_OidcToken_For_A_ShortLived_Key()
    {
        var workflow = ReadWorkflow();

        // Pinned by digest, never by a moving tag.
        workflow.ShouldContain("NuGet/login@8d196754b4036150537f80ac539e15c2f1028841");
        workflow.ShouldContain("secrets.NUGET_USER");
        workflow.ShouldContain("steps.nuget-login.outputs.NUGET_API_KEY");
    }

    [Fact]
    public void Continuous_Workflow_Should_Gate_The_Login_On_A_Version_Tag()
    {
        // Gating on the TRIGGER, not on NUGET_USER: a fork's PR cannot obtain this repo's OIDC
        // token, and a secret-based guard would turn a missing policy into a green build with an
        // unpublished version. Do not "simplify" this to `if: env.NUGET_USER != ''`.
        ReadWorkflow().ShouldContain("startsWith(github.ref, 'refs/tags/v')");
    }

    [Fact]
    public void No_LongLived_NuGetApiKey_Secret_Should_Be_Referenced_Anywhere()
    {
        ReadWorkflow().ShouldNotContain("secrets.NUGET_API_KEY");
        ReadBuildScript().ShouldNotContain("ImportSecrets");
    }
}
