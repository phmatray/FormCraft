using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class FieldGroups
{
    private EmployeeModel _model = new();
    private bool _isSubmitted;
    private bool _isSubmitting;
    private IFormConfiguration<EmployeeModel> _formConfiguration = null!;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "field-groups",
        Title = "Field Groups Layout",
        Description = "This demonstrates organizing form fields into logical groups with different column layouts, card containers, and custom header content. Field groups provide a powerful way to structure complex forms while maintaining clean, maintainable code.",
        Icon = Icons.Material.Filled.GridView,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.GridView, Color = Color.Primary, Text = "Built-in field groups support" },
            new() { Icon = Icons.Material.Filled.Code, Color = Color.Secondary, Text = "Single form configuration" },
            new() { Icon = Icons.Material.Filled.ViewModule, Color = Color.Tertiary, Text = "Flexible column layouts" },
            new() { Icon = Icons.Material.Filled.Dashboard, Color = Color.Info, Text = "Named group sections" },
            new() { Icon = Icons.Material.Filled.Speed, Color = Color.Success, Text = "Optimized rendering" },
            new() { Icon = Icons.Material.Filled.AutoAwesome, Color = Color.Warning, Text = "Fluent API design" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "Field Groups", Usage = "Organize fields into logical sections", Example = ".AddFieldGroup(group => group...)" },
            new() { Feature = "Group Name", Usage = "Provide a title for each section", Example = ".WithGroupName(\"Personal Information\")" },
            new() { Feature = "Column Layout", Usage = "Set number of columns per row", Example = ".WithColumns(2)" },
            new() { Feature = "Card Display", Usage = "Show group in a card container", Example = ".ShowInCard()" },
            new() { Feature = "Header Content", Usage = "Add custom content to group headers", Example = ".WithHeaderRightContent<InfoTooltip>(p => p[\"Text\"] = \"Info\")" },
            new() { Feature = "Responsive Design", Usage = "Columns automatically stack on mobile", Example = "Built-in responsive behavior" }
        ],
        CodeExamples =
        [
            new() { Title = "Field Groups Configuration", Language = "csharp", CodeProvider = GetGeneratedCodeStatic }
        ],
        WhenToUse = "Use field groups when you need to organize complex forms into logical sections with different layouts. Perfect for multi-step processes, employee onboarding, registration forms, or any scenario where grouping related fields improves user experience and form clarity.",
        CommonPitfalls =
        [
            "Don't nest field groups - they are designed to work at a single level",
            "Remember that WithColumns() affects only that specific group, not the entire form",
            "ShowInCard() adds visual separation - use it sparingly to avoid cluttered UI",
            "Custom header content must be a component - simple strings should use WithGroupName()"
        ],
        RelatedDemoIds = ["fluent", "improved", "complex-dependencies", "custom-layout"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    private List<FormGuidelines.GuidelineItem> _sidebarFeatures => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        // Build form with field groups using the new fluent API
        _formConfiguration = FormBuilder<EmployeeModel>
            .Create()
            // Row 1: 2 fields (First Name, Last Name)
            .AddFieldGroup(group => group
                .WithGroupName("Personal Information")
                .WithColumns(2)
                .ShowInCard()
                .WithHeaderRightContent<InfoTooltip>(p => p["Text"] = "This information is required for all new contacts")
                .AddField(x => x.FirstName, field => field
                    .WithLabel("First Name")
                    .Required("First name is required")
                    .WithPlaceholder("Enter first name"))
                .AddField(x => x.LastName, field => field
                    .WithLabel("Last Name")
                    .Required("Last name is required")
                    .WithPlaceholder("Enter last name")))
            // Row 2: 3 fields (Email, Phone, Department)
            .AddFieldGroup(group => group
                .WithGroupName("Contact Details")
                .WithColumns(3)
                .ShowInCard()
                .WithHeaderRightContent<OptionalBadge>()
                .AddField(x => x.Email, field => field
                    .WithLabel("Email")
                    .Required("Email is required")
                    .WithPlaceholder("email@example.com"))
                .AddField(x => x.Phone, field => field
                    .WithLabel("Phone")
                    .WithPlaceholder("(555) 123-4567"))
                .AddField(x => x.Department, field => field
                    .WithLabel("Department")
                    .Required("Department is required")
                    .WithOptions(
                        ("IT", "Information Technology"),
                        ("HR", "Human Resources"),
                        ("SALES", "Sales"),
                        ("MARKETING", "Marketing"),
                        ("FINANCE", "Finance")
                    )))
            // Row 3: 1 field (Biography)
            .AddFieldGroup(group => group
                .WithGroupName("Additional Information")
                .WithColumns(1)
                .ShowInCard()
                .AddField(x => x.Biography, field => field
                    .WithLabel("Biography")
                    .WithPlaceholder("Tell us about yourself...")
                    .WithHelpText("Brief description of experience and background")))
            .Build();
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        StateHasChanged();

        // Simulate API call
        await Task.Delay(2000);

        _isSubmitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new EmployeeModel();
        _isSubmitted = false;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        var items = new List<FormSuccessDisplay.DataDisplayItem>
        {
            new() { Label = "Full Name", Value = $"{_model.FirstName} {_model.LastName}" },
            new() { Label = "Email", Value = _model.Email ?? "" },
            new() { Label = "Phone", Value = _model.Phone ?? "" },
            new() { Label = "Department", Value = _model.Department ?? "" }
        };

        if (!string.IsNullOrEmpty(_model.Biography))
        {
            var bio = _model.Biography.Length > 50
                ? _model.Biography[..50] + "..."
                : _model.Biography;
            items.Add(new() { Label = "Biography", Value = bio });
        }

        return items;
    }

    // Helper methods removed - now using generic component syntax

    private string GetGeneratedCode() => GetGeneratedCodeStatic();

    private static string GetGeneratedCodeStatic()
    {
        const string code = @"// Build form with field groups using the new fluent API
_formConfiguration = FormBuilder<EmployeeModel>
    .Create()
    // Row 1: 2 fields (First Name, Last Name)
    .AddFieldGroup(group => group
        .WithGroupName(""Personal Information"")
        .WithColumns(2)
        .ShowInCard()
        // Method 1: Use generic component with parameters
        .WithHeaderRightContent<InfoTooltip>(p => p[""Text""] = ""This information is required for all new contacts"")
        .AddField(x => x.FirstName, field => field
            .WithLabel(""First Name"")
            .Required(""First name is required"")
            .WithPlaceholder(""Enter first name""))
        .AddField(x => x.LastName, field => field
            .WithLabel(""Last Name"")
            .Required(""Last name is required"")
            .WithPlaceholder(""Enter last name"")))
    // Row 2: 3 fields (Email, Phone, Department)
    .AddFieldGroup(group => group
        .WithGroupName(""Contact Details"")
        .WithColumns(3)
        .ShowInCard()
        // Method 2: Use generic component without parameters
        .WithHeaderRightContent<OptionalBadge>()
        .AddField(x => x.Email, field => field
            .WithLabel(""Email"")
            .Required(""Email is required"")
            .WithPlaceholder(""email@example.com""))
        .AddField(x => x.Phone, field => field
            .WithLabel(""Phone"")
            .WithPlaceholder(""(555) 123-4567""))
        .AddField(x => x.Department, field => field
            .WithLabel(""Department"")
            .Required(""Department is required"")
            .WithOptions(
                (""IT"", ""Information Technology""),
                (""HR"", ""Human Resources""),
                (""SALES"", ""Sales""),
                (""MARKETING"", ""Marketing""),
                (""FINANCE"", ""Finance"")
            )))
    // Row 3: 1 field (Biography)
    .AddFieldGroup(group => group
        .WithGroupName(""Additional Information"")
        .WithColumns(1)
        .ShowInCard()
        .AddField(x => x.Biography, field => field
            .WithLabel(""Biography"")
            .WithPlaceholder(""Tell us about yourself..."")
            .WithHelpText(""Brief description of experience and background"")))
    .Build();

// Use in Razor component
<FormCraftComponent
    TModel=""EmployeeModel"" 
    Model=""@_model"" 
    Configuration=""@_formConfiguration""
    OnValidSubmit=""@HandleValidSubmit"" />";

        return code;
    }
}