using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Services;

/// <summary>
/// Registry containing metadata for all demo pages.
/// </summary>
public class DemoRegistry : IDemoRegistry
{
    private static readonly IReadOnlyList<DemoMetadata> AllDemos = new List<DemoMetadata>
    {
        // Form Examples
        new()
        {
            Id = "simplified",
            Title = "Simplified API",
            Description = "Quick forms with minimal configuration using pre-built templates.",
            Icon = Icons.Material.Filled.AutoFixHigh,
            Category = "form-examples",
            Order = 1
        },
        new()
        {
            Id = "improved",
            Title = "Type-Safe Builder",
            Description = "Full control with compile-time safety and IntelliSense support.",
            Icon = Icons.Material.Filled.Engineering,
            Category = "form-examples",
            Order = 2
        },
        new()
        {
            Id = "fluent",
            Title = "Fluent API",
            Description = "Helper methods for common field types and streamlined form building.",
            Icon = Icons.Material.Filled.FlashOn,
            Category = "form-examples",
            Order = 3
        },
        new()
        {
            Id = "attribute-based-forms",
            Title = "Attribute-Based Forms",
            Description = "Generate forms from model attributes and data annotations.",
            Icon = Icons.Material.Filled.Label,
            Category = "form-examples",
            Order = 4
        },
        new()
        {
            Id = "field-groups",
            Title = "Field Groups",
            Description = "Organize form fields into rows with different column layouts.",
            Icon = Icons.Material.Filled.GridView,
            Category = "form-examples",
            Order = 5
        },
        new()
        {
            Id = "custom-renderers",
            Title = "Custom Renderers",
            Description = "Create custom field renderers for specialized input types.",
            Icon = Icons.Material.Filled.Palette,
            Category = "form-examples",
            Order = 6
        },
        new()
        {
            Id = "password-demo",
            Title = "Password Fields",
            Description = "Password input with visibility toggle and strength indicators.",
            Icon = Icons.Material.Filled.Lock,
            Category = "form-examples",
            Order = 7
        },
        new()
        {
            Id = "file-upload",
            Title = "File Upload",
            Description = "File upload handling with drag-and-drop support.",
            Icon = Icons.Material.Filled.CloudUpload,
            Category = "form-examples",
            Order = 8
        },
        new()
        {
            Id = "form-slots",
            Title = "Form Slots",
            Description = "Custom content slots for headers, footers, and between fields.",
            Icon = Icons.Material.Filled.ViewCarousel,
            Category = "form-examples",
            Order = 9
        },
        new()
        {
            Id = "stepper-form",
            Title = "Stepper Form",
            Description = "Multi-step forms with MudStepper integration.",
            Icon = Icons.Material.Filled.LinearScale,
            Category = "form-examples",
            Order = 10
        },
        new()
        {
            Id = "tabbed-form",
            Title = "Tabbed Form",
            Description = "Organize form sections into tabs.",
            Icon = Icons.Material.Filled.Tab,
            Category = "form-examples",
            Order = 11
        },
        new()
        {
            Id = "security-demo",
            Title = "Security Features",
            Description = "Field encryption, CSRF protection, rate limiting, and audit logging.",
            Icon = Icons.Material.Filled.Security,
            Category = "form-examples",
            Order = 12
        },
        new()
        {
            Id = "fluent-validation-demo",
            Title = "FluentValidation Demo",
            Description = "Integration with FluentValidation for complex validation rules.",
            Icon = Icons.Material.Filled.CheckCircle,
            Category = "form-examples",
            Order = 13
        },
        new()
        {
            Id = "dialog-demo",
            Title = "Dialog Integration",
            Description = "Forms inside MudBlazor dialogs.",
            Icon = Icons.Material.Filled.OpenInNew,
            Category = "form-examples",
            Order = 14
        },
        new()
        {
            Id = "cross-field-validation",
            Title = "Cross-Field Validation",
            Description = "Validations that depend on multiple fields like date ranges and password confirmation.",
            Icon = Icons.Material.Filled.CompareArrows,
            Category = "form-examples",
            Order = 15
        },
        new()
        {
            Id = "complex-dependencies",
            Title = "Complex Dependencies",
            Description = "Advanced field dependencies with cascading updates and conditional logic.",
            Icon = Icons.Material.Filled.AccountTree,
            Category = "form-examples",
            Order = 16
        },
        new()
        {
            Id = "async-value-provider",
            Title = "Async Value Providers",
            Description = "Load field values asynchronously from APIs or databases.",
            Icon = Icons.Material.Filled.CloudSync,
            Category = "form-examples",
            Order = 17
        },

        // Documentation
        new()
        {
            Id = "docs/getting-started",
            Title = "Getting Started",
            Description = "Installation and basic usage guide.",
            Icon = Icons.Material.Filled.PlayArrow,
            Category = "documentation",
            Order = 1
        },
        new()
        {
            Id = "docs/api-reference",
            Title = "API Reference",
            Description = "Complete API documentation.",
            Icon = Icons.Material.Filled.Code,
            Category = "documentation",
            Order = 2
        },
        new()
        {
            Id = "docs/examples",
            Title = "Examples",
            Description = "Code examples and patterns.",
            Icon = Icons.Material.Filled.AutoAwesome,
            Category = "documentation",
            Order = 3
        },
        new()
        {
            Id = "docs/customization",
            Title = "Customization",
            Description = "Custom renderers and validators guide.",
            Icon = Icons.Material.Filled.Palette,
            Category = "documentation",
            Order = 4
        },
        new()
        {
            Id = "docs/fluent-validation",
            Title = "FluentValidation",
            Description = "FluentValidation integration guide.",
            Icon = Icons.Material.Filled.CheckCircle,
            Category = "documentation",
            Order = 5
        },
        new()
        {
            Id = "docs/security",
            Title = "Security",
            Description = "Security features documentation.",
            Icon = Icons.Material.Filled.Security,
            Category = "documentation",
            Order = 6
        },
        new()
        {
            Id = "docs/troubleshooting",
            Title = "Troubleshooting",
            Description = "Common issues and solutions.",
            Icon = Icons.Material.Filled.BugReport,
            Category = "documentation",
            Order = 7
        }
    };

    private static readonly Dictionary<string, string> CategoryDisplayNames = new()
    {
        ["form-examples"] = "Form Examples",
        ["documentation"] = "Documentation"
    };

    public IReadOnlyList<DemoMetadata> GetAllDemos() => AllDemos;

    public IReadOnlyList<DemoMetadata> GetDemosByCategory(string category) =>
        AllDemos.Where(d => d.Category == category).OrderBy(d => d.Order).ToList();

    public DemoMetadata? GetDemo(string id) =>
        AllDemos.FirstOrDefault(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public (DemoMetadata? Previous, DemoMetadata? Next) GetAdjacentDemos(string currentId)
    {
        var current = GetDemo(currentId);
        if (current == null)
            return (null, null);

        var categoryDemos = GetDemosByCategory(current.Category).ToList();
        var index = categoryDemos.FindIndex(d => d.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase));

        var previous = index > 0 ? categoryDemos[index - 1] : null;
        var next = index < categoryDemos.Count - 1 ? categoryDemos[index + 1] : null;

        return (previous, next);
    }

    public string GetCategoryDisplayName(string category) =>
        CategoryDisplayNames.TryGetValue(category, out var name) ? name : category;
}
