#!/bin/bash

echo "Setting up git hooks for automatic changelog generation..."

# Create hooks directory if it doesn't exist
mkdir -p .git/hooks

# Create pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/bash

# Pre-commit hook to generate changelog before committing

echo "Generating changelog..."

# Determine which script to run based on OS
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" || "$OSTYPE" == "win32" ]]; then
    # Windows - use PowerShell
    powershell -ExecutionPolicy Bypass -File "$(git rev-parse --show-toplevel)/generate-changelog.ps1"
else
    # Unix-like systems - use bash
    bash "$(git rev-parse --show-toplevel)/generate-changelog.sh"
fi

# Check if changelog generation was successful
if [ $? -ne 0 ]; then
    echo "Error: Failed to generate changelog"
    exit 1
fi

# Add the CHANGELOG.md file to the commit if it exists and has changes
CHANGELOG_PATH="$(git rev-parse --show-toplevel)/CHANGELOG.md"
if [ -f "$CHANGELOG_PATH" ]; then
    git add "$CHANGELOG_PATH"
    echo "CHANGELOG.md has been updated and staged"
fi

exit 0
EOF

# Make hook executable
chmod +x .git/hooks/pre-commit

# Set up git alias
git config --local alias.ccommit '!f() { ./generate-changelog.sh && git add CHANGELOG.md 2>/dev/null; git commit "$@"; }; f'

echo "Git hooks have been set up successfully!"
echo ""
echo "The changelog will now be automatically generated before each commit."
echo "You can also use 'git ccommit' as an alternative to 'git commit'."
echo ""
echo "To disable automatic changelog generation, run:"
echo "  rm .git/hooks/pre-commit"