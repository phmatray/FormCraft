using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.UnitTests.Integration;

public class FileUploadIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFieldRendererService _rendererService;

    public FileUploadIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDynamicForms();
        _serviceProvider = services.BuildServiceProvider();
        _rendererService = _serviceProvider.GetRequiredService<IFieldRendererService>();
    }

    private class FileUploadTestModel
    {
        public string Name { get; set; } = string.Empty;
        public IBrowserFile? Resume { get; set; }
        public IReadOnlyList<IBrowserFile>? Certificates { get; set; }
        public bool AgreeToTerms { get; set; }
    }

    [Fact]
    public void FormWithFileUpload_BuildsCorrectConfiguration()
    {
        // Arrange & Act
        var formConfig = FormBuilder<FileUploadTestModel>.Create()
            .AddRequiredTextField(x => x.Name, "Full Name")
            .AddFileUploadField(x => x.Resume!, "Upload Resume",
                acceptedFileTypes: new[] { ".pdf", ".doc", ".docx" },
                maxFileSize: 5 * 1024 * 1024,
                required: true)
            .AddMultipleFileUploadField(x => x.Certificates!, "Upload Certificates",
                maxFiles: 3,
                acceptedFileTypes: new[] { ".pdf", ".jpg", ".png" },
                maxFileSize: 2 * 1024 * 1024)
            .AddField(x => x.AgreeToTerms)
                .WithLabel("I agree to terms")
                .Required("You must agree")
            .Build();

        // Assert
        formConfig.Fields.Count.ShouldBe(4);
        
        // Check Name field
        var nameField = formConfig.Fields[0];
        nameField.FieldName.ShouldBe("Name");
        nameField.Label.ShouldBe("Full Name");
        nameField.IsRequired.ShouldBeTrue();
        
        // Check Resume field
        var resumeField = formConfig.Fields[1];
        resumeField.FieldName.ShouldBe("Resume");
        resumeField.Label.ShouldBe("Upload Resume");
        resumeField.IsRequired.ShouldBeTrue();
        resumeField.AdditionalAttributes.ShouldContainKey("FileUploadConfiguration");
        
        var resumeConfig = resumeField.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        resumeConfig.ShouldNotBeNull();
        resumeConfig.AcceptedFileTypes.ShouldBe(new[] { ".pdf", ".doc", ".docx" });
        resumeConfig.MaxFileSize.ShouldBe(5 * 1024 * 1024);
        resumeConfig.MaxFiles.ShouldBe(1);
        
        // Check Certificates field
        var certificatesField = formConfig.Fields[2];
        certificatesField.FieldName.ShouldBe("Certificates");
        certificatesField.Label.ShouldBe("Upload Certificates");
        certificatesField.IsRequired.ShouldBeFalse();
        
        var certsConfig = certificatesField.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        certsConfig.ShouldNotBeNull();
        certsConfig.MaxFiles.ShouldBe(3);
        certsConfig.Multiple.ShouldBeTrue();
        
        // Check AgreeToTerms field
        var agreeField = formConfig.Fields[3];
        agreeField.FieldName.ShouldBe("AgreeToTerms");
        agreeField.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void FieldRendererService_SelectsFileUploadRenderer_ForIBrowserFile()
    {
        // Arrange
        var model = new FileUploadTestModel();
        var field = new FieldConfiguration<FileUploadTestModel, IBrowserFile>(x => x.Resume!)
        {
            Label = "Resume",
            CustomRendererType = typeof(FileUploadFieldRenderer)
        };
        
        field.AdditionalAttributes["CustomRendererInstance"] = new FileUploadFieldRenderer();
        
        var fieldWrapper = new FieldConfigurationWrapper<FileUploadTestModel, IBrowserFile>(field);

        // Act
        var renderFragment = _rendererService.RenderField(
            model,
            fieldWrapper,
            EventCallback.Factory.Create<object?>(this, _ => { }),
            EventCallback.Factory.Create(this, () => { }));

        // Assert
        renderFragment.ShouldNotBeNull();
    }

    [Fact]
    public async Task FileUploadValidation_WorksWithRequiredValidator()
    {
        // Arrange
        var formConfig = FormBuilder<FileUploadTestModel>.Create()
            .AddFileUploadField(x => x.Resume!, "Resume", required: true)
            .Build();
            
        var model = new FileUploadTestModel { Resume = null };
        var resumeField = formConfig.Fields.First();
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var validators = resumeField.Validators;
        var validationResults = new List<ValidationResult>();
        foreach (var validator in validators)
        {
            if (validator is IFieldValidator<FileUploadTestModel, object> fieldValidator)
            {
                var result = await fieldValidator.ValidateAsync(model, null!, services);
                validationResults.Add(result);
            }
        }

        // Assert
        validators.Count.ShouldBe(1);
        validationResults.ShouldContain(r => !r.IsValid);
        validationResults.First().ErrorMessage.ShouldNotBeNull();
        validationResults.First().ErrorMessage.ShouldContain("Resume is required");
    }

    [Fact]
    public async Task FileUploadValidation_PassesWhenFileProvided()
    {
        // Arrange
        var formConfig = FormBuilder<FileUploadTestModel>.Create()
            .AddFileUploadField(x => x.Resume!, "Resume", required: true)
            .Build();
            
        var file = A.Fake<IBrowserFile>();
        var model = new FileUploadTestModel { Resume = file };
        var resumeField = formConfig.Fields.First();
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var validators = resumeField.Validators;
        var validationResults = new List<ValidationResult>();
        foreach (var validator in validators)
        {
            if (validator is IFieldValidator<FileUploadTestModel, object> fieldValidator)
            {
                var result = await fieldValidator.ValidateAsync(model, file, services);
                validationResults.Add(result);
            }
        }

        // Assert
        validationResults.ShouldAllBe(r => r.IsValid);
    }

    [Fact]
    public async Task MultipleFileUploadValidation_WorksWithRequiredValidator()
    {
        // Arrange
        var formConfig = FormBuilder<FileUploadTestModel>.Create()
            .AddMultipleFileUploadField(x => x.Certificates!, "Certificates", required: true)
            .Build();
            
        var model = new FileUploadTestModel { Certificates = null };
        var certificatesField = formConfig.Fields.First();
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var validators = certificatesField.Validators;
        var validationResults = new List<ValidationResult>();
        foreach (var validator in validators)
        {
            if (validator is IFieldValidator<FileUploadTestModel, object> fieldValidator)
            {
                var result = await fieldValidator.ValidateAsync(model, null!, services);
                validationResults.Add(result);
            }
        }

        // Assert
        validators.Count.ShouldBe(1);
        validationResults.ShouldContain(r => !r.IsValid);
        validationResults.First().ErrorMessage.ShouldNotBeNull();
        validationResults.First().ErrorMessage.ShouldContain("At least one certificates is required");
    }

    [Fact]
    public void ComplexFormWithFileUploads_ConfiguresAllFieldsCorrectly()
    {
        // Arrange & Act
        var formConfig = FormBuilder<FileUploadTestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Applicant Name")
                .Required("Name is required")
                .WithMinLength(2)
                .WithMaxLength(100)
            .AddField(x => x.Resume!)
                .WithLabel("Resume (PDF only)")
                .AsFileUpload(
                    acceptedFileTypes: new[] { ".pdf" },
                    maxFileSize: 10 * 1024 * 1024)
                .Required("Please upload your resume")
                .WithHelpText("Maximum file size: 10MB")
            .AddField(x => x.Certificates!)
                .WithLabel("Certifications")
                .AsMultipleFileUpload(
                    maxFiles: 5,
                    acceptedFileTypes: new[] { ".pdf", ".jpg", ".png" },
                    maxFileSize: 5 * 1024 * 1024)
                .WithHelpText("Upload up to 5 certificates (PDF or images)")
            .AddField(x => x.AgreeToTerms)
                .WithLabel("I agree to the terms and conditions")
                .Required("You must agree to proceed")
            .Build();

        // Assert
        formConfig.Fields.Count.ShouldBe(4);
        
        // Verify all fields are properly configured
        formConfig.Fields.ShouldAllBe(f => !string.IsNullOrEmpty(f.Label));
        formConfig.Fields.Where(f => f.IsRequired).Count().ShouldBe(3); // All except Certificates
        
        // Verify file upload fields have custom renderer
        var fileFields = formConfig.Fields
            .Where(f => f.FieldName == "Resume" || f.FieldName == "Certificates")
            .ToList();
            
        fileFields.ShouldAllBe(f => f.CustomRendererType == typeof(FileUploadFieldRenderer));
        fileFields.ShouldAllBe(f => f.AdditionalAttributes.ContainsKey("FileUploadConfiguration"));
    }
}