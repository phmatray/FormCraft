# Contributing to FormCraft

Thank you for your interest in contributing to FormCraft! This document provides guidelines and instructions for contributing.

🌐 **Try the [live demo](https://phmatray.github.io/FormCraft/)** to explore the library's capabilities before contributing.

## Code of Conduct

By participating in this project, you are expected to uphold our Code of Conduct:
- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Respect differing viewpoints and experiences

## How to Contribute

### Reporting Issues

- Check if the issue already exists
- Include a clear title and description
- Provide steps to reproduce the issue
- Include code samples if applicable
- Mention your environment (OS, .NET version, etc.)

### Suggesting Features

- Open an issue with the "enhancement" label
- Clearly describe the feature and its benefits
- Provide use cases and examples
- Be open to discussion and feedback

### Pull Requests

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add or update tests as needed
5. Ensure all tests pass (`dotnet test`)
6. Update documentation if needed
7. Commit your changes (`git commit -m 'Add amazing feature'`)
8. Push to your branch (`git push origin feature/amazing-feature`)
9. Open a Pull Request

## Development Setup

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git

### Building the Project

```bash
# Clone the repository
git clone https://github.com/phmatray/DynamicFormBlazor.git
cd DynamicFormBlazor

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Creating a Local Package

```bash
# Windows
./pack-local.ps1

# macOS/Linux
./pack-local.sh
```

## Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation to public APIs
- Keep methods small and focused
- Write unit tests for new functionality
- Maintain existing code style

## Testing

- Write unit tests for all new code
- Ensure existing tests continue to pass
- Aim for high code coverage
- Test edge cases and error conditions
- Use the existing test patterns

## Versioning

This project follows [Semantic Versioning](https://semver.org/), and **the version is never chosen
by hand**. [release-please](https://github.com/googleapis/release-please) derives it from the
Conventional Commits that have landed on `dev` and creates the `vX.Y.Z` tag when the release PR is
merged; [MinVer](https://github.com/adamralph/minver) then reads that tag to stamp the assemblies and
packages. The tag is the single source of truth — no file in the repository records the version.

Between releases MinVer produces height-based pre-release versions (e.g. `3.1.1-preview.4`) from your
local commits, which is what you will see when you build or pack locally.

## Commit Messages and Changelog

### Conventional Commits

We use [Conventional Commits](https://www.conventionalcommits.org/) for commit messages:
- `feat:` for new features
- `fix:` for bug fixes
- `docs:` for documentation changes
- `style:` for formatting changes
- `refactor:` for code refactoring
- `test:` for test additions/changes
- `chore:` for maintenance tasks

Examples:
```
feat: add field groups layout support
fix: resolve null reference in field renderer
docs: update API documentation
```

### Your PR title matters more than your commit messages

Pull requests are **squash-merged**, so the **PR title** — not the individual commits on your branch —
becomes the commit message on `dev`, and that is what release-please parses. A PR titled
`feat(mudblazor): add a colour-picker field (#123)` produces a minor bump and a changelog entry; a PR
titled `Update stuff` produces neither, silently.

`.github/workflows/pr-title-lint.yml` checks this on every non-draft PR, so a non-conforming title is
caught before merge rather than quietly dropping your change out of the next release.

The full convention is `<type>(<scope>): <subject> (#<issue>)`.

### The changelog is generated — never edit it

`CHANGELOG.md` is owned entirely by release-please, which rewrites it in the standing release PR.

- **Do not hand-edit `CHANGELOG.md`**, and do not add an entry for your change — your PR title is the
  entry.
- Nothing in the build generates it either. git-cliff, `cliff.toml`, the Nuke `GenerateChangelog`
  target and the changelog pre-commit hook were all removed: with release-please owning the file, a
  second generator would rewrite it out from under the open release PR.

#### If you ever ran `./setup-hooks.sh`, remove the hook

Those installer scripts are gone, but they are not self-uninstalling — a hook they wrote is still in
your local clone, and it still tries to regenerate and `git add` `CHANGELOG.md` on every commit. That
is a second writer to a file release-please now owns. Check and remove it:

```bash
cat .git/hooks/pre-commit     # if it mentions generate-changelog, remove it:
rm .git/hooks/pre-commit
```

(`git commit --no-verify` skips it for a single commit, but removing it is the fix.)

## Documentation

- Update XML documentation for public APIs
- Update README.md if adding features
- Add examples for new functionality
- Keep documentation clear and concise

## Release Process

Releasing is one action: **merge the release PR**. Nothing is tagged by hand.

1. Every push to `dev` runs `.github/workflows/release-please.yml`, which keeps a release PR open
   showing the next version and the changelog it would publish. If nothing releasable has landed
   (for example only `chore(deps):` commits), no PR is opened — that is correct.
2. A maintainer reviews that PR and merges it.
3. release-please tags `vX.Y.Z` and creates the GitHub Release.
4. In the same workflow run, the `nupkg` job checks out the new tag — MinVer resolves the version
   from it — and publishes `FormCraft` and `FormCraft.ForMudBlazor` to NuGet.org via Trusted
   Publishing (OIDC, a short-lived key; no long-lived secret), then attaches the packages to the
   release.

Publishing lives in that same workflow run by necessity: release-please creates the tag with
`GITHUB_TOKEN`, and GitHub does not fire `on: push: tags` for events created by that token, so a
tag-triggered publish workflow would never run.

### If the release run fails

The tag and the GitHub Release are created **before** the `nupkg` job builds anything, so a failure
there leaves a tagged, announced release with no packages attached.

Recover with **“Re-run failed jobs”**. Do *not* use “Re-run all jobs”: that re-runs release-please,
which sees the release already exists, reports `release_created: false`, and skips the `nupkg` job
entirely — so nothing is retried and the failure looks resolved. Pushing to NuGet is idempotent
(`--skip-duplicate`), so re-running the publish is always safe.

### Forcing a release when nothing releasable has landed

Only `feat`, `fix`, `perf`, `refactor` and `revert` commits produce a release; `chore`, `ci`, `docs`,
`style`, `test` and `build` are hidden and do not trigger one. That is intentional — it matches what
the previous git-cliff config did — but it means a period of pure `chore(deps):` updates opens no
release PR, even if one of those bumps matters (a shipped runtime dependency, say).

Hand-tagging is no longer an escape hatch. To force a release, add a `Release-As:` footer to the
squashed commit, e.g. a PR whose body ends with:

```
Release-As: 3.2.1
```

## Questions?

Feel free to:
- Open an issue for questions
- Start a discussion in GitHub Discussions
- Contact the maintainers

Thank you for contributing to FormCraft!