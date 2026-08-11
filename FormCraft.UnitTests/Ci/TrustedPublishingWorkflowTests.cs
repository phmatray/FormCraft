namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the NuGet Trusted Publishing wiring (#173). The publish path runs only on a version
/// tag — a handful of times a year — so a regression here is invisible until a release breaks.
/// These tests fail if the OIDC exchange is removed from the workflow, if any workflow starts
/// referencing the long-lived key again, or if build/Build.cs loses the gate that keeps branch
/// and pull-request runs from publishing.
/// </summary>
public class TrustedPublishingWorkflowTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

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

    private static string ReadWorkflow() =>
        File.ReadAllText(Path.Combine(WorkflowsDirectory, "continuous.yml"));

    private static string ReadBuildScript() =>
        File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs"));

    /// <summary>
    /// Drops whole-line comments so a "not referenced" assertion fires on wiring rather than on
    /// prose. Both files carry long explanatory comment blocks about this very key, so without
    /// this a documentation-only edit would turn the suite red — and the natural repair under
    /// time pressure is to delete the assertion. Only leading-<paramref name="marker"/> lines are
    /// stripped: a trailing-comment strip would also mangle the "https://" in a URL.
    /// </summary>
    private static string WithoutComments(string text, string marker) =>
        string.Join(
            '\n',
            text.Split('\n').Where(line => !line.TrimStart().StartsWith(marker, StringComparison.Ordinal)));

    /// <summary>
    /// The `nuget-login` step alone, so an assertion about its `if:` cannot be satisfied by the
    /// same expression sitting on some other step. Runs from the `id:` line to the start of the
    /// next list item, which covers the step's `if:`, `uses:` and `with:`.
    /// </summary>
    private static string NuGetLoginStep()
    {
        var lines = ReadWorkflow().Split('\n');
        var start = Array.FindIndex(lines, l => l.Contains("id: nuget-login", StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(0, "continuous.yml no longer has a step with `id: nuget-login`");

        var end = Array.FindIndex(lines, start + 1, l => l.TrimStart().StartsWith("- ", StringComparison.Ordinal));
        if (end < 0)
        {
            end = lines.Length;
        }

        return string.Join('\n', lines[start..end]);
    }

    [Fact]
    public void Continuous_Workflow_Should_Request_The_OidcToken()
    {
        WithoutComments(ReadWorkflow(), "#").ShouldContain("id-token: write");
    }

    [Fact]
    public void Continuous_Workflow_Should_Exchange_The_OidcToken_For_A_ShortLived_Key()
    {
        var step = NuGetLoginStep();

        // Pinned by digest, never by a moving tag — asserted as a shape rather than as one literal
        // SHA, so a legitimate Renovate digest bump does not turn this suite red for a reason that
        // has nothing to do with the wiring. `NuGet/login@v1` still fails.
        step.ShouldMatch(@"uses:\s*NuGet/login@[0-9a-f]{40}\b");
        step.ShouldContain("secrets.NUGET_USER");

        // The minted key has to actually reach the build; this lives on the later `Run` step.
        WithoutComments(ReadWorkflow(), "#").ShouldContain("steps.nuget-login.outputs.NUGET_API_KEY");
    }

    [Fact]
    public void Continuous_Workflow_Should_Gate_The_Login_On_A_Version_Tag()
    {
        // Gating on the TRIGGER, not on NUGET_USER: a fork's PR cannot obtain this repo's OIDC
        // token, and a secret-based guard would turn a missing policy into a green build with an
        // unpublished version. Do not "simplify" this to `if: env.NUGET_USER != ''`.
        // Scoped to the login step: the same expression on an unrelated step would leave a real
        // nuget.org key minted on every push and pull request.
        NuGetLoginStep().ShouldMatch(@"if:\s*startsWith\(github\.ref, 'refs/tags/v'\)");
    }

    [Fact]
    public void No_Workflow_Should_Reference_The_LongLived_NuGetApiKey_Secret()
    {
        // Every workflow, not just continuous.yml: release.yml fires on the same `v*` tag, so a
        // publish step added there with ${{ secrets.NUGET_API_KEY }} would re-arm exactly the
        // credential #198 exists to retire, with continuous.yml still perfectly clean.
        var workflows = Directory
            .EnumerateFiles(WorkflowsDirectory, "*.*")
            .Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        workflows.ShouldNotBeEmpty("no workflow files found — the scan would pass vacuously");

        var offenders = workflows
            .Where(f => WithoutComments(File.ReadAllText(f), "#")
                .Contains("secrets.NUGET_API_KEY", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        // Shouldly prints the collection on failure, so this names the workflow that re-armed it.
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void BuildScript_Should_Not_Import_The_LongLived_NuGetApiKey_Secret()
    {
        // The key arrives as a step output now, not as an imported secret.
        WithoutComments(ReadBuildScript(), "//").ShouldNotContain("ImportSecrets");
    }

    [Fact]
    public void BuildScript_Should_Keep_The_Continuous_Workflow_HandMaintained()
    {
        // The [GitHubActions] attribute cannot express the OIDC exchange, so continuous.yml is
        // written by hand. Flipping this to true regenerates the file and silently deletes the
        // NuGet/login block — the wipe lands on the next ./build.cmd, long after the edit, so
        // assert the flag itself rather than trusting the tests to run after a regeneration.
        WithoutComments(ReadBuildScript(), "//").ShouldContain("AutoGenerate = false");
    }

    [Fact]
    public void BuildScript_Should_Gate_Publishing_On_A_Version_Tag()
    {
        // The workflow leaves NUGET_API_KEY empty on branch and PR runs, but a skipped step yields
        // an empty string rather than an unset variable — so this static gate, not the emptiness of
        // the key, is what keeps a push to dev from reaching DotNetNuGetPush.
        var build = WithoutComments(ReadBuildScript(), "//");
        build.ShouldMatch(@"OnlyWhenStatic\(\(\) => IsOnVersionTag\(\)\)");
        build.ShouldMatch(@"OnlyWhenStatic\(\(\) => IsServerBuild\)");
    }
}
