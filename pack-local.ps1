# PowerShell script to build and pack FormCraft locally for testing

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "./nupkg",
    [switch]$SkipTests
)

Write-Host "Building FormCraft NuGet package..." -ForegroundColor Green

# Clean output directory
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore

# Run tests unless skipped
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed! Aborting package creation." -ForegroundColor Red
        exit 1
    }
}

# Pack FormCraft
Write-Host "Creating NuGet package..." -ForegroundColor Yellow
dotnet pack ./FormCraft/FormCraft.csproj --configuration $Configuration --no-build --output $OutputPath

# Display package info
$packages = Get-ChildItem -Path $OutputPath -Filter "*.nupkg"
Write-Host "`nCreated packages:" -ForegroundColor Green
foreach ($package in $packages) {
    Write-Host "  - $($package.Name)" -ForegroundColor Cyan
}

Write-Host "`nPackage location: $((Resolve-Path $OutputPath).Path)" -ForegroundColor Green
Write-Host "`nTo test the package locally, add this directory as a NuGet source:" -ForegroundColor Yellow
Write-Host "  dotnet nuget add source $((Resolve-Path $OutputPath).Path) -n FormCraftLocal" -ForegroundColor White