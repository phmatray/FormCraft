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
/// casual read — it survived in <c>CLAUDE.md</c> until #277 measured it, and <c>CLAUDE.md</c> is the
/// file auto-loaded into every session.
/// </para>
/// <para>
/// <b>Asserted on the command form, not on the bare substring, deliberately.</b> The corrected prose
/// has to be able to <i>name</i> the broken flags in order to warn about them ("⛔ <c>dotnet test
/// --filter</c> is inert"), so a substring check would forbid the very sentence that prevents the
/// regression. Only a line that <i>is</i> a command — one whose code begins <c>dotnet test</c> —
/// counts, and inline-code prose never does.
/// </para>
/// <para>
/// Continued lines are joined before matching. A wrapped command is the obvious way this guard would
/// otherwise be evaded by accident: the <c>--filter</c> sits on a continuation line that does not
/// itself begin <c>dotnet test</c>, so a naive per-line scan reads it as prose and passes.
/// </para>
/// <para>
/// The working replacements are <c>dotnet test &lt;csproj&gt; -c Release -- --filter-class &lt;FQN&gt;</c>
/// (everything after <c>--</c> reaches the test host) or the built host directly — documented with
/// their traps in <c>.claude/skills/repo-profile.md</c> under <i>Build &amp; test</i>. Note
/// <c>--filter-class</c> must keep passing this guard, which is why the pattern below requires
/// <c>--filter</c> to be followed by something other than a name character.
/// </para>
/// </remarks>
public class ClaudeMdTestCommandsTests
{
    /// <summary>The always-loaded briefing this guard protects, relative to the repo root.</summary>
    private const string ClaudeMd = "CLAUDE.md";

    /// <summary>
    /// Matches a VSTest option that Microsoft.Testing.Platform silently discards.
    /// <c>(?![-\w])</c> is load-bearing: it lets the working <c>--filter-class</c> /
    /// <c>--filter-method</c> / <c>--filter-namespace</c> through while still catching a bare
    /// <c>--filter</c> that is followed by a space, a quote, or the end of the line.
    /// </summary>
    private static readonly Regex InertVsTestOption =
        new(@"--(?:filter(?![-\w])|collect)", RegexOptions.Compiled);

    /// <summary>
    /// Every shell command in <c>CLAUDE.md</c> that invokes <c>dotnet test</c>, with backslash
    /// continuations already folded into the line that starts them.
    /// </summary>
    private static IReadOnlyList<string> DotnetTestCommands()
    {
        string[] lines = File.ReadAllLines(Path.Combine(WorkflowSource.RepoRoot, ClaudeMd));
        List<string> commands = [];

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (!line.StartsWith("dotnet test", StringComparison.Ordinal))
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
    public void ClaudeMd_Should_Not_Teach_Test_Commands_That_The_Runner_Ignores()
    {
        string[] offenders = DotnetTestCommands()
            .Where(command => InertVsTestOption.IsMatch(command))
            .ToArray();

        offenders.ShouldBeEmpty(
            $"CLAUDE.md documents {offenders.Length} `dotnet test` command(s) using VSTest options that "
            + "Microsoft.Testing.Platform ignores (MTP0001) — the whole suite runs, or no coverage file "
            + "is written, and either way the command exits 0. Use `dotnet test <csproj> -c Release "
            + "-- --filter-class <FQN>` instead; see .claude/skills/repo-profile.md (Build & test). "
            + $"Offending command(s): {string.Join(" | ", offenders)}");
    }

    [Fact]
    public void The_Guarded_Briefing_Should_Still_Be_Where_This_File_Guards_It()
    {
        // A rename would make the assertion above vacuous rather than failing, so it is asserted
        // instead of assumed — the same reason GitignoreTests pins the profile's path.
        File.Exists(Path.Combine(WorkflowSource.RepoRoot, ClaudeMd))
            .ShouldBeTrue($"{ClaudeMd} was not found at the repo root; this guard would silently pass");
    }
}
