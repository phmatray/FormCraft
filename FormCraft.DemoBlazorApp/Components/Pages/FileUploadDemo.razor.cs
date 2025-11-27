using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class FileUploadDemo
{
    private JobApplicationModel _model = new();
    private IFormConfiguration<JobApplicationModel> _formConfiguration = null!;
    private bool _isSubmitted;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "file-upload",
        Title = "File Upload Demo",
        Description = "This demonstrates how to use file upload fields with various configurations including single and multiple file uploads. Learn how to handle file uploads with FormCraft, configure file type restrictions, size limits, and implement drag-and-drop functionality using MudBlazor components.",
        Icon = Icons.Material.Filled.CloudUpload,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Upload, Color = Color.Primary, Text = "Single file upload for documents like resumes" },
            new() { Icon = Icons.Material.Filled.FileCopy, Color = Color.Secondary, Text = "Multiple file upload with configurable limits" },
            new() { Icon = Icons.Material.Filled.CheckCircle, Color = Color.Success, Text = "File type validation with accepted formats" },
            new() { Icon = Icons.Material.Filled.Storage, Color = Color.Info, Text = "File size constraints and validation" },
            new() { Icon = Icons.Material.Filled.DragIndicator, Color = Color.Warning, Text = "Drag and drop support for easy uploads" },
            new() { Icon = Icons.Material.Filled.CloudDone, Color = Color.Tertiary, Text = "Visual feedback for upload progress" }
        ],
        ApiGuidelines =
        [
            new() { Feature = ".AddField(x => x.File)", Usage = "Basic file upload field with default settings", Example = ".AddField(x => x.Resume, field => field.WithLabel(\"Upload Resume\").Required())" },
            new() { Feature = ".AsMultipleFileUpload()", Usage = "Enable multiple file selection and upload", Example = ".AddField(x => x.Certificates).AsMultipleFileUpload(maxFiles: 3, maxFileSize: 2 * 1024 * 1024)" },
            new() { Feature = "acceptedFileTypes", Usage = "Restrict file types to specific formats", Example = "acceptedFileTypes: [\".pdf\", \".doc\", \".docx\"]" },
            new() { Feature = "maxFileSize", Usage = "Set maximum file size in bytes", Example = "maxFileSize: 5 * 1024 * 1024 // 5MB" },
            new() { Feature = "maxFiles", Usage = "Limit number of files for multiple uploads", Example = "maxFiles: 3 // Allow up to 3 files" },
            new() { Feature = "IBrowserFile", Usage = "File interface provided by Blazor", Example = "public IBrowserFile? Resume { get; set; }" }
        ],
        CodeExamples =
        [
            new() { Title = "Job Application Form with File Uploads", Language = "csharp", CodeProvider = GetGeneratedCodeStatic }
        ],
        WhenToUse = "Use file upload fields when you need to collect documents, images, or other files from users. Single file upload is ideal for required documents like resumes or profile pictures. Multiple file upload is perfect for scenarios where users need to provide several documents, such as certificates, portfolio items, or supporting documentation. Always configure appropriate file type restrictions and size limits to ensure security and prevent abuse.",
        CommonPitfalls =
        [
            "Don't forget to configure maxFileSize to prevent memory issues with large files",
            "Always specify acceptedFileTypes to restrict uploads to safe file formats",
            "Remember to handle file validation on the server side, not just client side",
            "Avoid allowing unlimited file uploads - always set a reasonable maxFiles limit",
            "Don't expose actual file paths or system information in error messages"
        ],
        RelatedDemoIds = ["fluent", "validation", "field-dependencies"]
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

        _formConfiguration = FormBuilder<JobApplicationModel>.Create()
            .AddField(x => x.FullName, field => field
                .WithLabel("Full Name")
                .WithPlaceholder("Enter your full name")
                .Required("Full name is required"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .Required("Email is required")
                .WithEmailValidation())
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .Required("Phone number is required"))
            .AddField(x => x.Resume, field => field
                .WithLabel("Upload Resume")
                .Required("Resume is required"))
            .AddField(x => x.Certificates, field => field
                .AsMultipleFileUpload(
                    maxFiles: 3,
                    acceptedFileTypes: [".pdf", ".jpg", ".png"],
                    maxFileSize: 2 * 1024 * 1024) // 2MB per file
                .WithLabel("Upload Certificates"))
            .AddField(x => x.Position, field => field
                .WithLabel("Position")
                .WithOptions(
                    ("developer", "Software Developer"),
                    ("designer", "UI/UX Designer"),
                    ("manager", "Project Manager"),
                    ("analyst", "Business Analyst")))
            .AddField(x => x.CoverLetter, field => field
                .AsTextArea(lines: 5, maxLength: 1000)
                .WithLabel("Cover Letter")
                .WithPlaceholder("Tell us why you're a great fit for this position...")
                .Required("Please provide a cover letter"))
            .AddField(x => x.AgreeToTerms, field => field
                .WithLabel("I agree to the terms and conditions")
                .Required("You must agree to proceed with your application"))
            .Build();
    }

    private async Task HandleSubmit(JobApplicationModel model)
    {
        // In a real application, you would handle file uploads here
        // For demo purposes, we'll just show success
        _isSubmitted = true;
        await Task.CompletedTask;
    }

    private void ResetForm()
    {
        _model = new JobApplicationModel();
        _isSubmitted = false;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        var items = new List<FormSuccessDisplay.DataDisplayItem>
        {
            new() { Label = "Full Name", Value = _model.FullName },
            new() { Label = "Email", Value = _model.Email },
            new() { Label = "Phone", Value = _model.Phone ?? "Not provided" },
            new() { Label = "Position", Value = _model.Position }
        };

        if (_model.Resume != null)
        {
            var resumeSize = _model.Resume.Size / 1024.0;
            items.Add(new FormSuccessDisplay.DataDisplayItem
            {
                Label = "Resume",
                Value = $"{_model.Resume.Name} ({resumeSize:F1} KB)"
            });
        }

        if (_model.Certificates.Any())
        {
            items.Add(new FormSuccessDisplay.DataDisplayItem
            {
                Label = "Certificates",
                Value = $"{_model.Certificates.Count} file(s) uploaded"
            });
        }

        items.Add(new FormSuccessDisplay.DataDisplayItem
        {
            Label = "Terms Accepted",
            Value = _model.AgreeToTerms ? "Yes" : "No"
        });

        return items;
    }

    private string GetGeneratedCode() => GetGeneratedCodeStatic();

    private static string GetGeneratedCodeStatic()
    {
        return @"_formConfiguration = FormBuilder<JobApplicationModel>.Create()
    .AddRequiredTextField(x => x.FullName, ""Full Name"", ""Enter your full name"")
    .AddEmailField(x => x.Email)
    .AddPhoneField(x => x.Phone!, required: true)
    .AddFileUploadField(x => x.Resume!, ""Upload Resume"",
        acceptedFileTypes: ["".pdf"", "".doc"", "".docx""],
        maxFileSize: 5 * 1024 * 1024, // 5MB
        required: true)
    .AddMultipleFileUploadField(x => x.Certificates!, ""Upload Certificates"",
        maxFiles: 3,
        acceptedFileTypes: ["".pdf"", "".jpg"", "".png""],
        maxFileSize: 2 * 1024 * 1024) // 2MB per file
    .AddDropdownField(x => x.Position, ""Position"",
        (""developer"", ""Software Developer""),
        (""designer"", ""UI/UX Designer""),
        (""manager"", ""Project Manager""),
        (""analyst"", ""Business Analyst""))
    .AddField(x => x.CoverLetter, field => field
        .AsTextArea(lines: 5, maxLength: 1000)
        .WithLabel(""Cover Letter"")
        .WithPlaceholder(""Tell us why you're a great fit for this position..."")
        .Required(""Please provide a cover letter""))
    .AddCheckboxField(x => x.AgreeToTerms, ""I agree to the terms and conditions"",
        ""You must agree to proceed with your application"")
    .Build();

// Use in Razor component
<FormCraftComponent
    TModel=""JobApplicationModel""
    Model=""@_model""
    Configuration=""@_formConfiguration""
    OnValidSubmit=""@HandleSubmit"" />";
    }
}