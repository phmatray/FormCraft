using FormCraft.ForMudBlazor.UnitTests.TestSupport;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Pins issue #338: <c>MudFileUpload</c> renders its own built-in file list
/// (<c>.mud-file-upload-filelist</c> / <c>div.mud-file-upload-files</c>) <i>in addition to</i> the
/// <c>CustomContent</c> drop zone FormCraft supplies, so a selected file can appear twice, each copy
/// with its own remove control.
/// </summary>
/// <remarks>
/// <para>
/// <b>Outcome of Task 1's measurement (not assumed from documentation):</b> MudBlazor 9.10.0's
/// <c>MudFileUpload.razor</c> renders the built-in list only when its own <c>SelectedTemplate</c>
/// parameter is <c>null</c> — "When <c>null</c>, a default chip list is shown." Supplying a non-null
/// <see cref="MudFileUpload{T}.SelectedTemplate"/> (even one that renders nothing) suppresses that
/// list entirely, while leaving <c>CustomContent</c> untouched. That is Approach A from #338: this
/// duplication is suppressible, so Task 2 keeps FormCraft's own chips (and their working
/// <c>RemoveFile</c> close button, which #318's focus handling and #324's chip tests already depend
/// on) and sets an empty <c>SelectedTemplate</c> on both upload components instead of dropping them.
/// </para>
/// <para>
/// The single-file component duplicates too, but <i>asymmetrically</i> — measured in
/// <see cref="One_Selected_File_Renders_One_Close_Button_Today"/> rather than inferred from the
/// multiple-file case, per the issue's own instruction (it binds <c>Files</c>/<c>FilesChanged</c>
/// one-way where the multiple-file component uses <c>@bind-Files</c>). FormCraft's own chip there
/// carries no <c>OnClose</c> at all — a single file is only ever removed via "Clear" — so only
/// MudBlazor's built-in chip contributes a close button; there is exactly one duplicate list, not two
/// close buttons on the same file.
/// </para>
/// </remarks>
public class FileUploadFileListTests : FocusAssertingTestBase
{
    [Fact]
    public void MudFileUpload_SelectedTemplate_Parameter_Suppresses_The_Built_In_File_List()
    {
        // Throwaway-turned-regression proof (Task 1 Step 3) for the remarks above: an empty
        // SelectedTemplate leaves no MudBlazor chip, and no MudBlazor filelist wrapper, behind.
        var component = Render<MudFileUpload<IBrowserFile>>(parameters => parameters
            .Add(p => p.Files, new StubBrowserFile("x.png"))
            .Add(p => p.SelectedTemplate, (RenderFragment<IBrowserFile?>)(_ => builder => { })));

        component.FindAll(".mud-chip-close-button").ShouldBeEmpty();
        component.FindAll(".mud-file-upload-filelist").ShouldBeEmpty();
    }

    [Fact]
    public void Two_Selected_Files_Render_Four_Close_Buttons_Today()
    {
        // CHARACTERISATION — pins today's #338 defect; inverted by Task 2. FormCraft's own two
        // chips (each with a working OnClose→RemoveFile) plus MudBlazor's own built-in two chips
        // (each with MudBlazor's own OnClose→its internal removal) is 4 close buttons for 2 files.
        var component = RenderStandaloneMultipleUpload(new TestModel
        {
            Uploads = new List<IBrowserFile> { new StubBrowserFile("a.png"), new StubBrowserFile("b.png") },
        });

        component.FindAll(".mud-chip-close-button").Count.ShouldBe(4);
    }

    [Fact]
    public void One_Selected_File_Renders_One_Close_Button_Today()
    {
        // CHARACTERISATION — the single-file component's own chip has no OnClose (see class
        // remarks), so only MudBlazor's built-in chip contributes a close button: one duplicate
        // list, one close button — not the symmetrical "two per file" the multiple-file case has.
        var component = RenderStandaloneSingleUpload(new TestModel { Upload = new StubBrowserFile("passport.png") });

        component.FindAll(".mud-chip-close-button").Count.ShouldBe(1);
    }

    private IRenderedComponent<MudBlazorFileUploadFieldComponent<TestModel>> RenderStandaloneSingleUpload(
        TestModel model)
    {
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IBrowserFile),
            CurrentValue = model.Upload,
        };

        return Render<MudBlazorFileUploadFieldComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private IRenderedComponent<MudBlazorMultipleFileUploadComponent<TestModel>> RenderStandaloneMultipleUpload(
        TestModel model)
    {
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Uploads, f => f.WithLabel("Certificates"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IReadOnlyList<IBrowserFile>),
            CurrentValue = model.Uploads,
        };

        return Render<MudBlazorMultipleFileUploadComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private sealed class TestModel
    {
        public IBrowserFile? Upload { get; set; }

        public IReadOnlyList<IBrowserFile>? Uploads { get; set; }
    }

    private sealed class StubBrowserFile(string name) : IBrowserFile
    {
        public string Name => name;

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => 1024;

        public string ContentType => "image/png";

        public Stream OpenReadStream(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default) => new MemoryStream();
    }
}
