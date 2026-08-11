# Build and Release Setup

> **This document has moved.** FormCraft no longer releases by hand-pushing a version tag. Since
> [#197](https://github.com/phmatray/FormCraft/issues/197) the release is derived from the
> Conventional Commits on `dev` by [release-please](https://github.com/googleapis/release-please).
>
> - **How to release** → [CONTRIBUTING.md § Release Process](CONTRIBUTING.md#release-process)
> - **Build targets, versioning, workflows** → [build/README.md](build/README.md)

## Why hand-tagging no longer works

Creating and pushing a `vX.Y.Z` tag yourself will **not** produce a release any more, and will not
fail loudly either — it will simply do nothing:

- There is no longer a workflow triggered by `on: push: tags`. `release.yml` was deleted and
  `continuous.yml`'s tag trigger was removed, because publishing now happens inside
  `release-please.yml`'s own run.
- That is a requirement, not a preference: release-please creates the tag with `GITHUB_TOKEN`, and
  GitHub deliberately does not fire `on: push: tags` / `on: release` for events created by that
  token. A tag-triggered publish workflow could therefore never run again.

Release by merging the release PR that release-please keeps open. That single action produces the
version, the changelog, the tag, the GitHub Release, and both NuGet packages.
