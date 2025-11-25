namespace FormCraft.UnitTests.Core;

public class FileUploadConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var config = new FileUploadConfiguration();

        // Assert
        config.AcceptedFileTypes.ShouldBeNull();
        config.MaxFileSize.ShouldBeNull();
        config.MaxFiles.ShouldBe(1);
        config.Multiple.ShouldBeFalse();
        config.ShowPreview.ShouldBeTrue();
        config.EnableDragDrop.ShouldBeTrue();
        config.Accept.ShouldBeNull();
    }

    [Fact]
    public void Multiple_ReturnsTrue_WhenMaxFilesGreaterThanOne()
    {
        // Arrange
        var config = new FileUploadConfiguration { MaxFiles = 5 };

        // Act & Assert
        config.Multiple.ShouldBeTrue();
    }

    [Fact]
    public void Multiple_ReturnsFalse_WhenMaxFilesIsOne()
    {
        // Arrange
        var config = new FileUploadConfiguration { MaxFiles = 1 };

        // Act & Assert
        config.Multiple.ShouldBeFalse();
    }

    [Fact]
    public void Accept_ReturnsNull_WhenNoAcceptedFileTypes()
    {
        // Arrange
        var config = new FileUploadConfiguration { AcceptedFileTypes = null };

        // Act & Assert
        config.Accept.ShouldBeNull();
    }

    [Fact]
    public void Accept_ReturnsCommaSeparatedString_WhenAcceptedFileTypesSet()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            AcceptedFileTypes = new[] { ".pdf", ".doc", ".docx" }
        };

        // Act
        var result = config.Accept;

        // Assert
        result.ShouldBe(".pdf,.doc,.docx");
    }

    [Fact]
    public void ValidateFile_ReturnsSuccess_WhenFileIsNull()
    {
        // Arrange
        var config = new FileUploadConfiguration();

        // Act
        var result = config.ValidateFile(null!);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void ValidateFile_ReturnsFailure_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            MaxFileSize = 1024 * 1024 // 1MB
        };
        var file = A.Fake<IBrowserFile>();
        A.CallTo(() => file.Size).Returns(2 * 1024 * 1024); // 2MB

        // Act
        var result = config.ValidateFile(file);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("File size exceeds the maximum allowed size of 1.00 MB");
    }

    [Fact]
    public void ValidateFile_ReturnsSuccess_WhenFileSizeWithinLimit()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            MaxFileSize = 5 * 1024 * 1024 // 5MB
        };
        var file = A.Fake<IBrowserFile>();
        A.CallTo(() => file.Size).Returns(2 * 1024 * 1024); // 2MB

        // Act
        var result = config.ValidateFile(file);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateFile_ReturnsFailure_WhenFileTypeNotAccepted()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            AcceptedFileTypes = new[] { ".pdf", ".doc" }
        };
        var file = A.Fake<IBrowserFile>();
        A.CallTo(() => file.Name).Returns("test.jpg");

        // Act
        var result = config.ValidateFile(file);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("File type '.jpg' is not allowed");
        result.ErrorMessage.ShouldContain("Accepted types: .pdf, .doc");
    }

    [Fact]
    public void ValidateFile_ReturnsSuccess_WhenFileTypeAccepted()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            AcceptedFileTypes = new[] { ".pdf", ".PDF" } // Test case insensitive
        };
        var file = A.Fake<IBrowserFile>();
        A.CallTo(() => file.Name).Returns("document.PDF");

        // Act
        var result = config.ValidateFile(file);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ValidateFile_ReturnsSuccess_WhenNoFileTypeRestrictions()
    {
        // Arrange
        var config = new FileUploadConfiguration();
        var file = A.Fake<IBrowserFile>();
        A.CallTo(() => file.Name).Returns("anything.xyz");

        // Act
        var result = config.ValidateFile(file);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void GetConstraintsDescription_ReturnsEmpty_WhenNoConstraints()
    {
        // Arrange
        var config = new FileUploadConfiguration();

        // Act
        var result = config.GetConstraintsDescription();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetConstraintsDescription_IncludesAcceptedFormats_WhenSpecified()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            AcceptedFileTypes = new[] { ".pdf", ".doc" }
        };

        // Act
        var result = config.GetConstraintsDescription();

        // Assert
        result.ShouldContain("Accepted formats: .pdf, .doc");
    }

    [Fact]
    public void GetConstraintsDescription_IncludesMaxSize_WhenSpecified()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            MaxFileSize = 5 * 1024 * 1024 // 5MB
        };

        // Act
        var result = config.GetConstraintsDescription();

        // Assert
        result.ShouldContain("Max size: 5.00 MB");
    }

    [Fact]
    public void GetConstraintsDescription_IncludesMaxFiles_WhenGreaterThanOne()
    {
        // Arrange
        var config = new FileUploadConfiguration { MaxFiles = 3 };

        // Act
        var result = config.GetConstraintsDescription();

        // Assert
        result.ShouldContain("Max files: 3");
    }

    [Fact]
    public void GetConstraintsDescription_DoesNotIncludeMaxFiles_WhenOne()
    {
        // Arrange
        var config = new FileUploadConfiguration { MaxFiles = 1 };

        // Act
        var result = config.GetConstraintsDescription();

        // Assert
        result.ShouldNotContain("Max files");
    }

    [Fact]
    public void GetConstraintsDescription_CombinesAllConstraints()
    {
        // Arrange
        var config = new FileUploadConfiguration
        {
            AcceptedFileTypes = new[] { ".pdf", ".doc" },
            MaxFileSize = 10 * 1024 * 1024, // 10MB
            MaxFiles = 5
        };

        // Act
        var result = config.GetConstraintsDescription();

        // Assert
        result.ShouldContain("Accepted formats: .pdf, .doc");
        result.ShouldContain("Max size: 10.00 MB");
        result.ShouldContain("Max files: 5");
        result.ShouldContain(" • "); // Separator
    }
}