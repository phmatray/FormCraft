using FluentValidation;
using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
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

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new()
        {
            Icon = Icons.Material.Filled.CompareArrows,
            Color = Color.Primary,
            Text = "Compare values across multiple fields"
        },
        new()
        {
            Icon = Icons.Material.Filled.DateRange,
            Color = Color.Secondary,
            Text = "Date range validation (start/end)"
        },
        new()
        {
            Icon = Icons.Material.Filled.Password,
            Color = Color.Tertiary,
            Text = "Password confirmation matching"
        },
        new()
        {
            Icon = Icons.Material.Filled.Calculate,
            Color = Color.Info,
            Text = "Numeric range and sum validations"
        },
        new()
        {
            Icon = Icons.Material.Filled.Rule,
            Color = Color.Success,
            Text = "Conditional field requirements"
        },
        new()
        {
            Icon = Icons.Material.Filled.Security,
            Color = Color.Warning,
            Text = "Type-safe expression-based rules"
        }
    ];

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "DependsOn()",
            Usage = "Declare field dependencies",
            Example = "field.DependsOn(x => x.OtherField)"
        },
        new()
        {
            Feature = "WithCrossFieldValidator()",
            Usage = "Custom cross-field validation",
            Example = "field.WithCrossFieldValidator((model, value) => ...)"
        },
        new()
        {
            Feature = "FluentValidation Rules",
            Usage = "When() and Unless() conditions",
            Example = "RuleFor(x => x.Field).GreaterThan(x => x.OtherField)"
        },
        new()
        {
            Feature = "Must()",
            Usage = "Custom validation with model access",
            Example = "RuleFor(x => x.End).Must((m, end) => end > m.Start)"
        },
        new()
        {
            Feature = "Conditional Validation",
            Usage = "Apply rules conditionally",
            Example = "When(x => x.HasValue, () => RuleFor(...))"
        },
        new()
        {
            Feature = "WithMessage()",
            Usage = "Dynamic error messages",
            Example = ".WithMessage(x => $\"Must be after {x.Start}\")"
        }
    ];

    protected override void OnInitialized()
    {
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

    private string GetCodeExample()
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

    private string GetValidatorCodeExample()
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
