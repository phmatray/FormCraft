using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class ImprovedForm
{
    private ContactModel _model = new();
    private bool _isSubmitted;
    private bool _isSubmitting;
    private IFormConfiguration<ContactModel> _formConfiguration = null!;

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "Type-Safe Builder",
            Usage = "Strongly typed field configuration",
            Example = ".AddField(x => x.FirstName)"
        },
        new()
        {
            Feature = "Built-in Validation",
            Usage = "Common validation rules",
            Example = ".WithMinLength(2, \"Must be at least 2 characters\")"
        },
        new()
        {
            Feature = "Email Validation",
            Usage = "Pre-built email validator",
            Example = ".WithEmailValidation()"
        },
        new()
        {
            Feature = "Range Validation",
            Usage = "Min/max value constraints",
            Example = ".WithRange(16, 100, \"Age must be between 16 and 100\")"
        },
        new()
        {
            Feature = "Automatic Rendering",
            Usage = "Single component handles all rendering",
            Example = "&lt;FormCraftComponent TModel=\"ContactModel\" /&gt;"
        }
    ];

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new()
        {
            Icon = Icons.Material.Filled.Security,
            Color = Color.Primary,
            Text = "Type-safe form builder API"
        },
        new()
        {
            Icon = Icons.Material.Filled.Verified,
            Color = Color.Secondary,
            Text = "Fluent validation integration"
        },
        new()
        {
            Icon = Icons.Material.Filled.AccountTree,
            Color = Color.Tertiary,
            Text = "Field dependencies & visibility"
        },
        new()
        {
            Icon = Icons.Material.Filled.AutoAwesome,
            Color = Color.Info,
            Text = "Automatic field rendering"
        },
        new()
        {
            Icon = Icons.Material.Filled.Extension,
            Color = Color.Success,
            Text = "Extensible with custom validators"
        },
        new()
        {
            Icon = Icons.Material.Filled.Speed,
            Color = Color.Warning,
            Text = "Real-time validation feedback"
        }
    ];

    protected override void OnInitialized()
    {
        // This is our improved API in action!
        _formConfiguration = FormBuilder<ContactModel>
            .Create()
            .AddField(x => x.FirstName)
            .WithLabel("First Name")
            .Required("First name is required")
            .WithMinLength(2, "Must be at least 2 characters")
            .WithPlaceholder("Enter your first name")
            .AddField(x => x.LastName)
            .WithLabel("Last Name")
            .Required("Last name is required")
            .WithMinLength(2, "Must be at least 2 characters")
            .WithPlaceholder("Enter your last name")
            .AddField(x => x.Email)
            .WithLabel("Email Address")
            .Required("Email is required")
            .WithEmailValidation()
            .WithPlaceholder("your.email@example.com")
            .AddField(x => x.Age)
            .WithLabel("Age")
            .Required("Age is required")
            .WithRange(16, 100, "Age must be between 16 and 100")
            .AddField(x => x.Country)
            .WithLabel("Country")
            .Required("Please select your country")
            .WithOptions(
                ("US", "United States"),
                ("CA", "Canada"),
                ("UK", "United Kingdom"),
                ("DE", "Germany"),
                ("FR", "France")
            )
            .AddField(x => x.City)
            .WithLabel("City")
            .WithPlaceholder("Enter your city")
            .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
            .DependsOn(x => x.Country, (model, country) =>
            {
                if (string.IsNullOrEmpty(country))
                {
                    model.City = null;
                }
            })
            .AddField(x => x.SubscribeToNewsletter)
            .WithLabel("Subscribe to Newsletter")
            .WithHelpText("Get updates about new features")
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

    private string GetGeneratedCode()
    {
        var formCode = CodeGenerator.GenerateFormBuilderCode(_formConfiguration);

        // Add usage example for manual rendering
        const string usageExample = @"

// Use the FormCraftComponent for automatic rendering
<FormCraftComponent TModel=""ContactModel"" 
    Model=""@_model"" 
    Configuration=""@_formConfiguration""
    OnValidSubmit=""@HandleValidSubmit""
    OnFieldChanged=""@((args) => HandleFieldChanged(args.fieldName, args.value))""
    IsSubmitting=""@_isSubmitting""
    ShowSubmitButton=""true""
    SubmitButtonText=""Submit Application"" />";

        return formCode + usageExample;
    }
}