using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// The one reader the <c>Ci/</c> guard suites use to reach the repository's own files (#255): repo
/// root, workflow enumeration and text, comment stripping, and the scan that isolates a single
/// workflow step.
/// </summary>
/// <remarks>
/// <para>
/// These suites assert on repository *files* rather than on library behaviour, because the things
/// they guard run a handful of times a year (a release, a publish) or fail silently (#231's inert
/// reporting) — so a regression is invisible until it is expensive.
/// </para>
/// <para>
/// The step scan is an approximation of YAML, and a deliberate one: no parser dependency is added
/// for five files the repo controls, and several assertions work on raw *text* on purpose — see
/// <see cref="WithoutComments" />, which exists so a documentation edit cannot redden the suite, a
/// distinction a parsed model discards. What is not deliberate is an approximation living in two
/// places at once. It did until #255: both suites carried byte-identical copies of everything here,
/// explanatory comments included, so a fix to one had no way of reaching the other. #205 is the
/// same pattern with a measured cost — a copied comment carried a factual error into a second file
/// and hid a render path from coverage.
/// </para>
/// </remarks>
internal static class WorkflowSource
{
    /// <summary>The repository root, located once from the test output directory.</summary>
    internal static string RepoRoot { get; } = LocateRepoRoot();

    internal static string WorkflowsDirectory => Path.Combine(RepoRoot, ".github", "workflows");

    /// <summary>
    /// Every workflow file by name, read once. These are pure text assertions over files that
    /// cannot change during a run, so the read is cached rather than repeated per assertion.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> All { get; } = ReadAllWorkflows();

    /// <summary>
    /// One workflow's raw text. Asserts rather than throwing a bare <c>KeyNotFoundException</c>:
    /// these suites exist to tell a reader *which* CI file broke, and a dictionary miss that names
    /// neither the file nor the directory is the one failure message that cannot do that.
    /// </summary>
    internal static string Read(string fileName)
    {
        All.TryGetValue(fileName, out var text).ShouldBeTrue(
            $"{fileName} is not in {WorkflowsDirectory} — found: {string.Join(", ", All.Keys.Order(StringComparer.Ordinal))}");

        return text!;
    }

    /// <summary>
    /// One workflow, comment-stripped and cached — the text nearly every assertion actually reads.
    /// Stripping is not free (a <c>Split</c>/<c>Where</c>/<c>Join</c> over the whole file) and was
    /// being repeated per call site, per assertion, over text that cannot change during a run.
    /// Going through here also keeps the <c>"#"</c> marker in one place instead of at every caller.
    /// </summary>
    internal static string Stripped(string fileName) =>
        StrippedCache.GetOrAdd(fileName, name => WithoutComments(Read(name), "#"));

    private static readonly ConcurrentDictionary<string, string> StrippedCache = new(StringComparer.Ordinal);

    /// <summary>
    /// <c>build/Build.cs</c>, comment-stripped and cached — the text every <c>Build.cs</c> claim in
    /// either suite reads. Shared rather than copied: both suites had a byte-identical private
    /// helper for this, which is the duplication class #255 exists to remove.
    /// </summary>
    internal static string BuildScript { get; } =
        WithoutComments(File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs")), "//");

    /// <summary>
    /// Drops whole-line comments so a "not referenced" assertion fires on wiring rather than on
    /// prose — these files carry long explanatory blocks about this very wiring, and without this a
    /// documentation-only edit would turn the suite red for a reason unrelated to the build, whose
    /// natural repair under time pressure is to delete the assertion. Only leading-<paramref
    /// name="marker" /> lines are stripped: a trailing-comment strip would also mangle the
    /// "https://" in a URL.
    /// </summary>
    internal static string WithoutComments(string text, string marker) =>
        string.Join(
            '\n',
            text.Split('\n').Where(line => !line.TrimStart().StartsWith(marker, StringComparison.Ordinal)));

    /// <summary>
    /// The workflow files whose comment-stripped text matches <paramref name="pattern" />, in a
    /// stable order. Discovery rather than a hardcoded list, so a workflow added later is held to
    /// the same contract on the day it lands — the failure mode #231 was about, where
    /// <c>release-please.yml</c> was left behind by #225 and nobody noticed.
    /// </summary>
    internal static IReadOnlyList<string> Matching(string pattern) =>
        All.Keys
            .Where(fileName => Regex.IsMatch(Stripped(fileName), pattern))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// A workflow's jobs by name, each mapped to that job's own slice of the file — so a claim
    /// about a step can be held against the job that owns it rather than against the whole file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why it matters: every job gets a fresh workspace. A workflow that ran the build in job A and
    /// uploaded <c>test-results/</c> from job B would satisfy a file-scoped assertion while
    /// uploading nothing at all, and with <c>if-no-files-found: ignore</c> pinned on every upload
    /// (#252) that failure is completely silent — the same "declared but inert" shape #231 was filed
    /// about, moved to the job boundary. <c>release-please.yml</c> is already a two-job workflow; it
    /// is correct today only because its build and its upload happen to share the <c>nupkg</c> job.
    /// </para>
    /// <para>
    /// Split by indentation rather than by a YAML parser: <c>jobs:</c> is a top-level key and each
    /// job is its own two-space-indented key beneath it. That is a smaller approximation than the
    /// step scan already relied on, and it buys the scoping without a parser dependency the project
    /// deliberately does not take (see the class remarks).
    /// </para>
    /// <para>
    /// ⚠️ Comments are stripped <em>before</em> the split, not after. These files discuss their own
    /// wiring at length — <c>release-please.yml</c>'s non-publishing job carries a comment block
    /// mentioning <c>./build.cmd Pack</c> — so splitting raw text would let a commented-out
    /// invocation mark a job as one that runs the build.
    /// </para>
    /// </remarks>
    internal static IReadOnlyDictionary<string, string> JobsOf(string fileName)
    {
        var jobs = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = Stripped(fileName).Split('\n');

        // Tolerates a trailing comment (`jobs: # …`), which WithoutComments deliberately leaves
        // alone — it only drops whole-line comments, so that a URL's "//" survives.
        var index = Array.FindIndex(lines, l => JobsKey.IsMatch(l));
        if (index < 0)
        {
            // This workflow declares no `jobs:` key, so it contributes no pairs.
            //
            // ⚠️ An empty map is NOT caught by a caller's whole-set vacuity guard: that only fires
            // when *every* workflow yields nothing. A single file dropping out is exactly what such
            // a guard cannot see, so a caller that needs each matched workflow represented has to
            // assert that per workflow — TestReportingTests.Every_Workflow_That_Runs_Tests_Should_
            // Contribute_A_Job does, and is what turns a split this reader botched into a red run
            // rather than a quietly smaller guard set.
            return jobs;
        }

        string? current = null;
        var body = new List<string>();

        for (index++; index < lines.Length; index++)
        {
            var line = lines[index];

            if (JobHeader.Match(line) is { Success: true } header)
            {
                if (current is not null)
                {
                    jobs[current] = string.Join('\n', body);
                }

                current = header.Groups["name"].Value;
                body = [];
                continue;
            }

            // A non-blank line back at column 0 is a sibling of `jobs:`, so the block has ended.
            if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
            {
                break;
            }

            body.Add(line);
        }

        if (current is not null)
        {
            jobs[current] = string.Join('\n', body);
        }

        return jobs;
    }

    /// <summary>The top-level <c>jobs:</c> key, with or without a trailing comment.</summary>
    private static readonly Regex JobsKey = new(@"^jobs:\s*(#.*)?$", RegexOptions.Compiled);

    /// <summary>
    /// A job's key: exactly two spaces, then a name. Anchored at two so it cannot match the deeper
    /// keys that make up a job's body — <c>runs-on:</c>, <c>steps:</c>, every <c>with:</c> entry.
    /// </summary>
    /// <remarks>
    /// Recognises the convention every workflow in this repo (and GitHub's own documentation) uses:
    /// two-space indentation and an unquoted name. A four-space or quoted layout is legal YAML and
    /// would not split — which is why the caller asserts that each matched workflow contributes a
    /// job, so an unrecognised layout fails loudly instead of shrinking the guard set.
    /// </remarks>
    private static readonly Regex JobHeader = new(@"^  (?<name>[A-Za-z0-9_-]+):", RegexOptions.Compiled);

    /// <summary>
    /// A single step of a workflow, so an assertion about its <c>if:</c> or <c>path:</c> cannot be
    /// satisfied by the same text sitting on some other step. Runs from the <c>- name:</c> line to
    /// the start of the next list item, which covers the step's <c>if:</c>, <c>uses:</c> and
    /// <c>with:</c>.
    /// </summary>
    /// <param name="scope">
    /// The text to search — a whole workflow, or one job's slice of it from
    /// <see cref="JobsOf" />. Taking text rather than a filename is what lets the same primitive
    /// serve a whole-file assertion and a job-scoped one.
    /// </param>
    /// <param name="stepName">The step's <c>name:</c>, as quoted in the workflow.</param>
    /// <param name="scopeDescription">
    /// What <paramref name="scope" /> is, for the failure message only — a filename, or a
    /// "workflow/job" pair. The scan cannot infer it from text.
    /// </param>
    internal static string StepNamed(string scope, string stepName, string? scopeDescription = null)
    {
        var step = TryStepNamed(scope, stepName);

        return step.ShouldNotBeNull(
            $"{scopeDescription ?? "the searched text"} no longer has a step named '{stepName}'");
    }

    /// <summary>
    /// <see cref="StepNamed" /> without the assertion: the step's own lines, or <c>null</c> when the
    /// scope has no such step.
    /// </summary>
    /// <remarks>
    /// For callers that have something better to say about absence than this helper does (#267).
    /// Asserting from in here means one missing step reddens every test that reaches for it, each
    /// reporting the same single root cause as an apparent *helper* failure — so the one test whose
    /// actual subject is "the step is present" is drowned out by four whose subject is what the step
    /// contains. A caller collecting offenders can name the job instead, once, per test that cares.
    /// </remarks>
    internal static string? TryStepNamed(string scope, string stepName)
    {
        var lines = scope.Split('\n');
        var start = Array.FindIndex(
            lines,
            l => l.TrimStart().StartsWith($"- name: '{stepName}'", StringComparison.Ordinal));

        return start < 0 ? null : StepFrom(lines, start);
    }

    /// <summary>
    /// The same boundary as <see cref="StepNamed" />, matched on a step's <c>id:</c> instead of its
    /// name — so an assertion about what a step is gated on cannot be satisfied by another step's
    /// <c>if:</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matched as an exact <c>id:</c> key rather than as a substring (#267). A <c>Contains</c> search
    /// for <c>login</c> also hits <c>id: login-legacy</c>, <c>id: login2</c>, and any <c>with:</c>
    /// value or comment carrying the text — and taking the <em>first</em> hit means an unrelated
    /// earlier line silently redefines the whole slice, so every claim about the intended step is
    /// then answered by the wrong one. That is precisely the failure this primitive exists to
    /// prevent, and it landed on the primitive #226 depends on, where a wrong answer is a green run
    /// on a version that never reached nuget.org.
    /// </para>
    /// <para>
    /// Two steps claiming one id fails loudly instead of resolving to the first: anchoring the match
    /// buys nothing if an ambiguous one still quietly picks a winner, which is the same silent wrong
    /// answer wearing a different hat.
    /// </para>
    /// <para>
    /// ⚠️ Step ids are unique per <b>job</b>, not per workflow, so this is <em>not</em> a claim that
    /// the workflow is invalid. A caller that passes a whole file as the scope — as
    /// <c>TrustedPublishingWorkflowTests</c> does — can therefore see two legitimate matches from two
    /// different jobs, and would fail here on a perfectly valid config. That caller wants
    /// <see cref="JobsOf" /> scoping, which <c>TestReportingTests</c> has had since #255 and which is
    /// tracked for this suite on #198; until then the failure is loud and explains itself, which is
    /// the safer half of the trade for a guard on the release path.
    /// </para>
    /// <para>
    /// The slice starts at the step's own <c>- </c> list item rather than at the <c>id:</c> line, so
    /// this returns the same shape <see cref="StepNamed" /> does for the same step — its <c>- name:</c>
    /// where it has one, its <c>- uses:</c> where it does not. Starting at the <c>id:</c> excluded the
    /// step's own name, which made the two primitives disagree about what "a step" is and left this
    /// one unable to assert anything about the name at all.
    /// </para>
    /// </remarks>
    internal static string StepWithId(string scope, string stepId, string? scopeDescription = null)
    {
        var step = StepWithIdOrNull(scope, stepId, scopeDescription);

        return step.ShouldNotBeNull(
            $"{scopeDescription ?? "the searched text"} no longer has a step with `id: {stepId}`");
    }

    /// <summary>
    /// <see cref="StepWithId" /> without the absence assertion: the step's own lines, or <c>null</c>
    /// when the scope has no step carrying that id.
    /// </summary>
    /// <remarks>
    /// An <em>ambiguous</em> id still fails loudly here rather than reading as absent. The two are
    /// different answers with different remedies, and reporting a duplicated step as a missing one
    /// would quietly undo the anchoring above, one call further out.
    /// </remarks>
    internal static string? TryStepWithId(string scope, string stepId) =>
        StepWithIdOrNull(scope, stepId, null);

    /// <summary>
    /// The shared body of <see cref="StepWithId" /> and <see cref="TryStepWithId" />: <c>null</c> for
    /// absent, the step's lines otherwise, and a loud failure for an ambiguous id either way.
    /// </summary>
    /// <remarks>
    /// Private, and carrying the <paramref name="scopeDescription" /> the public <c>Try</c> form does
    /// not take, so the ambiguity message can still name a scope when the caller knows one.
    /// </remarks>
    private static string? StepWithIdOrNull(string scope, string stepId, string? scopeDescription)
    {
        var lines = scope.Split('\n');
        var matches = LinesDeclaring(lines, stepId);

        matches.Count.ShouldBeLessThan(
            2,
            $"{scopeDescription ?? "the searched text"} has {matches.Count} steps claiming `id: {stepId}` — an ambiguous id cannot be resolved to one step");

        // Both "no such id" and "an id belonging to no list item" are absence: the second is a
        // malformed scope, and slicing it from line 0 would hand back the file's preamble dressed up
        // as a step. One answer covers both because the caller's remedy is the same either way.
        var header = matches.Count == 1 ? HeaderAtOrAbove(lines, matches[0]) : -1;

        return header < 0 ? null : StepFrom(lines, header);
    }

    /// <summary>
    /// The indexes of every line in <paramref name="lines" /> that declares the key
    /// <c>id: &lt;stepId&gt;</c> and nothing else — optionally opening a list item, and optionally
    /// followed by a whitespace-separated <c># comment</c>.
    /// </summary>
    /// <remarks>
    /// Every index, not the first: the count is what tells <see cref="StepWithId" /> apart absent,
    /// found, and ambiguous — and only the last of those can pass itself off as the middle one.
    /// A trailing <c># comment</c> is tolerated because <see cref="WithoutComments" /> drops only
    /// whole-line comments (so a URL's <c>//</c> survives), and callers may hand over unstripped text
    /// anyway — <c>TrustedPublishingWorkflowTests</c> does.
    /// </remarks>
    private static List<int> LinesDeclaring(string[] lines, string stepId)
    {
        // Built per call rather than compiled once, unlike JobsKey/JobHeader: the pattern depends on
        // the id being looked for, and RegexOptions.Compiled pays its cost up front — strictly a loss
        // for the handful of scans a run performs. The shape mirrors JobsKey's, trailing-comment
        // tolerance included.
        //
        // The optional dash covers `- id: login`, where the id is the list item's first key. That is
        // legal and common, no workflow here currently writes it, and an anchored `^id:` would lose
        // it — reporting a step that is plainly present as absent. Loud, unlike the bug this replaces,
        // but still a wrong answer, and the walk-back stops on that same line as the step's header.
        // `-\s+` rather than `- ` because `-  id:` and `-<tab>id:` are the same YAML.
        //
        // ⚠️ The comment clause requires WHITESPACE before the `#`, and that is the whole point of
        // spelling it `\s+#` rather than `\s*#`: YAML only starts a comment after whitespace, so
        // `id: login#1` is a single scalar naming a *different* step. With `\s*` the `#.*` swallowed
        // the `#1` and a search for `login` bound to it — a longer id re-entering through the very
        // clause added to keep longer ids out.
        var key = new Regex($@"^(?:-\s+)?id:\s*{Regex.Escape(stepId)}(?:\s+#.*)?\s*$");

        var matches = new List<int>();
        for (var index = 0; index < lines.Length; index++)
        {
            if (key.IsMatch(lines[index].Trim()))
            {
                matches.Add(index);
            }
        }

        return matches;
    }

    /// <summary>
    /// The header of the list item that <em>contains</em> the key at <paramref name="from" />, or
    /// <c>-1</c> when that key belongs to no list item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Starts <em>at</em> <paramref name="from" /> rather than above it, so an id written on the list
    /// item itself (<c>- id: login</c>) finds that line instead of running past it into the previous
    /// step. Returning <c>-1</c> rather than falling back to 0 is what makes a malformed scope read as
    /// absent: an <c>id:</c> under some <c>env:</c> block belongs to no step, and a slice from the top
    /// of the file would answer a question the scope cannot answer.
    /// </para>
    /// <para>
    /// ⚠️ Indentation is what makes this <em>containment</em> rather than "the nearest dash above",
    /// and the difference is not academic — the loose version reintroduced the defect this whole
    /// change removes, by the other door. A list item indented at or deeper than the key is nested
    /// <em>inside</em> the step (a sequence under <c>with:</c>, a bullet in a block scalar), and
    /// stopping on it returns a fragment of a step as the step. A plain key indented shallower is a
    /// container the key sits under, and reaching one means no header intervened — so the search ends
    /// there rather than crossing a job boundary to adopt the previous job's last step.
    /// </para>
    /// </remarks>
    private static int HeaderAtOrAbove(string[] lines, int from)
    {
        // The matched line may itself be the header, in which case there is nothing to walk back to.
        if (OpensListItem(lines[from]))
        {
            return from;
        }

        var depth = IndentOf(lines[from]);

        for (var index = from - 1; index >= 0; index--)
        {
            var line = lines[index];
            var trimmed = line.Trim();

            // Blank lines and comments carry no structure and sit at whatever indentation a human
            // felt like — release-please.yml's own step comments are indented to their step's dash.
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            var indent = IndentOf(line);

            if (OpensListItem(line))
            {
                if (indent < depth)
                {
                    return index;
                }

                // Nested deeper than the key: part of this step's own value, not its header.
                continue;
            }

            if (indent < depth)
            {
                // A containing key, reached without passing a header — the key is not in a list item.
                return -1;
            }
        }

        return -1;
    }

    /// <summary>
    /// Whether a line opens a YAML list item: a <c>-</c> that is alone on the line or followed by
    /// whitespace.
    /// </summary>
    /// <remarks>
    /// Not <c>StartsWith("- ")</c>. `-` alone (the item's keys starting on the next line) and
    /// <c>-&lt;tab&gt;</c> are the same YAML, and missing them made the walk-back skip a real header
    /// and return the <em>previous</em> step — silently, on the primitive #226 depends on. Excludes
    /// <c>---</c>, a document separator rather than an item.
    /// </remarks>
    private static bool OpensListItem(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith('-')
            && (trimmed.Length == 1 || char.IsWhiteSpace(trimmed[1]));
    }

    /// <summary>The line's leading-whitespace width — its YAML nesting depth.</summary>
    private static int IndentOf(string line) => line.Length - line.TrimStart().Length;

    /// <summary>
    /// From a step's header line to the start of the next <em>sibling</em> list item, or to the end of
    /// <paramref name="lines" /> when it is the last step.
    /// </summary>
    /// <remarks>
    /// Sibling, not merely "next list item": a sequence nested inside the step — <c>args:</c> entries,
    /// a bullet in a <c>path: |</c> block — is part of the step's own value, and ending there returns a
    /// truncated fragment as the whole step. That direction of error is the dangerous one for the
    /// negative assertions this reader serves: a <c>ShouldNotContain</c> over a slice cut short passes
    /// because the text it rejects fell outside the fragment, not because the step is clean. Depth is
    /// the same rule <see cref="HeaderAtOrAbove" /> walks back by, applied forwards.
    /// </remarks>
    private static string StepFrom(string[] lines, int start)
    {
        var depth = IndentOf(lines[start]);
        var end = Array.FindIndex(
            lines,
            start + 1,
            l => OpensListItem(l) && IndentOf(l) <= depth);

        if (end < 0)
        {
            end = lines.Length;
        }

        return string.Join('\n', lines[start..end]);
    }

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

    private static Dictionary<string, string> ReadAllWorkflows() =>
        Directory
            .EnumerateFiles(WorkflowsDirectory, "*.*")
            .Where(f => f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(f => Path.GetFileName(f), f => File.ReadAllText(f), StringComparer.Ordinal);
}
