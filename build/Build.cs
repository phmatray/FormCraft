using System;
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
    AbsolutePath FluentUIDirectory => RootDirectory / "FormCraft.ForFluentUI";
    AbsolutePath TestsDirectory => RootDirectory / "FormCraft.UnitTests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => RootDirectory / "test-results";

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
        .Produces(TestResultsDirectory / "*.html")
        .Produces(TestResultsDirectory / "*.log")
        .Executes(() =>
        {
            // Both test projects run on Microsoft.Testing.Platform (UseMicrosoftTestingPlatformRunner),
            // which ignores DotNetTest's VSTest surface. This target used to call
            // .SetResultsDirectory()/.SetLoggers(); `dotnet test` forwards those as the MSBuild
            // properties VSTestResultsDirectory/VSTestLogger, and MTP drops both with warning
            // MTP0001. So the target produced nothing at all while its .Produces(...) lines claimed
            // otherwise — and `*.xml` named a file neither runner has ever written (#231).
            //
            // The options below are MTP's own and are honoured. Everything after `--` is forwarded
            // verbatim to each test application, which is where xunit.v3's reporters live; they ship
            // with the runner, so none of this needs an extra package reference.
            //
            // --results-directory carries the most weight of the three: besides the reports, it
            // relocates MTP's per-assembly diagnostic log — the artifact #225 added, carrying the
            // failing test names and Shouldly detail that never reach stdout — out of
            // <project>/bin/<cfg>/<tfm>/TestResults/ and into test-results/. That is why all three
            // workflows can now upload one directory and get both.
            //
            // Report file names are left at their defaults (<user>_<machine>_<timestamp>): the
            // solution has two test assemblies writing into one directory, and a fixed
            // --report-xunit-trx-filename would have the second silently overwrite the first.
            //
            // Which is also why the directory is emptied first. Timestamped names never collide, so
            // nothing overwrites a previous run's reports — they simply accumulate, and a red run
            // followed by a green one would leave a trx recording failures that no longer exist,
            // published under an artifact the next reader takes for the current result. The tradeoff
            // is deliberate: re-running a red suite to reproduce discards the first run's reports, so
            // copy them out first if a flaky failure is what you are chasing. "This directory is the
            // last run" is the less surprising of the two contracts, and `Clean` already reads that
            // way. CI checks out fresh, so only a local `./build.sh Test` loop ever notices.
            TestResultsDirectory.CreateOrCleanDirectory();

            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetProcessAdditionalArguments(
                    "--",
                    "--results-directory", TestResultsDirectory,
                    "--report-xunit-trx",
                    "--report-xunit-html"));

            // Enforce the contract rather than merely declaring it — the whole point of #231. The
            // defect it was filed about produced no error of any kind: `dotnet test` succeeded, the
            // build went green, `.Produces(...)` named files nobody checked for, and the reporting
            // stayed dead for months. The same silence is available to any future SDK or MTP release
            // that stops honouring these options, exactly as `dotnet test` stopped honouring
            // VSTestResultsDirectory. Then the workflows would upload an empty directory under
            // `if-no-files-found: ignore` and nothing anywhere would say so. Cheapest possible guard,
            // and it turns that whole class of regression into a failed build on the next run.
            var reports = TestResultsDirectory.GlobFiles("*.trx");
            Assert.NotEmpty(
                reports,
                $"The test run produced no *.trx under {TestResultsDirectory}. The reporters are "
                + "wired but emitted nothing — check whether the runner still honours "
                + "--results-directory / --report-xunit-trx (see #231).");
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Produces(ArtifactsDirectory / "*.snupkg")
        .Executes(() =>
        {
            // CHANGELOG.md is owned by release-please: it is rewritten in the release PR and is
            // committed by the time we pack. Nothing in this build generates it any more (git-cliff
            // used to, which meant a local Pack rewrote the file out from under the open release PR).
            //
            // Nothing copies it either, since #222. FormCraft.csproj packs `../CHANGELOG.md` by link,
            // so the packaged changelog is the root file itself and cannot lag behind it. The two
            // mirrors this target used to write were git-TRACKED, which made `Pack` mutate tracked
            // files — the shape of the portfolio rule about builds rewriting things git is watching —
            // and one of them (FormCraft.ForMudBlazor's) was packed by nothing at all.

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

            // Pack FormCraft.ForFluentUI package (#260). Each package is named explicitly rather
            // than globbed, so a new adapter project is NOT picked up until it is added here.
            DotNetPack(s => s
                .SetProject(FluentUIDirectory)
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
            Serilog.Log.Information("📦 Package: FormCraft.ForFluentUI {Version}", CurrentVersion);
            Serilog.Log.Information("🔗 NuGet: https://www.nuget.org/packages/FormCraft/{Version}", CurrentVersion);
            Serilog.Log.Information("🔗 NuGet: https://www.nuget.org/packages/FormCraft.ForMudBlazor/{Version}", CurrentVersion);
            Serilog.Log.Information("🔗 NuGet: https://www.nuget.org/packages/FormCraft.ForFluentUI/{Version}", CurrentVersion);
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