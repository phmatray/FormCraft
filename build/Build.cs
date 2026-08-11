using System;
using System.IO;
using System.Linq;
using FormCraft.Build;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// Kept in sync with the hand-maintained .github/workflows/continuous.yml. AutoGenerate is false, so
// this generates nothing — but it is the only in-repo declaration of that workflow's shape, and a
// stale one would silently reinstate the tag-triggered publish path if anyone ever regenerated.
// There is deliberately no OnPushTags and no ImportSecrets: nothing publishes from this workflow any
// more, and the NuGet key is minted per-run by release-please.yml via OIDC rather than stored.
[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = false,
    OnPushBranches = ["main", "dev"],
    OnPullRequestBranches = ["main", "dev"],
    InvokedTargets = [nameof(Pack)],
    EnableGitHubToken = true,
    FetchDepth = 0,
    CacheKeyFiles = ["global.json", "**/*.csproj"])]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("NuGet API Key for publishing packages")]
    readonly string NuGetApiKey;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;

    [GitRepository]
    readonly GitRepository GitRepository;

    string CurrentVersion => GetCurrentVersion();

    AbsolutePath SourceDirectory => RootDirectory / "FormCraft";
    AbsolutePath MudBlazorDirectory => RootDirectory / "FormCraft.ForMudBlazor";
    AbsolutePath TestsDirectory => RootDirectory / "FormCraft.UnitTests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => RootDirectory / "test-results";
    AbsolutePath ChangelogPath => RootDirectory / "CHANGELOG.md";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration));

            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
            TestResultsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // Versioning (Version, AssemblyVersion, FileVersion, InformationalVersion,
            // PackageVersion) is handled by MinVer (see Directory.Build.props), which is
            // prerelease-safe: passing a raw tag like "3.0.0-rc.1" as AssemblyVersion or
            // FileVersion would fail compilation (CS7034/CS7035).
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Produces(TestResultsDirectory / "*.trx")
        .Produces(TestResultsDirectory / "*.xml")
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers(
                    "trx",
                    $"html;LogFileName={TestResultsDirectory / "test-results.html"}"));
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Produces(ArtifactsDirectory / "*.snupkg")
        .Executes(() =>
        {
            // CHANGELOG.md is owned by release-please: it is rewritten in the release PR and is
            // committed by the time we pack. Nothing in this build generates it any more (git-cliff
            // used to, which meant a local Pack rewrote the file out from under the open release
            // PR). We only mirror the committed file into the package directories so FormCraft.csproj
            // can pack it — unconditionally, local and CI alike, so a package can never ship a
            // changelog older than the one at the root.
            if (ChangelogPath.FileExists())
            {
                File.Copy(ChangelogPath, SourceDirectory / "CHANGELOG.md", overwrite: true);
                File.Copy(ChangelogPath, MudBlazorDirectory / "CHANGELOG.md", overwrite: true);
            }

            // Package versions are computed by MinVer from git tags (MinVer's targets
            // override any /p:Version passed on the command line, so we don't set one here).
            // Pack FormCraft main package
            DotNetPack(s => s
                .SetProject(SourceDirectory)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableIncludeSymbols()
                .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg));

            // Pack FormCraft.ForMudBlazor package
            DotNetPack(s => s
                .SetProject(MudBlazorDirectory)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableIncludeSymbols()
                .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg));
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Requires(() => IsOnVersionTag() || GitRepository.IsOnMainBranch() || GitRepository.IsOnReleaseBranch())
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Triggers(Announce)
        .Executes(() =>
        {
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg", "*.snupkg");

            DotNetNuGetPush(s => s
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(NuGetApiKey)
                .EnableSkipDuplicate()
                .CombineWith(packages, (ss, package) => ss
                    .SetTargetPath(package)));
        });

    Target Announce => _ => _
        .TriggeredBy(Publish)
        .Executes(() =>
        {
            Serilog.Log.Information("🎉 Version {Version} has been successfully published!", CurrentVersion);
            Serilog.Log.Information("📦 Package: FormCraft {Version}", CurrentVersion);
            Serilog.Log.Information("📦 Package: FormCraft.ForMudBlazor {Version}", CurrentVersion);
            Serilog.Log.Information("🔗 NuGet: https://www.nuget.org/packages/FormCraft/{Version}", CurrentVersion);
            Serilog.Log.Information("🔗 NuGet: https://www.nuget.org/packages/FormCraft.ForMudBlazor/{Version}", CurrentVersion);
        });

    Target Continuous => _ => _
        .DependsOn(Test, Pack)
        .Triggers(PublishIfNeeded);

    Target PublishIfNeeded => _ => _
        .OnlyWhenStatic(() => IsOnVersionTag())
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
            var isVersionTag = IsOnVersionTag();
            var currentTag = GetCurrentTag();
            var branch = GitRepository.Branch;

            Serilog.Log.Information("PublishIfNeeded conditions:");
            Serilog.Log.Information("  - IsServerBuild: {IsServerBuild}", IsServerBuild);
            Serilog.Log.Information("  - Current branch: {Branch}", branch);
            Serilog.Log.Information("  - IsOnVersionTag: {IsVersionTag}", isVersionTag);
            Serilog.Log.Information("  - Current tag: {CurrentTag}", currentTag ?? "none");
            Serilog.Log.Information("  - Current version: {Version}", CurrentVersion);
        })
        .DependsOn(Publish);
        // Note: the GitHub Release is created exclusively by release-please, in
        // .github/workflows/release-please.yml, which then runs this publish in the same job.
        // Do NOT add a release-creating target back into this chain — two producers would race
        // to create a release for the same tag (already_exists errors).

    Target Release => _ => _
        .Description("Creates a new release (NuGet + GitHub)")
        .DependsOn(Pack)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            Serilog.Log.Information("📦 Creating release for version {Version}", CurrentVersion);
            Serilog.Log.Information("This target should be triggered by CI/CD on version tags");
        });

    // Helper methods
    string GetCurrentVersion()
    {
        // Try to get version from current tag
        var currentTag = GetCurrentTag();
        if (!string.IsNullOrEmpty(currentTag))
        {
            return currentTag.TrimStart('v');
        }

        // Try to get version from MinVer or GitVersion
        var minVerVersion = EnvironmentInfo.GetVariable("MINVER_VERSION");
        if (!string.IsNullOrEmpty(minVerVersion))
        {
            return minVerVersion;
        }

        // Fallback to latest tag
        try
        {
            var process = ProcessTasks.StartProcess("git", "describe --tags --abbrev=0", RootDirectory, logOutput: false);
            process.WaitForExit();
            if (process.ExitCode == 0 && process.Output.Any())
            {
                return process.Output.First().Text.TrimStart('v');
            }
        }
        catch { }

        return "1.0.0";
    }

    string GetCurrentTag()
    {
        try
        {
            var process = ProcessTasks.StartProcess("git", "describe --exact-match --tags HEAD", RootDirectory, logOutput: false);
            process.WaitForExit();
            if (process.ExitCode == 0 && process.Output.Any())
            {
                return process.Output.First().Text;
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Whether HEAD carries a release tag — the static gate behind <see cref="PublishIfNeeded"/>.
    /// </summary>
    /// <remarks>
    /// The rule itself lives in <see cref="FormCraft.Build.BuildVersioning.IsVersionTag"/> so it can
    /// be tested without instantiating this class; <c>VersionTagRuleTests</c> pins its accept and
    /// reject sets, including why a prerelease tag must keep publishing (#198's rehearsal) and why a
    /// legal-but-nonsense prerelease label is not excluded (#227).
    /// </remarks>
    bool IsOnVersionTag() => BuildVersioning.IsVersionTag(GetCurrentTag());
}