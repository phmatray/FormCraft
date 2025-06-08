# FormCraft Build System

This project uses [Nuke](https://nuke.build/) as its build automation system with [git-cliff](https://github.com/orhun/git-cliff) for changelog generation.

## Prerequisites

- .NET SDK 9.0 or later
- Git
- git-cliff (for changelog generation - auto-installed on macOS/Linux)

## Quick Start

```bash
# Run default target (Compile)
./build.sh

# Run tests
./build.sh Test

# Create NuGet packages
./build.sh Pack

# Generate changelog
./build.sh Changelog
```

## Build Targets

### Core Targets

- **Clean**: Cleans build outputs and artifacts
- **Restore**: Restores NuGet packages
- **Compile**: Builds the solution (default target)
- **Test**: Runs unit tests with test results in TRX and HTML formats
- **Pack**: Creates NuGet packages (.nupkg and .snupkg)

### Publishing & Release

- **Publish**: Publishes packages to NuGet.org
  - Only runs on main branch with release configuration
  - Requires `NUGET_API_KEY` environment variable
  - Automatically triggered by version tags (v*)
  
- **Announce**: Logs publication success with package details
  - Automatically triggered after successful publish

### Continuous Integration

- **Continuous**: Main CI target that runs Test and Pack
  - Triggers conditional publishing and changelog updates
  
- **PublishIfNeeded**: Publishes when on a version tag
  - Only runs in CI on main branch with version tags
  
- **ChangelogIfNeeded**: Updates changelog on main branch
  - Only runs in CI on main branch without version tags
  - Automatically commits changes with [skip ci]

### Changelog Management

- **Changelog**: Generates CHANGELOG.md using git-cliff
  - Uses conventional commits format
  - Groups changes by type (features, fixes, etc.)
  - Automatically installs git-cliff if not available (macOS/Linux)

## GitHub Actions Workflow

The build system generates a single, comprehensive workflow (`continuous.yml`) that:

1. **On Push/PR to main/develop**: Runs tests and creates packages
2. **On Version Tags (v*)**: Additionally publishes to NuGet
3. **On Push to main**: Updates changelog (if not a tag)

### Workflow Features

- Runs on Ubuntu latest
- Full git history fetch for changelog generation
- Automatic git-cliff setup using official GitHub Action
- Write permissions for committing changelog updates
- Caches NuGet packages for faster builds
- Conditional steps based on branch and tags
- Secure secret handling for NuGet API key

### Permissions

The workflow has `contents: write` permission enabled to allow:
- Committing and pushing changelog updates
- Creating releases (future enhancement)

## Changelog Generation with git-cliff

The project uses [git-cliff](https://github.com/orhun/git-cliff) for generating changelogs from conventional commits.

### Configuration

The changelog is configured in `cliff.toml` with:
- Conventional commit parsing
- Automatic grouping by change type
- GitHub commit links
- Breaking change highlighting

### Commit Types

| Type | Description | Group |
|------|-------------|-------|
| `feat` | New features | ✨ Features |
| `fix` | Bug fixes | 🐛 Bug Fixes |
| `docs` | Documentation | 📚 Documentation |
| `perf` | Performance improvements | ⚡ Performance |
| `refactor` | Code refactoring | ♻️ Refactor |
| `style` | Code style changes | 💄 Styling |
| `test` | Test changes | ✅ Testing |
| `chore` | Maintenance tasks | 🔧 Miscellaneous Tasks |

### Breaking Changes

Mark breaking changes with `!`:
```
feat!: new API that breaks compatibility
fix!: critical security fix with breaking changes
```

### Installation

**In GitHub Actions**: git-cliff is automatically set up using the official [git-cliff-action](https://github.com/orhun/git-cliff-action).

**For local development**: git-cliff is automatically installed when running the Changelog target on macOS and Linux. For Windows, install manually:

```powershell
# Windows (using Scoop)
scoop install git-cliff

# Or download from GitHub releases
# https://github.com/orhun/git-cliff/releases
```

## Configuration

### Environment Variables

- `NUGET_API_KEY`: Required for publishing to NuGet.org

### Parameters

```bash
# Specify configuration
./build.sh --configuration Release

# Run specific targets
./build.sh Test Pack --skip Restore
```

## Local Development

### Running Specific Targets

```bash
# Clean everything
./build.sh Clean

# Run tests
./build.sh Test

# Generate changelog locally
./build.sh Changelog
```

### Viewing Execution Plan

```bash
# Show execution plan in HTML
./build.sh --plan
```

## Troubleshooting

### Regenerate GitHub Actions Workflow

If the workflow is out of sync:
```bash
./build.sh --generate-configuration GitHubActions_continuous --host GitHubActions
```

**Note**: The workflow includes custom modifications for git-cliff installation. These are preserved in `.github/workflow-modifications.yml` and automatically applied when regenerating.

### Manual git-cliff Installation

If automatic installation fails:

```bash
# macOS
brew install git-cliff

# Linux
wget -qO- https://github.com/orhun/git-cliff/releases/latest/download/git-cliff-x86_64-unknown-linux-gnu.tar.gz | tar -xz
sudo mv git-cliff /usr/local/bin/

# Verify installation
git-cliff --version
```

### Common Issues

1. **Workflow generation errors**: Remove `.github/workflows/*.yml` and run build again
2. **Version detection**: Ensure tags follow `v*` pattern (e.g., v1.0.0)
3. **Changelog commits**: Failed with [skip ci] to prevent infinite loops
4. **git-cliff not found**: Install manually using instructions above

## Benefits of This Approach

1. **Single Workflow**: One comprehensive workflow for all CI/CD needs
2. **Professional Changelogs**: git-cliff generates well-formatted, consistent changelogs
3. **Conventional Commits**: Enforces good commit message practices
4. **Type-Safe Build**: C# build scripts with IntelliSense and compile-time checks
5. **Cross-Platform**: Works on Windows, Linux, and macOS