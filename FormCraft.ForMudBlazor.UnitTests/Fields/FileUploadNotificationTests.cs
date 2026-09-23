namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Notification-sequence tests for issue #319.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect (fixed by Task 2 of #319).</b> The multiple-file component used to assign
/// <c>CurrentValue = new List&lt;IBrowserFile&gt;()</c> directly in its own <c>ClearAsync</c>, then
/// <c>MudFileUpload.ClearAsync()</c> raised <c>FilesChanged</c> on the two-way
/// <c>@bind-Files="CurrentValue"</c> binding, which wrote a second, <c>null</c> value straight back
/// into <c>CurrentValue</c> — so the field notified TWICE, ending on <c>null</c>, instead of once with
/// an empty list. The component now binds <c>Files</c>/<c>FilesChanged</c> one-way, mirroring the
/// single-file component, with an explicit <c>OnFilesChanged</c> handler that normalises MudBlazor's
/// <c>null</c> to an empty list and suppresses the extra notification <c>ClearAsync()</c> still raises
/// even when the field was already empty.
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
/// <para>
/// The collection-item test below is the one exception: it renders through
/// <c>FormCraftComponent</c> with a real <c>EditContext</c> rather than this standalone harness,
/// because its claim — an empty list lands under <c>Applications[i].Attachments</c> — is
/// specifically about the parent <c>EditContext</c>'s nested-identifier wiring (#91), which a
/// standalone <see cref="FieldRenderContext{TModel}"/> does not exercise.
/// </para>
/// </remarks>
public class FileUploadNotificationTests : MudBlazorTestBase
{
    [Fact]
    public async Task Clearing_A_Multiple_File_Upload_Notifies_Once_With_A_Non_Null_Empty_List()
    {
        // Arrange
        var notifications = new List<object?>();
        var model = new TestModel { Uploads = new List<IBrowserFile> { new StubBrowserFile() } };
        var component = RenderStandaloneMultipleUpload(model, notifications);

        // Act - "Clear All" is the second toolbar button once a file is present
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - exactly ONE notification, carrying a non-null, empty list. MudFileUpload.ClearAsync()
        // raises FilesChanged(null), and OnFilesChanged is now the ONLY writer of CurrentValue on this
        // path (there is no second writer left to echo), normalising that null into an empty list.
        // The "already empty" guard below is not exercised here — see the next test for that.
        notifications.ShouldHaveSingleItem();
        var value = notifications[0].ShouldBeAssignableTo<IReadOnlyList<IBrowserFile>>();
        value!.ShouldBeEmpty();
    }

    [Fact]
    public async Task Clearing_An_Already_Empty_Multiple_File_Upload_Notifies_Zero_Times()
    {
        // Arrange - the toolbar's Clear All button does not render when the field is already empty
        // (`@if (CurrentValue?.Any() == true)`), so this path is reached by calling MudFileUpload's
        // own ClearAsync() directly (public API, not reflection) rather than a button click.
        var notifications = new List<object?>();
        var model = new TestModel { Uploads = new List<IBrowserFile>() };
        var component = RenderStandaloneMultipleUpload(model, notifications);

        // Act
        var fileUpload = component.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
        await component.InvokeAsync(() => fileUpload.Instance.ClearAsync());

        // Assert - MudFileUpload.ClearAsync() still raises FilesChanged(null) even though there was
        // nothing to clear; OnFilesChanged's guard recognises the field is already empty and
        // suppresses the would-be spurious notification.
        notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Clearing_A_Null_Starting_Multiple_File_Upload_Notifies_Zero_Times()
    {
        // Arrange - a model whose property is left at its default (null, not an empty list) is
        // "already empty" the same way an empty list is (#319 review). The guard checked only
        // `CurrentValue is { Count: 0 }`, which is false for null, so a null-starting field took the
        // "real change" branch and got assigned+notified with a spurious empty list.
        var notifications = new List<object?>();
        var model = new TestModel { Uploads = null };
        var component = RenderStandaloneMultipleUpload(model, notifications);

        // Act
        var fileUpload = component.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
        await component.InvokeAsync(() => fileUpload.Instance.ClearAsync());

        // Assert - null is already "no files"; clearing it must not raise a notification either.
        notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Selecting_Files_Notifies_Once_With_The_Selected_Files()
    {
        // Arrange - MudFileUpload itself reads the browser's file picker result and raises
        // FilesChanged with the selection; invoked directly here (public API on the rendered
        // component instance) rather than driving the hidden <input type="file"> through bUnit.
        var notifications = new List<object?>();
        var model = new TestModel();
        var component = RenderStandaloneMultipleUpload(model, notifications);
        var selected = new List<IBrowserFile> { new StubBrowserFile(), new StubBrowserFile() };

        // Act
        var fileUpload = component.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
        await component.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(selected));

        // Assert - one notification, carrying exactly the selected files
        notifications.ShouldHaveSingleItem();
        notifications[0].ShouldBe(selected);
    }

    [Fact]
    public async Task Removing_The_Last_File_Notifies_Once_With_An_Empty_List_Not_Null()
    {
        // Arrange - RemoveFile is untouched by Task 2 (it mutates CurrentValue directly and never
        // goes through OnFilesChanged/ClearAsync), so this pins that it stays unaffected.
        var notifications = new List<object?>();
        var model = new TestModel { Uploads = new List<IBrowserFile> { new StubBrowserFile() } };
        var component = RenderStandaloneMultipleUpload(model, notifications);

        // Act
        var closeButtons = component.FindAll(".mud-chip-close-button");
        closeButtons.Count.ShouldBe(1);
        await component.InvokeAsync(() => closeButtons[0].Click());

        // Assert
        notifications.ShouldHaveSingleItem();
        var value = notifications[0].ShouldBeAssignableTo<IReadOnlyList<IBrowserFile>>();
        value!.ShouldBeEmpty();
    }

    [Fact]
    public async Task Removing_One_Of_Two_Files_Notifies_Once_With_The_Remaining_File()
    {
        // Arrange - the "once per removal" half of RemoveFile's contract: with a file still left,
        // removing one must not also trip the (unrelated) OnFilesChanged/ClearAsync path.
        var notifications = new List<object?>();
        var keep = new StubBrowserFile();
        var model = new TestModel { Uploads = new List<IBrowserFile> { new StubBrowserFile(), keep } };
        var component = RenderStandaloneMultipleUpload(model, notifications);

        // Act - remove the first of two files
        var closeButtons = component.FindAll(".mud-chip-close-button");
        closeButtons.Count.ShouldBe(2);
        await component.InvokeAsync(() => closeButtons[0].Click());

        // Assert
        notifications.ShouldHaveSingleItem();
        var value = notifications[0].ShouldBeAssignableTo<IReadOnlyList<IBrowserFile>>();
        value!.Count.ShouldBe(1);
        value.ShouldContain(keep);
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

    [Fact]
    public async Task A_Non_Nullable_Uploads_Property_Is_Never_Notified_Null_On_Any_Path()
    {
        // Arrange - FormCraft's own "no files" representation for this field is an empty list, not
        // null, so a model declaring a non-nullable IReadOnlyList<IBrowserFile> property must never
        // see a null notification on any path that can touch CurrentValue.
        var notifications = new List<object?>();
        var model = new NonNullableTestModel { Uploads = new List<IBrowserFile> { new StubBrowserFile() } };
        var component = RenderStandaloneMultipleUploadNonNullable(model, notifications);

        // Act 1 - clear
        var buttons = component.FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Act 2 - select
        var fileUpload = component.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>();
        var selected = new List<IBrowserFile> { new StubBrowserFile() };
        await component.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(selected));

        // Act 3 - remove the (only, just-selected) file
        var closeButtons = component.FindAll(".mud-chip-close-button");
        closeButtons.Count.ShouldBe(1);
        await component.InvokeAsync(() => closeButtons[0].Click());

        // Assert - three notifications (clear, select, remove-last), none of them null
        notifications.Count.ShouldBe(3);
        notifications.ShouldAllBe(n => n != null);
    }

    [Fact]
    public async Task Clearing_A_Multiple_File_Upload_Inside_A_Collection_Item_Notifies_Once_And_Leaves_The_Other_Row_Untouched()
    {
        // Arrange - item fields render through this same MudBlazorMultipleFileUploadComponent since
        // #203 (one component, one code path, regardless of placement), so Task 2's fix must hold
        // here too. Rendered through FormCraftComponent (a real EditContext) rather than the
        // standalone FieldRenderContext harness the rest of this file uses: "writes an empty list
        // under Items[i].Field" is specifically about the parent EditContext's nested-identifier
        // wiring (#91). Two rows so clearing one proves it does not also touch the other.
        var model = new ApplicationModel
        {
            Applications =
            {
                new ApplicationRecord { Attachments = new List<IBrowserFile> { new StubBrowserFile() } },
                new ApplicationRecord { Attachments = new List<IBrowserFile> { new StubBrowserFile() } },
            },
        };
        var config = FormBuilder<ApplicationModel>
            .Create()
            .AddCollectionField(x => x.Applications, collection => collection
                .WithLabel("Applications")
                .WithItemForm(item => item
                    .AddField(x => x.Attachments, field => field.WithLabel("Attachments"))))
            .Build();

        EditContext? editContext = null;
        var component = this.RenderItemForm(model, config,
            parameters => parameters.Add(p => p.OnEditContextCreated, ctx => editContext = ctx));

        editContext.ShouldNotBeNull();
        var nestedField = new FieldIdentifier(model, "Applications[0].Attachments");
        var nestedChangeCount = 0;
        editContext!.OnFieldChanged += (_, args) =>
        {
            if (args.FieldIdentifier.Equals(nestedField))
            {
                nestedChangeCount++;
            }
        };

        // Scoped per row rather than a flat button index, the way CollectionFocusTests/
        // FileUploadClearFocusTests scope per-field lookups - two rows means two instances.
        var rows = component.FindComponents<MudBlazorMultipleFileUploadComponent<ApplicationRecord>>();
        rows.Count.ShouldBe(2);

        // Act - clear ONLY the first row
        var buttons = rows[0].FindAll(".mud-toolbar button");
        buttons.Count.ShouldBe(2);
        await component.InvokeAsync(() => buttons[1].Click());

        // Assert - row 0's model list is a non-null empty list (not null), row 1 is untouched, and
        // the nested identifier for row 0's field changed exactly once.
        model.Applications[0].Attachments.ShouldNotBeNull();
        model.Applications[0].Attachments.ShouldBeEmpty();
        model.Applications[1].Attachments.Count.ShouldBe(1);
        nestedChangeCount.ShouldBe(1);
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

    private IRenderedComponent<MudBlazorMultipleFileUploadComponent<NonNullableTestModel>> RenderStandaloneMultipleUploadNonNullable(
        NonNullableTestModel model,
        List<object?> notifications)
    {
        var config = FormBuilder<NonNullableTestModel>
            .Create()
            .AddField(x => x.Uploads, f => f.WithLabel("Certificates").Required("A certificate is required"))
            .Build();

        var context = new FieldRenderContext<NonNullableTestModel>
        {
            Model = model,
            Field = config.Fields.First(),
            ActualFieldType = typeof(IReadOnlyList<IBrowserFile>),
            CurrentValue = model.Uploads,
            OnValueChanged = EventCallback.Factory.Create<object?>(this, v => notifications.Add(v)),
        };

        return Render<MudBlazorMultipleFileUploadComponent<NonNullableTestModel>>(parameters => parameters
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

    /// <summary>
    /// Same field, but declared non-nullable — used to pin that no path ever hands the model a
    /// <c>null</c> it was never typed to hold.
    /// </summary>
    private sealed class NonNullableTestModel
    {
        public IReadOnlyList<IBrowserFile> Uploads { get; set; } = new List<IBrowserFile>();
    }

    /// <summary>
    /// Root model for the collection-item path - no shared fixture declares a file-upload item form,
    /// so this stays local rather than growing <c>FormCraft.TestSupport.CollectionItemFixture</c> for
    /// one consumer (see that fixture's own remarks on why models there stay narrow).
    /// </summary>
    private sealed class ApplicationModel
    {
        public List<ApplicationRecord> Applications { get; set; } = new();
    }

    /// <summary>Item for the collection-item path - one multiple-file field, nothing else.</summary>
    private sealed class ApplicationRecord
    {
        public IReadOnlyList<IBrowserFile> Attachments { get; set; } = new List<IBrowserFile>();
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
