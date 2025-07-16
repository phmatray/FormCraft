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
    WritePermissions = [GitHubActionsPermissions.Contents, GitHubActionsPermissions.Packages])]
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

    [Parameter("GitHub personal access token")]
    readonly string GitHubToken;

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

    Target GenerateChangelog => _ => _
        .Description("Generate CHANGELOG.md using git-cliff")
        .OnlyWhenStatic(() => IsLocalBuild) // Only run changelog generation in local builds
        .Executes(() =>
        {
            try
            {
                // Check if git-cliff is installed
                var checkProcess = ProcessTasks.StartProcess("git-cliff", "--version", RootDirectory, logOutput: false);
                checkProcess.WaitForExit();
                
                if (checkProcess.ExitCode != 0)
                {
                    throw new Exception("git-cliff is not installed. Please install it from https://github.com/orhun/git-cliff");
                }
                
                // Generate the full changelog
                var process = ProcessTasks.StartProcess(
                    "git-cliff", 
                    "--config cliff.toml --output CHANGELOG.md", 
                    RootDirectory, 
                    logOutput: true);
                process.WaitForExit();
                
                if (process.ExitCode == 0)
                {
                    Serilog.Log.Information("✅ Changelog generated successfully at {Path}", ChangelogPath);
                    
                    // Copy changelog to FormCraft project directory for packaging
                    var projectChangelogPath = SourceDirectory / "CHANGELOG.md";
                    File.Copy(ChangelogPath, projectChangelogPath, overwrite: true);
                    
                    // Also copy to ForMudBlazor project
                    var mudBlazorChangelogPath = MudBlazorDirectory / "CHANGELOG.md";
                    File.Copy(ChangelogPath, mudBlazorChangelogPath, overwrite: true);
                    
                    Serilog.Log.Information("📄 Changelog copied to project directories");
                }
                else
                {
                    throw new Exception($"git-cliff exited with code {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to generate changelog");
                throw;
            }
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .DependsOn(GenerateChangelog)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Produces(ArtifactsDirectory / "*.snupkg")
        .Executes(() =>
        {
            // If changelog generation was skipped (in CI), check if we have existing changelog files
            if (IsServerBuild)
            {
                // In CI, we'll use the committed CHANGELOG.md files if they exist
                if (ChangelogPath.FileExists())
                {
                    var projectChangelogPath = SourceDirectory / "CHANGELOG.md";
                    if (!projectChangelogPath.FileExists())
                    {
                        File.Copy(ChangelogPath, projectChangelogPath, overwrite: true);
                    }
                    
                    var mudBlazorChangelogPath = MudBlazorDirectory / "CHANGELOG.md";
                    if (!mudBlazorChangelogPath.FileExists())
                    {
                        File.Copy(ChangelogPath, mudBlazorChangelogPath, overwrite: true);
                    }
                }
            }
            
            // Pack FormCraft main package
            DotNetPack(s => s
                .SetProject(SourceDirectory)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetOutputDirectory(ArtifactsDirectory)
                .SetVersion(CurrentVersion)
                .EnableIncludeSymbols()
                .SetSymbolPackageFormat(DotNetSymbolPackageFormat.snupkg));
                
            // Pack FormCraft.ForMudBlazor package
            DotNetPack(s => s
                .SetProject(MudBlazorDirectory)
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

    Target CreateGitHubRelease => _ => _
        .DependsOn(Pack)
        .Requires(() => GitHubToken)
        .Requires(() => IsOnVersionTag())
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(async () =>
        {
            var releaseTag = $"v{CurrentVersion}";
            
            // Generate changelog for this release using git-cliff
            var changelogContent = GenerateChangelogForRelease(releaseTag);
            
            // Get the repository owner and name
            var (owner, name) = GetOwnerAndRepositoryName();
            
            // Initialize GitHub client
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("FormCraft"))
            {
                Credentials = new Octokit.Credentials(GitHubToken)
            };
            
            // Create GitHub release
            var release = await client
                .Repository
                .Release
                .Create(owner, name, new Octokit.NewRelease(releaseTag)
                {
                    Name = $"FormCraft {CurrentVersion}",
                    Body = changelogContent,
                    Draft = false,
                    Prerelease = CurrentVersion.Contains("-")
                });
            
            // Upload NuGet packages as release assets
            var packages = ArtifactsDirectory.GlobFiles("*.nupkg", "*.snupkg");
            foreach (var package in packages)
            {
                await using var stream = File.OpenRead(package);
                var assetUpload = new Octokit.ReleaseAssetUpload
                {
                    FileName = Path.GetFileName(package),
                    ContentType = "application/octet-stream",
                    RawData = stream
                };
                await client
                    .Repository
                    .Release
                    .UploadAsset(release, assetUpload);
            }
            
            Serilog.Log.Information("📦 GitHub Release created: {ReleaseUrl}", release.HtmlUrl);
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
        .DependsOn(Publish)
        .Triggers(CreateGitHubRelease);
    
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
    
    bool IsOnVersionTag()
    {
        var tag = GetCurrentTag();
        return !string.IsNullOrEmpty(tag) && Regex.IsMatch(tag, @"^v\d+\.\d+\.\d+");
    }

    string GenerateChangelogForRelease(string tag)
    {
        try
        {
            // Run git-cliff to generate changelog for this specific release
            var process = ProcessTasks.StartProcess(
                "git-cliff", 
                $"--config cliff.toml --unreleased --tag {tag} --strip all", 
                RootDirectory, 
                logOutput: false);
            process.WaitForExit();
            
            if (process.ExitCode == 0)
            {
                return string.Join(Environment.NewLine, process.Output.Select(x => x.Text));
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning("Failed to generate changelog with git-cliff: {Message}", ex.Message);
        }
        
        // Fallback to reading from CHANGELOG.md if git-cliff fails
        if (ChangelogPath.FileExists())
        {
            var changelogContent = ChangelogPath.ReadAllText();
            var versionSection = ExtractVersionSection(changelogContent, CurrentVersion);
            if (!string.IsNullOrEmpty(versionSection))
                return versionSection;
        }
        
        // Default changelog if all else fails
        return $"## What's Changed in v{CurrentVersion}\n\nSee the [full changelog](https://github.com/phmatray/FormCraft/blob/main/CHANGELOG.md) for details.";
    }
    
    string ExtractVersionSection(string changelog, string version)
    {
        var pattern = $@"##\s*\[?{Regex.Escape(version)}\]?.*?(?=##\s*\[?|\z)";
        var match = Regex.Match(changelog, pattern, RegexOptions.Singleline);
        return match.Success ? match.Value.Trim() : string.Empty;
    }
    
    (string owner, string name) GetOwnerAndRepositoryName()
    {
        var remoteUrl = GitRepository.HttpsUrl ?? GitRepository.SshUrl ?? "";
        var match = Regex.Match(remoteUrl, @"github\.com[:/]([^/]+)/([^/\.]+)");
        
        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value);
        }
        
        // Fallback values
        return ("phmatray", "FormCraft");
    }
}