namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Regression and parity tests for #340: <c>.AsFileUpload(...)</c>/<c>.AsMultipleFileUpload(...)</c>
/// constraints must reach every upload component identically, through one resolution path
/// (<see cref="UploadConstraintResolver"/>) rather than each component picking its own keys.
/// </summary>
/// <remarks>
/// Before the fix, the single-file component never read the <c>FileUploadConfiguration</c> attribute
/// at all — <see cref="Single_File_Upload_Should_Resolve_Configured_Accept_And_Max_Size"/> pins that.
/// The multiple-file component already worked; its mirror test below guards against a regression
/// while the two components move onto the shared resolver.
/// </remarks>
public class FileUploadConstraintTests : MudBlazorTestBase
{
    [Fact]
    public void Single_File_Upload_Should_Resolve_Configured_Accept_And_Max_Size()
    {
        // Arrange
        var model = new TestModel();
        var component = RenderStandaloneSingleUpload(model, f => f
            .WithLabel("Resume")
            .AsFileUpload(acceptedFileTypes: [".pdf"], maxFileSize: 5 * 1024 * 1024));

        // Assert - both null before the fix, since this component read only the raw keys
        // .AsFileUpload never writes.
        component.Instance.Accept.ShouldBe(".pdf");
        component.Instance.MaxFileSize.ShouldBe(5 * 1024 * 1024);
    }

    [Fact]
    public void Multiple_File_Upload_Should_Resolve_Configured_Accept_And_Max_Size()
    {
        // Arrange - the contrast test: this component already read FileUploadConfiguration before
        // #340, so this must keep passing once it moves onto the shared resolver.
        var model = new MultiTestModel();
        var component = RenderStandaloneMultipleUpload(model, f => f
            .WithLabel("Documents")
            .AsMultipleFileUpload(acceptedFileTypes: [".pdf"], maxFileSize: 5 * 1024 * 1024));

        // Assert
        component.Instance.Accept.ShouldBe(".pdf");
        component.Instance.MaxFileSize.ShouldBe(5 * 1024 * 1024);
    }

    [Fact]
    public void The_Same_Configuration_Should_Resolve_Identically_On_Both_Components()
    {
        // Arrange - the drift itself is the defect, not either single reading: one configuration,
        // applied through each component's own builder extension, must resolve to the same Accept
        // and MaxFileSize on both.
        var singleModel = new TestModel();
        var single = RenderStandaloneSingleUpload(singleModel, f => f
            .AsFileUpload(acceptedFileTypes: [".png", ".jpg"], maxFileSize: 2 * 1024 * 1024));

        var multiModel = new MultiTestModel();
        var multi = RenderStandaloneMultipleUpload(multiModel, f => f
            .AsMultipleFileUpload(acceptedFileTypes: [".png", ".jpg"], maxFileSize: 2 * 1024 * 1024));

        // Assert
        single.Instance.Accept.ShouldBe(multi.Instance.Accept);
        single.Instance.MaxFileSize.ShouldBe(multi.Instance.MaxFileSize);
    }

    [Fact]
    public void A_Raw_Int_MaxFileSize_Attribute_Should_Bind_On_The_Single_File_Component()
    {
        // Arrange - a hand-written .WithAttribute(...) call (no .AsFileUpload) boxes the literal as
        // `int`; GetAttribute<long?> alone would fail `is long?` and silently return null.
        var model = new TestModel();
        var component = RenderStandaloneSingleUpload(model, f => f.WithAttribute("MaxFileSize", 5_000_000));

        // Assert
        component.Instance.MaxFileSize.ShouldBe(5_000_000L);
    }

    [Fact]
    public void A_Raw_Int_MaxFileSize_Attribute_Should_Bind_On_The_Multiple_File_Component()
    {
        // Arrange
        var model = new MultiTestModel();
        var component = RenderStandaloneMultipleUpload(model, f => f.WithAttribute("MaxFileSize", 5_000_000));

        // Assert - and the untouched defaults (#340's edge case: resolving must not let this
        // component inherit the single-file path's MaxFiles=1) still hold.
        component.Instance.MaxFileSize.ShouldBe(5_000_000L);
        component.Instance.MaxFiles.ShouldBe(10);
    }

    private IRenderedComponent<MudBlazorFileUploadFieldComponent<TestModel>> RenderStandaloneSingleUpload(
        TestModel model,
        Action<FieldBuilder<TestModel, IBrowserFile?>> configure)
    {
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, configure)
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IBrowserFile),
            CurrentValue = model.Upload,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, _ => { }),
        };

        return Render<MudBlazorFileUploadFieldComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private IRenderedComponent<MudBlazorMultipleFileUploadComponent<MultiTestModel>> RenderStandaloneMultipleUpload(
        MultiTestModel model,
        Action<FieldBuilder<MultiTestModel, IReadOnlyList<IBrowserFile>>> configure)
    {
        var config = FormBuilder<MultiTestModel>
            .Create()
            .AddField(x => x.Documents, configure)
            .Build();

        var context = new FieldRenderContext<MultiTestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IReadOnlyList<IBrowserFile>),
            CurrentValue = model.Documents,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, _ => { }),
        };

        return Render<MudBlazorMultipleFileUploadComponent<MultiTestModel>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private sealed class TestModel
    {
        public IBrowserFile? Upload { get; set; }
    }

    private sealed class MultiTestModel
    {
        public IReadOnlyList<IBrowserFile>? Documents { get; set; }
    }
}
