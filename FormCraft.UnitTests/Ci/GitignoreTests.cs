namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the <c>.gitignore</c> entries whose absence is silent (#264). Two of them were once a
/// single collapsed line — <c>/test-results/.claude/worktrees/</c> — which git reads as one path
/// that does not exist, so <b>neither</b> <c>/test-results/</c> <b>nor</b> <c>.claude/worktrees/</c>
/// was ignored. The line looks correct at a glance, which is how it survived; it was split back
/// apart in #223 and this is the guard that stops it collapsing again.
/// </summary>
/// <remarks>
/// <para>
/// The cost of the gap is not a noisy diff. A git worktree under <c>.claude/worktrees/</c> is a full
/// checkout with its own <c>.git</c>, so a <c>git add -A</c> at the repo root stages it as a
/// <b>single gitlink</b> — <c>160000 &lt;sha&gt; 0 .claude/worktrees/&lt;branch&gt;</c> — one line,
/// pointing at a commit no clone can fetch, with no submodule URL and no remote. Git warns, and on a
/// busy console that warning is the only thing between this and a silent commit.
/// </para>
/// <para>
/// <b>Asserted on the tracked file's text, deliberately, rather than by shelling out to
/// <c>git check-ignore</c>.</b> That command is also satisfied by <c>.git/info/exclude</c> and by
/// <c>core.excludesFile</c>, both machine-local and neither committed — and the first cannot be
/// neutralised by any environment variable. A developer whose personal excludes happen to list
/// <c>.claude/worktrees/</c> (a common agent-tooling setup, and the case measured while filing #264)
/// would see such a test pass for a reason the repository does not carry, while every teammate's and
/// CI's <c>git add -A</c> still staged the worktree. Reading the tracked <c>.gitignore</c> asks the
/// question the repository actually answers, and matches the text-only grain of the rest of
/// <c>Ci/</c>.
/// </para>
/// </remarks>
public class GitignoreTests
{
    /// <summary>Build/test output. Anchored with a leading slash: repo root only.</summary>
    private const string TestResultsEntry = "/test-results/";

    /// <summary>
    /// The convention home for agent-created git worktrees. Deliberately <b>not</b> anchored, so it
    /// matches at any depth.
    /// </summary>
    private const string WorktreesEntry = ".claude/worktrees/";

    /// <summary>
    /// Committed on purpose — the issue/PR lifecycle skills read it, so it travels with the repo.
    /// It is the reason "just ignore <c>.claude/</c> wholesale" is a trap rather than a
    /// simplification, and the reason that trap is asserted against below.
    /// </summary>
    private const string CommittedProfile = ".claude/skills/repo-profile.md";

    /// <summary>
    /// Every meaningful line of the tracked <c>.gitignore</c>, read once. The repo root comes from
    /// <see cref="WorkflowSource.RepoRoot" />, which walks up from the test assembly's own location
    /// to <c>FormCraft.sln</c> — not from the current directory, which the runner does not promise.
    /// </summary>
    private static readonly IReadOnlyList<string> Entries = ReadEntries();

    private static IReadOnlyList<string> ReadEntries() =>
        File.ReadAllLines(Path.Combine(WorkflowSource.RepoRoot, ".gitignore"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();

    [Theory]
    [InlineData(TestResultsEntry)]
    [InlineData(WorktreesEntry)]
    public void Gitignore_Should_Carry_Each_Required_Entry_On_Its_Own_Line(string entry)
    {
        // Equality against a whole line, not a substring: the collapsed form *contained* both of
        // these and ignored neither, so "the text appears somewhere in the file" is exactly the
        // check that would have passed throughout the defect's lifetime.
        Entries.ShouldContain(
            entry,
            $"'{entry}' must be its own line in .gitignore — see the collapse this file guards against");
    }

    [Theory]
    [InlineData(TestResultsEntry, "test-results")]
    [InlineData(WorktreesEntry, ".claude/worktrees")]
    public void Gitignore_Should_Not_Mention_A_Required_Entry_On_Any_Other_Line(string entry, string token)
    {
        // The other half of the same invariant, and the half that names the regression precisely. A
        // re-collapse — by a careless merge resolution, most likely, since .gitignore is a classic
        // conflict site — produces a line that *mentions* the token while *being* neither entry.
        // The test above already reddens for that; this one says why, in the failure message, so the
        // next reader does not have to rediscover the mechanism.
        var offenders = Entries
            .Where(line => line != entry && line.Contains(token, StringComparison.Ordinal))
            .ToList();

        offenders.ShouldBeEmpty(
            $"'{token}' appears outside its own '{entry}' line — two entries collapsed onto one line "
            + "read as a single path that does not exist, so neither is ignored");
    }

    [Fact]
    public void Gitignore_Should_Not_Ignore_The_Committed_Repo_Profile()
    {
        // Without this, "simplify .claude/worktrees/ to .claude/" passes both tests above while
        // quietly untracking repo-profile.md — a strictly worse failure than the one being fixed,
        // because it breaks the tooling rather than merely failing to help it. Recorded as rejected
        // in #264's brainstorm precisely so nobody re-derives it as an improvement.
        //
        // Path-prefix reasoning only: this is not a gitignore glob engine, and does not need to be.
        // It catches the directory and exact-path rules a human would plausibly write, which is the
        // whole realistic surface here.
        var swallowed = Entries
            .Where(line => !line.StartsWith('!') && Covers(line))
            .ToList();

        // A negated entry re-includes the path, so it answers the charge.
        var reIncluded = Entries.Any(line => line.StartsWith('!') && Covers(line));
        IReadOnlyList<string> effective = reIncluded ? [] : swallowed;

        effective.ShouldBeEmpty(
            $"'{CommittedProfile}' is committed on purpose — the lifecycle skills read it — "
            + "so no .gitignore entry may swallow it");
    }

    /// <summary>
    /// Whether <paramref name="entry" /> would swallow <see cref="CommittedProfile" />, treating the
    /// entry as a plain path prefix. A trailing slash means "this directory and everything under
    /// it"; anything else has to match the file outright or be a parent directory of it.
    /// </summary>
    private static bool Covers(string entry)
    {
        var pattern = entry.TrimStart('!').Trim();

        // A leading `**/` and a leading `/` both drop out for this comparison: the first says "at
        // any depth" and the profile path is already written from the repo root, the second only
        // anchors a match that is anchored here anyway.
        pattern = pattern.StartsWith("**/", StringComparison.Ordinal)
            ? pattern[3..]
            : pattern.TrimStart('/');

        if (pattern.Length == 0)
        {
            return false;
        }

        return pattern.EndsWith('/')
            ? CommittedProfile.StartsWith(pattern, StringComparison.Ordinal)
            : CommittedProfile == pattern
              || CommittedProfile.StartsWith(pattern + "/", StringComparison.Ordinal);
    }
}
