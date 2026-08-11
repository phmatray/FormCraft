# FormCraft Build System

This project uses [Nuke](https://nuke.build/) as its build automation system.

Nuke builds, tests, packs and publishes. It does **not** decide *when* to release and it does **not**
generate the changelog — [release-please](https://github.com/googleapis/release-please) owns both.
See [CONTRIBUTING.md](../CONTRIBUTING.md#release-process) for the release flow.

## Prerequisites

- .NET SDK `10.0.302` or later (pinned in `global.json`, `rollForward: latestFeature`)
- Git — with **full history**, because MinVer derives the version from tags (`fetch-depth: 0` in CI)

## Quick Start

```bash
# Run default target (Compile)
./build.sh

# Run tests
./build.sh Test

# Create NuGet packages
./build.sh Pack
```

On Windows use `build.cmd` / `build.ps1`; `build.cmd` also works on macOS and Linux (it shells out to
`build.sh`), which is why the CI workflows call `./build.cmd`.

## Build Targets

### Core

- **Clean** — cleans build outputs and artifacts
- **Restore** — restores NuGet packages
- **Compile** — builds the solution (default target)
- **Test** — runs the unit tests, writing TRX and HTML results to `test-results/`
- **Pack** — creates the NuGet packages (`.nupkg` + `.snupkg`) in `artifacts/`, and mirrors the
  committed root `CHANGELOG.md` into the two package directories so it ships inside the package

### Publishing

- **Publish** — pushes the packages to NuGet.org. Requires `NUGET_API_KEY`, a Release configuration,
  and a version tag / main / release branch. Uses `--skip-duplicate`, so re-running a release is
  idempotent.
- **Announce** — logs the published package details; triggered after a successful `Publish`
- **PublishIfNeeded** — the CI gate in front of `Publish`:
  `OnlyWhenStatic(IsOnVersionTag() && IsServerBuild)`. This is what makes the release workflow thin —
  it checks out the tag, so the gate is satisfied without any conditional logic in the workflow.
- **Continuous** — `DependsOn(Test, Pack)`, triggering `PublishIfNeeded`. Invoked by both
  `continuous.yml` (where no ref is ever a version tag, so nothing publishes) and by
  `release-please.yml`'s `nupkg` job (where the tag *is* checked out, so it publishes).
- **Release** — informational only; logs the version it would release

## Versioning

Versions come from [MinVer](https://github.com/adamralph/minver), which reads the nearest `v*` git
tag (`MinVerTagPrefix=v` in `Directory.Build.props`). No file in the repository records the version,
and `Pack` deliberately passes no `/p:Version` — MinVer's targets would override it anyway.

Between releases you get height-based pre-release versions such as `3.1.1-preview.4`.

The `vX.Y.Z` tag itself is created by release-please when the release PR is merged. That is also why
`release-please-config.json` sets `include-component-in-tag: false`: a component tag
(`formcraft-v3.2.0`) would not match `MinVerTagPrefix=v`, MinVer would find no tag at all, and both
packages would pack as `0.0.0-alpha.0.N`.

## Changelog

`CHANGELOG.md` is generated and owned by release-please, in the standing release PR. **Nothing in
this build generates it** — the former `GenerateChangelog` target, `cliff.toml` and the git-cliff
dependency were removed, because a second generator would rewrite the file out from under the open
release PR. `Pack` only *copies* the committed file into the package directories.

Do not hand-edit `CHANGELOG.md`. Your PR title is the changelog entry.

## GitHub Actions

| Workflow | Trigger | What it does |
|---|---|---|
| `ci.yml` | push / PR | `./build.cmd Test` |
| `continuous.yml` | push to `main`/`dev`, PRs | `./build.cmd Continuous` — build, test, pack. **Never publishes**: no ref it builds is a version tag. |
| `release-please.yml` | push to `dev`, `workflow_dispatch` | keeps the release PR open; on merge tags the release and, in the same run, publishes both packages via Trusted Publishing |
| `pr-title-lint.yml` | `pull_request_target` | rejects a PR title that is not a Conventional Commit |

The publish lives inside `release-please.yml` by necessity: release-please creates the tag with
`GITHUB_TOKEN`, and GitHub does not fire `on: push: tags` for events created by that token, so a
tag-triggered publish workflow would silently never run.

> The `[GitHubActions]` attribute on `Build` has `AutoGenerate = false`. The workflow files are
> maintained by hand; regenerating them from the attribute would overwrite that.

## Configuration

### Environment variables

- `NUGET_API_KEY` — required only by `Publish`. In CI this is **not** a stored secret:
  `release-please.yml` obtains a key valid about an hour from NuGet
  [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) via GitHub's
  OIDC token and passes it in through this variable, so the build needs no change. Only a local,
  manual publish uses a real API key you supply yourself.
- `NUGET_USER` — repository secret holding the nuget.org profile name, used by the OIDC login step.

### Parameters

```bash
# Specify configuration
./build.sh --configuration Release

# Run specific targets
./build.sh Test Pack --skip Restore

# Show the execution plan in HTML
./build.sh --plan
```

## Troubleshooting

1. **Packages version as `0.0.0-alpha…`** — MinVer found no tag. Check out the tag itself and fetch
   full history (`fetch-depth: 0`); a shallow clone has no tags.
2. **Version detection** — tags must follow the `v*` pattern (e.g. `v1.0.0`).
3. **A build warning fails the build** — that is deliberate: `Directory.Build.props` sets
   `TreatWarningsAsErrors=true`. Fix the warning rather than relaxing the setting.
