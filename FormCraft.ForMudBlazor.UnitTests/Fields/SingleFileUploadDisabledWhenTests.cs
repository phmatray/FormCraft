namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// A MudBlazor single-file upload field configured with <c>.DisabledWhen(...)</c> must disable
/// <c>MudFileUpload</c> and the Browse button, re-evaluated on every render — the same contract
/// every other field type already has since #479, closed here for file upload (#482).
/// </summary>
public class SingleFileUploadDisabledWhenTests : MudBlazorTestBase
{
    [Fact]
    public void A_Condition_True_At_First_Render_Should_Disable_The_Upload_And_Browse()
    {
        // Arrange
        var model = new LockModel { Mode = "locked" };

        // Act
        var component = RenderForm(model);

        // Assert
        FileUpload(component).Disabled.ShouldBeTrue();
        BrowseButton(component).Disabled.ShouldBeTrue();
    }

    [Fact]
    public void The_Control_Should_Follow_The_Condition_As_The_Watched_Field_Changes()
    {
        // Arrange
        var model = new LockModel { Mode = "open" };
        var component = RenderForm(model);
        FileUpload(component).Disabled.ShouldBeFalse();
        BrowseButton(component).Disabled.ShouldBeFalse();

        // Act + Assert - Mode is the first rendered input
        component.FindAll("input")[0].Input("locked");
        component.WaitForAssertion(() =>
        {
            FileUpload(component).Disabled.ShouldBeTrue();
            BrowseButton(component).Disabled.ShouldBeTrue();
        });

        component.FindAll("input")[0].Input("open");
        component.WaitForAssertion(() =>
        {
            FileUpload(component).Disabled.ShouldBeFalse();
            BrowseButton(component).Disabled.ShouldBeFalse();
        });
    }

    [Fact]
    public void Clicking_The_Drop_Zone_While_Disabled_Should_Not_Open_The_File_Picker()
    {
        // Arrange - enabled first, as a positive control: proves the selector really opens the
        // picker before asserting the negative, so a wrong selector cannot pass this test silently.
        var model = new LockModel { Mode = "open" };
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

    private IRenderedComponent<FormCraftComponent<LockModel>> RenderForm(LockModel model)
    {
        var config = FormBuilder<LockModel>.Create()
            .AddField(x => x.Mode, f => f.WithLabel("Mode"))
            .AddField(x => x.Resume, f => f.WithLabel("Resume").DisabledWhen(m => m.Mode == "locked"))
            .Build();

        return Render<FormCraftComponent<LockModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
    }

    private static MudFileUpload<IBrowserFile> FileUpload(IRenderedComponent<FormCraftComponent<LockModel>> component) =>
        component.FindComponent<MudFileUpload<IBrowserFile>>().Instance;

    private static MudButton BrowseButton(IRenderedComponent<FormCraftComponent<LockModel>> component) =>
        component.FindComponents<MudButton>().Single(b => b.Markup.Contains("Browse")).Instance;

    /// <summary>Model whose <see cref="Mode"/> drives whether <see cref="Resume"/> is disabled.</summary>
    public class LockModel
    {
        /// <summary>The watched field.</summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>The field under test.</summary>
        public IBrowserFile? Resume { get; set; }
    }
}
