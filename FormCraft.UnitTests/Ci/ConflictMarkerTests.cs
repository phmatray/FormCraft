namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Fails <c>dotnet test</c> when a tracked <c>.cs</c> file carries a merge-conflict marker (#347).
/// </summary>
/// <remarks>
/// <c>dotnet format</c> in apply mode computes a fix once per target framework, and on this repo's
/// multi-targeted projects (<c>net8.0;net10.0</c>) a disagreement between the two can be written as
/// a literal <c>&lt;&lt;&lt;&lt;&lt;&lt;&lt;</c> conflict block into the <c>.cs</c> file instead of
/// failing — a file that then does not compile (<c>CS8300</c>, measured in #301 on
/// <c>FieldRendererBase.cs</c> and again in #307 on two <c>FormCraft.ForFluentUI</c> files). Verify
/// mode (<c>--verify-no-changes</c>, the <c>Format</c> CI gate) never writes, so the exposure is
/// entirely on the apply path — the command <c>CLAUDE.md</c> tells a contributor to run when that
/// gate fails. Before this guard, the only defence was a human remembering to
/// <c>grep -rl '&lt;&lt;&lt;&lt;&lt;&lt;&lt; TODO' --include='*.cs' .</c>.
/// </remarks>
public class ConflictMarkerTests
{
    /// <summary>Built at runtime so this file's own guard can never match itself.</summary>
    private static readonly string ConflictStart = new('<', 7);

    private static readonly string ConflictEnd = new('>', 7);

    /// <summary>
    /// Directory segments to skip. <c>.claude</c> is load-bearing rather than tidiness:
    /// <c>.claude/worktrees/</c> holds other agents' full checkouts, which may legitimately be
    /// mid-merge with real conflict markers in them. <c>bin</c>/<c>obj</c> are generated sources.
    /// </summary>
    private static readonly string[] SkippedSegments = ["bin", "obj", ".claude"];

    internal readonly record struct Offence(string File, int Line, string Text);

    /// <summary>
    /// Scans each source's content for lines starting with a conflict marker. Takes content
    /// directly — not file paths — so the detection path is exercised against synthetic text with
    /// nothing written to disk.
    /// </summary>
    internal static IReadOnlyList<Offence> FindMarkers(IEnumerable<(string File, string Content)> sources)
    {
        List<Offence> offences = [];

        foreach (var (file, content) in sources)
        {
            var lines = content.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (line.StartsWith(ConflictStart, StringComparison.Ordinal)
                    || line.StartsWith(ConflictEnd, StringComparison.Ordinal))
                {
                    offences.Add(new Offence(file, i + 1, line));
                }
            }
        }

        return offences;
    }

    [Fact]
    public void FindMarkers_Should_Report_Both_Conflict_Markers_With_Their_Line_Numbers()
    {
        var content = string.Join(
            '\n',
            "namespace Foo;",
            ConflictStart + " TODO: Unmerged change from project 'FormCraft(net10.0)', Before:",
            "    return false;",
            "=======",
            ConflictEnd + " After");

        var offences = FindMarkers([("Synthetic.cs", content)]);

        offences.Select(o => o.Line).ShouldBe([2, 5]);
        offences.ShouldAllBe(o => o.File == "Synthetic.cs");
    }

    [Fact]
    public void FindMarkers_Should_Not_Report_A_Plausible_Comment_Divider_Or_Mid_Sentence_Prose()
    {
        // A line of exactly seven '=' is a plausible comment divider, not a conflict marker — only
        // the angle-bracket lines are. Mentioning a marker mid-sentence (as this guard's own doc
        // comments do) must not trip line-start detection either.
        var content = string.Join(
            '\n',
            "// =======",
            "// A conflict marker looks like " + ConflictStart + " at the start of a line.");

        FindMarkers([("Synthetic.cs", content)]).ShouldBeEmpty();
    }

    [Fact]
    public void No_Tracked_Cs_File_Should_Carry_A_Merge_Conflict_Marker()
    {
        var files = ScannedCsFiles();

        // Without this, a broken walk (wrong root, over-eager exclusion) would pass vacuously
        // forever, guarding nothing — the same failure mode CollectionItemShapeGuardTests guards
        // against for its own detector.
        files.ShouldNotBeEmpty("the scan found no .cs files at all — the walk is broken");

        var offences = FindMarkers(files.Select(f => (f, File.ReadAllText(f))));

        var report = string.Join('\n', offences.Select(o => $"  {o.File}:{o.Line}: {o.Text}"));
        offences.ShouldBeEmpty(
            "dotnet format left a conflict marker behind:\n" + report
            + "\nRe-run `dotnet format FormCraft.sln --include <file>` scoped to just that file, "
            + "then hand-resolve — do not commit as-is.");
    }

    [Fact]
    public void The_Scan_Should_Exclude_Bin_Obj_And_Claude_Directories()
    {
        // The exclusions are load-bearing (see SkippedSegments), so a regression there must fail
        // this guard for reasons inside the repo, not for reasons outside it — an unrelated
        // worktree mid-merge under .claude/worktrees/, or generated Razor source-generator output
        // under bin/obj.
        var files = ScannedCsFiles();

        files.ShouldNotContain(f => RelativeSegments(f).Contains(".claude"));
        files.ShouldNotContain(f => RelativeSegments(f).Contains("bin"));
        files.ShouldNotContain(f => RelativeSegments(f).Contains("obj"));
    }

    private static IReadOnlyList<string> ScannedCsFiles() =>
        Directory
            .EnumerateFiles(WorkflowSource.RepoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !RelativeSegments(f).Any(SkippedSegments.Contains))
            .ToList();

    /// <summary>
    /// Path segments <b>relative to <see cref="WorkflowSource.RepoRoot" /></b> — never the full
    /// absolute path. An agent worktree's own checkout commonly lives under a path that itself
    /// contains <c>.claude</c> (e.g. <c>.claude/worktrees/&lt;name&gt;</c>); matching on the
    /// absolute path would make every file "under .claude" by accident of where the repo happens
    /// to be checked out, and the scan would find nothing.
    /// </summary>
    private static string[] RelativeSegments(string path) =>
        Path.GetRelativePath(WorkflowSource.RepoRoot, path)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
