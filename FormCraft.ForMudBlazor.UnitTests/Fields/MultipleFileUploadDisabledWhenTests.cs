namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// The multiple-file counterpart of <see cref="SingleFileUploadDisabledWhenTests"/> (#482):
/// <c>.DisabledWhen(...)</c> must drive <c>MudFileUpload</c>, Browse and Clear All, and a disabled
/// field's file chip must not respond to its close affordance either — the one interactive control
/// none of those three bindings reach, since the chips are FormCraft's own <c>CustomContent</c> and
/// not part of <c>MudFileUpload</c>'s render tree.
/// </summary>
public class MultipleFileUploadDisabledWhenTests : MudBlazorTestBase
{
    [Fact]
    public void A_Condition_True_At_First_Render_Should_Disable_Upload_Browse_And_Clear_All()
    {
        // Arrange - a file already selected so Clear All is present to assert on
        var model = new MultiLockModel
        {
            Mode = "locked",
            Documents = new List<IBrowserFile> { new StubBrowserFile() },
        };

        // Act
        var component = RenderForm(model);

        // Assert
        FileUpload(component).Disabled.ShouldBeTrue();
        BrowseButton(component).Disabled.ShouldBeTrue();
        ClearAllButton(component).Disabled.ShouldBeTrue();
    }

    [Fact]
    public void The_Control_Should_Follow_The_Condition_As_The_Watched_Field_Changes()
    {
        // Arrange
        var model = new MultiLockModel
        {
            Mode = "open",
            Documents = new List<IBrowserFile> { new StubBrowserFile() },
        };
        var component = RenderForm(model);
        FileUpload(component).Disabled.ShouldBeFalse();
        BrowseButton(component).Disabled.ShouldBeFalse();
        ClearAllButton(component).Disabled.ShouldBeFalse();

        // Act + Assert - Mode is the first rendered input
        component.FindAll("input")[0].Input("locked");
        component.WaitForAssertion(() =>
        {
            FileUpload(component).Disabled.ShouldBeTrue();
            BrowseButton(component).Disabled.ShouldBeTrue();
            ClearAllButton(component).Disabled.ShouldBeTrue();
        });

        component.FindAll("input")[0].Input("open");
        component.WaitForAssertion(() =>
        {
            FileUpload(component).Disabled.ShouldBeFalse();
            BrowseButton(component).Disabled.ShouldBeFalse();
            ClearAllButton(component).Disabled.ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Closing_A_Chip_While_Disabled_Should_Not_Change_The_Model()
    {
        // Arrange - AC4: the chip's OnClose is FormCraft's own CustomContent, so none of
        // MudFileUpload/Browse/Clear All's Disabled bindings reach it; RemoveFile needs its own guard.
        var model = new MultiLockModel
        {
            Mode = "locked",
            Documents = new List<IBrowserFile> { new StubBrowserFile() },
        };
        var component = RenderForm(model);

        var closeButtons = component.FindAll(".mud-chip-close-button");
        closeButtons.Count.ShouldBe(1);

        // Act
        await component.InvokeAsync(() => closeButtons[0].Click());

        // Assert - RemoveFile no-oped: the model is unchanged and the chip is still there
        model.Documents!.Count.ShouldBe(1);
        component.FindAll(".mud-chip-close-button").Count.ShouldBe(1);
    }

    [Fact]
    public void Clicking_The_Drop_Zone_While_Disabled_Should_Not_Open_The_File_Picker()
    {
        // Arrange - enabled first, as a positive control: proves the selector really opens the
        // picker before asserting the negative, so a wrong selector cannot pass this test silently.
        var model = new MultiLockModel { Mode = "open" };
        var component = RenderForm(model);
        component.Find(".mud-file-upload .mud-paper").Click();
        JSInterop.Invocations.Count(i => i.Identifier.Contains("openFilePicker", StringComparison.Ordinal)).ShouldBe(1);

        // Act - the field becomes disabled; the drop zone's own @onclick calls OpenFilePickerAsync
        // directly, bypassing the Browse button's Disabled binding entirely (#482).
        component.FindAll("input")[0].Input("locked");
        component.WaitForAssertion(() => FileUpload(component).Disabled.ShouldBeTrue());
        component.Find(".mud-file-upload .mud-paper").Click();

        // Assert - no second picker request was issued
        JSInterop.Invocations.Count(i => i.Identifier.Contains("openFilePicker", StringComparison.Ordinal)).ShouldBe(1);
    }

    private IRenderedComponent<FormCraftComponent<MultiLockModel>> RenderForm(MultiLockModel model)
    {
        var config = FormBuilder<MultiLockModel>.Create()
            .AddField(x => x.Mode, f => f.WithLabel("Mode"))
            .AddField(x => x.Documents, f => f.WithLabel("Documents").DisabledWhen(m => m.Mode == "locked"))
            .Build();

        return Render<FormCraftComponent<MultiLockModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
    }

    private static MudFileUpload<IReadOnlyList<IBrowserFile>> FileUpload(
        IRenderedComponent<FormCraftComponent<MultiLockModel>> component) =>
        component.FindComponent<MudFileUpload<IReadOnlyList<IBrowserFile>>>().Instance;

    private static MudButton BrowseButton(IRenderedComponent<FormCraftComponent<MultiLockModel>> component) =>
        component.FindComponents<MudButton>().Single(b => b.Markup.Contains("Browse")).Instance;

    private static MudButton ClearAllButton(IRenderedComponent<FormCraftComponent<MultiLockModel>> component) =>
        component.FindComponents<MudButton>().Single(b => b.Markup.Contains("Clear All")).Instance;

    /// <summary>Model whose <see cref="Mode"/> drives whether <see cref="Documents"/> is disabled.</summary>
    public class MultiLockModel
    {
        /// <summary>The watched field.</summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>The field under test.</summary>
        public IReadOnlyList<IBrowserFile>? Documents { get; set; }
    }

    private sealed class StubBrowserFile : IBrowserFile
    {
        public string Name => "resume.pdf";

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => 1024;

        public string ContentType => "application/pdf";

        public Stream OpenReadStream(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default) => new MemoryStream();
    }
}
