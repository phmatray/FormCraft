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
/// <b>The fix (Approach A from #338, established by measurement in Task 1, not from documentation):
/// </b> MudBlazor 9.10.0's <c>MudFileUpload.razor</c> renders the built-in list only when its own
/// <c>SelectedTemplate</c> parameter is <c>null</c> — "When <c>null</c>, a default chip list is
/// shown." Supplying a non-null <see cref="MudFileUpload{T}.SelectedTemplate"/> (even one that
/// renders nothing — <see cref="MudBlazorFileUploadComponentBase{TModel,TValue}.SuppressBuiltInFileList"/>)
/// suppresses that list entirely, while leaving <c>CustomContent</c> untouched. So Task 2 keeps
/// FormCraft's own chips — and their working <c>RemoveFile</c> close button, which #318's focus
/// handling and #324's chip tests already depend on — and suppresses MudBlazor's duplicate instead
/// of dropping FormCraft's own.
/// </para>
/// <para>
/// The single-file component duplicated too, but <i>asymmetrically</i> — measured rather than
/// inferred from the multiple-file case, per the issue's own instruction (it binds
/// <c>Files</c>/<c>FilesChanged</c> one-way where the multiple-file component uses
/// <c>@bind-Files</c>). FormCraft's own chip there carries no <c>OnClose</c> at all — a single file
/// is only ever removed via "Clear", which already routes through <c>ClearAsync</c> and #281's
/// focus-restore — so MudBlazor's own chip was the *only* close button the field ever had, and it
/// never went through that path. Suppressing it removes an inconsistent removal affordance, not a
/// control the field promised.
/// </para>
/// </remarks>
public class FileUploadFileListTests : FocusAssertingTestBase
{
    [Fact]
    public void MudFileUpload_SelectedTemplate_Parameter_Suppresses_The_Built_In_File_List()
    {
        // Task 1 Step 3's throwaway proof, kept as a regression guard for the mechanism the rest of
        // this file's fix relies on: an empty SelectedTemplate leaves no MudBlazor chip, and no
        // MudBlazor filelist wrapper, behind.
        var component = Render<MudFileUpload<IBrowserFile>>(parameters => parameters
            .Add(p => p.Files, new StubBrowserFile("x.png"))
            .Add(p => p.SelectedTemplate, (RenderFragment<IBrowserFile?>)(_ => builder => { })));

        component.FindAll(".mud-chip-close-button").ShouldBeEmpty();
        component.FindAll(".mud-file-upload-filelist").ShouldBeEmpty();
    }

    [Fact]
    public void Two_Selected_Files_Should_Render_Exactly_Two_Close_Buttons()
    {
        // Inverted from Task 1's characterisation (was 4: FormCraft's own 2 + MudBlazor's duplicate
        // 2). MudBlazor's own list is now suppressed, so only FormCraft's chips — each routed
        // through RemoveFile — remain.
        var component = RenderStandaloneMultipleUpload(new TestModel
        {
            Uploads = new List<IBrowserFile> { new StubBrowserFile("a.png"), new StubBrowserFile("b.png") },
        });

        component.FindAll(".mud-chip-close-button").Count.ShouldBe(2);
    }

    [Fact]
    public void A_Single_Selected_File_Should_Render_No_Close_Button()
    {
        // Inverted from Task 1's characterisation (was 1: MudBlazor's own duplicate chip — see the
        // class remarks for why 0, not 1, is the correct end state here).
        var component = RenderStandaloneSingleUpload(new TestModel { Upload = new StubBrowserFile("passport.png") });

        component.FindAll(".mud-chip-close-button").ShouldBeEmpty();
    }

    [Fact]
    public async Task Removing_The_Only_File_Chip_Should_Move_Focus_To_Browse()
    {
        // Task 2 Step 4 — the surviving close button is unambiguous now that MudBlazor's duplicate
        // is gone, so its identity is asserted by consequence: only RemoveFile's own focus-restore
        // (#318) moves focus to Browse when the last file goes. A click that reached MudBlazor's own
        // (now-suppressed) removal instead would leave focus wherever the click landed.
        var component = RenderStandaloneMultipleUpload(new TestModel
        {
            Uploads = new List<IBrowserFile> { new StubBrowserFile("passport.png") },
        });

        var browseId = await LearnElementIdAsync(component, component.FindComponents<MudButton>()[0].Instance);
        var focusesBefore = FocusCount();

        var closeButtons = component.FindAll(".mud-chip-close-button");
        closeButtons.Count.ShouldBe(1);
        await component.InvokeAsync(() => closeButtons[0].Click());

        component.FindAll(".mud-chip-close-button").ShouldBeEmpty();
        FocusCount().ShouldBe(focusesBefore + 1);
        LastFocusedElementId().ShouldBe(browseId);
    }

    [Fact]
    public void More_Than_Three_Selected_Files_Should_Still_Render_Only_Three_Close_Buttons()
    {
        // Edge case named by the issue's own Spec: FormCraft's chip loop caps at
        // CurrentValue.Take(3) plus a non-closable "+N more" summary chip, so a 4th+ file has no
        // individual removal affordance even after #338 — asserted explicitly rather than left
        // implicit, since the two lists no longer even exist to "agree" or "disagree" about it.
        var component = RenderStandaloneMultipleUpload(new TestModel
        {
            Uploads = new List<IBrowserFile>
            {
                new StubBrowserFile("a.png"),
                new StubBrowserFile("b.png"),
                new StubBrowserFile("c.png"),
                new StubBrowserFile("d.png"),
            },
        });

        component.FindAll(".mud-chip-close-button").Count.ShouldBe(3);
    }

    [Fact]
    public void Zero_Selected_Files_Should_Render_No_Close_Buttons_On_The_Multiple_File_Component()
    {
        var component = RenderStandaloneMultipleUpload(new TestModel());

        component.FindAll(".mud-chip-close-button").ShouldBeEmpty();
    }

    [Fact]
    public void Zero_Selected_Files_Should_Render_No_Close_Buttons_On_The_Single_File_Component()
    {
        var component = RenderStandaloneSingleUpload(new TestModel());

        component.FindAll(".mud-chip-close-button").ShouldBeEmpty();
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
