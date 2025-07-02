using System.Timers;
using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class FormSlots : IDisposable
{
    private ContactModel _model = new();
    private IFormConfiguration<ContactModel> _formConfiguration = null!;
    private bool _isSubmitting;
    private bool _isSubmitted;
    private int _activeStep = 0;
    private bool _showCountdown = true;
    private string _countdownText = "";
    private System.Timers.Timer? _countdownTimer;

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "BeforeForm",
            Usage = "Add content before the form fields",
            Example = "<BeforeForm>@YourContent</BeforeForm>"
        },
        new()
        {
            Feature = "AfterForm",
            Usage = "Add content after the form fields",
            Example = "<AfterForm>@YourContent</AfterForm>"
        },
        new()
        {
            Feature = "Conditional Content",
            Usage = "Show/hide slot content dynamically",
            Example = "@if (condition) { <content> }"
        },
        new()
        {
            Feature = "Interactive Elements",
            Usage = "Include interactive components in slots",
            Example = "<MudStepper>, <MudAlert>, etc."
        },
        new()
        {
            Feature = "Flexible Layout",
            Usage = "Use any layout within slots",
            Example = "<MudGrid>, <MudCard>, custom HTML"
        }
    ];

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new() { Text = "Add instructions and context before forms", Icon = Icons.Material.Filled.Info },
        new() { Text = "Display help content and resources after forms", Icon = Icons.Material.Filled.Help },
        new() { Text = "Show progress indicators and steps", Icon = Icons.Material.Filled.Timeline },
        new() { Text = "Include alerts and important notices", Icon = Icons.Material.Filled.Warning },
        new() { Text = "Fully customizable with any Blazor content", Icon = Icons.Material.Filled.Dashboard }
    ];

    protected override void OnInitialized()
    {
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

    private string GetExampleCode()
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

    private string GetImplementationCode()
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