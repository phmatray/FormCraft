using System.Text.RegularExpressions;

namespace FormCraft.Build;

/// <summary>
/// The rule that decides whether a git tag is a release tag, extracted from <c>Build.IsOnVersionTag</c>
/// so it can be tested without instantiating Nuke's <c>Build</c> (#227).
/// </summary>
/// <remarks>
/// This file is <b>linked</b> into <c>FormCraft.UnitTests</c> rather than referenced. A
/// <c>ProjectReference</c> to <c>_build.csproj</c> would drag Nuke and its whole dependency graph
/// into the library test assemblies, so it deliberately depends on nothing but
/// <c>System.Text.RegularExpressions</c> — keep it that way.
/// </remarks>
internal static class BuildVersioning
{
    /// <summary>
    /// Matches a release tag: <c>v</c> followed by a strict semver version, with optional prerelease
    /// and build-metadata parts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The prerelease part is <b>deliberately permitted</b>, and that is the load-bearing looseness:
    /// it is what lets a prerelease tag exercise the real publish path, which #198's rehearsal
    /// depends on. A rule that accepted only <c>vX.Y.Z</c> would make every rehearsal a no-op that
    /// still reported success — the "green run, nothing published" failure this repo keeps designing
    /// against. ⛔ Do not restrict this to final versions.
    /// </para>
    /// <para>
    /// It is, however, <b>anchored</b> at both ends. The original <c>^v\d+\.\d+\.\d+</c> had no
    /// <c>$</c>, which bought the prerelease case above but also accepted <c>v1.0.0.0</c>,
    /// <c>v1.0.0garbage</c> and <c>v01.0.0</c> — looseness nobody chose, riding along with the
    /// looseness that was intended (#227).
    /// </para>
    /// <para>
    /// A nonsense-but-legal prerelease label such as <c>v1.0.0-donotship</c> still matches, by
    /// decision rather than omission. Excluding it would need an allow-list of labels, and that is
    /// declined on purpose: since #197 nothing publishes off a pushed tag (release-please creates the
    /// tag and runs the publish in the same job), so this predicate is defence in depth rather than
    /// the control that decides to release — an allow-list would look like a safety net while the
    /// real one lives elsewhere, and would break the first rehearsal to use an unforeseen label.
    /// </para>
    /// </remarks>
    private static readonly Regex VersionTagPattern = new(
        @"^v(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)"
        + @"(?:-((?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*))?"
        + @"(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Whether <paramref name="tag"/> is a release tag that may publish.
    /// </summary>
    /// <param name="tag">
    /// The tag to test — typically the output of <c>git describe --exact-match --tags HEAD</c>, or
    /// null/empty when HEAD carries no tag.
    /// </param>
    /// <remarks>
    /// Trimmed before matching: the caller feeds this straight from git's stdout, and a trailing
    /// newline would fail an anchored pattern while passing the old prefix-only one. That is the one
    /// way anchoring could have broken a real release, so it is handled here rather than left to
    /// every caller.
    /// </remarks>
    internal static bool IsVersionTag(string? tag) =>
        !string.IsNullOrWhiteSpace(tag) && VersionTagPattern.IsMatch(tag.Trim());
}
