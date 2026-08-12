using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the build's test-reporting contract (#231). Every claim here was inert before that issue:
/// <c>Build.cs</c> asked for a results directory and two loggers through <see cref="DotNetTest" />'s
/// VSTest surface, <c>dotnet test</c> forwarded them as the MSBuild properties
/// <c>VSTestResultsDirectory</c>/<c>VSTestLogger</c>, and Microsoft.Testing.Platform ignored both
/// outright (warning <c>MTP0001</c>). So <c>test-results/</c> was never created, no trx or html
/// report was ever produced, and the <c>Test</c> target's <c>.Produces(...)</c> lines described files
/// that did not exist.
/// </summary>
/// <remarks>
/// Nothing about that failure was visible: it broke no build and reddened no run — the wiring simply
/// *read* correct and produced nothing, which is why it survived several CI reworks (#200, #225,
/// #230). A regression here would be equally quiet, so it is asserted on the build script's text
/// rather than left to be noticed.
/// </remarks>
public class TestReportingTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    /// <summary>
    /// Maps an artifact extension to the runner option that has to be present for the build to
    /// actually emit it. Deliberately lists every reporter xunit.v3's Microsoft.Testing.Platform
    /// runner offers, not only the two in use, so switching format stays a one-line change here
    /// rather than a puzzle: the point of the table is to reject an extension backed by *nothing*.
    /// </summary>
    private static readonly Dictionary<string, string> ReporterForExtension = new(StringComparer.Ordinal)
    {
        ["trx"] = "--report-xunit-trx",
        ["html"] = "--report-xunit-html",
        ["xunit"] = "--report-xunit",
        ["junit"] = "--report-junit",
        ["nunit"] = "--report-nunit",
        ["ctrf"] = "--report-ctrf",
        // Not a reporter: Microsoft.Testing.Platform writes one per-assembly diagnostic log — the
        // artifact #225 exists to preserve — into whatever --results-directory names.
        ["log"] = "--results-directory",
    };

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

    private static string ReadBuildScript() =>
        File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs"));

    /// <summary>
    /// Drops whole-line comments so a "not referenced" assertion fires on wiring rather than on
    /// prose — <c>Build.cs</c> carries long explanatory blocks about this very wiring, and without
    /// this a documentation-only edit would turn the suite red for a reason unrelated to the build.
    /// Only leading-<paramref name="marker" /> lines are stripped: a trailing-comment strip would
    /// also mangle the "https://" in a URL.
    /// </summary>
    private static string WithoutComments(string text, string marker) =>
        string.Join(
            '\n',
            text.Split('\n').Where(line => !line.TrimStart().StartsWith(marker, StringComparison.Ordinal)));

    [Fact]
    public void BuildScript_Should_Not_Set_The_VSTest_Properties_That_Mtp_Ignores()
    {
        // Both of these reach dotnet test as VSTest-only MSBuild properties. Under
        // Microsoft.Testing.Platform they are not merely ineffective, they are announced as
        // ignored (MTP0001) on every single run — warning noise in a repo whose entire build runs
        // under TreatWarningsAsErrors, which is exactly how readers get trained past warnings.
        var build = WithoutComments(ReadBuildScript(), "//");

        build.ShouldNotContain("SetResultsDirectory");
        build.ShouldNotContain("SetLoggers");
    }

    [Fact]
    public void BuildScript_Should_Emit_Reports_Through_The_Testing_Platform()
    {
        // The native equivalents, which the runner does honour. --results-directory carries the
        // most weight of the three: it is what puts the reports *and* MTP's per-assembly diagnostic
        // log under test-results/, which is the single path all three workflows upload.
        var build = WithoutComments(ReadBuildScript(), "//");

        build.ShouldContain("--results-directory");
        build.ShouldContain("--report-xunit-trx");
        build.ShouldContain("--report-xunit-html");
    }

    [Fact]
    public void Test_Target_Should_Only_Promise_Artifacts_The_Runner_Is_Asked_To_Write()
    {
        // The defect this pins is not "the wrong glob", it is a .Produces(...) contract that named
        // files nothing emitted — `*.xml` was never written by anything, in either the VSTest or the
        // MTP world. Cross-checking the promise against the flags means the assertion cannot be
        // satisfied by copying a literal list, and a future format switch that updates one half
        // without the other turns red here.
        var build = WithoutComments(ReadBuildScript(), "//");

        var promised = Regex
            .Matches(build, """\.Produces\(TestResultsDirectory\s*/\s*"\*\.(?<ext>[A-Za-z]+)"\)""")
            .Select(match => match.Groups["ext"].Value)
            .ToList();

        promised.ShouldNotBeEmpty("the Test target promises no test-results artifact at all");

        // Shouldly prints the collection on failure, so this names the offending extension.
        var unbacked = promised
            .Where(ext => !ReporterForExtension.TryGetValue(ext, out var option)
                          || !build.Contains(option, StringComparison.Ordinal))
            .ToList();

        unbacked.ShouldBeEmpty();
    }
}
