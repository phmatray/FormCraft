using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// <c>IBrowserFile</c> fields render a Fluent file input, single or multiple according to the
/// field's own type (#278).
/// </summary>
/// <remarks>
/// The required marker follows the MudBlazor adapter's #262 finding rather than the ordinary
/// <c>aria-required</c> route: a file input is a visually hidden control behind a drop zone, so an
/// ARIA attribute on it reaches no one navigating by focus. The requirement is announced on the
/// field's own visible label instead, which is a channel both mouse and keyboard users meet.
/// </remarks>
public class FluentUIFileUploadFieldComponentTests : FluentUITestBase
{
    [Fact]
    public void A_Single_File_Field_Should_Render_A_Fluent_Input_File()
    {
        // Act
        var component = RenderSingleFileField();

        // Assert
        var input = component.FindComponent<FluentInputFile>().Instance;
        input.Multiple.ShouldBeFalse();
    }

    [Fact]
    public void A_Multiple_File_Field_Should_Render_In_Multiple_Mode()
    {
        // Arrange
        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Attachments, f => f.WithLabel("Attachments"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));

        // Assert
        var input = component.FindComponent<FluentInputFile>().Instance;
        input.Multiple.ShouldBeTrue();
    }

    [Fact]
    public void The_Canonical_Multi_Upload_Type_Should_Render()
    {
        // Arrange - IReadOnlyList<IBrowserFile> is the type core's .AsMultipleFileUpload(...) is
        // declared on, so this is what a form built through the public API actually carries.
        // Matching only List<> left it falling through to "Unsupported field type".
        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Documents, f => f.WithLabel("Documents"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.FindComponent<FluentInputFile>().Instance.Multiple.ShouldBeTrue();
        component.Markup.ShouldNotContain("Unsupported field type");
    }

    [Fact]
    public void Upload_Constraints_Configured_Through_The_Builder_Should_Reach_The_Input()
    {
        // Arrange - .AsFileUpload(...) writes a FileUploadConfiguration, not the raw attribute
        // keys; reading only the raw keys silently accepted every type at the default size cap.
        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Resume, f => f
                .WithLabel("Resume")
                .AsFileUpload(acceptedFileTypes: [".pdf", ".docx"], maxFileSize: 2 * 1024 * 1024))
            .Build();

        // Act
        var component = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));

        // Assert
        var input = component.FindComponent<FluentInputFile>().Instance;
        input.Accept.ShouldBe(".pdf,.docx");
        input.MaximumFileSize.ShouldBe(2 * 1024 * 1024);
    }

    [Fact]
    public void A_File_Field_Should_Render_Its_Label()
    {
        // Act
        var component = RenderSingleFileField();

        // Assert
        component.Markup.ShouldContain("Resume");
    }

    [Fact]
    public void A_Required_File_Field_Should_Mark_Its_Visible_Label()
    {
        // Arrange & Act
        var component = RenderSingleFileField(f => f.WithLabel("Resume").Required("Resume is required"));

        // Assert - a marker a sighted user sees, on the label rather than the hidden input (#262)
        component.Find("[data-testid=formcraft-upload-required-marker]").TextContent.ShouldContain("*");
    }

    [Fact]
    public void An_Optional_File_Field_Should_Carry_No_Required_Marker()
    {
        // Act
        var component = RenderSingleFileField();

        // Assert
        component.FindAll("[data-testid=formcraft-upload-required-marker]").ShouldBeEmpty();
    }

    [Fact]
    public void A_Required_File_Field_Should_Describe_The_Requirement_To_Assistive_Technology()
    {
        // Arrange & Act
        var component = RenderSingleFileField(f => f.WithLabel("Resume").Required("Resume is required"));

        // Assert - the hint exists and something points at it
        var hint = component.Find("[data-testid=formcraft-upload-required-hint]");
        hint.Id.ShouldNotBeNullOrEmpty();
        component.Find($"[aria-describedby~='{hint.Id}']").ShouldNotBeNull();
    }

    [Fact]
    public void Two_Required_Upload_Fields_Should_Not_Share_A_Hint_Id()
    {
        // Arrange - the same field rendered twice is the real collision case (#262): a field name
        // is not unique in a document once collections and repeated forms exist.
        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Resume, f => f.WithLabel("Resume").Required("Resume is required"))
            .Build();

        // Act - two independent renders of the same configuration
        var first = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));
        var second = Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));

        // Assert
        var firstId = first.Find("[data-testid=formcraft-upload-required-hint]").Id;
        var secondId = second.Find("[data-testid=formcraft-upload-required-hint]").Id;
        firstId.ShouldNotBe(secondId);
    }

    private IRenderedComponent<FormCraftComponent<UploadModel>> RenderSingleFileField(
        Action<FieldBuilder<UploadModel, IBrowserFile?>>? configure = null)
    {
        configure ??= f => f.WithLabel("Resume");

        var config = FormBuilder<UploadModel>.Create()
            .AddField(x => x.Resume, configure)
            .Build();

        return Render<FormCraftComponent<UploadModel>>(p => p
            .Add(c => c.Model, new UploadModel())
            .Add(c => c.Configuration, config));
    }

    /// <summary>Model with single- and multiple-file fields.</summary>
    public class UploadModel
    {
        /// <summary>A single file.</summary>
        public IBrowserFile? Resume { get; set; }

        /// <summary>Several files, as a concrete list.</summary>
        public List<IBrowserFile> Attachments { get; set; } = [];

        /// <summary>Several files, typed as core's <c>.AsMultipleFileUpload(...)</c> declares them.</summary>
        public IReadOnlyList<IBrowserFile> Documents { get; set; } = [];
    }
}
