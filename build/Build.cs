using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultsDirectory => RootDirectory / "test-results";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration));

            // Enumerated from the solution rather than hand-listed (#275). The list this replaced
            // named FormCraft and FormCraft.UnitTests — two of the solution's eight projects — so
            // the other six kept their build output through every Clean: FormCraft.ForMudBlazor
            // (a packaged, published library), FormCraft.ForMudBlazor.UnitTests,
            // FormCraft.ForFluentUI and FormCraft.ForFluentUI.UnitTests (added later by #261, so
            // never swept at all), FormCraft.DemoBlazorApp, and _build. Driving the sweep from the
            // solution is what gets a project cleaned on the day it lands rather than on the day
            // someone notices it never was.
            //
            // Deliberately NOT RootDirectory.GlobDirectories: that is shorter and would also delete
            // build output under .claude/worktrees/, where this repo keeps other agents' full
            // checkouts. Staying inside solution projects is what bounds the blast radius.
            //
            // ⚠️ _build is in FormCraft.sln, so this deletes build/bin and build/obj — the output
            // the running Nuke process itself was launched from (build.sh/ps1 do
            // `dotnet run --project build/_build.csproj --no-build`). On macOS/Linux the unlink
            // succeeds and `Clean` exits 0 (measured). On Windows a loaded image is locked by the
            // OS, so this may throw instead; CI is ubuntu-only and would not catch it. Sweeping
            // every project is #275's explicit decision — its spec rules the _build sweep "correct
            // and harmless" — so it is kept rather than quietly narrowed, and the Windows exposure
            // is tracked as a follow-up on the PR instead.
            //
            // Materialised before deleting: SelectMany is lazy, so without ToList each project's
            // glob would run after earlier projects had already been deleted. Harmless on today's
            // flat layout, wrong the moment one project directory nests inside another.
            Solution.AllProjects
                .Select(project => project.Directory)
                .SelectMany(directory => directory.GlobDirectories("**/bin", "**/obj"))
                .ToList()
                .ForEach(directory => directory.DeleteDirectory());

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

    // Verifies the tree matches .editorconfig. Fails on any diff; changes nothing (#301).
    //
    // ⚠️ Deliberately NOT a DependsOn of Test or Pack, and deliberately not EnforceCodeStyleInBuild.
    // Those IDE* analyzers are opt-in at build time, and switching them on next to this repo's
    // TreatWarningsAsErrors=true would make one missing brace break `dotnet build` mid-edit and add
    // an analyzer pass to every incremental build. The rules are enforced where regression actually
    // has to be caught — CI — and `./build.sh Test` stays a correctness-only, fast path.
    //
    // Before #301 nothing read .editorconfig at all: its severities say `warning`, the build turns
    // warnings into errors, and the two never met because EnforceCodeStyleInBuild was unset and no
    // workflow ran `dotnet format`. 574 violations across 201 files had accumulated behind that gap.
    //
    // Raw DotNet(...) rather than a typed task: Nuke exposes no DotNetFormat wrapper covering
    // --verify-no-changes, and the raw call is exactly the command a developer runs to fix a
    // failure here (drop --verify-no-changes to apply).
    Target Format => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNet($"format {Solution.Path} --verify-no-changes --no-restore");
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Produces(TestResultsDirectory / "**/*.trx")
        .Produces(TestResultsDirectory / "**/*.html")
        .Produces(TestResultsDirectory / "**/*.log")
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
            // One invocation PER TEST PROJECT, each into test-results/<project>/ (#256). A single
            // solution-wide `dotnet test` sent both assemblies' reports to one directory under the
            // runner's default names — <user>_<machine>_<timestamp> — where the two trx files
            // differed only in the microsecond field. The per-assembly .log files in the same
            // artifact were named properly, so half of it said where it came from and half did not,
            // and a reader who downloaded it after a red run had to open both to learn which suite
            // broke. Identity now comes from the path.
            //
            // Report file names are still left at their defaults rather than pinned with
            // --report-xunit-trx-filename. That option sets a FIXED name, so under the old single
            // invocation the second assembly would have silently overwritten the first — losing a
            // whole suite's report, which is strictly worse than an opaque name. One directory per
            // assembly already makes the defaults unique, so the directory is what carries the
            // identity and the filename is left to the runner.
            //
            // Discovered by the property that MAKES a project an MTP test application — the same
            // property that gives the `--` arguments below their meaning — rather than by a name
            // convention. The solution-wide invocation this replaced ran every test project in the
            // solution whatever it was called; matching on "*Tests" would quietly narrow that, so a
            // suite added later as FormCraft.AcceptanceTest or FormCraft.E2E would simply stop being
            // run, green and unremarked. That is the silent-omission shape #231 was filed about
            // (release-please.yml left behind by #225), and the assert below does not catch it: it
            // fires only when *nothing* matches, so one-of-three matching passes it happily.
            // Read from the project FILE, not through Project.GetProperty(). That helper evaluates
            // the csproj with MSBuild inside this process, and on CI (ubuntu, SDK 10.0.400) the
            // evaluation dies before it can answer:
            //
            //   InvalidProjectFileException: The expression
            //   "[MSBuild]::GetTargetFrameworkIdentifier(net10.0)" cannot be evaluated. Could not
            //   load file or assembly 'NuGet.Frameworks, Version=7.9.0.0' — the located assembly's
            //   manifest definition does not match the assembly reference.
            //
            // Nuke's embedded MSBuild and the installed SDK disagree about NuGet.Frameworks. It
            // reproduces on no developer machine tried so far and fails every CI run, which is the
            // #231 lesson in miniature: a local green is not a CI green. A text read needs no
            // evaluation and cannot acquire that dependency.
            var testProjects = Solution.AllProjects
                .Where(project => project.Path.FileExists())
                .Where(project => Regex.IsMatch(
                    project.Path.ReadAllText(),
                    @"<UseMicrosoftTestingPlatformRunner>\s*true\s*</UseMicrosoftTestingPlatformRunner>",
                    RegexOptions.IgnoreCase))
                .OrderBy(project => project.Name, StringComparer.Ordinal)
                .ToList();

            Assert.NotEmpty(
                testProjects,
                "No project in the solution sets UseMicrosoftTestingPlatformRunner. Discovery reads "
                + "that property, so a suite that stopped setting it would be skipped without a word "
                + "— which is the regression this guards.");

            // Which is also why the directory is emptied first. Timestamped names never collide, so
            // nothing overwrites a previous run's reports — they simply accumulate, and a red run
            // followed by a green one would leave a trx recording failures that no longer exist,
            // published under an artifact the next reader takes for the current result. The tradeoff
            // is deliberate: re-running a red suite to reproduce discards the first run's reports, so
            // copy them out first if a flaky failure is what you are chasing. "This directory is the
            // last run" is the less surprising of the two contracts, and `Clean` already reads that
            // way. CI checks out fresh, so only a local `./build.sh Test` loop ever notices.
            //
            // Emptied only AFTER discovery has proved the run can proceed: wiping first would
            // destroy the previous run's reports on a misconfiguration that then produces none of
            // its own, and the local loop is the only place that has anything to lose.
            TestResultsDirectory.CreateOrCleanDirectory();

            // Every project runs before any failure is surfaced. A bare foreach would let the first
            // red suite's exception abort the loop, hiding a second broken suite until the first was
            // fixed — one red run per bug instead of one red run naming both. The solution-wide
            // invocation this replaced ran both assemblies and reported both, so failing fast here
            // would be a real regression in diagnosis rather than a stylistic choice. Recorded and
            // re-thrown below.
            var failed = new List<string>();

            // One home for the path, because the run and the guard below have to agree on it: if
            // they ever computed it differently the guard would inspect a directory nothing wrote
            // to, and fail every run for a reason that has nothing to do with the reporters.
            AbsolutePath ResultsDirectoryFor(Project project) => TestResultsDirectory / project.Name;

            foreach (var project in testProjects)
            {
                try
                {
                    DotNetTest(s => s
                        .SetProjectFile(project)
                        .SetConfiguration(Configuration)
                        .EnableNoRestore()
                        .EnableNoBuild()
                        .SetProcessAdditionalArguments(
                            "--",
                            "--results-directory", ResultsDirectoryFor(project),
                            "--report-xunit-trx",
                            "--report-xunit-html"));
                }
                catch (Exception exception)
                {
                    // Recorded rather than thrown, so the remaining suites still run. The exception
                    // is logged WHOLE: this catch is deliberately broad and will also see failures
                    // that are nothing to do with a red test — an SDK that cannot launch the test
                    // host, an unwritable results directory, a project that turns out not to be a
                    // test application at all — and reducing those to `exception.Message` discards
                    // the only evidence of which kind it was. Hence "did not complete" rather than
                    // "tests failed" in the summary below: this list cannot tell the two apart, and
                    // claiming the narrower one sends the reader to the wrong place.
                    failed.Add(project.Name);
                    Serilog.Log.Error(exception, "{Project} did not complete", project.Name);
                }
            }

            // Enforce the contract rather than merely declaring it — the whole point of #231. The
            // defect it was filed about produced no error of any kind: `dotnet test` succeeded, the
            // build went green, `.Produces(...)` named files nobody checked for, and the reporting
            // stayed dead for months. The same silence is available to any future SDK or MTP release
            // that stops honouring these options, exactly as `dotnet test` stopped honouring
            // VSTestResultsDirectory. Then the workflows would upload an empty directory under
            // `if-no-files-found: ignore` and nothing anywhere would say so. Cheapest possible guard,
            // and it turns that whole class of regression into a failed build on the next run.
            //
            // Asked PER PROJECT rather than over the directory as a whole (#256). "Some suite
            // emitted a trx" is a strictly weaker claim than "each did", and the gap is not
            // hypothetical: one reporting suite is enough to keep a whole-directory glob green
            // while the other emits nothing, which is half an artifact and no warning anywhere —
            // the same silence at half scale. Each project owns a subdirectory now, so each can be
            // asked directly, and the message names the one that came up empty. An artifact reader
            // told only "no trx was produced" is no better off, because the suite that should have
            // written one is precisely what they are trying to identify.
            //
            // Asked only of the projects that actually COMPLETED. A suite that died — a crashed test
            // host, a rejected argument, an SDK that could not launch it — writes no trx either, and
            // blaming that on the reporters would send the reader off to audit MTP wiring for a
            // failure that has nothing to do with it. Its own entry in `failed` already says what
            // happened, accurately. What is left is the case this guard is actually for: a project
            // that ran, passed, and silently produced no report.
            //
            // Note this still runs on red runs, which the old single invocation did not — it threw
            // from DotNetTest the moment any suite went red, skipping the guard on exactly the runs
            // whose artifact someone was about to download. A sibling suite going red no longer
            // stops a genuine reporter regression from being reported.
            var missingReports = testProjects
                .Where(project => !failed.Contains(project.Name))
                .Where(project => !ResultsDirectoryFor(project).GlobFiles("*.trx").Any())
                .Select(project => $"{project.Name} (nothing in {ResultsDirectoryFor(project)})")
                .ToList();

            // Both lists surfaced together, in one throw — the same "one red run naming both" rule
            // the run loop follows, and for the same reason. They are independent: a suite can fail
            // its tests while a different, passing suite quietly stops emitting a report, and
            // whichever threw first would hide the other. Each entry carries its per-project
            // directory, so the failure summary points at the artifact #256 just made attributable
            // rather than making the reader go find it.
            var problems = new List<string>();

            if (missingReports.Count > 0)
            {
                problems.Add(
                    $"produced no *.trx: {string.Join(", ", missingReports)} — the reporters are "
                    + "wired but emitted nothing, so check whether the runner still honours "
                    + "--results-directory / --report-xunit-trx (see #231)");
            }

            if (failed.Count > 0)
            {
                problems.Add(
                    "did not complete: "
                    + string.Join(
                        ", ",
                        failed.Select(name => $"{name} (reports, if any, in {TestResultsDirectory / name})")));
            }

            if (problems.Count > 0)
            {
                Assert.Fail($"Test target failed. Projects that {string.Join("; and that ", problems)}.");
            }
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