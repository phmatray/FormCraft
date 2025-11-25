namespace FormCraft.ForMudBlazor.UnitTests.Fields;

public class MudBlazorNumericFieldComponentTests : MudBlazorTestBase
{

    [Fact]
    public void IntField_Should_Render_As_NumericField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Age, field => field
                .WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<int>>();
        mudNumericField.ShouldNotBeNull();
        mudNumericField.Instance.Label.ShouldBe("Age");
    }

    [Fact]
    public void IntField_Should_Display_Initial_Value()
    {
        // Arrange
        var model = new TestModel { Age = 25 };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Age, field => field
                .WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<int>>();
        mudNumericField.ShouldNotBeNull();
        mudNumericField.Instance.Value.ShouldBe(25);
    }

    [Fact]
    public void DecimalField_Should_Render_As_NumericField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Price, field => field
                .WithLabel("Price"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<decimal>>();
        mudNumericField.ShouldNotBeNull();
        mudNumericField.Instance.Label.ShouldBe("Price");
    }

    [Fact]
    public void DoubleField_Should_Render_As_NumericField()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Rating, field => field
                .WithLabel("Rating"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<double>>();
        mudNumericField.ShouldNotBeNull();
        mudNumericField.Instance.Label.ShouldBe("Rating");
    }

    [Fact]
    public void NumericField_Should_Be_ReadOnly_When_Configured()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Age, field => field
                .WithLabel("Age")
                .ReadOnly())
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<int>>();
        mudNumericField.ShouldNotBeNull();
        mudNumericField.Instance.ReadOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task NumericField_Should_Update_Model_On_Change()
    {
        // Arrange
        var model = new TestModel { Age = 0 };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Age, field => field
                .WithLabel("Age"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudNumericField = component.FindComponent<MudNumericField<int>>();
        mudNumericField.ShouldNotBeNull();

        // Act
        await mudNumericField.InvokeAsync(() => mudNumericField.Instance.ValueChanged.InvokeAsync(30));

        // Assert
        model.Age.ShouldBe(30);
    }

    [Fact]
    public void NumericField_Should_Have_Placeholder()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Age, field => field
                .WithLabel("Age")
                .WithPlaceholder("Enter your age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<int>>();
        mudNumericField.ShouldNotBeNull();
        mudNumericField.Instance.Placeholder.ShouldBe("Enter your age");
    }

    private class TestModel
    {
        public int Age { get; set; }
        public decimal Price { get; set; }
        public double Rating { get; set; }
    }
}
