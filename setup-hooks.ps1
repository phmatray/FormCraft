Write-Host "Setting up git hooks for automatic changelog generation..." -ForegroundColor Green

# Create hooks directory if it doesn't exist
New-Item -ItemType Directory -Force -Path ".git/hooks" | Out-Null

# Create pre-commit hook content
$hookContent = @'
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
'@

# Write the hook file
Set-Content -Path ".git/hooks/pre-commit" -Value $hookContent -Encoding UTF8

Write-Host "Pre-commit hook created at .git/hooks/pre-commit" -ForegroundColor Yellow

Write-Host "`nGit hooks have been set up successfully!" -ForegroundColor Green
Write-Host "`nThe changelog will now be automatically generated before each commit."
Write-Host "This works with ANY Git interface including:" -ForegroundColor Cyan
Write-Host "  - JetBrains Rider"
Write-Host "  - Visual Studio"
Write-Host "  - VS Code" 
Write-Host "  - Command line"
Write-Host "`nThe current commit is excluded from the changelog to avoid the 'one commit ahead' problem." -ForegroundColor Yellow
Write-Host "`nTo disable automatic changelog generation, run:" -ForegroundColor Yellow
Write-Host "  Remove-Item .git/hooks/pre-commit"