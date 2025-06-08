# Contributing to FormCraft

Thank you for your interest in contributing to FormCraft! This document provides guidelines and instructions for contributing.

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

This project uses [MinVer](https://github.com/adamralph/minver) for semantic versioning:
- Tag releases with `v` prefix (e.g., `v1.0.0`)
- Pre-release versions are automatically generated
- Follow [Semantic Versioning](https://semver.org/)

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

### Automatic Changelog Generation

The changelog is automatically generated from commit messages before each commit.

#### Setup (one-time)
```bash
# macOS/Linux
./setup-hooks.sh

# Windows
./setup-hooks.ps1
```

This installs a pre-commit hook that:
- Generates the changelog based on existing commits
- Excludes the current commit to avoid the "one commit ahead" problem
- Automatically stages the updated CHANGELOG.md
- Works with ANY Git interface (Rider, VS, VS Code, CLI, etc.)

#### How it Works

When you commit (from any interface):
1. The pre-commit hook runs automatically
2. Changelog is generated from existing commits
3. Updated CHANGELOG.md is staged with your commit
4. Your commit proceeds normally

#### Manual Generation
If you need to generate the changelog manually:
```bash
# macOS/Linux
./generate-changelog.sh

# Windows
./generate-changelog.ps1
```

#### Disable Auto-Generation
To temporarily skip changelog generation:
```bash
git commit --no-verify
```

To permanently disable:
```bash
rm .git/hooks/pre-commit
```

The changelog follows the [Keep a Changelog](https://keepachangelog.com/) format.

## Documentation

- Update XML documentation for public APIs
- Update README.md if adding features
- Add examples for new functionality
- Keep documentation clear and concise

## Release Process

Releases are automated through GitHub Actions:
1. Maintainers create a new tag (e.g., `v1.1.0`)
2. CI/CD pipeline runs tests
3. Package is automatically published to NuGet.org

## Questions?

Feel free to:
- Open an issue for questions
- Start a discussion in GitHub Discussions
- Contact the maintainers

Thank you for contributing to FormCraft!