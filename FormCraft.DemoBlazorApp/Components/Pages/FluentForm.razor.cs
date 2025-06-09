using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class FluentForm
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
            Feature = "Text Fields",
            Usage = "Quick text field creation",
            Example = ".AddRequiredTextField(x => x.Name, \"Name\", \"Enter name\")"
        },
        new()
        {
            Feature = "Email Fields",
            Usage = "Email with built-in validation",
            Example = ".AddEmailField(x => x.Email)"
        },
        new()
        {
            Feature = "Numeric Fields",
            Usage = "Numbers with min/max constraints",
            Example = ".AddNumericField(x => x.Age, \"Age\", 16, 100)"
        },
        new()
        {
            Feature = "Field Dependencies",
            Usage = "Conditional visibility/updates",
            Example = ".VisibleWhen(m => !string.IsNullOrEmpty(m.Country))"
        },
        new()
        {
            Feature = "Layout Control",
            Usage = "Form layout configuration",
            Example = ".WithLayout(FormLayout.Horizontal)"
        }
    ];

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new()
        {
            Icon = Icons.Material.Filled.FlashOn,
            Color = Color.Primary,
            Text = "Fluent helper methods for common fields"
        },

        new()
        {
            Icon = Icons.Material.Filled.Speed,
            Color = Color.Secondary,
            Text = "Streamlined form building process"
        },

        new()
        {
            Icon = Icons.Material.Filled.AutoAwesome,
            Color = Color.Tertiary,
            Text = "Automatic validation and layout"
        },

        new()
        {
            Icon = Icons.Material.Filled.Tune,
            Color = Color.Info,
            Text = "Configurable form layouts"
        },

        new()
        {
            Icon = Icons.Material.Filled.Security,
            Color = Color.Success,
            Text = "Built-in field validation rules"
        },

        new()
        {
            Icon = Icons.Material.Filled.Psychology,
            Color = Color.Warning,
            Text = "Intelligent dependency handling"
        }
    ];

    protected override void OnInitialized()
    {
        // Much simpler form creation using fluent methods
        _formConfiguration = FormBuilder<ContactModel>
            .Create()
            .WithLayout(FormLayout.Horizontal)
            .AddRequiredTextField(x => x.FirstName, "First Name", "Enter your first name", 2)
            .AddRequiredTextField(x => x.LastName, "Last Name", "Enter your last name", 2)
            .AddEmailField(x => x.Email)
            .AddNumericField(x => x.Age, "Age", 16, 100)
            .AddField(x => x.ExpectedSalary, field => field
                .WithLabel("Expected Salary")
                .WithPlaceholder("$0.00")
                .WithHelpText("Enter amount in $"))
            .AddField(x => x.HourlyRate, field => field
                .WithLabel("Hourly Rate")
                .WithPlaceholder("50.00")
                .WithHelpText("Enter hourly rate"))
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .Required("Please select a country")
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

    private void ResetForm()
    {
        _model = new ContactModel();
        _isSubmitted = false;
        _isSubmitting = false;
        _fieldChanges.Clear();
        StateHasChanged();
    }

    private Task HandleFieldChanged(string fieldName, object? value)
    {
        _fieldChanges.Add($"{DateTime.Now:HH:mm:ss} - {fieldName}: {value}");
        StateHasChanged();
        return Task.CompletedTask;
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        var items = new List<FormSuccessDisplay.DataDisplayItem>
        {
            new() { Label = "Full Name", Value = $"{_model.FirstName} {_model.LastName}" },
            new() { Label = "Email", Value = _model.Email },
            new() { Label = "Age", Value = $"{_model.Age} years old" },
            new() { Label = "Country", Value = _model.Country }
        };

        if (!string.IsNullOrEmpty(_model.City))
            items.Add(new() { Label = "City", Value = _model.City });

        if (_model.ExpectedSalary.HasValue)
            items.Add(new() { Label = "Expected Salary", Value = $"${_model.ExpectedSalary.Value:N2}" });

        if (_model.HourlyRate.HasValue)
            items.Add(new() { Label = "Hourly Rate", Value = $"${_model.HourlyRate.Value:N2}/hr" });

        items.Add(new() { Label = "Newsletter", Value = _model.SubscribeToNewsletter ? "Subscribed" : "Not Subscribed" });

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
    OnFieldChanged=""@(args => HandleFieldChanged(args.fieldName, args.value))""
    ShowSubmitButton=""true""
    SubmitButtonText=""Submit Information"" />";

        return formCode + usageExample;
    }
}