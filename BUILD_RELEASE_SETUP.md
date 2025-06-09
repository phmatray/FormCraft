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

- `NUGET_API_KEY`: Your NuGet.org API key for package publishing
- `GITHUB_TOKEN`: Already available by default in GitHub Actions

### Permissions

The GitHub Actions workflow requires:
- `contents: write` - For creating releases
- `packages: write` - For publishing packages

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