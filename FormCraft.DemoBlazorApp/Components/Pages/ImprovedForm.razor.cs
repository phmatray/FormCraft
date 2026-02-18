using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class ImprovedForm
{
    private ContactModel _model = new();
    private bool _isSubmitted;
    private bool _isSubmitting;
    private IFormConfiguration<ContactModel> _formConfiguration = null!;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "improved",
        Title = "Improved Dynamic Form API",
        Description = "Build dynamic forms using a type-safe fluent API with comprehensive validation, field dependencies, conditional visibility, and advanced configuration options for complex form scenarios.",
        Icon = Icons.Material.Filled.Engineering,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Security, Color = Color.Primary, Text = "Type-safe form builder API" },
            new() { Icon = Icons.Material.Filled.Verified, Color = Color.Secondary, Text = "Fluent validation integration" },
            new() { Icon = Icons.Material.Filled.AccountTree, Color = Color.Tertiary, Text = "Field dependencies & visibility" },
            new() { Icon = Icons.Material.Filled.AutoAwesome, Color = Color.Info, Text = "Automatic field rendering" },
            new() { Icon = Icons.Material.Filled.Extension, Color = Color.Success, Text = "Extensible with custom validators" },
            new() { Icon = Icons.Material.Filled.Speed, Color = Color.Warning, Text = "Real-time validation feedback" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "Type-Safe Builder", Usage = "Strongly typed field configuration", Example = ".AddField(x => x.FirstName)" },
            new() { Feature = "Built-in Validation", Usage = "Common validation rules", Example = ".WithMinLength(2, \"Must be at least 2 characters\")" },
            new() { Feature = "Email Validation", Usage = "Pre-built email validator", Example = ".WithEmailValidation()" },
            new() { Feature = "Range Validation", Usage = "Min/max value constraints", Example = ".WithRange(16, 100, \"Age must be between 16 and 100\")" },
            new() { Feature = "Automatic Rendering", Usage = "Single component handles all rendering", Example = "&lt;FormCraftComponent TModel=\"ContactModel\" /&gt;" }
        ],
        CodeExamples =
        [
            new() { Title = "Type-Safe Form Configuration", Language = "csharp", CodeProvider = GetGeneratedCodeStatic }
        ],
        WhenToUse = "Use the improved API when you need full control over form configuration with type safety. It's ideal for complex forms requiring custom validation, field dependencies, conditional visibility, and advanced field configuration. Choose this approach when building enterprise applications that need maintainable, testable form logic.",
        CommonPitfalls =
        [
            "Don't forget to call .Build() at the end of the fluent chain to get the immutable configuration",
            "Remember that Required() adds validation but not the HTML5 required attribute - this is intentional",
            "Field dependencies use DependsOn() callbacks to update model values - VisibleWhen() controls visibility separately",
            "When using WithRange() or WithMinLength(), always provide user-friendly error messages for better UX"
        ],
        RelatedDemoIds = ["fluent", "simplified", "fluent-validation-demo"]
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

        // This is our improved API in action!
        _formConfiguration = FormBuilder<ContactModel>
            .Create()
            .AddField(x => x.FirstName, field => field
                .WithLabel("First Name")
                .Required("First name is required")
                .WithMinLength(2, "Must be at least 2 characters")
                .WithPlaceholder("Enter your first name"))
            .AddField(x => x.LastName, field => field
                .WithLabel("Last Name")
                .Required("Last name is required")
                .WithMinLength(2, "Must be at least 2 characters")
                .WithPlaceholder("Enter your last name"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email Address")
                .Required("Email is required")
                .WithEmailValidation()
                .WithPlaceholder("your.email@example.com"))
            .AddField(x => x.Age, field => field
                .WithLabel("Age")
                .Required("Age is required")
                .WithRange(16, 100, "Age must be between 16 and 100"))
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .Required("Please select your country")
                .WithOptions(
                    ("US", "United States"),
                    ("CA", "Canada"),
                    ("UK", "United Kingdom"),
                    ("DE", "Germany"),
                    ("FR", "France")))
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
            .AddField(x => x.SubscribeToNewsletter, field => field
                .WithLabel("Subscribe to Newsletter")
                .WithHelpText("Get updates about new features"))
            .Build();
    }

    private Task HandleFieldChanged(string fieldName, object? value)
    {
        // Log field changes or perform any additional logic
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;

        // Simulate API call
        await Task.Delay(2000);

        _isSubmitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new ContactModel();
        _isSubmitted = false;
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

        return items;
    }

    private string GetGeneratedCode() => GetGeneratedCodeStatic();

    private static string GetGeneratedCodeStatic()
    {
        const string code = @"// This is our improved API in action!
_formConfiguration = FormBuilder<ContactModel>
    .Create()
    .AddField(x => x.FirstName, field => field
        .WithLabel(""First Name"")
        .Required(""First name is required"")
        .WithMinLength(2, ""Must be at least 2 characters"")
        .WithPlaceholder(""Enter your first name""))
    .AddField(x => x.LastName, field => field
        .WithLabel(""Last Name"")
        .Required(""Last name is required"")
        .WithMinLength(2, ""Must be at least 2 characters"")
        .WithPlaceholder(""Enter your last name""))
    .AddField(x => x.Email, field => field
        .WithLabel(""Email Address"")
        .Required(""Email is required"")
        .WithEmailValidation()
        .WithPlaceholder(""your.email@example.com""))
    .AddField(x => x.Age, field => field
        .WithLabel(""Age"")
        .Required(""Age is required"")
        .WithRange(16, 100, ""Age must be between 16 and 100""))
    .AddField(x => x.Country, field => field
        .WithLabel(""Country"")
        .Required(""Please select your country"")
        .WithOptions(
            (""US"", ""United States""),
            (""CA"", ""Canada""),
            (""UK"", ""United Kingdom""),
            (""DE"", ""Germany""),
            (""FR"", ""France"")))
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
    .AddField(x => x.SubscribeToNewsletter, field => field
        .WithLabel(""Subscribe to Newsletter"")
        .WithHelpText(""Get updates about new features""))
    .Build();

// Use the FormCraftComponent for automatic rendering
<FormCraftComponent
    TModel=""ContactModel""
    Model=""@_model""
    Configuration=""@_formConfiguration""
    OnValidSubmit=""@HandleValidSubmit""
    OnFieldChanged=""@((args) => HandleFieldChanged(args.fieldName, args.value))""
    IsSubmitting=""@_isSubmitting""
    ShowSubmitButton=""true""
    SubmitButtonText=""Submit Application"" />";

        return code;
    }
}