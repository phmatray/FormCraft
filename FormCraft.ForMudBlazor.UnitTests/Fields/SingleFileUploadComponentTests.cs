using FormCraft.ForMudBlazor.Extensions;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Regression tests for issue #149: the single-file upload component must bind
/// MudFileUpload with T = IBrowserFile so the underlying input does not carry
/// the 'multiple' attribute, while still wiring selected files to the model.
/// </summary>
public class SingleFileUploadComponentTests : MudBlazorTestBase
{
    public SingleFileUploadComponentTests()
    {
        ((IServiceCollection)Services).AddFormCraftMudBlazor();
    }

    private IRenderedComponent<FormCraftComponent<DocumentModel>> RenderSingleFileForm(DocumentModel model)
    {
        var config = FormBuilder<DocumentModel>
            .Create()
            .AddField(x => x.Resume, field => field
                .WithLabel("Upload Resume"))
            .Build();

        return Render<FormCraftComponent<DocumentModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));
    }

    [Fact]
    public void SingleFileUpload_Should_Not_Render_Multiple_Attribute_On_Input()
    {
        // Arrange
        var model = new DocumentModel();

        // Act
        var component = RenderSingleFileForm(model);

        // Assert - the OS file picker must not allow multi-selection
        var input = component.Find("input[type=file]");
        input.HasAttribute("multiple").ShouldBeFalse();
    }

    [Fact]
    public void SingleFileUpload_Should_Bind_With_Single_Value_T()
    {
        // Arrange
        var model = new DocumentModel();

        // Act
        var component = RenderSingleFileForm(model);

        // Assert - bound as MudFileUpload<IBrowserFile>, not IReadOnlyList<IBrowserFile>
        component.FindComponents<MudFileUpload<IBrowserFile>>().Count.ShouldBe(1);
        component.FindComponents<MudFileUpload<IReadOnlyList<IBrowserFile>>>().ShouldBeEmpty();
    }

    [Fact]
    public async Task SingleFileUpload_Should_Set_Model_Property_When_File_Selected()
    {
        // Arrange
        var model = new DocumentModel();
        var component = RenderSingleFileForm(model);
        var fileUpload = component.FindComponent<MudFileUpload<IBrowserFile>>();
        var file = A.Fake<IBrowserFile>();
        A.CallTo(() => file.Name).Returns("resume.pdf");
        A.CallTo(() => file.Size).Returns(1024);

        // Act - simulate MudFileUpload notifying a selected file
        await component.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(file));

        // Assert
        model.Resume.ShouldBeSameAs(file);
        component.Markup.ShouldContain("resume.pdf");
    }

    [Fact]
    public void SingleFileUpload_Input_Should_Have_Accessible_Label()
    {
        // Arrange
        var model = new DocumentModel();

        // Act
        var component = RenderSingleFileForm(model);

        // Assert - a11y: the hidden file input carries the field label (issue #153)
        var input = component.Find("input[type=file]");
        input.GetAttribute("aria-label").ShouldBe("Upload Resume");
    }

    private class DocumentModel
    {
        public IBrowserFile? Resume { get; set; }
    }
}
