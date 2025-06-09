# Blazor WebAssembly Conversion

This document describes the conversion of the FormCraft demo application from Blazor Server to Blazor WebAssembly for GitHub Pages deployment.

## Changes Made

### 1. Project Configuration
- Changed SDK from `Microsoft.NET.Sdk.Web` to `Microsoft.NET.Sdk.BlazorWebAssembly`
- Added WebAssembly-specific NuGet packages:
  - `Microsoft.AspNetCore.Components.WebAssembly`
  - `Microsoft.AspNetCore.Components.WebAssembly.DevServer`
- Added WebAssembly optimizations in project file

### 2. Program.cs
- Converted from `WebApplication` to `WebAssemblyHostBuilder`
- Changed service registration to use WebAssembly patterns
- Added root components configuration

### 3. App Structure
- Created `wwwroot/index.html` as the host page
- Updated `App.razor` to be a component instead of HTML page
- Removed `Routes.razor` (consolidated into App.razor)
- Removed server-specific render mode references

### 4. GitHub Pages Support
- Added `404.html` for SPA routing support
- Added `.nojekyll` file to prevent Jekyll processing
- Included SPA redirect script for proper routing
- Created GitHub Actions workflow for automated deployment

### 5. GitHub Actions Workflow
The workflow (`deploy-docs.yml`) automatically:
- Builds the Blazor WebAssembly app
- Updates the base href for GitHub Pages
- Deploys to GitHub Pages on pushes to main branch

## Local Development

To run the application locally:
```bash
cd FormCraft.DemoBlazorApp
dotnet run
```

The application will be available at the URL shown in the console output (e.g., `http://localhost:5228`)

## Deployment

The application automatically deploys to GitHub Pages when changes are pushed to the main branch.

The deployed site will be available at: `https://[username].github.io/FormCraft/`

## Important Notes

1. **Base Href**: The GitHub Actions workflow automatically updates the base href to `/FormCraft/` for GitHub Pages deployment
2. **SPA Routing**: The 404.html file ensures that deep links work properly on GitHub Pages
3. **Static Files**: All files in wwwroot are deployed as static assets