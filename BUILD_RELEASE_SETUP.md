# Build and Release Setup

This document describes the automated build and release process for FormCraft.

## Overview

The NUKE build system has been enhanced to automatically:
1. Push NuGet packages to NuGet.org
2. Create GitHub releases with changelog
3. Upload NuGet packages as release assets

## Release Process

When you push a version tag (e.g., `v1.0.0`) to the main branch:

1. **CI/CD Pipeline** runs automatically
2. **Tests** are executed
3. **NuGet packages** are created
4. **Packages are published** to NuGet.org
5. **GitHub release** is created with:
   - Changelog generated from commit history (using git-cliff)
   - NuGet packages attached as assets
   - Proper version tagging

## Configuration Requirements

### GitHub Repository Secrets

Ensure these secrets are configured in your GitHub repository:

- `NUGET_USER`: Your nuget.org **profile name** — the *Package Owner* named by the Trusted
  Publishing policy. This is not a credential; it only tells `NuGet/login` whose policy to match.
- `GITHUB_TOKEN`: Already available by default in GitHub Actions

> **No long-lived NuGet API key is stored.** Publishing uses
> [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing): on a
> `v*` tag, `continuous.yml` exchanges GitHub's OIDC token for a nuget.org key valid about an hour
> and passes it to the build as the `NUGET_API_KEY` *environment variable*. Do **not** create a
> `NUGET_API_KEY` repository secret — it would reintroduce exactly the standing credential this
> setup exists to avoid. A matching policy must exist on nuget.org (Account settings → Trusted
> Publishing) naming this repository and the workflow file `continuous.yml`; nuget.org does not
> validate that filename, so a typo there is accepted silently and fails only at the first tag.

### Permissions

The GitHub Actions workflow requires:
- `contents: write` - For creating releases
- `packages: write` - For publishing packages
- `id-token: write` - For requesting the OIDC token that Trusted Publishing exchanges for a key

## How to Create a Release

1. **Update version** in your project
2. **Commit changes** to main branch
3. **Create and push a version tag**:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

The CI/CD pipeline will automatically:
- Build and test the project
- Create NuGet packages
- Push to NuGet.org
- Create a GitHub release with changelog

## Build Targets

- `Continuous`: Runs on every push/PR (build, test, pack)
- `Publish`: Publishes to NuGet.org (requires tag on main branch)
- `CreateGitHubRelease`: Creates GitHub release (triggered after publish)

## Changelog Generation

Changelogs are generated using git-cliff based on conventional commits:
- `feat:` - Features
- `fix:` - Bug fixes
- `docs:` - Documentation
- `chore:` - Maintenance

## Local Testing

To test the build locally:
```bash
./build.sh Compile
./build.sh Test
./build.sh Pack
```

Note: Publishing requires proper credentials and should only be done via CI/CD.