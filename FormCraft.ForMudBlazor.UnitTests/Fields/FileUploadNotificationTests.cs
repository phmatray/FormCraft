namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Characterisation tests for issue #319 — clearing a multiple-file upload assigns an empty list via
/// <c>CurrentValue = new List&lt;IBrowserFile&gt;()</c>, then <c>MudFileUpload.ClearAsync()</c> raises
/// <c>FilesChanged</c> on the two-way <c>@bind-Files="CurrentValue"</c> binding, which writes that
/// second value straight back into <c>CurrentValue</c> — so the field notifies TWICE instead of once.
/// </summary>
/// <remarks>
/// <para>
/// <b>These tests pin TODAY's behaviour, defect included — they are not testing the fix.</b> Task 2
/// of #319 changes the multiple-file component so it notifies once with an empty list; when that
/// lands, <see cref="Clearing_A_Multiple_File_Upload_Notifies_Twice_Ending_Null_Pins_Issue_319_Defect"/>
/// must be inverted (or deleted) rather than left red.
/// </para>
/// <para>
/// The harness records every <c>OnValueChanged</c> payload <b>in order</b>, because the final value
/// alone cannot distinguish "notified once with an empty list" from "notified twice, second one
/// null" — both leave nothing else to inspect once the field is standalone (no cascaded
/// <c>EditContext</c> writing the model back). Built the same way
/// <c>FileUploadClearFocusTests</c> renders both upload components standalone, via a hand-built
/// <see cref="FieldRenderContext{TModel}"/>, but with <c>OnValueChanged</c> wired to append to a
/// <see cref="List{T}"/> instead of the default no-op.
/// </para>
/// </remarks>
public class FileUploadNotificationTests : MudBlazorTestBase
{
    [Fact]
    public async Task Clearing_A_Multiple_File_Upload_Notifies_Twice_Ending_Null_Pins_Issue_319_Defect()
    {
        // Arrange
        var notifications = new List<object?>();
        var model = new TestModel { Uploads = new List<IBrowserFile> { new StubBrowserFile() } };
        var component = RenderStandaloneMultipleUpload(model, notifications);

        // Act - "Clear All" is the second toolbar button once a file is present
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - TWO notifications: CurrentValue = new List<IBrowserFile>() fires the first (an
        // empty, non-null list), then MudFileUpload.ClearAsync() raises FilesChanged on the two-way
        // bind, which writes straight back into CurrentValue and fires a second one. Today that
        // second value is null, so the model ends up holding null instead of an empty list.
        notifications.Count.ShouldBe(2);
        var firstNotification = notifications[0].ShouldBeAssignableTo<IReadOnlyList<IBrowserFile>>();
        firstNotification!.ShouldBeEmpty();
        notifications[1].ShouldBeNull();
    }

    [Fact]
    public async Task Clearing_A_Single_File_Upload_Notifies_Once_With_Null()
    {
        // Arrange - establishing ON EVIDENCE whether the single-file component is already correct,
        // rather than assuming it: it uses one-way Files + an explicit FilesChanged handler
        // (OnFileChanged) instead of a two-way @bind-Files, so ClearAsync's own
        // `CurrentValue = null;` and the FilesChanged echo both target the same value, and the
        // CurrentValue setter's equality guard should suppress the echo as a no-op re-write.
        var notifications = new List<object?>();
        var model = new TestModel { Upload = new StubBrowserFile() };
        var component = RenderStandaloneSingleUpload(model, notifications);

        // Act
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - exactly one notification, confirming the single-file component is NOT affected by
        // #319: no double-notify, and the model would correctly end up holding null (its own value
        // representation of "no file", unlike the multiple-file field's empty list).
        notifications.ShouldHaveSingleItem();
        notifications[0].ShouldBeNull();
    }

    private IRenderedComponent<MudBlazorMultipleFileUploadComponent<TestModel>> RenderStandaloneMultipleUpload(
        TestModel model,
        List<object?> notifications)
    {
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Uploads, f => f.WithLabel("Certificates").Required("A certificate is required"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IReadOnlyList<IBrowserFile>),
            CurrentValue = model.Uploads,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, v => notifications.Add(v)),
        };

        return Render<MudBlazorMultipleFileUploadComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private IRenderedComponent<MudBlazorFileUploadFieldComponent<TestModel>> RenderStandaloneSingleUpload(
        TestModel model,
        List<object?> notifications)
    {
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Upload, f => f.WithLabel("Passport scan").Required("A scan is required"))
            .Build();

        var context = new FieldRenderContext<TestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IBrowserFile),
            CurrentValue = model.Upload,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, v => notifications.Add(v)),
        };

        return Render<MudBlazorFileUploadFieldComponent<TestModel>>(parameters => parameters
            .Add(p => p.Context, context));
    }

    private sealed class TestModel
    {
        public IBrowserFile? Upload { get; set; }

        /// <summary>
        /// Exactly <c>IReadOnlyList&lt;IBrowserFile&gt;</c> — that is what
        /// <c>MudBlazorMultipleFileUploadRenderer</c> matches on, so a different list type would
        /// silently render the single-file component instead. Not that it matters here: these tests
        /// render the components directly and never go through the renderer.
        /// </summary>
        public IReadOnlyList<IBrowserFile>? Uploads { get; set; }
    }

    private sealed class StubBrowserFile : IBrowserFile
    {
        public string Name => "passport.png";

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => 1024;

        public string ContentType => "image/png";

        public Stream OpenReadStream(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default) => new MemoryStream();
    }
}
