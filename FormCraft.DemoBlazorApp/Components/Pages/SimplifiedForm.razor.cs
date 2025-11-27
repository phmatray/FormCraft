using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class SimplifiedForm
{
    private ContactModel _model = new();
    private bool _isSubmitted;
    private bool _isSubmitting;
    private readonly List<string> _fieldChanges = [];
    private IFormConfiguration<ContactModel> _formConfiguration = null!;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "simplified",
        Title = "Simplified Dynamic Form API",
        Description = "Create forms quickly using fluent extension methods for common field types. One-line configuration for text, email, numeric, dropdown, and checkbox fields with built-in validation.",
        Icon = Icons.Material.Filled.AutoFixHigh,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Extension, Color = Color.Primary, Text = "Fluent extension methods for common fields" },
            new() { Icon = Icons.Material.Filled.FastForward, Color = Color.Secondary, Text = "One-line field configuration" },
            new() { Icon = Icons.Material.Filled.Email, Color = Color.Tertiary, Text = "Built-in email validation method" },
            new() { Icon = Icons.Material.Filled.Numbers, Color = Color.Info, Text = "Numeric fields with range validation" },
            new() { Icon = Icons.Material.Filled.ArrowDropDown, Color = Color.Success, Text = "Dropdown fields with inline options" },
            new() { Icon = Icons.Material.Filled.CheckBox, Color = Color.Warning, Text = "Checkbox fields with help text" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "Text Fields", Usage = "One-line required text field with validation", Example = ".AddRequiredTextField(x => x.Name, \"Name\", minLength: 2)" },
            new() { Feature = "Email Fields", Usage = "Email field with built-in format validation", Example = ".AddEmailField(x => x.Email)" },
            new() { Feature = "Numeric Fields", Usage = "Number input with range validation", Example = ".AddNumericField(x => x.Age, \"Age\", min: 18, max: 100)" },
            new() { Feature = "Dropdown Fields", Usage = "Select field with inline options", Example = ".AddDropdownField(x => x.Country, \"Country\", (\"US\", \"United States\"))" },
            new() { Feature = "Checkbox Fields", Usage = "Boolean field with help text", Example = ".AddCheckboxField(x => x.Subscribe, \"Subscribe\", helpText: \"Optional\")" }
        ],
        CodeExamples =
        [
            new() { Title = "Complete Form Configuration", Language = "csharp", CodeProvider = GetGeneratedCodeStatic }
        ],
        WhenToUse = "Use the simplified API when you need to quickly create forms with common field types. It's ideal for contact forms, registration forms, and any scenario where you need text, email, numeric, dropdown, or checkbox fields with standard validation patterns.",
        CommonPitfalls =
        [
            "Don't mix simplified methods with complex custom configurations on the same field",
            "Remember that AddRequiredTextField automatically adds validation - don't add redundant validators",
            "AddEmailField already includes format validation - no need to add a custom email validator",
            "Dropdown options are defined inline - for dynamic options, use the standard AddField approach"
        ],
        RelatedDemoIds = ["attribute-based-forms", "fluent", "field-groups"]
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

        // Much simpler configuration using FluentFormBuilderExtensions methods
        _formConfiguration = FormBuilder<ContactModel>
            .Create()
            .AddRequiredTextField(x => x.FirstName, "First Name", "Enter your first name", minLength: 2)
            .AddRequiredTextField(x => x.LastName, "Last Name", "Enter your last name", minLength: 2)
            .AddEmailField(x => x.Email)
            .AddNumericField(x => x.Age, "Age", min: 16, max: 100)
            .AddDropdownField(x => x.Country, "Country",
                ("US", "United States"),
                ("CA", "Canada"),
                ("UK", "United Kingdom"),
                ("DE", "Germany"),
                ("FR", "France"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .WithPlaceholder("Enter your city")
                .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
                .DependsOn(x => x.Country, (model, country) =>
                {
                    if (string.IsNullOrEmpty(country))
                    {
                        model.City = null;
                    }
                }))
            .AddCheckboxField(x => x.SubscribeToNewsletter, "Subscribe to Newsletter",
                helpText: "Get updates about new features")
            .Build();
    }

    private async Task HandleValidSubmit(ContactModel submittedModel)
    {
        _isSubmitting = true;
        StateHasChanged();

        // Simulate API call
        await Task.Delay(2000);

        _isSubmitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private Task HandleFieldChanged(string fieldName, object? value)
    {
        _fieldChanges.Add($"{DateTime.Now:HH:mm:ss} - {fieldName}: {value}");
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void ResetForm()
    {
        _model = new ContactModel();
        _isSubmitted = false;
        _fieldChanges.Clear();
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        var items = new List<FormSuccessDisplay.DataDisplayItem>
        {
            new() { Label = "Full Name", Value = $"{_model.FirstName} {_model.LastName}" },
            new() { Label = "Email", Value = _model.Email },
            new() { Label = "Age", Value = _model.Age.ToString() },
            new() { Label = "Country", Value = _model.Country }
        };

        if (!string.IsNullOrEmpty(_model.City))
            items.Add(new() { Label = "City", Value = _model.City });

        items.Add(new() { Label = "Newsletter", Value = _model.SubscribeToNewsletter ? "Yes" : "No" });

        if (_fieldChanges.Any())
            items.Add(new() { Label = "Field Changes", Value = $"{_fieldChanges.Count} changes tracked" });

        return items;
    }

    private string GetGeneratedCode() => GetGeneratedCodeStatic();

    private static string GetGeneratedCodeStatic()
    {
        // Show the actual code using FluentFormBuilderExtensions
        const string actualCode = @"// Much simpler configuration using FluentFormBuilderExtensions methods
_formConfiguration = FormBuilder<ContactModel>
    .Create()
    .AddRequiredTextField(x => x.FirstName, ""First Name"", ""Enter your first name"", minLength: 2)
    .AddRequiredTextField(x => x.LastName, ""Last Name"", ""Enter your last name"", minLength: 2)
    .AddEmailField(x => x.Email)
    .AddNumericField(x => x.Age, ""Age"", min: 16, max: 100)
    .AddDropdownField(x => x.Country, ""Country"",
        (""US"", ""United States""),
        (""CA"", ""Canada""),
        (""UK"", ""United Kingdom""),
        (""DE"", ""Germany""),
        (""FR"", ""France""))
    .AddField(x => x.City, field => field
        .WithLabel(""City"")
        .WithPlaceholder(""Enter your city"")
        .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
        .DependsOn(x => x.Country, (model, country) =>
        {
            if (string.IsNullOrEmpty(country))
            {
                model.City = null;
            }
        }))
    .AddCheckboxField(x => x.SubscribeToNewsletter, ""Subscribe to Newsletter"",
        helpText: ""Get updates about new features"")
    .Build();

// Use in Razor component
<FormCraftComponent
    TModel=""ContactModel""
    Model=""@_model""
    Configuration=""@_formConfiguration""
    OnValidSubmit=""@HandleValidSubmit""
    OnFieldChanged=""@(args => HandleFieldChanged(args.fieldName, args.value))"" />";

        return actualCode;
    }
}