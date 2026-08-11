using FormCraft.Build;

namespace FormCraft.UnitTests.Ci;

/// <summary>
/// Pins the rule that decides whether a git tag is a release tag (#227).
/// </summary>
/// <remarks>
/// <para>
/// <c>BuildVersioning.IsVersionTag</c> is the static gate behind Nuke's <c>PublishIfNeeded</c>
/// (<c>OnlyWhenStatic(() =&gt; IsOnVersionTag())</c>). It runs a handful of times a year, on the one
/// path where being wrong is expensive in both directions: too strict and a release publishes
/// nothing while reporting green; too loose and a stray tag pushes to nuget.org, which delists but
/// never deletes.
/// </para>
/// <para>
/// The source lives in <c>build/BuildVersioning.cs</c> and is <b>linked</b> into this project rather
/// than referenced, deliberately: a <c>ProjectReference</c> to <c>_build.csproj</c> would pull Nuke
/// and its whole dependency graph into the library test assemblies.
/// </para>
/// </remarks>
public class VersionTagRuleTests
{
    [Theory]
    [InlineData("v3.1.0")]                 // the ordinary release tag
    [InlineData("v10.20.30")]              // multi-digit components
    [InlineData("v0.0.1")]                 // zeros are legal semver components
    [InlineData("v3.1.1-rc.1")]            // prerelease rehearsal — #198 depends on this publishing
    [InlineData("v4.0.0-preview.2")]
    [InlineData("v1.0.0+build.5")]         // build metadata is legal semver
    [InlineData("v1.0.0-rc.1+build.5")]
    public void Should_Accept_A_Release_Tag(string tag)
    {
        BuildVersioning.IsVersionTag(tag).ShouldBeTrue();
    }

    [Fact]
    public void Should_Accept_A_Nonsense_Prerelease_Label()
    {
        // #227 Task 1, decided deliberately rather than by omission. `-donotship` is a *legal*
        // semver prerelease identifier, so anchoring to semver does not exclude it; excluding it
        // would need an allow-list of labels (rc, preview, beta...).
        //
        // No allow-list, for two reasons. Since #197 nothing publishes off a pushed tag —
        // release-please creates the tag and runs the publish in the same job — so this predicate is
        // defence in depth, not the control that decides to release; an allow-list would look like a
        // safety net while the real one lives elsewhere. And it answers the wrong question: "is this
        // a release tag" is decidable from the string, "did the human mean it" is not, and guessing
        // would break the first rehearsal to use an unforeseen label.
        BuildVersioning.IsVersionTag("v1.0.0-donotship").ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("3.1.0")]                  // no `v` prefix — MinVerTagPrefix is `v`
    [InlineData("v3.1")]                   // incomplete
    [InlineData("v3")]
    [InlineData("vNext")]
    [InlineData("release-3.1.0")]
    [InlineData("v3.1.0-")]                // empty prerelease is not legal semver
    public void Should_Reject_A_Tag_That_Is_Not_A_Version(string? tag)
    {
        BuildVersioning.IsVersionTag(tag).ShouldBeFalse();
    }

    [Theory]
    [InlineData("v1.0.0.0")]               // four components is not a version tag
    [InlineData("v1.0.0garbage")]          // a suffix that is not a legal prerelease
    [InlineData("v01.0.0")]                // leading zeros are not semver
    [InlineData("v1.0.0_hotfix")]          // `_` is not legal in a semver identifier
    public void Should_Reject_A_Malformed_Tag_That_The_Unanchored_Rule_Used_To_Accept(string tag)
    {
        // These are the cases #227 changed. The missing `$` in the old `^v\d+\.\d+\.\d+` was
        // deliberate for *prerelease* suffixes — that is what lets a rehearsal exercise the real
        // publish path — but it accepted these too, which nobody chose. Looseness by accident,
        // riding along with looseness by design.
        BuildVersioning.IsVersionTag(tag).ShouldBeFalse();
    }

    [Theory]
    [InlineData("v3.1.0\n")]
    [InlineData("  v3.1.0  ")]
    public void Should_Tolerate_Surrounding_Whitespace(string tag)
    {
        // The predicate is fed by `git describe --exact-match --tags HEAD`. A stray newline would
        // pass the old prefix-only pattern and fail a fully anchored one — the single way anchoring
        // could have broken a real release, so it is trimmed and pinned here.
        BuildVersioning.IsVersionTag(tag).ShouldBeTrue();
    }
}
