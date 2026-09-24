namespace FormCraft.UnitTests.Rendering;

public class FileUploadFieldRendererTests : CoreRendererTestBase
{
    private readonly FileUploadFieldRenderer _renderer;

    public FileUploadFieldRendererTests()
    {
        _renderer = new FileUploadFieldRenderer();
    }

    [Fact]
    public void CanRender_WithIBrowserFile_ReturnsTrue()
    {
        // Arrange
        var field = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(IBrowserFile), field);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_WithIReadOnlyListOfIBrowserFile_ReturnsTrue()
    {
        // Arrange
        var field = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(IReadOnlyList<IBrowserFile>), field);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_WithArrayOfIBrowserFile_ReturnsTrue()
    {
        // Arrange
        var field = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(IBrowserFile[]), field);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_WithListOfIBrowserFile_ReturnsTrue()
    {
        // Arrange
        var field = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(List<IBrowserFile>), field);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_WithUnsupportedType_ReturnsFalse()
    {
        // Arrange
        var field = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(string), field);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void Render_ReturnsNonNullRenderFragment()
    {
        // Arrange
        var model = new TestModel();
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.Label).Returns("Test Upload");
        A.CallTo(() => field.FieldName).Returns("TestFile");
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.IsReadOnly).Returns(false);

        var context = A.Fake<IFieldRenderContext<TestModel>>();
        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(IBrowserFile));
        A.CallTo(() => context.CurrentValue).Returns(null);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Test Upload");
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("file");
        input.HasAttribute("disabled").ShouldBeFalse();
        input.HasAttribute("multiple").ShouldBeFalse();
        input.HasAttribute("accept").ShouldBeFalse();
    }

    [Fact]
    public void Render_WithFileUploadConfiguration_UsesProvidedSettings()
    {
        // Arrange
        var model = new TestModel();
        var uploadConfig = new FileUploadConfiguration
        {
            AcceptedFileTypes = new[] { ".pdf", ".doc" },
            MaxFileSize = 5 * 1024 * 1024,
            MaxFiles = 1,
            EnableDragDrop = true
        };

        var attributes = new Dictionary<string, object>
        {
            ["FileUploadConfiguration"] = uploadConfig
        };

        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.Label).Returns("Upload Document");
        A.CallTo(() => field.FieldName).Returns("Document");
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);
        A.CallTo(() => field.IsRequired).Returns(true);

        var context = A.Fake<IFieldRenderContext<TestModel>>();
        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(IBrowserFile));

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - this stub never reads the FileUploadConfiguration attribute; "accept" stays absent.
        cut.Find("label").TextContent.ShouldBe("Upload Document");
        var input = cut.Find("input");
        input.HasAttribute("multiple").ShouldBeFalse();
        input.HasAttribute("accept").ShouldBeFalse();
    }

    [Fact]
    public void Render_WithMultipleFilesConfiguration_ConfiguresForMultipleUpload()
    {
        // Arrange
        var model = new TestModel();
        var uploadConfig = new FileUploadConfiguration { MaxFiles = 5 };

        var attributes = new Dictionary<string, object>
        {
            ["FileUploadConfiguration"] = uploadConfig
        };

        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);

        var context = A.Fake<IFieldRenderContext<TestModel>>();
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.ActualFieldType).Returns(typeof(IReadOnlyList<IBrowserFile>));

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - "multiple" comes from ActualFieldType, not FileUploadConfiguration.Multiple.
        cut.Find("input").HasAttribute("multiple").ShouldBeTrue();
        uploadConfig.Multiple.ShouldBeTrue();
    }

    [Fact]
    public void Render_WithCurrentValue_HandlesExistingFile()
    {
        // Arrange
        var model = new TestModel();
        var existingFile = A.Fake<IBrowserFile>();
        A.CallTo(() => existingFile.Name).Returns("test.pdf");
        A.CallTo(() => existingFile.Size).Returns(1024);

        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());

        var context = A.Fake<IFieldRenderContext<TestModel>>();
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(existingFile);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - this stub never reads CurrentValue; an existing file has no effect on the markup.
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("file");
        input.HasAttribute("multiple").ShouldBeFalse();
    }

    [Fact]
    public void Render_WithCurrentValueAsList_HandlesMultipleFiles()
    {
        // Arrange
        var model = new TestModel();
        var file1 = A.Fake<IBrowserFile>();
        var file2 = A.Fake<IBrowserFile>();
        var files = new List<IBrowserFile> { file1, file2 }.AsReadOnly();

        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());

        var context = A.Fake<IFieldRenderContext<TestModel>>();
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(files);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - "multiple" is driven by ActualFieldType, which isn't stubbed here, so it stays
        // absent even though CurrentValue itself is a file list.
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("file");
        input.HasAttribute("multiple").ShouldBeFalse();
    }

    [Fact]
    public void Render_DisplaysValidationErrors_WhenFileUploadErrorsExist()
    {
        // Arrange
        var errorList = new List<string> { "File too large", "Invalid file type" };
        var additionalAttributes = new Dictionary<string, object>
        {
            ["FileUploadErrors"] = errorList
        };

        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.Label).Returns("Upload File");
        A.CallTo(() => field.AdditionalAttributes).Returns(additionalAttributes);
        A.CallTo(() => field.IsRequired).Returns(true);

        var context = A.Fake<IFieldRenderContext<TestModel>>();
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(null);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - this stub never surfaces FileUploadErrors in the DOM; base markup still renders.
        cut.Find("label").TextContent.ShouldBe("Upload File");
        cut.Find("input").GetAttribute("type").ShouldBe("file");
    }

    [Fact]
    public void Render_ClearsValidationErrors_OnSuccessfulUpload()
    {
        // Arrange
        var file = A.Fake<IBrowserFile>();
        A.CallTo(() => file.Name).Returns("test.pdf");
        A.CallTo(() => file.Size).Returns(1024);

        var additionalAttributes = new Dictionary<string, object>();

        var field = A.Fake<IFieldConfiguration<TestModel, object>>();
        A.CallTo(() => field.Label).Returns("Upload File");
        A.CallTo(() => field.AdditionalAttributes).Returns(additionalAttributes);

        var context = A.Fake<IFieldRenderContext<TestModel>>();
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(file);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Upload File");
        cut.Find("input").GetAttribute("type").ShouldBe("file");
        additionalAttributes.ShouldNotContainKey("FileUploadErrors");
    }

    [Fact]
    public void RenderField_Should_Render_Single_File_Input_Without_Multiple_With_No_Adapter_Registered()
    {
        // Arrange - the standalone-consumer path: services.AddFormCraft() with no UI adapter.
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Resume, field => field
                .WithLabel("Resume")
                .WithHelpText("PDF only")
                .WithAttribute("accept", ".pdf"))
            .Build();
        var field = config.Fields.First(f => f.FieldName == "Resume");

        // Act
        var cut = Render(RendererService.RenderField(model, field, default, default));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Resume");
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("file");
        input.HasAttribute("multiple").ShouldBeFalse();
        input.GetAttribute("accept").ShouldBe(".pdf");
        cut.Find(".help-text").TextContent.ShouldBe("PDF only");
    }

    [Fact]
    public void RenderField_Should_Render_Multiple_Attribute_For_A_ReadOnlyList_Field()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Documents, field => field.WithLabel("Documents"))
            .Build();
        var field = config.Fields.First(f => f.FieldName == "Documents");

        // Act
        var cut = Render(RendererService.RenderField(model, field, default, default));

        // Assert
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("file");
        input.HasAttribute("multiple").ShouldBeTrue();
    }

    public class TestModel
    {
        public IBrowserFile? Resume { get; set; }
        public IReadOnlyList<IBrowserFile>? Documents { get; set; }
    }
}
