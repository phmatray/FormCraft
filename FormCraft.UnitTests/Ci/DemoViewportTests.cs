using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards every demo application's <c>wwwroot/index.html</c> against a viewport meta that disables
/// pinch-zoom (#341). <c>FormCraft.DemoFluentApp</c> shipped
/// <c>maximum-scale=1.0, user-scalable=no</c> — the stock Blazor WASM template line, carried in
/// unedited — while its sibling <c>FormCraft.DemoBlazorApp</c> does not. iOS Safari honours
/// <c>maximum-scale</c>, so a low-vision visitor to that demo could not pinch-zoom: a WCAG 2.1 SC
/// 1.4.4 (Level AA) failure in a repository that has repeatedly treated accessibility conformance as
/// a hard requirement rather than a nicety (#199, #262, #281, #285).
/// </summary>
/// <remarks>
/// <para>
/// <b>Discovered, not hardcoded.</b> Every top-level directory under the repo root is checked for its
/// own <c>wwwroot/index.html</c> — a single wildcard level, <c>*/wwwroot/index.html</c> — rather than
/// naming <c>FormCraft.DemoBlazorApp</c> and <c>FormCraft.DemoFluentApp</c> by path. A third demo app
/// scaffolded from the same template is covered automatically. A build's
/// <c>bin/&lt;config&gt;/&lt;tfm&gt;/wwwroot/index.html</c> copy is excluded by construction: it sits
/// two or more directory levels below its project directory, past the single wildcard this scan
/// allows, so a stale build artifact can never make this suite pass or fail for the wrong reason.
/// </para>
/// <para>
/// <see cref="The_Scan_Should_Actually_Find_Both_Known_Demo_Shells" /> and
/// <see cref="Every_Demo_Shell_Should_Declare_A_Viewport_Meta" /> are the load-bearing tests — every
/// other assertion here is "no offender was found", which an empty scan or a regex that stopped
/// matching also reports. Without them, moving a demo project or reformatting its viewport tag would
/// silently stop guarding anything (the vacuity hole fixed in <see cref="ClaudeMdTestCommandsTests" />
/// under #299).
/// </para>
/// </remarks>
public class DemoViewportTests
{
    private static readonly Regex ViewportMeta = new(
        @"<meta\s+name\s*=\s*[""']viewport[""'][^>]*>",
        RegexOptions.IgnoreCase);

    private static readonly Regex UserScalableNo = new(
        @"user-scalable\s*=\s*[""']?no",
        RegexOptions.IgnoreCase);

    private static readonly Regex MaximumScale = new(
        @"maximum-scale",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// Every demo shell found by the discovery rule, keyed by its path relative to the repo root and
    /// read once — these are pure text assertions over files that cannot change during a run.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> DemoShells = FindDemoShells();

    private static IReadOnlyDictionary<string, string> FindDemoShells()
    {
        var shells = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var projectDir in Directory.EnumerateDirectories(WorkflowSource.RepoRoot))
        {
            var indexPath = Path.Combine(projectDir, "wwwroot", "index.html");
            if (File.Exists(indexPath))
            {
                var relative = Path.GetRelativePath(WorkflowSource.RepoRoot, indexPath);
                shells[relative] = File.ReadAllText(indexPath);
            }
        }

        return shells;
    }

    [Fact]
    public void The_Scan_Should_Actually_Find_Both_Known_Demo_Shells()
    {
        // Without this, every assertion below is vacuously true the moment the scan stops matching —
        // a moved or renamed demo project is enough to cause that silently.
        DemoShells.Keys.ShouldContain(Path.Combine("FormCraft.DemoBlazorApp", "wwwroot", "index.html"));
        DemoShells.Keys.ShouldContain(Path.Combine("FormCraft.DemoFluentApp", "wwwroot", "index.html"));
    }

    [Fact]
    public void Every_Demo_Shell_Should_Declare_A_Viewport_Meta()
    {
        // If this regex stopped matching a real <meta name="viewport" ...> tag, the "no disabling
        // clause" assertion below would pass vacuously — as if the barrier had been removed, when
        // really nothing was ever checked.
        var missing = DemoShells
            .Where(shell => !ViewportMeta.IsMatch(shell.Value))
            .Select(shell => shell.Key)
            .ToList();

        missing.ShouldBeEmpty(
            "no <meta name=\"viewport\" ...> tag found in: " + string.Join(", ", missing)
            + " — the zoom-suppression scan below would pass vacuously without one");
    }

    [Fact]
    public void No_Demo_Viewport_May_Disable_User_Scaling()
    {
        var offenders = DemoShells
            .Where(shell =>
            {
                var meta = ViewportMeta.Match(shell.Value).Value;
                return UserScalableNo.IsMatch(meta) || MaximumScale.IsMatch(meta);
            })
            .Select(shell => shell.Key)
            .ToList();

        offenders.ShouldBeEmpty(
            "these demo viewport metas disable pinch-zoom (`user-scalable=no` / `maximum-scale`), a "
            + "WCAG 2.1 SC 1.4.4 (Level AA) failure on iOS Safari, which honours `maximum-scale`: "
            + string.Join(", ", offenders));
    }
}
