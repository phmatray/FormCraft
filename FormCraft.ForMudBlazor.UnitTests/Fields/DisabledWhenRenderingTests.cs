namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// <c>.DisabledWhen(...)</c> must drive the rendered control's <c>Disabled</c> parameter,
/// re-evaluated on every render, and take precedence over the static <c>.Disabled(bool)</c> (#476).
/// </summary>
public class DisabledWhenRenderingTests : MudBlazorTestBase
{
    [Fact]
    public void A_Condition_True_At_First_Render_Should_Disable_The_Control()
    {
        // Arrange
        var model = new LockModel { Mode = "locked" };

        // Act
        var component = RenderForm(model, f => f.DisabledWhen(m => m.Mode == "locked"));

        // Assert
        NameField(component).Disabled.ShouldBeTrue();
    }

    [Fact]
    public void The_Control_Should_Follow_The_Condition_As_The_Watched_Field_Changes()
    {
        // Arrange
        var model = new LockModel { Mode = "open" };
        var component = RenderForm(model, f => f.DisabledWhen(m => m.Mode == "locked"));
        NameField(component).Disabled.ShouldBeFalse();

        // Act + Assert - Mode is the first rendered input
        component.FindAll("input")[0].Input("locked");
        component.WaitForAssertion(() => NameField(component).Disabled.ShouldBeTrue());

        component.FindAll("input")[0].Input("open");
        component.WaitForAssertion(() => NameField(component).Disabled.ShouldBeFalse());
    }

    [Fact]
    public void Without_A_Condition_The_Static_Disabled_Flag_Should_Still_Apply()
    {
        // Act
        var component = RenderForm(new LockModel(), f => f.Disabled());

        // Assert
        NameField(component).Disabled.ShouldBeTrue();
    }

    [Fact]
    public void A_Condition_Should_Take_Precedence_Over_The_Static_Disabled_Flag()
    {
        // Act
        var component = RenderForm(new LockModel(), f => f.Disabled().DisabledWhen(_ => false));

        // Assert
        NameField(component).Disabled.ShouldBeFalse();
    }

    private IRenderedComponent<FormCraftComponent<LockModel>> RenderForm(
        LockModel model,
        Action<FieldBuilder<LockModel, string>> configureName)
    {
        var config = FormBuilder<LockModel>.Create()
            .AddField(x => x.Mode, f => f.WithLabel("Mode"))
            .AddField(x => x.Name, f => configureName(f.WithLabel("Name")))
            .Build();

        return Render<FormCraftComponent<LockModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));
    }

    private static MudTextField<string> NameField(IRenderedComponent<FormCraftComponent<LockModel>> component) =>
        component.FindComponents<MudTextField<string>>()
            .Single(f => f.Instance.Label == "Name")
            .Instance;

    /// <summary>Model whose <see cref="Mode"/> drives whether <see cref="Name"/> is disabled.</summary>
    public class LockModel
    {
        /// <summary>The watched field.</summary>
        public string Mode { get; set; } = string.Empty;

        /// <summary>The field under test.</summary>
        public string Name { get; set; } = string.Empty;
    }
}
