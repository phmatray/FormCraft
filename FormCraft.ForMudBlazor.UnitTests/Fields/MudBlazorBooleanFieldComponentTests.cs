namespace FormCraft.ForMudBlazor.UnitTests.Fields;

public class MudBlazorBooleanFieldComponentTests : MudBlazorTestBase
{

    [Fact]
    public void BooleanField_Should_Render_As_Checkbox()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudCheckBox = component.FindComponent<MudCheckBox<bool>>();
        mudCheckBox.ShouldNotBeNull();
        mudCheckBox.Instance.Label.ShouldBe("Is Active");
    }

    [Fact]
    public void BooleanField_Should_Reflect_Initial_True_Value()
    {
        // Arrange
        var model = new TestModel { IsActive = true };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudCheckBox = component.FindComponent<MudCheckBox<bool>>();
        mudCheckBox.ShouldNotBeNull();
        mudCheckBox.Instance.Value.ShouldBeTrue();
    }

    [Fact]
    public void BooleanField_Should_Reflect_Initial_False_Value()
    {
        // Arrange
        var model = new TestModel { IsActive = false };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudCheckBox = component.FindComponent<MudCheckBox<bool>>();
        mudCheckBox.ShouldNotBeNull();
        mudCheckBox.Instance.Value.ShouldBeFalse();
    }

    [Fact]
    public void BooleanField_Should_Be_ReadOnly_When_Configured()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active")
                .ReadOnly())
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudCheckBox = component.FindComponent<MudCheckBox<bool>>();
        mudCheckBox.ShouldNotBeNull();
        mudCheckBox.Instance.ReadOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task BooleanField_Should_Update_Model_On_Change()
    {
        // Arrange
        var model = new TestModel { IsActive = false };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudCheckBox = component.FindComponent<MudCheckBox<bool>>();
        mudCheckBox.ShouldNotBeNull();

        // Act
        await mudCheckBox.InvokeAsync(() => mudCheckBox.Instance.ValueChanged.InvokeAsync(true));

        // Assert
        model.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void BooleanField_Should_Render_Multiple_Checkboxes()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.IsActive, field => field
                .WithLabel("Is Active"))
            .AddField(x => x.IsVerified, field => field
                .WithLabel("Is Verified"))
            .AddField(x => x.AcceptTerms, field => field
                .WithLabel("Accept Terms"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var checkboxes = component.FindComponents<MudCheckBox<bool>>();
        checkboxes.Count.ShouldBe(3);
    }

    private class TestModel
    {
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public bool AcceptTerms { get; set; }
    }
}
