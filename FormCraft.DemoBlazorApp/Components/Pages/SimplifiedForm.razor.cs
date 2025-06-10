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
            Feature = "Text Fields",
            Usage = "One-line required text field with validation",
            Example = ".AddRequiredTextField(x => x.Name, \"Name\", minLength: 2)"
        },
        new()
        {
            Feature = "Email Fields",
            Usage = "Email field with built-in format validation",
            Example = ".AddEmailField(x => x.Email)"
        },
        new()
        {
            Feature = "Numeric Fields",
            Usage = "Number input with range validation",
            Example = ".AddNumericField(x => x.Age, \"Age\", min: 18, max: 100)"
        },
        new()
        {
            Feature = "Dropdown Fields",
            Usage = "Select field with inline options",
            Example = ".AddDropdownField(x => x.Country, \"Country\", (\"US\", \"United States\"))"
        },
        new()
        {
            Feature = "Checkbox Fields",
            Usage = "Boolean field with help text",
            Example = ".AddCheckboxField(x => x.Subscribe, \"Subscribe\", helpText: \"Optional\")"
        }
    ];

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new()
        {
            Icon = Icons.Material.Filled.Extension,
            Color = Color.Primary,
            Text = "Fluent extension methods for common fields"
        },
        new()
        {
            Icon = Icons.Material.Filled.FastForward,
            Color = Color.Secondary,
            Text = "One-line field configuration"
        },
        new()
        {
            Icon = Icons.Material.Filled.Email,
            Color = Color.Tertiary,
            Text = "Built-in email validation method"
        },
        new()
        {
            Icon = Icons.Material.Filled.Numbers,
            Color = Color.Info,
            Text = "Numeric fields with range validation"
        },
        new()
        {
            Icon = Icons.Material.Filled.ArrowDropDown,
            Color = Color.Success,
            Text = "Dropdown fields with inline options"
        },
        new()
        {
            Icon = Icons.Material.Filled.CheckBox,
            Color = Color.Warning,
            Text = "Checkbox fields with help text"
        }
    ];

    protected override void OnInitialized()
    {
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

    private string GetGeneratedCode()
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