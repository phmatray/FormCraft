using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards <c>CLAUDE.md</c> against VSTest-era test invocations that do not work (#299, #371). The
/// test projects run on Microsoft.Testing.Platform (<c>OutputType=Exe</c> +
/// <c>UseMicrosoftTestingPlatformRunner=true</c>), and <c>dotnet test</c> runs them in the SDK's
/// native MTP mode (<c>global.json</c>, #371).
/// </summary>
/// <remarks>
/// <para>
/// <b>What is still broken, re-measured under native mode (#371).</b> The MSBuild-property form
/// (<c>-p:VSTestTestCaseFilter=…</c>) is <b>silent and green</b>: the whole project runs
/// (<c>total: 914</c>), exit <c>0</c>, and — unlike under the old VSTest bridge, which at least warned
/// <c>MTP0001</c> — nothing is printed about it. That is why it needs a guard rather than a reader.
/// <c>--collect "XPlat Code Coverage"</c> now fails loudly (<c>Zero tests ran</c>, exit <c>5</c>);
/// still worth keeping out of the always-loaded briefing, since there is no coverage extension for it
/// to reach.
/// </para>
/// <para>
/// <b>What is no longer guarded.</b> A bare <c>dotnet test --filter "…"</c> was inert under the
/// bridge (#299) and now really filters (measured: 6 tests for <c>FullyQualifiedName~Gitignore</c>),
/// so it has left the pattern. <see cref="TestReportingTests.BuildScript_Should_Not_Use_DotNetTests_VSTest_Logger_Or_Results_Settings" />
/// pins the same class of mistake in <c>build/Build.cs</c>; this class covers the documentation.
/// </para>
/// <para>
/// <b>Scanned at command position, not by substring.</b> The corrected prose has to be able to
/// <i>name</i> the broken flags in order to warn about them ("⛔ <c>dotnet test --filter</c> is
/// inert"), so a substring check would forbid the very sentence that prevents the regression. Only a
/// line whose <i>command</i> is a test run counts: the start of a line or the start of a
/// shell-separated clause. Inline-code prose and <c>#</c> comments therefore never match, which is
/// deliberate — the sanctioned way to warn about a broken flag is prose, not a commented-out example.
/// </para>
/// <para>
/// The scan covers the direct-host invocation as well as <c>dotnet test</c>. <c>CLAUDE.md</c> now
/// leads with the host binary for fast iteration, so that is the most likely place a future inert
/// flag lands — anchoring only on <c>dotnet test</c> would leave the recommended path unguarded.
/// Continuations are folded first: a wrapped command puts its flags on a line that does not itself
/// begin a command, which a naive per-line scan reads as prose.
/// </para>
/// <para>
/// <b><see cref="The_Scan_Should_Actually_Find_The_Documented_Commands" /> is the load-bearing
/// test.</b> Every other assertion here is of the form "no offender was found", which is also what an
/// empty scan reports. Reformat the code fence — a <c>$ </c> prompt, a different indent, a renamed
/// binary — and a scanner without that assertion passes forever while the broken commands sit
/// unread three lines below.
/// </para>
/// </remarks>
public class ClaudeMdTestCommandsTests
{
    /// <summary>The always-loaded briefing this guard protects, relative to the repo root.</summary>
    private const string ClaudeMd = "CLAUDE.md";

    /// <summary>
    /// A test run at command position: line start or after a shell separator, optionally behind a
    /// <c>$ </c> prompt. Matches <c>dotnet test</c> (any spacing) and the built MTP hosts, whose
    /// file name is the project name, with or without the Windows <c>.exe</c>.
    /// </summary>
    private static readonly Regex TestInvocation = new(
        @"(?:^|[;&|]\s*)\$?\s*(?:dotnet\s+test\b|\S*FormCraft\.\w*\.?UnitTests(?:\.exe)?(?=\s|$))",
        RegexOptions.Compiled);

    /// <summary>
    /// A VSTest option that does not work under native MTP mode: <c>--collect</c> or <c>--logger</c>
    /// (both fail, exit 5) or any <c>-p:VSTest…</c> MSBuild property (silently ignored). Bare
    /// <c>--filter</c> is absent on purpose — it filters now (#371).
    /// </summary>
    private static readonly Regex BrokenVsTestOption = new(
        @"--(?:collect|logger)\b|[-/]p:VSTest\w+",
        RegexOptions.Compiled);

    [Theory]
    [InlineData("dotnet test -p:VSTestTestCaseFilter=FullyQualifiedName~X", true)]
    [InlineData("dotnet test --collect \"XPlat Code Coverage\"", true)]
    [InlineData("dotnet test --logger trx", true)]
    [InlineData("dotnet test FormCraft.UnitTests/FormCraft.UnitTests.csproj -- --filter-class Foo", false)]
    [InlineData("dotnet test --filter \"FullyQualifiedName~X\"", false)]
    public void BrokenVsTestOption_Should_Flag_Only_The_Options_Measured_Broken(string command, bool broken)
    {
        // Pins the pattern itself: the guard below only asserts "no offender in CLAUDE.md", which
        // stays green if the pattern is weakened while the file happens to be clean.
        BrokenVsTestOption.IsMatch(command).ShouldBe(broken);
    }

    private static string ClaudeMdPath => Path.Combine(WorkflowSource.RepoRoot, ClaudeMd);

    /// <summary>
    /// Every shell command in <c>CLAUDE.md</c> that runs tests, with backslash continuations folded
    /// into the line that starts them.
    /// </summary>
    private static IReadOnlyList<string> TestCommands()
    {
        string[] lines = File.ReadAllLines(ClaudeMdPath);
        List<string> commands = [];

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (!TestInvocation.IsMatch(line))
            {
                continue;
            }

            // Fold `\`-continued lines so a wrapped command is matched as the single command it is.
            while (line.EndsWith('\\') && i + 1 < lines.Length)
            {
                line = string.Concat(line.AsSpan(0, line.Length - 1).TrimEnd(), " ", lines[++i].Trim());
            }

            commands.Add(line);
        }

        return commands;
    }

    [Fact]
    public void The_Scan_Should_Actually_Find_The_Documented_Commands()
    {
        // Without this, every other assertion below is vacuously true the moment the scan stops
        // matching — which a formatting change alone is enough to cause.
        TestCommands().ShouldNotBeEmpty(
            $"no test command was found in {ClaudeMd}, so the guards in this class assert nothing. "
            + "Either the Running Tests examples were removed, or their formatting changed in a way "
            + $"{nameof(TestInvocation)} no longer recognises — fix the pattern, not this assertion.");
    }

    [Fact]
    public void ClaudeMd_Should_Not_Teach_VSTest_Options_That_Do_Not_Work()
    {
        string[] offenders = TestCommands()
            .Where(command => BrokenVsTestOption.IsMatch(command))
            .ToArray();

        offenders.ShouldBeEmpty(
            $"CLAUDE.md documents {offenders.Length} test command(s) using VSTest options that do not "
            + "work under native MTP mode — a -p:VSTest… property is silently ignored (the whole suite "
            + "runs, exit 0) and --collect fails with exit 5. Use `dotnet test <csproj> -c Release "
            + "-- --filter-class <FQN>` instead; see .claude/skills/repo-profile.md (Build & test). "
            + $"Offending command(s): {string.Join(" | ", offenders)}");
    }

    [Fact]
    public void Every_Documented_Filter_Class_Should_Resolve_To_A_Real_Test_Class()
    {
        // A documented filter that matches nothing reports `Zero tests ran` / `Total: 0` — which the
        // profile itself calls indistinguishable at a glance from a real regression. So a rename must
        // break this test rather than quietly turn the flagship example into a no-op.
        string[] documented = Regex
            .Matches(File.ReadAllText(ClaudeMdPath), @"--filter-class\s+([A-Za-z0-9_.]+)")
            .Select(match => match.Groups[1].Value)
            .Where(fqn => fqn.StartsWith("FormCraft.UnitTests.", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        documented.ShouldNotBeEmpty($"{ClaudeMd} no longer shows a --filter-class example to verify");

        foreach (string fqn in documented)
        {
            typeof(ClaudeMdTestCommandsTests).Assembly.GetType(fqn).ShouldNotBeNull(
                $"{ClaudeMd} documents `--filter-class {fqn}`, but no such type exists in this "
                + "assembly — the documented command would run zero tests and still look plausible");
        }
    }
}
