using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards <c>CLAUDE.md</c> against VSTest-era test invocations (#299). The test projects run on
/// Microsoft.Testing.Platform (<c>OutputType=Exe</c> + <c>UseMicrosoftTestingPlatformRunner=true</c>),
/// which <b>ignores</b> VSTest options: <c>--filter</c> arrives as the MSBuild property
/// <c>VSTestTestCaseFilter</c> and <c>--collect:</c> as <c>VSTestCollect</c>, both discarded with a
/// single <c>MTP0001</c> build warning.
/// </summary>
/// <remarks>
/// <para>
/// The failure is <b>silent and green</b>, which is why it needs a guard rather than a reader. A
/// filtered run executes the <i>entire</i> suite and still prints <c>Passed!</c> with exit <c>0</c>;
/// a <c>--collect:"XPlat Code Coverage"</c> run writes <b>no coverage file at all</b> and also exits
/// <c>0</c>. Nothing in either outcome looks like a mistake, so the stale instruction survives every
/// casual read — it did survive, in the file auto-loaded into every session, until #277 measured it.
/// </para>
/// <para>
/// <b>Both spellings are covered.</b> The flag form (<c>--filter</c>, <c>--collect</c>) and the
/// MSBuild-property form (<c>-p:VSTestTestCaseFilter=…</c>) are equally inert — the property form was
/// measured for #299 and produced <c>MTP0001</c> with all 158 tests of the target project running.
/// <see cref="TestReportingTests.BuildScript_Should_Not_Set_The_VSTest_Properties_That_Mtp_Ignores" />
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
    /// A VSTest option Microsoft.Testing.Platform silently discards, in either spelling.
    /// <c>(?![-\w])</c> is load-bearing: it lets the working <c>--filter-class</c> /
    /// <c>--filter-method</c> / <c>--filter-namespace</c> through while still catching a bare
    /// <c>--filter</c> followed by a space, a quote, or the end of the line.
    /// </summary>
    private static readonly Regex InertVsTestOption = new(
        @"--(?:filter(?![-\w])|collect)|[-/]p:VSTest\w+",
        RegexOptions.Compiled);

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
    public void ClaudeMd_Should_Not_Teach_Test_Commands_That_The_Runner_Ignores()
    {
        string[] offenders = TestCommands()
            .Where(command => InertVsTestOption.IsMatch(command))
            .ToArray();

        offenders.ShouldBeEmpty(
            $"CLAUDE.md documents {offenders.Length} test command(s) using VSTest options that "
            + "Microsoft.Testing.Platform ignores (MTP0001) — the whole suite runs, or no coverage file "
            + "is written, and either way the command exits 0. Use `dotnet test <csproj> -c Release "
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
