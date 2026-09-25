using System.Globalization;
using System.Text.RegularExpressions;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Guards the demo site's MudBlazor / Fluent UI adapter selector (#490) at the level of the static
/// files it ships: the Fluent accent tokens in <c>tokens.css</c> must cover every accent token the
/// default palette defines and keep each text/background pair at WCAG AA.
/// </summary>
/// <remarks>
/// The demo app has no bUnit project, so the interactive behaviour is checked in a real browser; what
/// can be pinned from disk is pinned here. <see cref="Default_Palette_Should_Yield_The_Accent_Tokens" />
/// is the non-vacuity guard (the same shape as <see cref="DemoViewportTests" />): if the parser stopped
/// matching, "every accent token is overridden" would pass over an empty set.
/// </remarks>
public class DemoAdapterThemeTests
{
    private const string FluentSelector = ":root[data-adapter=\"fluentui\"]";

    private static readonly string DemoRoot = Path.Combine(WorkflowSource.RepoRoot, "FormCraft.DemoBlazorApp", "wwwroot");

    private static readonly string TokensCss = File.ReadAllText(Path.Combine(DemoRoot, "css", "tokens.css"));

    [Fact]
    public void Default_Palette_Should_Yield_The_Accent_Tokens()
    {
        AccentKeys(ReadTokens(":root")).Count.ShouldBeGreaterThanOrEqualTo(8);
    }

    [Fact]
    public void Fluent_Block_Overrides_Every_Accent_Token()
    {
        var fluent = ReadTokens(FluentSelector);

        AccentKeys(ReadTokens(":root")).Where(key => !fluent.ContainsKey(key)).ShouldBeEmpty(
            "every accent token must be overridden, or that accent stays MudBlazor violet under Fluent UI");
    }

    [Theory]
    [InlineData("--fc-violet-ink", "--fc-bg")]
    [InlineData("#fff", "--fc-violet")]
    [InlineData("#fff", "--fc-violet-hover")]
    [InlineData("--fc-violet-onwhite", "#fff")]
    [InlineData("--fc-on-violet-2", "--fc-violet-slab")]
    public void Fluent_Accent_Pairs_Meet_WCAG_AA(string foreground, string background)
    {
        // The Fluent block only overrides accents; everything else (e.g. --fc-bg) comes from :root.
        var tokens = new Dictionary<string, string>(ReadTokens(":root"));
        foreach (var (key, value) in ReadTokens(FluentSelector))
        {
            tokens[key] = value;
        }

        string Resolve(string token) => token.StartsWith('#') ? token : tokens[token];

        ContrastRatio(Resolve(foreground), Resolve(background)).ShouldBeGreaterThanOrEqualTo(
            4.5, $"{foreground} on {background} under Fluent UI");
    }

    [Fact]
    public void Index_Sets_Adapter_Attribute_Before_Blazor_Boots()
    {
        var index = File.ReadAllText(Path.Combine(DemoRoot, "index.html"));
        var boot = index.IndexOf("blazor.webassembly", StringComparison.Ordinal);
        boot.ShouldBeGreaterThan(0, "index.html no longer loads blazor.webassembly.js");

        // A pre-boot script is what keeps the first paint from being violet under Fluent UI (AC3).
        var script = Regex.Matches(index[..boot], @"<script[^>]*>(?<body>.*?)</script>", RegexOptions.Singleline)
            .Select(m => m.Groups["body"].Value)
            .SingleOrDefault(body => body.Contains("fc-adapter", StringComparison.Ordinal)
                && body.Contains("dataset.adapter", StringComparison.Ordinal));

        script.ShouldNotBeNull("no inline script reading `fc-adapter` into `dataset.adapter` before Blazor boots");
        script.ShouldContain("try", Case.Sensitive, "localStorage can throw (blocked site data) and must not break boot");
        script.ShouldContain("'fluentui'", Case.Sensitive, "only the literal value may reach the attribute");
    }

    [Theory]
    [InlineData("### 1. Install the Package")]
    [InlineData("### 2. Configure Services")]
    [InlineData("### 3. Add Required References")]
    public void Getting_Started_Carries_Both_Adapters_For_Each_Setup_Section(string heading)
    {
        var doc = File.ReadAllText(Path.Combine(DemoRoot, "docs", "getting-started.md")).ReplaceLineEndings("\n");
        var start = doc.IndexOf(heading + "\n", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, $"getting-started.md lost its `{heading}` section");
        var end = doc.IndexOf("\n#", start + heading.Length, StringComparison.Ordinal);
        var section = end < 0 ? doc[start..] : doc[start..end];

        var mud = Regex.Matches(section, @"^::: adapter-mudblazor\n(?<body>.*?)^:::$", RegexOptions.Multiline | RegexOptions.Singleline);
        mud.Count.ShouldBe(1);
        var fluent = Regex.Matches(section, @"^::: adapter-fluentui\n(?<body>.*?)^:::$", RegexOptions.Multiline | RegexOptions.Singleline);
        fluent.Count.ShouldBe(1);

        // An unclosed MudBlazor container would nest the Fluent one inside it, hiding both under Fluent UI.
        mud[0].Groups["body"].Value.ShouldNotContain("::: adapter-");
        fluent[0].Groups["body"].Value.ShouldNotContain("::: adapter-");

        var body = fluent[0].Groups["body"].Value;
        body.ShouldNotContain("AddMudServices");
        body.ShouldNotContain("MudBlazor.min.css");
    }

    [Fact]
    public void Getting_Started_Fluent_Setup_Uses_The_Real_Adapter_Surface()
    {
        var doc = File.ReadAllText(Path.Combine(DemoRoot, "docs", "getting-started.md")).ReplaceLineEndings("\n");
        var fluent = string.Concat(Regex.Matches(doc, @"^::: adapter-fluentui\n(?<body>.*?)^:::$", RegexOptions.Multiline | RegexOptions.Singleline)
            .Select(m => m.Groups["body"].Value));

        fluent.ShouldContain("dotnet add package FormCraft.ForFluentUI");
        fluent.ShouldContain("AddFormCraftFluentUI()");
        fluent.ShouldContain("using FormCraft.ForFluentUI.Extensions;");
        fluent.ShouldContain("_content/Microsoft.FluentUI.AspNetCore.Components/css/reboot.css");
        fluent.ShouldContain("_content/FormCraft.ForFluentUI/css/formcraft-layout.css");
    }

    private static List<string> AccentKeys(IReadOnlyDictionary<string, string> tokens) =>
        tokens.Keys.Where(key => key.StartsWith("--fc-violet", StringComparison.Ordinal) || key == "--fc-on-violet-2").ToList();

    /// <summary>The custom properties declared in the first rule whose selector is exactly <paramref name="selector" />.</summary>
    private static IReadOnlyDictionary<string, string> ReadTokens(string selector)
    {
        var rule = new Regex(@"(?:^|\})\s*" + Regex.Escape(selector) + @"\s*\{(?<body>[^}]*)\}", RegexOptions.Multiline)
            .Match(TokensCss);
        rule.Success.ShouldBeTrue($"no `{selector} {{ … }}` rule in tokens.css");

        return Regex.Matches(rule.Groups["body"].Value, @"(?<key>--[\w-]+)\s*:\s*(?<value>[^;]+);")
            .ToDictionary(m => m.Groups["key"].Value, m => m.Groups["value"].Value.Trim(), StringComparer.Ordinal);
    }

    /// <summary>WCAG 2.x contrast ratio between two <c>#rgb</c> / <c>#rrggbb</c> colours.</summary>
    private static double ContrastRatio(string hexA, string hexB)
    {
        var a = Luminance(hexA);
        var b = Luminance(hexB);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double Luminance(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        }

        double Channel(int offset)
        {
            var c = int.Parse(hex.Substring(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(0)) + (0.7152 * Channel(2)) + (0.0722 * Channel(4));
    }
}
