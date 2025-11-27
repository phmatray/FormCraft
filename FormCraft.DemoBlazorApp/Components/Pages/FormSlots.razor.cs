using System.Timers;
using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class FormSlots : IDisposable
{
    private ContactModel _model = new();
    private IFormConfiguration<ContactModel> _formConfiguration = null!;
    private bool _isSubmitting;
    private bool _isSubmitted;
    private int _activeStep;
    private bool _showCountdown = true;
    private string _countdownText = "";
    private System.Timers.Timer? _countdownTimer;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "form-slots",
        Title = "Form Slots (Before/After Content)",
        Description = "This demonstrates how to add custom content before and after the form using BeforeForm and AfterForm render fragments. Form slots provide powerful extensibility points for adding instructions, progress indicators, alerts, help content, and any other custom content to enhance the user experience.",
        Icon = Icons.Material.Filled.ViewCarousel,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Info, Color = Color.Primary, Text = "Add instructions and context before forms" },
            new() { Icon = Icons.Material.Filled.Help, Color = Color.Secondary, Text = "Display help content and resources after forms" },
            new() { Icon = Icons.Material.Filled.Timeline, Color = Color.Tertiary, Text = "Show progress indicators and steps" },
            new() { Icon = Icons.Material.Filled.Warning, Color = Color.Info, Text = "Include alerts and important notices" },
            new() { Icon = Icons.Material.Filled.Dashboard, Color = Color.Success, Text = "Fully customizable with any Blazor content" },
            new() { Icon = Icons.Material.Filled.DynamicFeed, Color = Color.Warning, Text = "Support for conditional and dynamic content" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "BeforeForm", Usage = "Add content before the form fields", Example = "<BeforeForm>@YourContent</BeforeForm>" },
            new() { Feature = "AfterForm", Usage = "Add content after the form fields", Example = "<AfterForm>@YourContent</AfterForm>" },
            new() { Feature = "Conditional Content", Usage = "Show/hide slot content dynamically", Example = "@if (condition) { <BeforeForm>...</BeforeForm> }" },
            new() { Feature = "Interactive Elements", Usage = "Include interactive components in slots", Example = "<BeforeForm><MudStepper>...</MudStepper></BeforeForm>" },
            new() { Feature = "Flexible Layout", Usage = "Use any layout within slots", Example = "<AfterForm><MudGrid>...</MudGrid></AfterForm>" },
            new() { Feature = "Multiple Components", Usage = "Combine multiple components in a single slot", Example = "<BeforeForm><MudAlert /><MudStepper /></BeforeForm>" }
        ],
        CodeExamples =
        [
            new() { Title = "Using Form Slots", Language = "razor", CodeProvider = GetExampleCodeStatic },
            new() { Title = "Component Implementation", Language = "csharp", CodeProvider = GetImplementationCodeStatic }
        ],
        WhenToUse = "Use form slots when you need to provide context, instructions, or additional information around your forms. Perfect for multi-step wizards with progress indicators, event registrations with countdowns, forms with important notices or alerts, help documentation alongside forms, or any scenario where you need to guide users through the form completion process.",
        CommonPitfalls =
        [
            "Don't overload slots with too much content - keep it focused and relevant to the form",
            "Remember that slot content is part of the component tree - state changes will trigger re-renders",
            "Use conditional rendering (@if) to show/hide slot content based on form state",
            "BeforeForm and AfterForm are optional - only use them when they add value to the user experience",
            "Avoid placing form fields directly in slots - they won't be part of the form configuration"
        ],
        RelatedDemoIds = ["fluent", "field-groups", "improved", "custom-layout"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    private List<FormGuidelines.GuidelineItem> _sidebarFeatures => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        _formConfiguration = FormBuilder<ContactModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Personal Information")
                .WithColumns(2)
                .AddField(x => x.FirstName, field => field
                    .WithLabel("First Name")
                    .Required()
                    .WithPlaceholder("Enter your first name"))
                .AddField(x => x.LastName, field => field
                    .WithLabel("Last Name")
                    .Required()
                    .WithPlaceholder("Enter your last name")))
            .AddFieldGroup(group => group
                .WithGroupName("Contact Details")
                .WithColumns(2)
                .AddField(x => x.Email, field => field
                    .WithLabel("Email Address")
                    .Required()
                    .WithEmailValidation()
                    .WithPlaceholder("your.email@example.com"))
                .AddField(x => x.Phone, field => field
                    .WithLabel("Phone Number")
                    .WithPlaceholder("(555) 123-4567")))
            .AddFieldGroup(group => group
                .WithGroupName("Event Preferences")
                .WithColumns(1)
                .ShowInCard()
                .AddField(x => x.Country, field => field
                    .WithLabel("Dietary Restrictions")
                    .WithOptions(
                        ("none", "None"),
                        ("vegetarian", "Vegetarian"),
                        ("vegan", "Vegan"),
                        ("gluten-free", "Gluten Free"),
                        ("other", "Other")
                    ))
                .AddField(x => x.Message, field => field
                    .AsTextArea(lines: 3)
                    .WithLabel("Special Requirements")
                    .WithPlaceholder("Let us know if you have any special requirements...")))
            .AddField(x => x.SubscribeToNewsletter, field => field
                .WithLabel("Send me updates about future events"))
            .Build();

        // Start countdown timer
        _countdownTimer = new System.Timers.Timer(1000);
        _countdownTimer.Elapsed += UpdateCountdown;
        _countdownTimer.Start();
        UpdateCountdown(null, null);

        // Simulate step progression
        _activeStep = 0;
    }

    private void UpdateCountdown(object? sender, ElapsedEventArgs? e)
    {
        var endDate = new DateTime(2024, 7, 1);
        var timeRemaining = endDate - DateTime.Now;

        if (timeRemaining.TotalSeconds > 0)
        {
            _countdownText = $"{timeRemaining.Days}d {timeRemaining.Hours}h {timeRemaining.Minutes}m {timeRemaining.Seconds}s";
        }
        else
        {
            _showCountdown = false;
            _countdownTimer?.Stop();
        }

        InvokeAsync(StateHasChanged);
    }

    private async Task HandleValidSubmit()
    {
        _activeStep = 1;
        _isSubmitting = true;
        StateHasChanged();

        // Simulate API call
        await Task.Delay(1500);

        _activeStep = 2;
        _isSubmitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new ContactModel();
        _isSubmitted = false;
        _activeStep = 0;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Name", Value = $"{_model.FirstName} {_model.LastName}" },
            new() { Label = "Email", Value = _model.Email },
            new() { Label = "Phone", Value = _model.Phone ?? "" },
            new() { Label = "Dietary Restrictions", Value = _model.Country },
            new() { Label = "Special Requirements", Value = _model.Message ?? "None" },
            new() { Label = "Newsletter", Value = _model.SubscribeToNewsletter ? "Subscribed" : "Not Subscribed" }
        ];
    }

    private string GetExampleCode() => GetExampleCodeStatic();

    private static string GetExampleCodeStatic()
    {
        return """
            <FormCraftComponent
                TModel="ContactModel"
                Model="@Model"
                Configuration="@Configuration"
                OnValidSubmit="@HandleSubmit">
                <BeforeForm>
                    <MudAlert Severity="Severity.Info" Class="mb-4">
                        <MudText Typo="Typo.h6">Welcome!</MudText>
                        <MudText>Please fill out the registration form below.</MudText>
                    </MudAlert>

                    <MudStepper @bind-ActiveIndex="@_activeStep" NonLinear="true">
                        <MudStep Title="Personal Info" />
                        <MudStep Title="Preferences" />
                        <MudStep Title="Review" />
                    </MudStepper>
                </BeforeForm>

                <AfterForm>
                    <MudDivider Class="my-4" />

                    <MudGrid>
                        <MudItem xs="12" md="6">
                            <MudCard>
                                <MudCardContent>
                                    <MudText Typo="Typo.h6">Need Help?</MudText>
                                    <MudText>Contact: support@example.com</MudText>
                                </MudCardContent>
                            </MudCard>
                        </MudItem>
                    </MudGrid>

                    @if (_showCountdown)
                    {
                        <MudAlert Severity="Severity.Warning">
                            Early Bird Ends In: @_countdownText
                        </MudAlert>
                    }
                </AfterForm>
            </FormCraftComponent>
            """;
    }

    private string GetImplementationCode() => GetImplementationCodeStatic();

    private static string GetImplementationCodeStatic()
    {
        return """
            // In FormCraftComponent.razor.cs
            [Parameter]
            public RenderFragment? BeforeForm { get; set; }

            [Parameter]
            public RenderFragment? AfterForm { get; set; }

            // In FormCraftComponent.razor
            @if (BeforeForm != null)
            {
                @BeforeForm
            }

            <EditForm Model="@Model" OnValidSubmit="@OnSubmit">
                @* Form fields rendered here *@
            </EditForm>

            @if (AfterForm != null)
            {
                @AfterForm
            }
            """;
    }

    public void Dispose()
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
    }
}