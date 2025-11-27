using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using FormCraft.ForMudBlazor;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class StepperForm
{
    private EmployeeModel _model = new();
    private MudStepper _stepper = null!;
    private bool _isSubmitted;
    private int _currentStep;
    private bool _hasValidationErrors;

    private IFormConfiguration<EmployeeModel> _personalInfoConfig = null!;
    private IFormConfiguration<EmployeeModel> _contactConfig = null!;
    private IFormConfiguration<EmployeeModel> _professionalConfig = null!;

    // Component references for validation (simplified API)
    private FormCraftComponent<EmployeeModel>? _personalFormComponent;
    private FormCraftComponent<EmployeeModel>? _contactFormComponent;
    private FormCraftComponent<EmployeeModel>? _professionalFormComponent;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "stepper-form",
        Title = "Multi-Step Form with Stepper",
        Description = "Build sophisticated multi-step forms using MudStepper with FormCraft. This demo showcases how to divide complex forms into logical steps, maintain data persistence across steps, validate each step independently, and create a comprehensive review step before final submission. Perfect for onboarding flows, surveys, and complex data entry scenarios.",
        Icon = Icons.Material.Filled.LinearScale,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.LinearScale, Color = Color.Primary, Text = "Divide complex forms into logical, manageable steps" },
            new() { Icon = Icons.Material.Filled.Storage, Color = Color.Secondary, Text = "Maintain single model instance across all steps for data persistence" },
            new() { Icon = Icons.Material.Filled.Security, Color = Color.Info, Text = "Validate each step independently before allowing progression" },
            new() { Icon = Icons.Material.Filled.Preview, Color = Color.Tertiary, Text = "Provide review step with comprehensive data summary" },
            new() { Icon = Icons.Material.Filled.PersonPin, Color = Color.Success, Text = "Display dynamic content based on data from previous steps" },
            new() { Icon = Icons.Material.Filled.Navigation, Color = Color.Warning, Text = "Control step navigation with custom validation logic" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "MudStepper Integration", Usage = "Divide forms into logical steps with MudStepper", Example = "<MudStepper @ref=\"_stepper\" @bind-ActiveIndex=\"_currentStep\">" },
            new() { Feature = "Shared Model Instance", Usage = "Use same model across all steps for persistence", Example = "Model=\"@_model\" (same instance for all FormCraftComponent)" },
            new() { Feature = "Step Validation", Usage = "Validate current step before allowing progression", Example = "_personalFormComponent?.Validate() ?? false" },
            new() { Feature = "Component References", Usage = "Store references to forms for validation", Example = "@ref=\"_personalFormComponent\"" },
            new() { Feature = "Dynamic Content", Usage = "Show data from previous steps in subsequent steps", Example = "Welcome, @_model.FirstName!" },
            new() { Feature = "Review Step", Usage = "Display all collected data before submission", Example = "Custom layout with cards showing categorized data" }
        ],
        CodeExamples =
        [
            new() { Title = "Multi-Step Form Implementation", Language = "razor", CodeProvider = GetExampleCode },
            new() { Title = "Step Validation", Language = "csharp", CodeProvider = GetValidationCode }
        ],
        WhenToUse = "Use multi-step forms when you have complex data entry that benefits from logical grouping. Perfect for employee onboarding, user registration flows, surveys, checkout processes, or any scenario with 10+ fields that can be categorized into distinct steps (personal info, contact details, preferences, etc.). Step-by-step progression reduces cognitive load and improves completion rates. Avoid for simple forms (under 8 fields) where a single page is more efficient.",
        CommonPitfalls =
        [
            "Not sharing the same model instance across steps - creates data loss between steps",
            "Allowing step progression without validation - leads to incomplete data",
            "Forgetting to show validation errors when blocking step progression",
            "Not providing a review step - users need to verify all data before submission",
            "Using ShowSubmitButton=\"true\" on step forms - breaks step navigation flow",
            "Missing component references for validation - prevents proper step validation",
            "Not resetting the stepper when resetting the form - leaves UI in inconsistent state",
            "Creating too many steps (5+) - consider grouping or using a different pattern"
        ],
        RelatedDemoIds = ["fluent", "field-groups", "validation", "dialog-demo"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        // Step 1: Personal Information
        _personalInfoConfig = FormBuilder<EmployeeModel>
            .Create()
            .AddFieldGroup(group => group
                .WithColumns(2)
                .AddField(x => x.FirstName, field => field
                    .WithLabel("First Name")
                    .Required("First name is required")
                    .WithPlaceholder("Enter your first name"))
                .AddField(x => x.LastName, field => field
                    .WithLabel("Last Name")
                    .Required("Last name is required")
                    .WithPlaceholder("Enter your last name")))
            .AddField(x => x.DateOfBirth, field => field
                .WithLabel("Date of Birth")
                .Required("Date of birth is required"))
            .Build();

        // Step 2: Contact Information
        _contactConfig = FormBuilder<EmployeeModel>
            .Create()
            .AddFieldGroup(group => group
                .WithColumns(2)
                .AddField(x => x.Email!, field => field
                    .WithLabel("Email Address")
                    .Required("Email is required")
                    .WithEmailValidation("Please enter a valid email")
                    .WithPlaceholder("your.email@company.com"))
                .AddField(x => x.Phone, field => field
                    .WithLabel("Phone Number")
                    .Required("Phone number is required")
                    .WithPlaceholder("(555) 123-4567")))
            .AddField(x => x.Address!, field => field
                .WithLabel("Home Address")
                .AsTextArea(lines: 2)
                .WithPlaceholder("Enter your full address"))
            .Build();

        // Step 3: Professional Information
        _professionalConfig = FormBuilder<EmployeeModel>
            .Create()
            .AddFieldGroup(group => group
                .WithColumns(2)
                .AddField(x => x.Department, field => field
                    .WithLabel("Department")
                    .Required("Please select a department")
                    .WithOptions(
                        ("engineering", "Engineering"),
                        ("sales", "Sales"),
                        ("marketing", "Marketing"),
                        ("hr", "Human Resources"),
                        ("finance", "Finance")
                    ))
                .AddField(x => x.Position, field => field
                    .WithLabel("Position")
                    .Required("Position is required")
                    .WithPlaceholder("e.g., Senior Developer")))
            .AddFieldGroup(group => group
                .WithColumns(2)
                .AddField(x => x.StartDate, field => field
                    .WithLabel("Start Date")
                    .Required("Start date is required"))
                .AddField(x => x.IsRemote, field => field
                    .WithLabel("Remote Work")))
            .Build();
    }

    private async Task PreviousStep()
    {
        if (_stepper != null)
        {
            await _stepper.PreviousStepAsync();
        }
    }

    private async Task NextStep()
    {
        // Validate based on current step
        var isValid = _currentStep switch
        {
            0 => ValidatePersonalInfo(),
            1 => ValidateContactInfo(),
            2 => ValidateProfessionalInfo(),
            _ => true
        };

        _hasValidationErrors = !isValid;

        if (isValid && _stepper != null)
        {
            await _stepper.NextStepAsync();
            _hasValidationErrors = false;
        }

        StateHasChanged();
    }

    private bool ValidatePersonalInfo()
    {
        return _personalFormComponent?.Validate() ?? false;
    }

    private bool ValidateContactInfo()
    {
        return _contactFormComponent?.Validate() ?? false;
    }

    private bool ValidateProfessionalInfo()
    {
        return _professionalFormComponent?.Validate() ?? false;
    }

    private Task SubmitForm()
    {
        // Final validation
        if (ValidatePersonalInfo() && ValidateContactInfo() && ValidateProfessionalInfo())
        {
            _isSubmitted = true;
            StateHasChanged();
        }
        return Task.CompletedTask;
    }

    private async Task ResetForm()
    {
        _model = new EmployeeModel();
        _isSubmitted = false;
        _hasValidationErrors = false;

        // Component references will be re-initialized when the form re-renders

        if (_stepper != null)
        {
            await _stepper.ResetAsync();
        }
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Name", Value = $"{_model.FirstName} {_model.LastName}" },
            new() { Label = "Date of Birth", Value = _model.DateOfBirth?.ToShortDateString() ?? "N/A" },
            new() { Label = "Email", Value = _model.Email ?? "N/A" },
            new() { Label = "Phone", Value = _model.Phone ?? "N/A" },
            new() { Label = "Address", Value = _model.Address ?? "N/A" },
            new() { Label = "Department", Value = _model.Department ?? "N/A" },
            new() { Label = "Position", Value = _model.Position ?? "N/A" },
            new() { Label = "Start Date", Value = _model.StartDate?.ToShortDateString() ?? "N/A" },
            new() { Label = "Remote Work", Value = _model.IsRemote ? "Yes" : "No" }
        ];
    }

    // Code example methods
    private static string GetExampleCode()
    {
        return """
            // Create separate configurations for each step
            var personalInfoConfig = FormBuilder<EmployeeModel>
                .Create()
                .AddField(x => x.FirstName, field => field
                    .WithLabel("First Name")
                    .Required())
                .AddField(x => x.LastName, field => field
                    .WithLabel("Last Name")
                    .Required())
                .Build();

            // Use MudStepper with FormCraftComponent in each step
            <MudStepper @ref="_stepper" @bind-ActiveIndex="_currentStep">
                <ChildContent>
                    <MudStep Title="Personal Info">
                        <ChildContent>
                            <FormCraftComponent @ref="_personalFormComponent"
                                TModel="EmployeeModel"
                                Model="@_model"
                                Configuration="@_personalInfoConfig"
                                ShowSubmitButton="false" />
                        </ChildContent>
                    </MudStep>

                    <MudStep Title="Contact Details">
                        <ChildContent>
                            <FormCraftComponent @ref="_contactFormComponent"
                                TModel="EmployeeModel"
                                Model="@_model"
                                Configuration="@_contactConfig"
                                ShowSubmitButton="false">
                                <BeforeForm>
                                    <!-- Show data from previous step -->
                                    <MudAlert>Welcome, @_model.FirstName!</MudAlert>
                                </BeforeForm>
                            </FormCraftComponent>
                        </ChildContent>
                    </MudStep>
                </ChildContent>
                <ActionContent>
                    <MudButton OnClick="@PreviousStep">Previous</MudButton>
                    <MudButton OnClick="@NextStep">Next</MudButton>
                </ActionContent>
            </MudStepper>
            """;
    }

    private static string GetValidationCode()
    {
        return """
            // Component references only - simplified API!
            private FormCraftComponent<EmployeeModel>? _personalFormComponent;
            private FormCraftComponent<EmployeeModel>? _contactFormComponent;
            private FormCraftComponent<EmployeeModel>? _professionalFormComponent;

            // In Razor markup - just use @ref:
            <FormCraftComponent @ref="_personalFormComponent"
                TModel="EmployeeModel"
                Model="@_model"
                Configuration="@_personalInfoConfig"
                ShowSubmitButton="false" />

            // Validation method - directly call Validate() on components
            private async Task NextStep()
            {
                // Validate based on current step
                var isValid = _currentStep switch
                {
                    0 => _personalFormComponent?.Validate() ?? false,
                    1 => _contactFormComponent?.Validate() ?? false,
                    2 => _professionalFormComponent?.Validate() ?? false,
                    _ => true
                };

                _hasValidationErrors = !isValid;

                if (isValid && _stepper != null)
                {
                    await _stepper.NextStepAsync();
                    _hasValidationErrors = false;
                }
            }
            """;
    }
}