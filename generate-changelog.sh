#!/bin/bash

# Generate CHANGELOG.md based on conventional commits and git tags
# Compatible with MinVer versioning

OUTPUT_PATH="${1:-CHANGELOG.md}"
TAG_PREFIX="${2:-v}"
REPOSITORY="${3:-https://github.com/phmatray/DynamicFormBlazor}"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color


# Function to parse conventional commit type
get_conventional_commit_type() {
    local message="$1"
    local type=""
    local scope=""
    local description=""
    local category=""
    local breaking=false
    
    # Check for breaking change
    if [[ "$message" == *"!"*":"* ]]; then
        breaking=true
    fi
    
    # Parse conventional commit - must match PowerShell behavior exactly
    # PowerShell regex: ^(\w+)(\(.+\))?!?:\s+(.+)$
    # Check if message has colon (required for conventional commit)
    if [[ "$message" =~ :\ .+ ]]; then
        # Extract type (everything before colon/scope/bang)
        if [[ "$message" =~ ^([a-z]+) ]]; then
            type="${BASH_REMATCH[1]}"
        fi
        
        # Extract scope if present using sed
        if echo "$message" | grep -qE '^[a-z]+\([^)]*\)'; then
            scope=$(echo "$message" | sed -n 's/^[a-z]*(\([^)]*\)).*/\1/p')
        fi
        
        # Extract description after colon
        if [[ "$message" =~ :\ (.+)$ ]]; then
            description="${BASH_REMATCH[1]}"
        fi
    else
        # Not a conventional commit (no colon) - treat as other
        type="other"
        description="$message"
    fi
    
    # Map type to category
    case "$type" in
        feat)
            category="✨ Features"
            ;;
        fix)
            category="🐛 Bug Fixes"
            ;;
        docs)
            category="📚 Documentation"
            ;;
        style)
            category="💄 Styles"
            ;;
        refactor)
            category="♻️ Code Refactoring"
            ;;
        perf)
            category="⚡ Performance Improvements"
            ;;
        test)
            category="✅ Tests"
            ;;
        build)
            category="🏗️ Build System"
            ;;
        ci)
            category="👷 Continuous Integration"
            ;;
        chore)
            category="🔧 Chores"
            ;;
        revert)
            category="⏪ Reverts"
            ;;
        *)
            category="📝 Other Changes"
            ;;
    esac
    
    # Return as tab-separated values
    echo -e "${type}\t${scope}\t${description}\t${category}\t${breaking}"
}

# Function to get git tags
get_git_tags() {
    git tag --sort=-version:refname | grep -E "^${TAG_PREFIX}[0-9]+\.[0-9]+\.[0-9]+" || true
}

# Function to get commits between refs
get_commits_between() {
    local from_tag="$1"
    local to_tag="$2"
    local range=""
    
    if [ -n "$from_tag" ]; then
        range="${from_tag}..${to_tag}"
    else
        range="$to_tag"
    fi
    
    git log $range --pretty=format:"%H|%s|%an|%ae|%ad" --date=short
}

# Function to format changelog
format_changelog() {
    # Initialize changelog header
    cat << 'EOF'
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
EOF
}

# Main execution
echo -e "${GREEN}Generating changelog...${NC}"

# Get all tags
tags=$(get_git_tags)
tag_count=$(echo "$tags" | grep -c . || echo 0)

# Start changelog
format_changelog > "$OUTPUT_PATH"

# Create temporary directory for organizing commits
temp_dir=$(mktemp -d)
trap "rm -rf $temp_dir" EXIT

# Process versions
versions_processed=0

if [ "$tag_count" -eq 0 ]; then
    echo -e "${YELLOW}No version tags found. Creating initial changelog for unreleased changes.${NC}"
    
    # Get all commits
    echo "" >> "$OUTPUT_PATH"
    echo "## [Unreleased] - $(date +%Y-%m-%d)" >> "$OUTPUT_PATH"
    
    # Clear category files
    rm -f "$temp_dir"/*
    
    # Process all commits
    while IFS='|' read -r hash message author email date; do
        if [ -n "$hash" ]; then
            parsed=$(get_conventional_commit_type "$message")
            type=$(echo "$parsed" | cut -f1)
            scope=$(echo "$parsed" | cut -f2)
            description=$(echo "$parsed" | cut -f3)
            category=$(echo "$parsed" | cut -f4)
            breaking=$(echo "$parsed" | cut -f5)
            
            # Format commit line
            commit_line="* "
            if [ -n "$scope" ]; then
                commit_line+="**${scope}:** "
            fi
            # Use short hash for display (7 chars) like PowerShell version
            short_hash="${hash:0:7}"
            commit_line+="${description} ([${short_hash}](${REPOSITORY}/commit/${short_hash}))"
            
            # Save to appropriate file
            if [ "$breaking" = "true" ]; then
                echo "$commit_line" >> "$temp_dir/BREAKING"
            else
                # Sanitize category name for filename
                safe_category=$(echo "$category" | sed 's/[^a-zA-Z0-9]/_/g')
                echo "${category}|${commit_line}" >> "$temp_dir/${safe_category}"
            fi
        fi
    done < <(git log --pretty=format:"%H|%s|%an|%ae|%ad" --date=short)
    
    versions_processed=1
else
    # Process unreleased changes
    latest_tag=$(echo "$tags" | head -n1)
    unreleased_commits=$(git log "${latest_tag}..HEAD" --oneline 2>/dev/null | wc -l | tr -d ' ')
    
    if [ "$unreleased_commits" -gt 0 ]; then
        echo "" >> "$OUTPUT_PATH"
        echo "## [Unreleased] - $(date +%Y-%m-%d)" >> "$OUTPUT_PATH"
        
        # Clear category files
        rm -f "$temp_dir"/*
        
        # Process unreleased commits
        commits_output=$(get_commits_between "$latest_tag" "HEAD")
        if [ -n "$commits_output" ]; then
            while IFS='|' read -r hash message author email date; do
                if [ -n "$hash" ]; then
                    parsed=$(get_conventional_commit_type "$message")
                    type=$(echo "$parsed" | cut -f1)
                    scope=$(echo "$parsed" | cut -f2)
                    description=$(echo "$parsed" | cut -f3)
                    category=$(echo "$parsed" | cut -f4)
                    breaking=$(echo "$parsed" | cut -f5)
                    
                    # Format commit line
                    commit_line="* "
                    if [ -n "$scope" ]; then
                        commit_line+="**${scope}:** "
                    fi
                    # Use short hash for display (7 chars) like PowerShell version
                    short_hash="${hash:0:7}"
                    commit_line+="${description} ([${short_hash}](${REPOSITORY}/commit/${short_hash}))"
                    
                    # Save to appropriate file
                    if [ "$breaking" = "true" ]; then
                        echo "$commit_line" >> "$temp_dir/BREAKING"
                    else
                        # Sanitize category name for filename
                        safe_category=$(echo "$category" | sed 's/[^a-zA-Z0-9]/_/g')
                        echo "${category}|${commit_line}" >> "$temp_dir/${safe_category}"
                    fi
                fi
            done <<< "$commits_output"
        fi
        
        # Output categorized commits
        if [ -f "$temp_dir/BREAKING" ]; then
            echo "" >> "$OUTPUT_PATH"
            echo "### ⚠️ BREAKING CHANGES" >> "$OUTPUT_PATH"
            echo "" >> "$OUTPUT_PATH"
            cat "$temp_dir/BREAKING" >> "$OUTPUT_PATH"
            rm "$temp_dir/BREAKING"
        fi
        
        # Output other categories in sorted order
        # Collect all categories and sort by name
        temp_categories_file="$temp_dir/categories.txt"
        > "$temp_categories_file"
        
        for file in "$temp_dir"/*; do
            if [ -f "$file" ] && [ -s "$file" ] && [ "$file" != "$temp_categories_file" ]; then
                # Extract category name from first line
                category_name=$(head -n1 "$file" | cut -d'|' -f1)
                echo "${category_name}|${file}" >> "$temp_categories_file"
            fi
        done
        
        # Sort by category name using C locale (same as PowerShell)
        LC_ALL=C sort "$temp_categories_file" | while IFS='|' read -r category_name file; do
            if [ -f "$file" ]; then
                echo "" >> "$OUTPUT_PATH"
                echo "### $category_name" >> "$OUTPUT_PATH"
                echo "" >> "$OUTPUT_PATH"
                # Output commit lines
                cut -d'|' -f2- "$file" >> "$OUTPUT_PATH"
            fi
        done
        
        versions_processed=$((versions_processed + 1))
    fi
    
    # Process each tagged version
    tag_array=()
    while IFS= read -r tag; do
        tag_array+=("$tag")
    done <<< "$tags"
    
    for i in "${!tag_array[@]}"; do
        current_tag="${tag_array[$i]}"
        previous_tag=""
        
        if [ $i -lt $((${#tag_array[@]} - 1)) ]; then
            previous_tag="${tag_array[$((i + 1))]}"
        fi
        
        tag_date=$(git log -1 --format=%ad --date=short "$current_tag")
        version_number="${current_tag#$TAG_PREFIX}"
        
        echo "" >> "$OUTPUT_PATH"
        echo "## [$version_number] - $tag_date" >> "$OUTPUT_PATH"
        
        # Clear category files
        rm -f "$temp_dir"/*
        
        # Get commits for this version
        commits_output=$(get_commits_between "$previous_tag" "$current_tag")
        if [ -n "$commits_output" ]; then
            while IFS='|' read -r hash message author email date; do
                if [ -n "$hash" ]; then
                    parsed=$(get_conventional_commit_type "$message")
                    type=$(echo "$parsed" | cut -f1)
                    scope=$(echo "$parsed" | cut -f2)
                    description=$(echo "$parsed" | cut -f3)
                    category=$(echo "$parsed" | cut -f4)
                    breaking=$(echo "$parsed" | cut -f5)
                    
                    # Format commit line
                    commit_line="* "
                    if [ -n "$scope" ]; then
                        commit_line+="**${scope}:** "
                    fi
                    # Use short hash for display (7 chars) like PowerShell version
                    short_hash="${hash:0:7}"
                    commit_line+="${description} ([${short_hash}](${REPOSITORY}/commit/${short_hash}))"
                    
                    # Save to appropriate file
                    if [ "$breaking" = "true" ]; then
                        echo "$commit_line" >> "$temp_dir/BREAKING"
                    else
                        # Sanitize category name for filename
                        safe_category=$(echo "$category" | sed 's/[^a-zA-Z0-9]/_/g')
                        echo "${category}|${commit_line}" >> "$temp_dir/${safe_category}"
                    fi
                fi
            done <<< "$commits_output"
        fi
        
        # Output categorized commits
        if [ -f "$temp_dir/BREAKING" ]; then
            echo "" >> "$OUTPUT_PATH"
            echo "### ⚠️ BREAKING CHANGES" >> "$OUTPUT_PATH"
            echo "" >> "$OUTPUT_PATH"
            cat "$temp_dir/BREAKING" >> "$OUTPUT_PATH"
            rm "$temp_dir/BREAKING"
        fi
        
        # Output other categories in sorted order
        # Collect all categories and sort by name
        temp_categories_file="$temp_dir/categories_v.txt"
        > "$temp_categories_file"
        
        for file in "$temp_dir"/*; do
            if [ -f "$file" ] && [ -s "$file" ] && [ "$file" != "$temp_categories_file" ] && [ "$file" != "$temp_dir/categories.txt" ]; then
                # Extract category name from first line
                category_name=$(head -n1 "$file" | cut -d'|' -f1)
                echo "${category_name}|${file}" >> "$temp_categories_file"
            fi
        done
        
        # Sort by category name using C locale (same as PowerShell)
        LC_ALL=C sort "$temp_categories_file" | while IFS='|' read -r category_name file; do
            if [ -f "$file" ]; then
                echo "" >> "$OUTPUT_PATH"
                echo "### $category_name" >> "$OUTPUT_PATH"
                echo "" >> "$OUTPUT_PATH"
                # Output commit lines
                cut -d'|' -f2- "$file" >> "$OUTPUT_PATH"
            fi
        done
        
        versions_processed=$((versions_processed + 1))
    done
fi

# Add links section
echo "" >> "$OUTPUT_PATH"
echo "" >> "$OUTPUT_PATH"

# Add Unreleased link if there were unreleased commits
if [ "$tag_count" -gt 0 ] && [ "$unreleased_commits" -gt 0 ]; then
    echo "[Unreleased]: ${REPOSITORY}/releases/tag/Unreleased" >> "$OUTPUT_PATH"
fi

# Add version links
if [ "$tag_count" -gt 0 ]; then
    while IFS= read -r tag; do
        version_number="${tag#$TAG_PREFIX}"
        echo "[${version_number}]: ${REPOSITORY}/releases/tag/${tag}" >> "$OUTPUT_PATH"
    done <<< "$tags"
fi

echo -e "${GREEN}Changelog generated successfully at: ${OUTPUT_PATH}${NC}"
echo -e "${CYAN}Total versions processed: ${versions_processed}${NC}"