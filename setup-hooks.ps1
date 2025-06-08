Write-Host "Setting up git hooks for automatic changelog generation..." -ForegroundColor Green

# Create hooks directory if it doesn't exist
New-Item -ItemType Directory -Force -Path ".git/hooks" | Out-Null

# Create pre-commit hook content
$hookContent = @'
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
'@

# Write the hook file
Set-Content -Path ".git/hooks/pre-commit" -Value $hookContent -Encoding UTF8

# Note: On Windows, the hook will work with Git Bash
Write-Host "Pre-commit hook created at .git/hooks/pre-commit" -ForegroundColor Yellow

# Set up git alias
git config --local alias.ccommit '!powershell -ExecutionPolicy Bypass -File ./generate-changelog.ps1 && git add CHANGELOG.md 2>$null; git commit'

Write-Host "`nGit hooks have been set up successfully!" -ForegroundColor Green
Write-Host "`nThe changelog will now be automatically generated before each commit."
Write-Host "You can also use 'git ccommit' as an alternative to 'git commit'."
Write-Host "`nTo disable automatic changelog generation, run:" -ForegroundColor Yellow
Write-Host "  Remove-Item .git/hooks/pre-commit"