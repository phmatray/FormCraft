using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class AttributeBasedForms : ComponentBase
{
    private UserRegistrationModel _model = new();
    private IFormConfiguration<UserRegistrationModel>? _formConfiguration;
    private bool _isSubmitted = false;
    private bool _isSubmitting = false;

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new() { Icon = Icons.Material.Filled.TextFields, Text = "[TextField] - Text inputs" },
        new() { Icon = Icons.Material.Filled.Email, Text = "[EmailField] - Email validation" },
        new() { Icon = Icons.Material.Filled.Numbers, Text = "[NumberField] - Numeric inputs" },
        new() { Icon = Icons.Material.Filled.DateRange, Text = "[DateField] - Date pickers" },
        new() { Icon = Icons.Material.Filled.ArrowDropDown, Text = "[SelectField] - Dropdowns" },
        new() { Icon = Icons.Material.Filled.CheckBox, Text = "[CheckboxField] - Checkboxes" },
        new() { Icon = Icons.Material.Filled.Notes, Text = "[TextArea] - Multiline text" },
        new() { Icon = Icons.Material.Filled.Check, Text = "Automatic validation" },
        new() { Icon = Icons.Material.Filled.Speed, Text = "Zero configuration" }
    ];

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "TextField Attribute",
            Usage = "Basic text input fields",
            Example = "[TextField(\"First Name\", \"Enter name\")]"
        },
        new()
        {
            Feature = "EmailField Attribute",
            Usage = "Email with format validation",
            Example = "[EmailField(\"Email Address\")]"
        },
        new()
        {
            Feature = "NumberField Attribute",
            Usage = "Numeric inputs with min/max",
            Example = "[NumberField(\"Age\")] [Range(18, 120)]"
        },
        new()
        {
            Feature = "DateField Attribute",
            Usage = "Date picker fields",
            Example = "[DateField(\"Birth Date\")]"
        },
        new()
        {
            Feature = "SelectField Attribute",
            Usage = "Dropdown with options",
            Example = "[SelectField(\"Country\", \"USA\", \"Canada\", ...)]"
        },
        new()
        {
            Feature = "CheckboxField Attribute",
            Usage = "Boolean checkbox fields",
            Example = "[CheckboxField(\"I agree\", \"Accept terms\")]"
        },
        new()
        {
            Feature = "TextArea Attribute",
            Usage = "Multiline text input",
            Example = "[TextArea(\"Comments\", \"Your feedback\")]"
        },
        new()
        {
            Feature = "Validation Attributes",
            Usage = "Standard DataAnnotations",
            Example = "[Required] [MinLength(2)] [MaxLength(50)]"
        }
    ];

    protected override void OnInitialized()
    {
        // Generate the entire form configuration from attributes with just one line!
        _formConfiguration = FormBuilder<UserRegistrationModel>
            .Create()
            .AddFieldsFromAttributes()
            .Build();
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        StateHasChanged();
        
        // Simulate async operation
        await Task.Delay(1500);
        
        _isSubmitting = false;
        _isSubmitted = true;
        StateHasChanged();
        
        // Hide success message after 5 seconds
        await Task.Delay(5000);
        _isSubmitted = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new UserRegistrationModel();
        _isSubmitted = false;
        _isSubmitting = false;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Name", Value = $"{_model.FirstName} {_model.LastName}" },
            new() { Label = "Email", Value = _model.Email },
            new() { Label = "Age", Value = _model.Age.ToString() },
            new() { Label = "Birth Date", Value = _model.DateOfBirth.ToShortDateString() },
            new() { Label = "Country", Value = _model.Country },
            new() { Label = "Language", Value = _model.PreferredLanguage },
            new() { Label = "Experience", Value = $"{_model.YearsOfExperience} years" },
            new() { Label = "Salary", Value = _model.ExpectedSalary.ToString("C") },
            new() { Label = "Newsletter", Value = _model.SubscribeToNewsletter ? "Subscribed" : "Not Subscribed" },
            new() { Label = "Terms", Value = _model.AcceptTerms ? "Accepted" : "Not Accepted" },
            new() { Label = "Contact Date", Value = _model.PreferredContactDate?.ToShortDateString() ?? "Not specified" },
            new() { Label = "Bio Length", Value = $"{_model.Bio?.Length ?? 0} characters" },
            new() { Label = "Has Comments", Value = string.IsNullOrEmpty(_model.Comments) ? "No" : "Yes" }
        ];
    }

    private string GetModelCode()
    {
        return @"public class UserRegistrationModel
{
    [TextField(""First Name"", ""Enter your first name"")]
    [Required(ErrorMessage = ""First name is required"")]
    [MinLength(2, ErrorMessage = ""First name must be at least 2 characters"")]
    [MaxLength(50, ErrorMessage = ""First name cannot exceed 50 characters"")]
    public string FirstName { get; set; } = string.Empty;

    [TextField(""Last Name"", ""Enter your last name"")]
    [Required(ErrorMessage = ""Last name is required"")]
    [MinLength(2, ErrorMessage = ""Last name must be at least 2 characters"")]
    public string LastName { get; set; } = string.Empty;

    [EmailField(""Email Address"")]
    [Required(ErrorMessage = ""Email is required"")]
    public string Email { get; set; } = string.Empty;

    [NumberField(""Age"", ""Your age in years"")]
    [Required(ErrorMessage = ""Age is required"")]
    [Range(18, 120, ErrorMessage = ""Age must be between 18 and 120"")]
    public int Age { get; set; }

    [DateField(""Date of Birth"")]
    [Required(ErrorMessage = ""Date of birth is required"")]
    public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-25);

    [SelectField(""Country"", ""United States"", ""Canada"", ""United Kingdom"", ...)]
    [Required(ErrorMessage = ""Please select a country"")]
    public string Country { get; set; } = string.Empty;

    [SelectField(""Preferred Language"", ""English"", ""Spanish"", ""French"", ...)]
    public string PreferredLanguage { get; set; } = ""English"";

    [NumberField(""Years of Experience"")]
    [Range(0, 50, ErrorMessage = ""Years of experience must be between 0 and 50"")]
    public int YearsOfExperience { get; set; }

    [TextArea(""Bio"", ""Tell us about yourself..."")]
    [MaxLength(500, ErrorMessage = ""Bio cannot exceed 500 characters"")]
    public string Bio { get; set; } = string.Empty;

    [CheckboxField(""Subscribe to Newsletter"", ""I want to receive promotional emails"")]
    public bool SubscribeToNewsletter { get; set; }

    [CheckboxField(""Accept Terms"", ""I accept the terms and conditions"")]
    [Required(ErrorMessage = ""You must accept the terms and conditions"")]
    public bool AcceptTerms { get; set; }

    [DateField(""Preferred Contact Date"")]
    public DateTime? PreferredContactDate { get; set; }

    [NumberField(""Expected Salary"", ""$0.00"")]
    [Range(0, 1000000, ErrorMessage = ""Salary must be between 0 and 1,000,000"")]
    public decimal ExpectedSalary { get; set; }

    [TextArea(""Additional Comments"", ""Any additional information..."")]
    public string Comments { get; set; } = string.Empty;
}";
    }

    private string GetFormGenerationCode()
    {
        return @"// That's it! Just one line to generate the entire form from attributes:
var formConfiguration = FormBuilder<UserRegistrationModel>
    .Create()
    .AddFieldsFromAttributes()  // ← This reads all attributes and builds the form
    .Build();

// Then use it in your Blazor component:
<FormCraftComponent TModel=""UserRegistrationModel"" 
                    Model=""@_model"" 
                    Configuration=""@_formConfiguration""
                    OnValidSubmit=""HandleValidSubmit"" />";
    }
}