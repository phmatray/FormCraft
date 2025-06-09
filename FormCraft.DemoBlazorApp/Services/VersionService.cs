using System.Reflection;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>
/// Service to provide version information for the application and FormCraft library.
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// Gets the current version of the FormCraft library.
    /// </summary>
    string FormCraftVersion { get; }
}

/// <summary>
/// Default implementation of the version service.
/// </summary>
public class VersionService : IVersionService
{
    /// <inheritdoc />
    public string FormCraftVersion
    {
        get
        {
            // Get the version from the FormCraft assembly
            var assembly = typeof(FormBuilder<>).Assembly;
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                       ?? assembly.GetName().Version?.ToString()
                       ?? "Unknown";
            
            // Remove any build metadata (e.g., +sha.123456)
            var plusIndex = version.IndexOf('+');
            if (plusIndex > 0)
            {
                version = version.Substring(0, plusIndex);
            }
            
            return version;
        }
    }
}