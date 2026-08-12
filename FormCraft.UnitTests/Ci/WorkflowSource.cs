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

    internal static string Read(string fileName) => All[fileName];

    internal static string ReadBuildScript() =>
        File.ReadAllText(Path.Combine(RepoRoot, "build", "Build.cs"));

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
        All
            .Where(entry => Regex.IsMatch(WithoutComments(entry.Value, "#"), pattern))
            .Select(entry => entry.Key)
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
        var lines = WithoutComments(Read(fileName), "#").Split('\n');

        var index = Array.FindIndex(lines, l => l.TrimEnd() == "jobs:");
        if (index < 0)
        {
            // No `jobs:` key at all (no workflow here has one today). An empty map contributes no
            // pairs; the caller's vacuity guard is what stops that from silently emptying the set.
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

    /// <summary>
    /// A job's key: exactly two spaces, then a name. Anchored at two so it cannot match the deeper
    /// keys that make up a job's body — <c>runs-on:</c>, <c>steps:</c>, every <c>with:</c> entry.
    /// </summary>
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
        var lines = scope.Split('\n');
        var start = Array.FindIndex(
            lines,
            l => l.TrimStart().StartsWith($"- name: '{stepName}'", StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(
            0,
            $"{scopeDescription ?? "the searched text"} no longer has a step named '{stepName}'");

        return StepFrom(lines, start);
    }

    /// <summary>
    /// The same boundary as <see cref="StepNamed" />, matched on a step's <c>id:</c> instead of its
    /// name — so an assertion about what a step is gated on cannot be satisfied by another step's
    /// <c>if:</c>.
    /// </summary>
    internal static string StepWithId(string scope, string stepId, string? scopeDescription = null)
    {
        var lines = scope.Split('\n');
        var start = Array.FindIndex(lines, l => l.Contains($"id: {stepId}", StringComparison.Ordinal));
        start.ShouldBeGreaterThanOrEqualTo(
            0,
            $"{scopeDescription ?? "the searched text"} no longer has a step with `id: {stepId}`");

        return StepFrom(lines, start);
    }

    /// <summary>
    /// From a step's header line to the start of the next list item, or to the end of
    /// <paramref name="lines" /> when it is the last step.
    /// </summary>
    private static string StepFrom(string[] lines, int start)
    {
        var end = Array.FindIndex(lines, start + 1, l => l.TrimStart().StartsWith("- ", StringComparison.Ordinal));
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
