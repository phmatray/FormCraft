using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = ["main", "develop"],
    OnPushTags = ["v*"],
    OnPullRequestBranches = ["main", "develop"],
    InvokedTargets = [nameof(Continuous)],
    EnableGitHubToken = true,
    FetchDepth = 0,
    ImportSecrets = ["NUGET_API_KEY"],
    CacheKeyFiles = ["global.json", "**/*.csproj"],
    WritePermissions = [GitHubActionsPermissions.Contents])]
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

    string CurrentVersion => GitRepository?.Tags?.FirstOrDefault()?.Replace("v", "") ?? "1.0.0";

    AbsolutePath SourceDirectory => RootDirectory / "FormCraft";
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
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetAssemblyVersion(CurrentVersion)
                .SetFileVersion(CurrentVersion)
                .SetInformationalVersion(CurrentVersion));
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
            DotNetPack(s => s
                .SetProject(SourceDirectory)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetOutputDirectory(ArtifactsDirectory)
                .SetVersion(CurrentVersion)
                .EnableIncludeSymbols()
                .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg));
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Requires(() => GitRepository.IsOnMainBranch() || GitRepository.IsOnReleaseBranch())
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
            Serilog.Log.Information("🔗 NuGet: https://www.nuget.org/packages/FormCraft/{Version}", CurrentVersion);
        });

    Target Continuous => _ => _
        .DependsOn(Test, Pack)
        .Triggers(PublishIfNeeded);

    Target PublishIfNeeded => _ => _
        .OnlyWhenStatic(() => GitRepository.IsOnMainBranch() && IsOnVersionTag())
        .OnlyWhenStatic(() => IsServerBuild)
        .DependsOn(Publish);

    // Helper methods
    bool IsOnVersionTag()
    {
        var process = ProcessTasks.StartProcess("git", "describe --exact-match --tags HEAD", RootDirectory, logOutput: false);
        process.WaitForExit();
        
        if (process.ExitCode == 0 && process.Output.Any())
        {
            var tag = process.Output.First().Text;
            return Regex.IsMatch(tag, @"^v\d+\.\d+\.\d+");
        }
        
        return false;
    }
}