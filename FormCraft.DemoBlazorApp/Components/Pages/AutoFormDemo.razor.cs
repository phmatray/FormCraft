using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class AutoFormDemo
{
    private AccountSignupModel _plainModel = new();
    private bool _isPlainSubmitted;
    private bool _isPlainSubmitting;
    private IFormConfiguration<AccountSignupModel> _plainConfiguration = null!;

    private readonly SpeakerProfileModel _annotatedModel = new();
    private bool _isAnnotatedSubmitted;
    private IFormConfiguration<SpeakerProfileModel> _annotatedConfiguration = null!;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "auto-form",
        Title = "Auto-Generated Forms",
        Description = "Demonstrates AddFieldsAuto(), which reflects over a model's public read-write properties and generates a complete form with sensible defaults - no attributes or per-field setup required. DataAnnotations are honored when present, and an options callback allows including, excluding, or customizing individual fields.",
        Icon = Icons.Material.Filled.AutoAwesome,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Bolt, Color = Color.Primary, Text = "One-line form generation from any POCO" },
            new() { Icon = Icons.Material.Filled.TextFields, Color = Color.Secondary, Text = "Humanized labels (FirstName → \"First Name\")" },
            new() { Icon = Icons.Material.Filled.Email, Color = Color.Tertiary, Text = "Email and password fields detected by name" },
            new() { Icon = Icons.Material.Filled.List, Color = Color.Info, Text = "Enums become selects automatically" },
            new() { Icon = Icons.Material.Filled.Label, Color = Color.Success, Text = "DataAnnotations honored when present" },
            new() { Icon = Icons.Material.Filled.Tune, Color = Color.Warning, Text = "Include, exclude, and per-field overrides" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "AddFieldsAuto()", Usage = "Generate fields for all supported public read-write properties", Example = "FormBuilder<MyModel>.Create().AddFieldsAuto().Build()" },
            new() { Feature = "Type Mapping", Usage = "string→text, numeric→number, bool→checkbox, DateTime/DateOnly→date, TimeOnly→time, enum→select, IBrowserFile→upload", Example = "public DateTime StartDate { get; set; } // date picker" },
            new() { Feature = "Name Conventions", Usage = "Properties containing \"Email\" get email validation; \"Password\" gets a password input", Example = "public string Email { get; set; }" },
            new() { Feature = "DataAnnotations", Usage = "[Required], [Range], [MaxLength], [EmailAddress], [Display(Name = ...)] are applied when present", Example = "[Required] [MaxLength(50)] public string Name { get; set; }" },
            new() { Feature = "ExcludeField", Usage = "Skip a property from generation", Example = "[ExcludeField] public int InternalId { get; set; }" },
            new() { Feature = "Options Callback", Usage = "Include/exclude properties or customize generated fields", Example = ".AddFieldsAuto(o => o.Exclude(x => x.Id).ConfigureField(x => x.Name, f => f.Required()))" }
        ],
        CodeExamples =
        [
            new() { Title = "Zero-Config Form From a Plain POCO", Language = "csharp", CodeProvider = GetPlainExampleCodeStatic },
            new() { Title = "DataAnnotations Are Honored When Present", Language = "csharp", CodeProvider = GetAnnotatedExampleCodeStatic },
            new() { Title = "Customizing the Generated Fields", Language = "csharp", CodeProvider = GetOptionsExampleCodeStatic }
        ],
        WhenToUse = "Use AddFieldsAuto() when you need a working form fast: admin screens, prototypes, internal data-entry tools, or CRUD pages over many backend tables where hand-configuring every field would be repetitive. Start with zero configuration, then layer on DataAnnotations or the options callback as the form's requirements grow. For highly customized public-facing forms, the explicit builder API gives you full control.",
        CommonPitfalls =
        [
            "Only public read-write properties are generated - get-only computed properties and indexers are skipped",
            "Complex objects and collections of complex types are skipped; use AddCollectionField() for one-to-many sub-forms",
            "Name conventions are substring-based: a property called \"EmailVisible\" (bool) maps to a checkbox, but \"EmailBackup\" (string) gets email validation",
            "The options callback runs after the defaults, so ConfigureField() can override the generated label, input type, or validators",
            "Field order follows property declaration order - reorder properties or use ConfigureField with WithOrder() to change it"
        ],
        RelatedDemoIds = ["attribute-based-forms", "simplified", "fluent"]
    };

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

        // Zero configuration: every supported public read-write property becomes a field.
        _plainConfiguration = FormBuilder<AccountSignupModel>
            .Create()
            .AddFieldsAuto()
            .Build();

        // DataAnnotations are honored, and the options callback customizes single fields.
        _annotatedConfiguration = FormBuilder<SpeakerProfileModel>
            .Create()
            .AddFieldsAuto(options => options
                .ConfigureField(x => x.Biography, field => field
                    .AsTextArea(lines: 3)
                    .WithPlaceholder("A short speaker bio...")))
            .Build();
    }

    private async Task HandlePlainSubmit()
    {
        _isPlainSubmitting = true;
        StateHasChanged();

        // Simulate API call
        if (!await DelayAsync(1500))
        {
            return;
        }

        _isPlainSubmitted = true;
        _isPlainSubmitting = false;
        StateHasChanged();
    }

    private void ResetPlainForm()
    {
        _plainModel = new AccountSignupModel();
        _isPlainSubmitted = false;
        StateHasChanged();
    }

    private Task HandleAnnotatedSubmit()
    {
        _isAnnotatedSubmitted = true;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetPlainDataDisplayItems()
    {
        return
        [
            new() { Label = "Name", Value = $"{_plainModel.FirstName} {_plainModel.LastName}".Trim() },
            new() { Label = "Email", Value = _plainModel.Email },
            new() { Label = "Age", Value = _plainModel.Age.ToString() },
            new() { Label = "Experience Level", Value = _plainModel.ExperienceLevel.ToString() },
            new() { Label = "Start Date", Value = _plainModel.StartDate.ToShortDateString() },
            new() { Label = "Accept Updates", Value = _plainModel.AcceptUpdates ? "Yes" : "No" }
        ];
    }

    private string GetPlainExampleCode() => GetPlainExampleCodeStatic();
    private string GetAnnotatedExampleCode() => GetAnnotatedExampleCodeStatic();
    private string GetOptionsExampleCode() => GetOptionsExampleCodeStatic();

    private static string GetPlainExampleCodeStatic()
    {
        return """
            // A plain POCO - no attributes, no configuration
            public class AccountSignupModel
            {
                public string FirstName { get; set; } = string.Empty;
                public string LastName { get; set; } = string.Empty;
                public string Email { get; set; } = string.Empty;      // email input + validation
                public string Password { get; set; } = string.Empty;   // password input
                public int Age { get; set; }                            // numeric input
                public ExperienceLevel ExperienceLevel { get; set; }    // select with enum values
                public DateTime StartDate { get; set; }                 // date picker
                public bool AcceptUpdates { get; set; }                 // checkbox
            }

            // One line generates the whole form:
            var config = FormBuilder<AccountSignupModel>
                .Create()
                .AddFieldsAuto()
                .Build();
            """;
    }

    private static string GetAnnotatedExampleCodeStatic()
    {
        return """
            // DataAnnotations are honored when present (but never required)
            public class SpeakerProfileModel
            {
                [Required(ErrorMessage = "Please tell us your name")]
                [Display(Name = "Full Name")]
                [MaxLength(50)]
                public string FullName { get; set; } = string.Empty;

                [Required]
                [EmailAddress]
                [Display(Name = "Contact Email")]
                public string ContactEmail { get; set; } = string.Empty;

                [Display(Name = "Years of Experience")]
                [Range(0, 50)]
                public int YearsOfExperience { get; set; }

                [MaxLength(200)]
                public string Biography { get; set; } = string.Empty;

                [ExcludeField] // never rendered
                public int InternalRating { get; set; }
            }

            var config = FormBuilder<SpeakerProfileModel>
                .Create()
                .AddFieldsAuto()
                .Build();
            """;
    }

    private static string GetOptionsExampleCodeStatic()
    {
        return """
            // The options callback restricts and customizes the generated fields
            var config = FormBuilder<SpeakerProfileModel>
                .Create()
                .AddFieldsAuto(options => options
                    // Skip properties without editing the model
                    .Exclude(x => x.YearsOfExperience)

                    // Or generate only a subset
                    //.Include(x => x.FullName)
                    //.Include(x => x.ContactEmail)

                    // Customize individual generated fields
                    .ConfigureField(x => x.Biography, field => field
                        .AsTextArea(lines: 3)
                        .WithPlaceholder("A short speaker bio...")))
                .Build();
            """;
    }
}
