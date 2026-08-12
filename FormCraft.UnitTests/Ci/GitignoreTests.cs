using System.Text;
using System.Text.RegularExpressions;

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
/// CI's <c>git add -A</c> still staged the worktree.
/// </para>
/// <para>
/// The cost of that choice is that ignore semantics have to be modelled here rather than asked of
/// git. <see cref="Covers" /> does so for the pattern forms git actually resolves — anchoring,
/// depth, directory-only, <c>*</c>/<c>**</c>/<c>?</c>, and case-insensitivity — and the modelling was
/// checked against real <c>git check-ignore</c> runs rather than reasoned about. Its scope is one
/// question: does an entry swallow a path that must stay tracked? A nested <c>.gitignore</c> could
/// answer that question differently, so the directories where one could exist are asserted empty
/// below rather than left silently out of scope.
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
    /// The only directories whose own <c>.gitignore</c> could change the answer for the paths
    /// guarded here — a nested file governs its own directory and below, and nothing else.
    /// </summary>
    private static readonly string[] DirectoriesThatCouldReIgnore =
    [
        "test-results",
        ".claude",
        ".claude/skills",
        ".claude/worktrees",
    ];

    private static readonly string[] ProfileSegments = CommittedProfile.Split('/');

    /// <summary>
    /// Every meaningful line of the tracked <c>.gitignore</c>, read once. The repo root comes from
    /// <see cref="WorkflowSource.RepoRoot" />, which walks up from the test assembly's own location
    /// to <c>FormCraft.sln</c> — not from the current directory, which the runner does not promise.
    /// </summary>
    private static readonly IReadOnlyList<string> Entries = ReadEntries();

    /// <summary>
    /// Trims the <b>end</b> of each line only. Git strips trailing whitespace from a pattern but
    /// treats leading whitespace as part of it, so <c>"  /test-results/"</c> is an entry for a
    /// directory literally named <c>"  /test-results"</c> — measured, and it ignores nothing. A
    /// <c>Trim()</c> here would tidy that away and report the file as correct, which is the same
    /// invisible-typo failure the whole class exists to catch.
    /// </summary>
    /// <remarks>
    /// This is also why the parsing is not routed through
    /// <see cref="WorkflowSource.WithoutComments" /> despite the near-duplication: that helper tests
    /// the marker after a <c>TrimStart()</c>, which is right for YAML and wrong here — under git a
    /// <c>"  # x"</c> line is a pattern, not a comment. The divergence is semantic rather than
    /// incidental, so the two readers are kept apart deliberately.
    /// </remarks>
    private static IReadOnlyList<string> ReadEntries() =>
        File.ReadAllLines(Path.Combine(WorkflowSource.RepoRoot, ".gitignore"))
            .Select(line => line.TrimEnd())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToList();

    [Theory]
    [InlineData(TestResultsEntry)]
    [InlineData(WorktreesEntry)]
    public void Gitignore_Should_Carry_Each_Required_Entry_Verbatim_On_Its_Own_Line(string entry)
    {
        // Equality against a whole line, not a substring: the collapsed form *contained* both of
        // these and ignored neither, so "the text appears somewhere in the file" is exactly the
        // check that would have passed throughout the defect's lifetime.
        //
        // Deliberately a pin rather than a behavioural equivalence — `**/.claude/worktrees/` would
        // ignore the same paths and still redden here. That is the intent: this file guards two
        // specific lines against a specific regression, and an equivalent rewrite is a change worth
        // a human reading this comment. The message says so, so nobody debugs a collapse that did
        // not happen.
        Entries.ShouldContain(
            entry,
            $"'{entry}' must appear verbatim as its own line in .gitignore. If you rewrote it to an "
            + "equivalent pattern, update this test deliberately; if it vanished or merged into "
            + "another line, that is the #264 regression this file guards");
    }

    [Fact]
    public void Gitignore_Should_Not_Collapse_The_Two_Entries_Onto_One_Line()
    {
        // The regression named exactly. The assertion above already reddens for it, but it cannot
        // say *why* — "the line is missing" and "the line merged into its neighbour" are different
        // repairs, and .gitignore is a classic merge-conflict site where the second is what actually
        // happens. Matching the collapse shape (one line carrying both) rather than "the token
        // appears anywhere else" keeps that message without rejecting legitimate future entries
        // like `/docs/test-results/` or `.claude/worktrees/*.log`.
        var collapsed = Entries
            .Where(line =>
                line.Contains("test-results", StringComparison.Ordinal)
                && line.Contains(".claude/worktrees", StringComparison.Ordinal))
            .ToList();

        collapsed.ShouldBeEmpty(
            "two entries collapsed onto one line read as a single path that does not exist, "
            + "so neither is ignored — this is the #264 defect, verbatim");
    }

    [Fact]
    public void The_Committed_Repo_Profile_Should_Still_Be_Where_This_File_Guards_It()
    {
        // Without this the assertion below rots into a vacuous pass: rename or move the profile and
        // it goes on guarding a path nothing writes to, green forever, while the real file sits
        // unguarded. Pinning the premise is what makes the guarantee mean something.
        File.Exists(Path.Combine(WorkflowSource.RepoRoot, CommittedProfile)).ShouldBeTrue(
            $"'{CommittedProfile}' is committed on purpose and is the path guarded below — "
            + "if it moved, move the guard with it");
    }

    [Fact]
    public void No_Nested_Gitignore_Should_Be_Able_To_Reignore_The_Guarded_Paths()
    {
        // Reading only the root .gitignore is sound exactly as long as no nested one governs these
        // paths. A nested file applies to its own directory and below, so this list is complete —
        // and asserting it empty is what keeps the whole-file reasoning honest instead of merely
        // unstated. (The repo does carry .idea/.idea.FormCraft/.idea/.gitignore; it cannot reach
        // any path guarded here, which is precisely why the check is scoped rather than global.)
        var nested = DirectoriesThatCouldReIgnore
            .Where(dir => File.Exists(Path.Combine(WorkflowSource.RepoRoot, dir, ".gitignore")))
            .ToList();

        nested.ShouldBeEmpty(
            "a nested .gitignore in one of these directories can re-ignore a path this file "
            + "guards, and nothing here reads it — fold its rules into the root .gitignore, or "
            + "widen this suite to read it");
    }

    [Fact]
    public void Gitignore_Should_Not_Ignore_The_Committed_Repo_Profile()
    {
        // Without this, "simplify .claude/worktrees/ to .claude/" passes every assertion above while
        // quietly untracking repo-profile.md — a strictly worse failure than the one being fixed,
        // because it breaks the tooling rather than merely failing to help it. Recorded as rejected
        // in #264's brainstorm precisely so nobody re-derives it as an improvement.
        //
        // A `!` re-include is NOT accepted as an answer, and that is not strictness for its own
        // sake: git cannot re-include a file whose parent directory is excluded. Measured —
        // `.claude/` plus `!.claude/skills/repo-profile.md` still reports the profile as ignored.
        // Treating a negation as a rescue would have gone green on the single most likely form of
        // this mistake, which is someone doing the trap and believing they had handled it.
        var swallowed = Entries
            .Where(line => !line.StartsWith('!') && Covers(line))
            .ToList();

        swallowed.ShouldBeEmpty(
            $"'{CommittedProfile}' is committed on purpose — the lifecycle skills read it — so no "
            + ".gitignore entry may swallow it. Note a '!' re-include cannot fix this: git will not "
            + "re-include a file under an excluded directory");
    }

    /// <summary>
    /// Whether <paramref name="entry" /> would cause git to ignore <see cref="CommittedProfile" />.
    /// </summary>
    /// <remarks>
    /// Models the parts of gitignore matching that decide this question, each verified against real
    /// <c>git check-ignore</c> runs: a pattern with no interior slash matches a segment at
    /// <b>any depth</b> (so <c>skills/</c> and <c>repo-profile.md</c> both swallow the profile);
    /// one with an interior slash is anchored to the repo root; a trailing slash restricts it to
    /// directories; and everything under an excluded <b>directory</b> is excluded too, which is what
    /// makes <c>.claude/*</c> — the canonical "ignore the directory, keep one file" idiom — swallow
    /// the profile even though the glob itself never matches its full path.
    /// </remarks>
    private static bool Covers(string entry)
    {
        // The caller decides what a leading '!' MEANS; this answers only whether the pattern matches.
        var pattern = entry.StartsWith('!') ? entry[1..] : entry;

        // Leading whitespace is part of the pattern under git, so such an entry matches a path
        // literally beginning with spaces — never one of ours.
        if (pattern.Length == 0 || char.IsWhiteSpace(pattern[0]))
        {
            return false;
        }

        var directoryOnly = pattern.EndsWith('/');
        pattern = pattern.TrimEnd('/');

        // A leading '/' anchors without contributing a path segment; a leading '**/' explicitly
        // restores the any-depth behaviour that a slash-free pattern already has.
        var anchored = pattern.StartsWith('/');
        pattern = anchored ? pattern[1..] : pattern;

        if (pattern.StartsWith("**/", StringComparison.Ordinal))
        {
            pattern = pattern[3..];
            anchored = false;
        }
        else if (pattern.Contains('/', StringComparison.Ordinal))
        {
            anchored = true;
        }

        if (pattern.Length == 0)
        {
            return false;
        }

        // Walk the profile's own path: each ancestor directory, then the file. Matching an ancestor
        // directory is enough — nothing under an excluded directory can be recovered.
        for (var i = 0; i < ProfileSegments.Length; i++)
        {
            var isDirectory = i < ProfileSegments.Length - 1;
            if (directoryOnly && !isDirectory)
            {
                continue;
            }

            var candidate = anchored
                ? string.Join('/', ProfileSegments.Take(i + 1))
                : ProfileSegments[i];

            if (Matches(pattern, candidate))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Glob-matches one gitignore pattern against one path, with <c>*</c> stopping at a separator,
    /// <c>**</c> spanning them, and <c>?</c> taking a single non-separator character.
    /// </summary>
    /// <remarks>
    /// Case-insensitive on purpose. Git honours <c>core.ignorecase</c>, which defaults to true on
    /// the macOS and Windows checkouts most contributors use, so a <c>.Claude/</c> entry would
    /// untrack the profile for them while an ordinal comparison here — and CI's case-sensitive
    /// Linux filesystem — both reported everything fine.
    /// </remarks>
    private static bool Matches(string pattern, string candidate)
    {
        var regex = new StringBuilder("^");

        for (var i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '*')
            {
                var isDoubled = i + 1 < pattern.Length && pattern[i + 1] == '*';
                regex.Append(isDoubled ? ".*" : "[^/]*");
                i += isDoubled ? 1 : 0;
            }
            else if (pattern[i] == '?')
            {
                regex.Append("[^/]");
            }
            else
            {
                regex.Append(Regex.Escape(pattern[i].ToString()));
            }
        }

        return Regex.IsMatch(candidate, regex.Append('$').ToString(), RegexOptions.IgnoreCase);
    }
}
