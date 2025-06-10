namespace FormCraft.UnitTests.Extensions;

public class FileUploadExtensionsTests
{
    private class TestModel
    {
        public IBrowserFile? SingleFile { get; set; }
        public IReadOnlyList<IBrowserFile>? MultipleFiles { get; set; }
    }

    [Fact]
    public void AsFileUpload_ConfiguresFieldForSingleFileUpload()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.SingleFile!, field => field
                .AsFileUpload(
                    acceptedFileTypes: new[] { ".pdf", ".doc" },
                    maxFileSize: 5 * 1024 * 1024,
                    showPreview: false,
                    enableDragDrop: false))
            .Build();

        // Assert
        formConfig.Fields.Count.ShouldBe(1);
        var field = formConfig.Fields.First();
        field.AdditionalAttributes.ShouldContainKey("FileUploadConfiguration");
        
        var uploadConfig = field.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        uploadConfig.ShouldNotBeNull();
        uploadConfig.AcceptedFileTypes.ShouldBe(new[] { ".pdf", ".doc" });
        uploadConfig.MaxFileSize.ShouldBe(5 * 1024 * 1024);
        uploadConfig.MaxFiles.ShouldBe(1);
        uploadConfig.ShowPreview.ShouldBeFalse();
        uploadConfig.EnableDragDrop.ShouldBeFalse();
    }

    [Fact]
    public void AsFileUpload_UsesDefaultValues_WhenNotSpecified()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.SingleFile!, field => field
                .AsFileUpload())
            .Build();

        // Assert
        var field = formConfig.Fields.First();
        var uploadConfig = field.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        
        uploadConfig.ShouldNotBeNull();
        uploadConfig.AcceptedFileTypes.ShouldBeNull();
        uploadConfig.MaxFileSize.ShouldBeNull();
        uploadConfig.MaxFiles.ShouldBe(1);
        uploadConfig.ShowPreview.ShouldBeTrue();
        uploadConfig.EnableDragDrop.ShouldBeTrue();
    }

    [Fact]
    public void AsMultipleFileUpload_ConfiguresFieldForMultipleFiles()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.MultipleFiles!, field => field
                .AsMultipleFileUpload(
                    maxFiles: 5,
                    acceptedFileTypes: new[] { ".jpg", ".png" },
                    maxFileSize: 2 * 1024 * 1024,
                    showPreview: true,
                    enableDragDrop: true))
            .Build();

        // Assert
        var field = formConfig.Fields.First();
        field.AdditionalAttributes.ShouldContainKey("FileUploadConfiguration");
        
        var uploadConfig = field.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        uploadConfig.ShouldNotBeNull();
        uploadConfig.AcceptedFileTypes.ShouldBe(new[] { ".jpg", ".png" });
        uploadConfig.MaxFileSize.ShouldBe(2 * 1024 * 1024);
        uploadConfig.MaxFiles.ShouldBe(5);
        uploadConfig.ShowPreview.ShouldBeTrue();
        uploadConfig.EnableDragDrop.ShouldBeTrue();
    }

    [Fact]
    public void AsMultipleFileUpload_UsesDefaultValues_WhenNotSpecified()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.MultipleFiles!, field => field
                .AsMultipleFileUpload())
            .Build();

        // Assert
        var field = formConfig.Fields.First();
        var uploadConfig = field.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        
        uploadConfig.ShouldNotBeNull();
        uploadConfig.AcceptedFileTypes.ShouldBeNull();
        uploadConfig.MaxFileSize.ShouldBeNull();
        uploadConfig.MaxFiles.ShouldBe(10); // Default for multiple
        uploadConfig.ShowPreview.ShouldBeTrue();
        uploadConfig.EnableDragDrop.ShouldBeTrue();
    }

    [Fact]
    public void AddFileUploadField_CreatesCompleteFieldConfiguration()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddFileUploadField(
                x => x.SingleFile!,
                "Upload Document",
                acceptedFileTypes: new[] { ".pdf" },
                maxFileSize: 1024 * 1024,
                required: true)
            .Build();

        // Assert
        formConfig.Fields.Count.ShouldBe(1);
        
        var field = formConfig.Fields.First();
        field.Label.ShouldBe("Upload Document");
        field.IsRequired.ShouldBeTrue();
        
        var uploadConfig = field.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        uploadConfig.ShouldNotBeNull();
        uploadConfig.AcceptedFileTypes.ShouldBe(new[] { ".pdf" });
        uploadConfig.MaxFileSize.ShouldBe(1024 * 1024);
    }

    [Fact]
    public void AddFileUploadField_WithOptionalField_DoesNotSetRequired()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddFileUploadField(
                x => x.SingleFile!,
                "Optional Upload",
                required: false)
            .Build();

        // Assert
        var field = formConfig.Fields.First();
        field.IsRequired.ShouldBeFalse();
    }

    [Fact]
    public void AddMultipleFileUploadField_CreatesCompleteFieldConfiguration()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddMultipleFileUploadField(
                x => x.MultipleFiles!,
                "Upload Photos",
                maxFiles: 3,
                acceptedFileTypes: new[] { ".jpg", ".png" },
                maxFileSize: 5 * 1024 * 1024,
                required: true)
            .Build();

        // Assert
        formConfig.Fields.Count.ShouldBe(1);
        
        var field = formConfig.Fields.First();
        field.Label.ShouldBe("Upload Photos");
        field.IsRequired.ShouldBeTrue();
        
        var uploadConfig = field.AdditionalAttributes["FileUploadConfiguration"] as FileUploadConfiguration;
        uploadConfig.ShouldNotBeNull();
        uploadConfig.MaxFiles.ShouldBe(3);
        uploadConfig.AcceptedFileTypes.ShouldBe(new[] { ".jpg", ".png" });
        uploadConfig.MaxFileSize.ShouldBe(5 * 1024 * 1024);
    }

    [Fact]
    public void AddMultipleFileUploadField_SetsAppropriateRequiredMessage()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddMultipleFileUploadField(
                x => x.MultipleFiles!,
                "Documents",
                required: true)
            .Build();

        // Assert
        var field = formConfig.Fields.First();
        
        // Assert
        field.IsRequired.ShouldBeTrue();
        field.Validators.Count.ShouldBeGreaterThan(0);
        
        // The validator is wrapped in ValidatorWrapper, so we need to check the wrapped validator
        var validatorWrapper = field.Validators.First();
        validatorWrapper.ShouldBeOfType<ValidatorWrapper<TestModel, IReadOnlyList<IBrowserFile>>>();
    }

    [Fact]
    public void FileUploadExtensions_SetCustomRendererInstance()
    {
        // Arrange & Act
        var formConfig = FormBuilder<TestModel>.Create()
            .AddField(x => x.SingleFile!, field => field
                .AsFileUpload())
            .Build();

        // Assert
        var field = formConfig.Fields.First();
        field.AdditionalAttributes.ShouldContainKey("CustomRendererInstance");
        field.AdditionalAttributes["CustomRendererInstance"].ShouldBeOfType<FileUploadFieldRenderer>();
        field.CustomRendererType.ShouldBe(typeof(FileUploadFieldRenderer));
    }
}