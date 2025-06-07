#!/bin/bash

# Bash script to build and pack FormCraft locally for testing

Configuration=${1:-Release}
OutputPath=${2:-./nupkg}
SkipTests=${3:-false}

echo -e "\033[32mBuilding FormCraft NuGet package...\033[0m"

# Clean output directory
if [ -d "$OutputPath" ]; then
    rm -rf "$OutputPath"
fi
mkdir -p "$OutputPath"

# Restore dependencies
echo -e "\033[33mRestoring dependencies...\033[0m"
dotnet restore

# Build solution
echo -e "\033[33mBuilding solution...\033[0m"
dotnet build --configuration "$Configuration" --no-restore

# Run tests unless skipped
if [ "$SkipTests" != "true" ]; then
    echo -e "\033[33mRunning tests...\033[0m"
    if ! dotnet test --configuration "$Configuration" --no-build --verbosity normal; then
        echo -e "\033[31mTests failed! Aborting package creation.\033[0m"
        exit 1
    fi
fi

# Pack FormCraft
echo -e "\033[33mCreating NuGet package...\033[0m"
dotnet pack ./FormCraft/FormCraft.csproj --configuration "$Configuration" --no-build --output "$OutputPath"

# Display package info
echo -e "\n\033[32mCreated packages:\033[0m"
for package in "$OutputPath"/*.nupkg; do
    echo -e "  - \033[36m$(basename "$package")\033[0m"
done

echo -e "\n\033[32mPackage location: $(realpath "$OutputPath")\033[0m"
echo -e "\n\033[33mTo test the package locally, add this directory as a NuGet source:\033[0m"
echo -e "  \033[37mdotnet nuget add source $(realpath "$OutputPath") -n FormCraftLocal\033[0m"