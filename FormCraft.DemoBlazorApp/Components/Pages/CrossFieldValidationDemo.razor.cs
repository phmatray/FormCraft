using FluentValidation;
using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class CrossFieldValidationDemo : ComponentBase
{
    private BookingModel _model = new();
    private IFormConfiguration<BookingModel>? _formConfig;
    private bool _submitted;
    private bool _isSubmitting;
    private List<string> _validationErrors = [];

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "cross-field-validation",
        Title = "Cross-Field Validation",
        Description = "Implement validations that depend on multiple fields, such as password confirmation, date range validation, and conditional requirements. FormCraft makes cross-field validation intuitive and type-safe using FluentValidation's powerful cross-field comparison features.",
        Icon = Icons.Material.Filled.CompareArrows,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.CompareArrows, Color = Color.Primary, Text = "Compare values across multiple fields" },
            new() { Icon = Icons.Material.Filled.DateRange, Color = Color.Secondary, Text = "Date range validation (start/end)" },
            new() { Icon = Icons.Material.Filled.Password, Color = Color.Tertiary, Text = "Password confirmation matching" },
            new() { Icon = Icons.Material.Filled.Calculate, Color = Color.Info, Text = "Numeric range and sum validations" },
            new() { Icon = Icons.Material.Filled.Rule, Color = Color.Success, Text = "Conditional field requirements" },
            new() { Icon = Icons.Material.Filled.Security, Color = Color.Warning, Text = "Type-safe expression-based rules" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "DependsOn()", Usage = "Declare field dependencies", Example = "field.DependsOn(x => x.OtherField)" },
            new() { Feature = "FluentValidation GreaterThan()", Usage = "Compare field values (dates, numbers)", Example = "RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)" },
            new() { Feature = "FluentValidation Equal()", Usage = "Ensure fields match (password confirmation)", Example = "RuleFor(x => x.ConfirmPassword).Equal(x => x.Password)" },
            new() { Feature = "FluentValidation LessThanOrEqualTo()", Usage = "Numeric comparison with other field", Example = "RuleFor(x => x.ExpectedGuests).LessThanOrEqualTo(x => x.VenueCapacity)" },
            new() { Feature = "FluentValidation Must()", Usage = "Custom validation with model access", Example = "RuleFor(x => x.End).Must((m, end) => end > m.Start)" },
            new() { Feature = "WithMessage()", Usage = "Dynamic error messages with field values", Example = ".WithMessage(x => $\"Must be after {x.Start}\")" }
        ],
        CodeExamples =
        [
            new() { Title = "Cross-Field Validation Implementation", Language = "csharp", CodeProvider = GetFormConfigCodeStatic },
            new() { Title = "FluentValidation Cross-Field Rules", Language = "csharp", CodeProvider = GetValidatorCodeStatic }
        ],
        WhenToUse = "Use cross-field validation when one field's validity depends on another field's value. Common scenarios include date ranges (end date after start date), password confirmation, numeric constraints (max greater than min), conditional requirements based on other fields, and capacity validation (guests not exceeding venue capacity). DependsOn() ensures dependent fields re-validate when their dependencies change.",
        CommonPitfalls =
        [
            "Forgetting to use DependsOn() - dependent fields won't re-validate when dependencies change",
            "Not handling null values in cross-field comparisons - can cause validation errors",
            "Creating circular dependencies between fields that validate each other",
            "Overly complex validation logic that's hard to debug - keep rules simple and focused",
            "Not providing clear error messages that indicate which fields are being compared"
        ],
        RelatedDemoIds = ["fluent", "validation", "field-dependencies", "async-validation"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<FormGuidelines.GuidelineItem> _sidebarFeatures => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        _formConfig = FormBuilder<BookingModel>
            .Create()
            .AddField(x => x.EventName, field => field
                .WithLabel("Event Name")
                .WithPlaceholder("Enter event name")
                .WithFluentValidation(x => x.EventName)
                .WithHelpText("Give your event a memorable name"))
            .AddField(x => x.StartDate, field => field
                .WithLabel("Start Date")
                .WithFluentValidation(x => x.StartDate)
                .WithHelpText("When does your event begin?"))
            .AddField(x => x.EndDate, field => field
                .WithLabel("End Date")
                .DependsOn(x => x.StartDate, (_, _) => { })
                .WithFluentValidation(x => x.EndDate)
                .WithHelpText("Must be after the start date"))
            .AddField(x => x.VenueCapacity, field => field
                .WithLabel("Venue Capacity")
                .WithPlaceholder("100")
                .WithFluentValidation(x => x.VenueCapacity)
                .WithHelpText("Maximum people the venue can hold"))
            .AddField(x => x.ExpectedGuests, field => field
                .WithLabel("Expected Guests")
                .WithPlaceholder("50")
                .DependsOn(x => x.VenueCapacity, (_, _) => { })
                .WithFluentValidation(x => x.ExpectedGuests)
                .WithHelpText("Cannot exceed venue capacity"))
            .AddField(x => x.Password, field => field
                .WithLabel("Access Password")
                .WithInputType("password")
                .WithFluentValidation(x => x.Password)
                .WithHelpText("Minimum 8 characters"))
            .AddField(x => x.ConfirmPassword, field => field
                .WithLabel("Confirm Password")
                .WithInputType("password")
                .DependsOn(x => x.Password, (_, _) => { })
                .WithFluentValidation(x => x.ConfirmPassword)
                .WithHelpText("Must match the password above"))
            .AddField(x => x.MinBudget, field => field
                .WithLabel("Minimum Budget")
                .WithPlaceholder("1000")
                .WithFluentValidation(x => x.MinBudget)
                .WithHelpText("Minimum cost for the event"))
            .AddField(x => x.MaxBudget, field => field
                .WithLabel("Maximum Budget")
                .WithPlaceholder("5000")
                .DependsOn(x => x.MinBudget, (_, _) => { })
                .WithFluentValidation(x => x.MaxBudget)
                .WithHelpText("Must be greater than or equal to minimum"))
            .Build();
    }

    private async Task HandleValidSubmit(BookingModel model)
    {
        _isSubmitting = true;
        _validationErrors.Clear();
        StateHasChanged();

        await Task.Delay(1500);

        _submitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new BookingModel();
        _submitted = false;
        _validationErrors.Clear();
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Event Name", Value = _model.EventName },
            new() { Label = "Date Range", Value = $"{_model.StartDate:d} - {_model.EndDate:d}" },
            new() { Label = "Duration", Value = $"{(_model.EndDate - _model.StartDate).Days} days" },
            new() { Label = "Venue Capacity", Value = _model.VenueCapacity.ToString() },
            new() { Label = "Expected Guests", Value = _model.ExpectedGuests.ToString() },
            new() { Label = "Budget Range", Value = $"${_model.MinBudget:N0} - ${_model.MaxBudget:N0}" }
        ];
    }

    // Code Examples
    private string GetCodeExample() => GetFormConfigCodeStatic();

    private static string GetFormConfigCodeStatic()
    {
        return """
            // FormCraft configuration with cross-field dependencies
            var formConfig = FormBuilder<BookingModel>
                .Create()
                .AddField(x => x.StartDate, field => field
                    .WithLabel("Start Date")
                    .WithFluentValidation(x => x.StartDate))
                .AddField(x => x.EndDate, field => field
                    .WithLabel("End Date")
                    // Declare dependency - re-validates when StartDate changes
                    .DependsOn(x => x.StartDate, (model, newDate) => {
                        // Optional: adjust EndDate if needed
                    })
                    .WithFluentValidation(x => x.EndDate))
                .AddField(x => x.Password, field => field
                    .WithLabel("Password")
                    .WithInputType("password"))
                .AddField(x => x.ConfirmPassword, field => field
                    .WithLabel("Confirm Password")
                    .WithInputType("password")
                    // Re-validate when Password changes
                    .DependsOn(x => x.Password, (model, pwd) => { })
                    .WithFluentValidation(x => x.ConfirmPassword))
                .Build();
            """;
    }

    private string GetValidatorCodeExample() => GetValidatorCodeStatic();

    private static string GetValidatorCodeStatic()
    {
        return """
            public class BookingValidator : AbstractValidator<BookingModel>
            {
                public BookingValidator()
                {
                    // Date range validation
                    RuleFor(x => x.EndDate)
                        .GreaterThan(x => x.StartDate)
                        .WithMessage("End date must be after start date");

                    // Password confirmation
                    RuleFor(x => x.ConfirmPassword)
                        .Equal(x => x.Password)
                        .WithMessage("Passwords must match");

                    // Numeric comparison
                    RuleFor(x => x.ExpectedGuests)
                        .LessThanOrEqualTo(x => x.VenueCapacity)
                        .WithMessage("Guests cannot exceed venue capacity");

                    RuleFor(x => x.MaxBudget)
                        .GreaterThanOrEqualTo(x => x.MinBudget)
                        .WithMessage("Max budget must be >= min budget");
                }
            }
            """;
    }

    // Model class
    public class BookingModel
    {
        public string EventName { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7);
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(8);
        public int VenueCapacity { get; set; } = 100;
        public int ExpectedGuests { get; set; } = 50;
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public decimal MinBudget { get; set; } = 1000;
        public decimal MaxBudget { get; set; } = 5000;
    }

    // FluentValidation validator with cross-field rules
    public class BookingValidator : AbstractValidator<BookingModel>
    {
        public BookingValidator()
        {
            RuleFor(x => x.EventName)
                .NotEmpty().WithMessage("Event name is required")
                .MinimumLength(3).WithMessage("Event name must be at least 3 characters");

            RuleFor(x => x.StartDate)
                .GreaterThanOrEqualTo(DateTime.Today)
                .WithMessage("Start date cannot be in the past");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date must be after start date");

            RuleFor(x => x.VenueCapacity)
                .GreaterThan(0).WithMessage("Venue capacity must be positive");

            RuleFor(x => x.ExpectedGuests)
                .GreaterThan(0).WithMessage("Expected guests must be positive")
                .LessThanOrEqualTo(x => x.VenueCapacity)
                .WithMessage("Expected guests cannot exceed venue capacity");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password)
                .WithMessage("Passwords do not match");

            RuleFor(x => x.MinBudget)
                .GreaterThanOrEqualTo(0).WithMessage("Minimum budget cannot be negative");

            RuleFor(x => x.MaxBudget)
                .GreaterThanOrEqualTo(x => x.MinBudget)
                .WithMessage("Maximum budget must be greater than or equal to minimum budget");
        }
    }
}
