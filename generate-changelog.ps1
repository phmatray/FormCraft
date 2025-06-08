#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates a CHANGELOG.md file based on conventional commits and git tags.
.DESCRIPTION
    This script analyzes git history using conventional commit format and generates
    a changelog grouped by version tags. It integrates with MinVer versioning.
.EXAMPLE
    ./generate-changelog.ps1
.EXAMPLE
    ./generate-changelog.ps1 -OutputPath ./CHANGELOG.md -TagPrefix "v"
#>

param(
    [string]$OutputPath = "CHANGELOG.md",
    [string]$TagPrefix = "v",
    [string]$Repository = "https://github.com/phmatray/DynamicFormBlazor"
)

function Get-ConventionalCommitType {
    param([string]$Message)
    
    $types = @{
        'feat'     = '✨ Features'
        'fix'      = '🐛 Bug Fixes'
        'docs'     = '📚 Documentation'
        'style'    = '💄 Styles'
        'refactor' = '♻️ Code Refactoring'
        'perf'     = '⚡ Performance Improvements'
        'test'     = '✅ Tests'
        'build'    = '🏗️ Build System'
        'ci'       = '👷 Continuous Integration'
        'chore'    = '🔧 Chores'
        'revert'   = '⏪ Reverts'
    }
    
    if ($Message -match '^(\w+)(\(.+\))?!?:\s+(.+)$') {
        $type = $Matches[1]
        $scope = $Matches[2] -replace '[()]', ''
        $description = $Matches[3]
        $breaking = $Message -match '!'
        
        $category = if ($types.ContainsKey($type)) { $types[$type] } else { '📝 Other Changes' }
        
        return @{
            Type = $type
            Scope = $scope
            Description = $description
            Category = $category
            Breaking = $breaking
        }
    }
    
    return @{
        Type = 'other'
        Scope = ''
        Description = $Message
        Category = '📝 Other Changes'
        Breaking = $false
    }
}

function Get-GitTags {
    $tags = git tag --sort=-version:refname | Where-Object { $_ -match "^$TagPrefix\d+\.\d+\.\d+" }
    return $tags
}

function Get-CommitsBetween {
    param(
        [string]$FromTag,
        [string]$ToTag
    )
    
    $range = if ($FromTag) { "$FromTag..$ToTag" } else { $ToTag }
    $commits = git log $range --pretty=format:"%H|%s|%an|%ae|%ad" --date=short
    
    $parsedCommits = @()
    foreach ($commit in $commits) {
        if ($commit) {
            $parts = $commit -split '\|'
            $parsed = Get-ConventionalCommitType -Message $parts[1]
            $parsedCommits += @{
                Hash = $parts[0].Substring(0, 7)
                Message = $parts[1]
                Author = $parts[2]
                Email = $parts[3]
                Date = $parts[4]
                Type = $parsed.Type
                Scope = $parsed.Scope
                Description = $parsed.Description
                Category = $parsed.Category
                Breaking = $parsed.Breaking
            }
        }
    }
    
    return $parsedCommits
}

function Format-Changelog {
    param(
        [array]$Versions
    )
    
    $changelog = @"
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

"@

    foreach ($version in $Versions) {
        $versionNumber = $version.Tag -replace "^$TagPrefix", ""
        $changelog += "`n## [$versionNumber]"
        
        if ($version.Date) {
            $changelog += " - $($version.Date)"
        }
        $changelog += "`n"
        
        # Group commits by category
        $grouped = $version.Commits | Group-Object -Property Category
        
        # Add breaking changes first if any
        $breakingChanges = $version.Commits | Where-Object { $_.Breaking }
        if ($breakingChanges) {
            $changelog += "`n### ⚠️ BREAKING CHANGES`n"
            foreach ($commit in $breakingChanges) {
                $scopeText = if ($commit.Scope) { "**$($commit.Scope):** " } else { "" }
                $changelog += "`n* $scopeText$($commit.Description)"
            }
            $changelog += "`n"
        }
        
        # Add other changes by category
        foreach ($group in $grouped | Sort-Object Name) {
            # Skip breaking changes as they're already listed
            $commits = $group.Group | Where-Object { -not $_.Breaking }
            if ($commits) {
                $changelog += "`n### $($group.Name)`n"
                foreach ($commit in $commits) {
                    $scopeText = if ($commit.Scope) { "**$($commit.Scope):** " } else { "" }
                    $changelog += "`n* $scopeText$($commit.Description) ([{0}]({1}/commit/{2}))" -f $commit.Hash, $Repository, $commit.Hash
                }
                $changelog += "`n"
            }
        }
    }
    
    # Add links section
    $changelog += "`n"
    foreach ($version in $Versions) {
        $versionNumber = $version.Tag -replace "^$TagPrefix", ""
        $changelog += "`n[$versionNumber]: $Repository/releases/tag/$($version.Tag)"
    }
    
    return $changelog
}

# Main execution
Write-Host "Generating changelog..." -ForegroundColor Green

# Get all tags
$tags = Get-GitTags

if ($tags.Count -eq 0) {
    Write-Host "No version tags found. Creating initial changelog for unreleased changes." -ForegroundColor Yellow
    
    # Get all commits
    $commits = git log --pretty=format:"%H|%s|%an|%ae|%ad" --date=short
    $parsedCommits = @()
    
    foreach ($commit in $commits) {
        if ($commit) {
            $parts = $commit -split '\|'
            $parsed = Get-ConventionalCommitType -Message $parts[1]
            $parsedCommits += @{
                Hash = $parts[0].Substring(0, 7)
                Message = $parts[1]
                Author = $parts[2]
                Email = $parts[3]
                Date = $parts[4]
                Type = $parsed.Type
                Scope = $parsed.Scope
                Description = $parsed.Description
                Category = $parsed.Category
                Breaking = $parsed.Breaking
            }
        }
    }
    
    $versions = @(
        @{
            Tag = "Unreleased"
            Date = Get-Date -Format "yyyy-MM-dd"
            Commits = $parsedCommits
        }
    )
} else {
    $versions = @()
    
    # Add unreleased changes
    $unreleasedCommits = Get-CommitsBetween -FromTag $tags[0] -ToTag "HEAD"
    if ($unreleasedCommits) {
        $versions += @{
            Tag = "Unreleased"
            Date = Get-Date -Format "yyyy-MM-dd"
            Commits = $unreleasedCommits
        }
    }
    
    # Process each tag
    for ($i = 0; $i -lt $tags.Count; $i++) {
        $currentTag = $tags[$i]
        $previousTag = if ($i -lt $tags.Count - 1) { $tags[$i + 1] } else { $null }
        
        $tagDate = git log -1 --format=%ad --date=short $currentTag
        $commits = Get-CommitsBetween -FromTag $previousTag -ToTag $currentTag
        
        $versions += @{
            Tag = $currentTag
            Date = $tagDate
            Commits = $commits
        }
    }
}

# Generate and save changelog
$changelogContent = Format-Changelog -Versions $versions
$changelogContent | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "Changelog generated successfully at: $OutputPath" -ForegroundColor Green
Write-Host "Total versions processed: $($versions.Count)" -ForegroundColor Cyan