namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Proves <c>.DisabledWhen(...)</c> is honored by the core stub renderers (no UI adapter
/// registered), not just by <see cref="FieldComponentBase{TModel,TValue}.IsDisabled"/> (#479).
/// </summary>
public class CoreRendererDisabledWhenTests : CoreRendererTestBase
{
    [Fact]
    public void RenderField_Should_Disable_String_Input_When_DisabledWhen_Condition_Is_True()
    {
        var model = new StringLockModel { Locked = true, Name = "Ada" };
        var config = FormBuilder<StringLockModel>.Create()
            .AddField(x => x.Name, f => f.DisabledWhen(m => m.Locked))
            .Build();
        var field = config.Fields.First();

        var cut = Render(RendererService.RenderField(model, field, default, default));

        cut.Find("input").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void RenderField_Should_Not_Disable_String_Input_When_DisabledWhen_Condition_Is_False()
    {
        var model = new StringLockModel { Locked = false, Name = "Ada" };
        var config = FormBuilder<StringLockModel>.Create()
            .AddField(x => x.Name, f => f.DisabledWhen(m => m.Locked))
            .Build();
        var field = config.Fields.First();

        var cut = Render(RendererService.RenderField(model, field, default, default));

        cut.Find("input").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void RenderField_Should_Disable_File_Upload_Input_When_DisabledWhen_Condition_Is_True()
    {
        var model = new FileLockModel { Locked = true };
        var config = FormBuilder<FileLockModel>.Create()
            .AddField(x => x.Upload, f => f.DisabledWhen(m => m.Locked))
            .Build();
        var field = config.Fields.First();

        var cut = Render(RendererService.RenderField(model, field, default, default));

        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("file");
        input.HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void RenderField_Should_Not_Disable_File_Upload_Input_When_DisabledWhen_Condition_Is_False()
    {
        var model = new FileLockModel { Locked = false };
        var config = FormBuilder<FileLockModel>.Create()
            .AddField(x => x.Upload, f => f.DisabledWhen(m => m.Locked))
            .Build();
        var field = config.Fields.First();

        var cut = Render(RendererService.RenderField(model, field, default, default));

        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("file");
        input.HasAttribute("disabled").ShouldBeFalse();
    }

    public class StringLockModel
    {
        public bool Locked { get; set; }
        public string? Name { get; set; }
    }

    public class FileLockModel
    {
        public bool Locked { get; set; }
        public IBrowserFile? Upload { get; set; }
    }
}
