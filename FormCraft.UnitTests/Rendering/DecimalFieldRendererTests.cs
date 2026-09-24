namespace FormCraft.UnitTests.Rendering;

public class DecimalFieldRendererTests : CoreRendererTestBase
{
    private readonly DecimalFieldRenderer _renderer;

    public DecimalFieldRendererTests()
    {
        _renderer = new DecimalFieldRenderer();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Decimal_Type()
    {
        // Arrange
        var fieldConfig = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(decimal), fieldConfig);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Nullable_Decimal_Type()
    {
        // Arrange
        var fieldConfig = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(decimal?), fieldConfig);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Non_Decimal_Types()
    {
        // Arrange
        var fieldConfig = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert
        _renderer.CanRender(typeof(string), fieldConfig).ShouldBeFalse();
        _renderer.CanRender(typeof(int), fieldConfig).ShouldBeFalse();
        _renderer.CanRender(typeof(double), fieldConfig).ShouldBeFalse();
        _renderer.CanRender(typeof(bool), fieldConfig).ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Create_MudNumericField_With_Correct_Attributes()
    {
        // Arrange
        var price = 123.45m;
        var model = new TestModel { Price = price };
        var field = CreateMockField("Price", "Enter the price", true);
        var context = CreateContext(model, field, price, typeof(decimal));

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Price");
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("number");
        input.GetAttribute("step").ShouldBe("0.01");
        input.GetAttribute("value").ShouldBe(price.ToString());
        cut.Find(".help-text").TextContent.ShouldBe("Enter the price");
    }

    [Fact]
    public void Render_Should_Handle_Null_Value_For_Non_Nullable()
    {
        // Arrange
        var model = new TestModel { Price = 0m };
        var field = CreateMockField("Amount");
        var context = CreateContext(model, field, null, typeof(decimal));

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - a null value renders no "value" attribute, and empty help text renders no div.
        cut.Find("label").TextContent.ShouldBe("Amount");
        cut.Find("input").HasAttribute("value").ShouldBeFalse();
        cut.Find("div.test-decimal-field").QuerySelector(".help-text").ShouldBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Nullable_Decimal_Field()
    {
        // Arrange
        var amount = 99.99m;
        var model = new TestModel { OptionalAmount = amount };
        var field = CreateMockField("Optional Amount");
        var context = CreateContext(model, field, amount, typeof(decimal?));

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Optional Amount");
        cut.Find("input").GetAttribute("value").ShouldBe(amount.ToString());
    }

    [Fact]
    public void Render_Should_Handle_Null_Value_For_Nullable()
    {
        // Arrange
        var model = new TestModel { OptionalAmount = null };
        var field = CreateMockField("Optional Amount");
        var context = CreateContext(model, field, null, typeof(decimal?));

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Optional Amount");
        cut.Find("input").HasAttribute("value").ShouldBeFalse();
    }

    private IFieldConfiguration<TestModel, object> CreateMockField(
        string label,
        string? helpText = null,
        bool isRequired = false,
        bool isDisabled = false)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();

        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(helpText ?? string.Empty);
        A.CallTo(() => field.Placeholder).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(isRequired);
        A.CallTo(() => field.IsDisabled).Returns(isDisabled);
        A.CallTo(() => field.DisabledCondition).Returns(null);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());

        return field;
    }

    private IFieldRenderContext<TestModel> CreateContext(
        TestModel model,
        IFieldConfiguration<TestModel, object> field,
        object? currentValue,
        Type actualFieldType)
    {
        var context = A.Fake<IFieldRenderContext<TestModel>>();

        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(actualFieldType);
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));

        return context;
    }

    [Fact]
    public void RenderField_Should_Render_Label_NumberInput_And_HelpText_With_No_Adapter_Registered()
    {
        // Arrange - the standalone-consumer path: services.AddFormCraft() with no UI adapter.
        // An integer-valued decimal avoids a culture-dependent decimal separator in the assertion.
        var model = new TestModel { Price = 100m };
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Price, field => field
                .WithLabel("Price")
                .WithHelpText("In USD"))
            .Build();
        var field = config.Fields.First(f => f.FieldName == "Price");

        // Act
        var cut = Render(RendererService.RenderField(model, field, default, default));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Price");
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("number");
        input.GetAttribute("step").ShouldBe("0.01");
        input.GetAttribute("value").ShouldBe("100");
        cut.Find(".help-text").TextContent.ShouldBe("In USD");
    }

    public class TestModel
    {
        public decimal Price { get; set; }
        public decimal? OptionalAmount { get; set; }
    }
}
