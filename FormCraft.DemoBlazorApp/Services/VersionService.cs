using System.Text.Json;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>
/// Service to provide version information for the application and FormCraft library.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Gets the current version of the FormCraft library.
    /// </summary>
    Task<string> GetFormCraftVersionAsync();
}

/// <summary>
/// Default implementation of the version service that fetches from NuGet.
/// </summary>
public class VersionService : IVersionService
{
    private readonly HttpClient _httpClient;
    private string? _cachedVersion;
    private DateTime _cacheExpiry = DateTime.MinValue;
    
    public VersionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    /// <inheritdoc />
    public async Task<string> GetFormCraftVersionAsync()
    {
        // Return cached version if still valid (cache for 1 hour)
        if (_cachedVersion != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedVersion;
        }
        
        try
        {
            // Try to fetch from NuGet API
            var response = await _httpClient.GetAsync("https://api.nuget.org/v3-flatcontainer/formcraft/index.json");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);
                
                if (document.RootElement.TryGetProperty("versions", out var versions))
                {
                    var versionArray = versions.EnumerateArray().ToList();
                    if (versionArray.Count > 0)
                    {
                        // Get the latest version (last in the array)
                        _cachedVersion = versionArray[^1].GetString() ?? "latest";
                        _cacheExpiry = DateTime.UtcNow.AddHours(1);
                        return _cachedVersion;
                    }
                }
            }
        }
        catch
        {
            // Fallback to GitHub API if NuGet fails
            try
            {
                var ghResponse = await _httpClient.GetAsync("https://api.github.com/repos/phmatray/FormCraft/releases/latest");
                if (ghResponse.IsSuccessStatusCode)
                {
                    var json = await ghResponse.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    
                    if (document.RootElement.TryGetProperty("tag_name", out var tagName))
                    {
                        var version = tagName.GetString() ?? "latest";
                        // Remove 'v' prefix if present
                        if (version.StartsWith("v"))
                        {
                            version = version.Substring(1);
                        }
                        _cachedVersion = version;
                        _cacheExpiry = DateTime.UtcNow.AddHours(1);
                        return _cachedVersion;
                    }
                }
            }
            catch
            {
                // Ignore secondary failure
            }
        }
        
        // Return a default if all else fails
        return "latest";
    }
}