using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class SimplifiedForm
{
    private ContactModel _model = new();
    private bool _isSubmitted;
    private bool _isSubmitting;
    private readonly List<string> _fieldChanges = [];
    private IFormConfiguration<ContactModel> _formConfiguration = null!;
    
    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "Dynamic Component",
            Usage = "Single component for complete forms",
            Example = "&lt;FormCraftComponent TModel=\"ContactModel\" /&gt;"
        },
        new()
        {
            Feature = "Field Events",
            Usage = "Track field changes in real-time",
            Example = "OnFieldChanged=\"@(args => HandleFieldChanged(...))\""
        },
        new()
        {
            Feature = "Submit Handling",
            Usage = "Async form submission support",
            Example = "OnValidSubmit=\"@HandleValidSubmit\""
        },
        new()
        {
            Feature = "Loading States",
            Usage = "Built-in loading indicators",
            Example = "IsSubmitting=\"@_isSubmitting\""
        },
        new()
        {
            Feature = "Automatic Binding",
            Usage = "Two-way binding out of the box",
            Example = "No manual binding code needed",
            IsCode = false
        }
    ];

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new()
        {
            Icon = Icons.Material.Filled.Widgets,
            Color = Color.Primary,
            Text = "Single component handles all rendering"
        },
        new()
        {
            Icon = Icons.Material.Filled.AutoAwesome,
            Color = Color.Secondary,
            Text = "Automatic field value binding"
        },
        new()
        {
            Icon = Icons.Material.Filled.AccountTree,
            Color = Color.Tertiary,
            Text = "Built-in dependency handling"
        },
        new()
        {
            Icon = Icons.Material.Filled.CodeOff,
            Color = Color.Info,
            Text = "No manual RenderTreeBuilder code"
        },
        new()
        {
            Icon = Icons.Material.Filled.Extension,
            Color = Color.Success,
            Text = "Extension methods for validation"
        },
        new()
        {
            Icon = Icons.Material.Filled.Speed,
            Color = Color.Warning,
            Text = "Real-time field change tracking"
        }
    ];

    protected override void OnInitialized()
    {
        // Much simpler configuration setup
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

    private string GetGeneratedCode()
    {
        var formCode = CodeGenerator.GenerateFormBuilderCode(_formConfiguration);

        // Add usage example
        const string usageExample = @"

// Use in Razor component
<FormCraftComponent TModel=""ContactModel"" 
    Model=""@_model"" 
    Configuration=""@_formConfiguration""
    OnValidSubmit=""@HandleValidSubmit""
    OnFieldChanged=""@(args => HandleFieldChanged(args.fieldName, args.value))"" />";

        return formCode + usageExample;
    }
}