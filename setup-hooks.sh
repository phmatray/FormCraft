#!/bin/bash

echo "Setting up git hooks for automatic changelog generation..."

# Create hooks directory if it doesn't exist
mkdir -p .git/hooks

# Create pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/bash

# Pre-commit hook to generate changelog
# This version excludes the current commit to avoid "one commit ahead" problem

echo "Generating changelog (excluding current commit)..."

# Get the repository root
REPO_ROOT="$(git rev-parse --show-toplevel)"

# Generate changelog
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    # Windows - use PowerShell
    powershell -ExecutionPolicy Bypass -File "$REPO_ROOT/generate-changelog.ps1"
else
    # Unix-like systems - use bash
    bash "$REPO_ROOT/generate-changelog.sh"
fi

# Check if changelog generation was successful
if [ $? -ne 0 ]; then
    echo "Warning: Failed to generate changelog, continuing anyway..."
    exit 0
fi

# Check if changelog was modified
cd "$REPO_ROOT"
if ! git diff --quiet --cached CHANGELOG.md || ! git diff --quiet CHANGELOG.md; then
    # Stage the changelog
    git add CHANGELOG.md
    echo "CHANGELOG.md has been updated and staged"
fi

exit 0
EOF

# Make hook executable
chmod +x .git/hooks/pre-commit

echo "Git hooks have been set up successfully!"
echo ""
echo "The changelog will now be automatically generated before each commit."
echo "This works with ANY Git interface including:"
echo "  - JetBrains Rider"
echo "  - Visual Studio"
echo "  - VS Code"
echo "  - Command line"
echo ""
echo "The current commit is excluded from the changelog to avoid the 'one commit ahead' problem."
echo ""
echo "To disable automatic changelog generation, run:"
echo "  rm .git/hooks/pre-commit"