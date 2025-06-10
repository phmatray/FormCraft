using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
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

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "MudStepper Integration",
            Usage = "Divide forms into logical steps",
            Example = "<MudStepper @ref=\"_stepper\">"
        },
        new()
        {
            Feature = "Shared Model",
            Usage = "Single model instance across all steps",
            Example = "Model=\"@_model\" (same for all steps)"
        },
        new()
        {
            Feature = "Step Validation",
            Usage = "Validate before allowing step change",
            Example = "Custom validation in NextStep method"
        },
        new()
        {
            Feature = "Dynamic Content",
            Usage = "Show data from previous steps",
            Example = "Welcome, @_model.FirstName!"
        },
        new()
        {
            Feature = "Review Step",
            Usage = "Display all collected data before submit",
            Example = "Custom review layout with all fields"
        }
    ];

    protected override void OnInitialized()
    {
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
        return !string.IsNullOrWhiteSpace(_model.FirstName) &&
               !string.IsNullOrWhiteSpace(_model.LastName) &&
               _model.DateOfBirth.HasValue;
    }

    private bool ValidateContactInfo()
    {
        return !string.IsNullOrWhiteSpace(_model.Email) &&
               !string.IsNullOrWhiteSpace(_model.Phone);
    }

    private bool ValidateProfessionalInfo()
    {
        return !string.IsNullOrWhiteSpace(_model.Department) &&
               !string.IsNullOrWhiteSpace(_model.Position) &&
               _model.StartDate.HasValue;
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

    private string GetExampleCode()
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
                            <FormCraftComponent
                                TModel="EmployeeModel"
                                Model="@_model"
                                Configuration="@_personalInfoConfig"
                                ShowSubmitButton="false" />
                        </ChildContent>
                    </MudStep>
                    
                    <MudStep Title="Contact Details">
                        <ChildContent>
                            <FormCraftComponent
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
                    <MudButton @onclick="@PreviousStep">Previous</MudButton>
                    <MudButton @onclick="@NextStep">Next</MudButton>
                </ActionContent>
            </MudStepper>
            """;
    }

    private string GetValidationCode()
    {
        return """
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
            }
            
            private bool ValidatePersonalInfo()
            {
                return !string.IsNullOrWhiteSpace(_model.FirstName) &&
                       !string.IsNullOrWhiteSpace(_model.LastName) &&
                       _model.DateOfBirth.HasValue;
            }
            """;
    }
}