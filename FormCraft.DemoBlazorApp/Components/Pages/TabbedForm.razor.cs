using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.ForMudBlazor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class TabbedForm
{
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    
    private EmployeeModel _model = new();
    private bool _isSubmitted;
    private int _activeTabIndex;
    
    private IFormConfiguration<EmployeeModel> _personalInfoConfig = null!;
    private IFormConfiguration<EmployeeModel> _contactConfig = null!;
    private IFormConfiguration<EmployeeModel> _professionalConfig = null!;
    
    // Component references for validation (simplified API)
    private FormCraftComponent<EmployeeModel>? _personalFormComponent;
    private FormCraftComponent<EmployeeModel>? _contactFormComponent;
    private FormCraftComponent<EmployeeModel>? _professionalFormComponent;

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "MudTabs Integration",
            Usage = "Organize forms into logical sections",
            Example = "<MudTabs @bind-ActivePanelIndex=\"_activeTabIndex\">"
        },
        new()
        {
            Feature = "Free Navigation",
            Usage = "Users can switch tabs without validation",
            Example = "No validation on tab change"
        },
        new()
        {
            Feature = "Visual Feedback",
            Usage = "Show completion status with badges",
            Example = "BadgeData=\"@GetValidationBadge()\""
        },
        new()
        {
            Feature = "Final Validation",
            Usage = "Validate all sections before submit",
            Example = "IsFormComplete() method"
        },
        new()
        {
            Feature = "Progress Tracking",
            Usage = "Display overall form completion",
            Example = "GetProgressText() helper"
},
        new()
        {
            Feature = "Smart Tooltips",
            Usage = "Context-aware hints for each tab",
            Example = "<MudTooltip Text=\"@GetTabTooltip(0)\">"
        },
        new()
        {
            Feature = "Summary Dashboard",
            Usage = "Visual completion overview",
            Example = "Completion Summary card"
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
    
    private EventCallback<(string fieldName, object? value)> OnFieldChangedCallback =>
        EventCallback.Factory.Create<(string fieldName, object? value)>(this, OnFieldChanged);
    
    private void OnFieldChanged((string fieldName, object? value) args)
    {
        // Field changed - can be used for tracking changes
    }

    private void PreviousTab()
    {
        if (_activeTabIndex > 0)
        {
            _activeTabIndex--;
        }
    }

    private void NextTab()
    {
        if (_activeTabIndex < 3)
        {
            _activeTabIndex++;
        }
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

    private bool IsFormComplete()
    {
        // Check if all required fields have values
        return !string.IsNullOrWhiteSpace(_model.FirstName) &&
               !string.IsNullOrWhiteSpace(_model.LastName) &&
               _model.DateOfBirth.HasValue &&
               !string.IsNullOrWhiteSpace(_model.Email) &&
               !string.IsNullOrWhiteSpace(_model.Phone) &&
               !string.IsNullOrWhiteSpace(_model.Department) &&
               !string.IsNullOrWhiteSpace(_model.Position) &&
               _model.StartDate.HasValue;
    }

    private object? GetValidationBadge()
    {
        if (IsFormComplete())
        {
            return Icons.Material.Filled.Check;
        }
        return null;
    }

    private Color GetBadgeColor()
    {
        return IsFormComplete() ? Color.Success : Color.Warning;
    }

    // Add per-tab validation indicators
    private object? GetPersonalInfoBadge()
    {
        if (IsPersonalInfoComplete())
            return Icons.Material.Filled.CheckCircle;
        if (HasPersonalInfoData())
            return Icons.Material.Filled.Warning;
        
        return null;
    }

    private object? GetContactInfoBadge()
    {
        if (IsContactInfoComplete())
            return Icons.Material.Filled.CheckCircle;
        if (HasContactInfoData())
            return Icons.Material.Filled.Warning;
        
        return null;
    }

    private object? GetProfessionalInfoBadge()
    {
        if (IsProfessionalInfoComplete())
            return Icons.Material.Filled.CheckCircle;
        if (HasProfessionalInfoData())
            return Icons.Material.Filled.Warning;
        
        return null;
    }

    private Color GetTabBadgeColor(bool isComplete, bool hasData)
    {
        if (isComplete)
            return Color.Success;
        if (hasData)
            return Color.Warning;
        
        return Color.Default;
    }

    // Helper methods for tab state
    private bool IsPersonalInfoComplete()
    {
        return !string.IsNullOrWhiteSpace(_model.FirstName) &&
               !string.IsNullOrWhiteSpace(_model.LastName) &&
               _model.DateOfBirth.HasValue;
    }

    private bool IsContactInfoComplete()
    {
        return !string.IsNullOrWhiteSpace(_model.Email) &&
               !string.IsNullOrWhiteSpace(_model.Phone);
    }

    private bool IsProfessionalInfoComplete()
    {
        return !string.IsNullOrWhiteSpace(_model.Department) &&
               !string.IsNullOrWhiteSpace(_model.Position) &&
               _model.StartDate.HasValue;
    }

    private bool HasPersonalInfoData()
    {
        return !string.IsNullOrWhiteSpace(_model.FirstName) ||
               !string.IsNullOrWhiteSpace(_model.LastName) ||
               _model.DateOfBirth.HasValue;
    }

    private bool HasContactInfoData()
    {
        return !string.IsNullOrWhiteSpace(_model.Email) ||
               !string.IsNullOrWhiteSpace(_model.Phone) ||
               !string.IsNullOrWhiteSpace(_model.Address);
    }

    private bool HasProfessionalInfoData()
    {
        return !string.IsNullOrWhiteSpace(_model.Department) ||
               !string.IsNullOrWhiteSpace(_model.Position) ||
               _model.StartDate.HasValue ||
               _model.IsRemote;
    }

    private string GetProgressText()
    {
        var completedSections = GetCompletedSectionsCount();
        return $"{completedSections} of 3 sections completed";
    }
    
    private int GetCompletedSectionsCount()
    {
        var completedSections = 0;
        
        if (IsPersonalInfoComplete())
        {
            completedSections++;
        }
        
        if (IsContactInfoComplete())
        {
            completedSections++;
        }
        
        if (IsProfessionalInfoComplete())
        {
            completedSections++;
        }
        
        return completedSections;
    }
    
    private int GetProgressPercentage()
    {
        return (GetCompletedSectionsCount() * 100) / 3;
    }
    
    private string GetTabTooltip(int tabIndex)
    {
        return tabIndex switch
        {
            0 => IsPersonalInfoComplete() ? "Personal information is complete" : "Fill in your name and date of birth",
            1 => IsContactInfoComplete() ? "Contact details are complete" : "Provide your email and phone number",
            2 => IsProfessionalInfoComplete() ? "Professional information is complete" : "Enter your department and position",
            3 => IsFormComplete() ? "Ready to submit" : "Complete all sections before submitting",
            _ => ""
        };
    }
    

    private async Task SubmitForm()
    {
        // Validate all tabs
        var isPersonalValid = ValidatePersonalInfo();
        var isContactValid = ValidateContactInfo();
        var isProfessionalValid = ValidateProfessionalInfo();
        
        if (isPersonalValid && isContactValid && isProfessionalValid)
        {
            _isSubmitted = true;
            Snackbar.Add("Employee profile submitted successfully!", Severity.Success);
            StateHasChanged();
        }
        else
        {
            // Show which tabs have errors and navigate to first invalid tab
            if (!isPersonalValid)
            {
                _activeTabIndex = 0;
                Snackbar.Add("Please complete personal information", Severity.Warning);
            }
            else if (!isContactValid)
            {
                _activeTabIndex = 1;
                Snackbar.Add("Please complete contact details", Severity.Warning);
            }
            else if (!isProfessionalValid)
            {
                _activeTabIndex = 2;
                Snackbar.Add("Please complete professional information", Severity.Warning);
            }
            
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ResetForm()
    {
        _model = new EmployeeModel();
        _isSubmitted = false;
        _activeTabIndex = 0;
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
            // Create separate configurations for each tab
            var personalInfoConfig = FormBuilder<EmployeeModel>
                .Create()
                .AddField(x => x.FirstName, field => field
                    .WithLabel("First Name")
                    .Required())
                .AddField(x => x.LastName, field => field
                    .WithLabel("Last Name")
                    .Required())
                .Build();
            
            // Use MudTabs with FormCraftComponent in each tab
            <MudTabs @bind-ActivePanelIndex="_activeTabIndex">
                <MudTabPanel Text="Personal Info" Icon="@Icons.Material.Filled.Person">
                    <FormCraftComponent @ref="_personalFormComponent"
                        TModel="EmployeeModel"
                        Model="@_model"
                        Configuration="@_personalInfoConfig"
                        ShowSubmitButton="false" />
                </MudTabPanel>
                
                <MudTabPanel Text="Contact Details" Icon="@Icons.Material.Filled.ContactMail">
                    <FormCraftComponent @ref="_contactFormComponent"
                        TModel="EmployeeModel"
                        Model="@_model"
                        Configuration="@_contactConfig"
                        ShowSubmitButton="false" />
                </MudTabPanel>
                
                <MudTabPanel Text="Review & Submit" BadgeData="@GetValidationBadge()">
                    <!-- Show review content -->
                    @if (IsFormComplete())
                    {
                        <MudButton OnClick="@SubmitForm">Submit</MudButton>
                    }
                </MudTabPanel>
            </MudTabs>
            """;
    }

    private string GetValidationCode()
    {
        return """
            // Check if form is complete (without blocking navigation)
            private bool IsFormComplete()
            {
                return !string.IsNullOrWhiteSpace(_model.FirstName) &&
                       !string.IsNullOrWhiteSpace(_model.LastName) &&
                       _model.DateOfBirth.HasValue &&
                       !string.IsNullOrWhiteSpace(_model.Email) &&
                       !string.IsNullOrWhiteSpace(_model.Phone) &&
                       !string.IsNullOrWhiteSpace(_model.Department) &&
                       !string.IsNullOrWhiteSpace(_model.Position) &&
                       _model.StartDate.HasValue;
            }
            
            // Submit with validation
            private Task SubmitForm()
            {
                // Validate all tabs
                var isPersonalValid = _personalFormComponent?.Validate() ?? false;
                var isContactValid = _contactFormComponent?.Validate() ?? false;
                var isProfessionalValid = _professionalFormComponent?.Validate() ?? false;
                
                if (isPersonalValid && isContactValid && isProfessionalValid)
                {
                    _isSubmitted = true;
                }
                else
                {
                    // Navigate to first tab with errors
                    if (!isPersonalValid) _activeTabIndex = 0;
                    else if (!isContactValid) _activeTabIndex = 1;
                    else if (!isProfessionalValid) _activeTabIndex = 2;
                }
                
                return Task.CompletedTask;
            }
            
            // Visual indicators
            private object? GetValidationBadge()
            {
                return IsFormComplete() ? Icons.Material.Filled.Check : null;
            }
            """;
    }
}