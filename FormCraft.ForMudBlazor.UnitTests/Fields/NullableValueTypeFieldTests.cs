namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Regression tests for #150: nullable value-type fields must display null as an
/// empty input and round-trip null (clearing the input writes null to the model,
/// not default(T)).
/// </summary>
public class NullableValueTypeFieldTests : MudBlazorTestBase
{
    /// <summary>
    /// Renders the form next to a MudPopoverProvider, which picker components require.
    /// </summary>
    private IRenderedComponent<FormCraftComponent<TestModel>> RenderForm(TestModel model, IFormConfiguration<TestModel> config)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<FormCraftComponent<TestModel>>(1);
            builder.AddComponentParameter(2, "Model", model);
            builder.AddComponentParameter(3, "Configuration", config);
            builder.CloseComponent();
        });

        return cut.FindComponent<FormCraftComponent<TestModel>>();
    }

    [Fact]
    public void NullableInt_Should_Render_Empty_When_Model_Value_Is_Null()
    {
        // Arrange
        var model = new TestModel { OptionalAge = null };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalAge, field => field.WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - binds MudNumericField<int?> (not int) and shows no value
        var mudNumericField = component.FindComponent<MudNumericField<int?>>();
        mudNumericField.Instance.Value.ShouldBeNull();
    }

    [Fact]
    public void NullableInt_Should_Display_Existing_Value()
    {
        // Arrange
        var model = new TestModel { OptionalAge = 25 };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalAge, field => field.WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudNumericField = component.FindComponent<MudNumericField<int?>>();
        mudNumericField.Instance.Value.ShouldBe(25);
    }

    [Fact]
    public async Task NullableInt_Should_RoundTrip_Entered_Value()
    {
        // Arrange
        var model = new TestModel { OptionalAge = null };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalAge, field => field.WithLabel("Age"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudNumericField = component.FindComponent<MudNumericField<int?>>();

        // Act
        await mudNumericField.InvokeAsync(() => mudNumericField.Instance.ValueChanged.InvokeAsync(42));

        // Assert
        model.OptionalAge.ShouldBe(42);
        component.FindComponent<MudNumericField<int?>>().Instance.Value.ShouldBe(42);
    }

    [Fact]
    public async Task NullableInt_Should_Write_Null_When_Cleared()
    {
        // Arrange
        var model = new TestModel { OptionalAge = 25 };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalAge, field => field.WithLabel("Age"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudNumericField = component.FindComponent<MudNumericField<int?>>();

        // Act - clearing the input raises ValueChanged with null
        await mudNumericField.InvokeAsync(() => mudNumericField.Instance.ValueChanged.InvokeAsync(null));

        // Assert - null reaches the model instead of being coerced to 0
        model.OptionalAge.ShouldBeNull();
    }

    [Fact]
    public async Task NullableDecimal_Should_Render_Null_And_RoundTrip()
    {
        // Arrange
        var model = new TestModel { OptionalPrice = 9.99m };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalPrice, field => field.WithLabel("Price"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudNumericField = component.FindComponent<MudNumericField<decimal?>>();
        mudNumericField.Instance.Value.ShouldBe(9.99m);

        // Act
        await mudNumericField.InvokeAsync(() => mudNumericField.Instance.ValueChanged.InvokeAsync(null));

        // Assert
        model.OptionalPrice.ShouldBeNull();
    }

    [Fact]
    public async Task NullableDouble_Should_Render_Null_And_RoundTrip()
    {
        // Arrange
        var model = new TestModel { OptionalRating = null };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalRating, field => field.WithLabel("Rating"))
            .Build();

        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        var mudNumericField = component.FindComponent<MudNumericField<double?>>();
        mudNumericField.Instance.Value.ShouldBeNull();

        // Act
        await mudNumericField.InvokeAsync(() => mudNumericField.Instance.ValueChanged.InvokeAsync(4.5));

        // Assert
        model.OptionalRating.ShouldBe(4.5);

        // Act - clear again
        await mudNumericField.InvokeAsync(() => mudNumericField.Instance.ValueChanged.InvokeAsync(null));

        // Assert
        model.OptionalRating.ShouldBeNull();
    }

    [Fact]
    public void NonNullableInt_Should_Still_Render_MudNumericField_Of_Int()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Age, field => field.WithLabel("Age"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert - existing non-nullable contract is unchanged
        var mudNumericField = component.FindComponent<MudNumericField<int>>();
        mudNumericField.Instance.Value.ShouldBe(0);
    }

    [Fact]
    public async Task NullableDateTime_Should_Render_Empty_And_RoundTrip_Null()
    {
        // Arrange
        var model = new TestModel { OptionalDate = null };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalDate, field => field.WithLabel("Date"))
            .Build();

        var component = RenderForm(model, config);

        var datePicker = component.FindComponent<MudDatePicker>();
        datePicker.Instance.Date.ShouldBeNull();

        // Act - pick a date
        var picked = new DateTime(2025, 6, 15);
        await datePicker.InvokeAsync(() => datePicker.Instance.DateChanged.InvokeAsync(picked));

        // Assert
        model.OptionalDate.ShouldBe(picked);

        // Act - clear the picker
        await datePicker.InvokeAsync(() => datePicker.Instance.DateChanged.InvokeAsync(null));

        // Assert - null round-trips instead of DateTime.MinValue
        model.OptionalDate.ShouldBeNull();
    }

    [Fact]
    public async Task NonNullableDateTime_Should_Write_Default_When_Cleared()
    {
        // Arrange - existing behavior for non-nullable DateTime is preserved
        var model = new TestModel { BirthDate = new DateTime(2000, 1, 1) };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, field => field.WithLabel("Birth Date"))
            .Build();

        var component = RenderForm(model, config);

        var datePicker = component.FindComponent<MudDatePicker>();

        // Act
        await datePicker.InvokeAsync(() => datePicker.Instance.DateChanged.InvokeAsync(null));

        // Assert
        model.BirthDate.ShouldBe(default);
    }

    [Fact]
    public async Task NullableDateOnly_Should_Render_Empty_And_RoundTrip_Null()
    {
        // Arrange
        var model = new TestModel { OptionalDateOnly = null };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalDateOnly, field => field.WithLabel("Date"))
            .Build();

        var component = RenderForm(model, config);

        var datePicker = component.FindComponent<MudDatePicker>();
        datePicker.Instance.Date.ShouldBeNull();

        // Act - pick a date
        await datePicker.InvokeAsync(() => datePicker.Instance.DateChanged.InvokeAsync(new DateTime(2025, 3, 10)));

        // Assert
        model.OptionalDateOnly.ShouldBe(new DateOnly(2025, 3, 10));

        // Act - clear the picker
        await datePicker.InvokeAsync(() => datePicker.Instance.DateChanged.InvokeAsync(null));

        // Assert - null round-trips instead of DateOnly.MinValue (0001-01-01)
        model.OptionalDateOnly.ShouldBeNull();
    }

    [Fact]
    public async Task NullableTimeOnly_Should_Render_Empty_And_RoundTrip_Null()
    {
        // Arrange
        var model = new TestModel { OptionalTimeOnly = null };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.OptionalTimeOnly, field => field.WithLabel("Time"))
            .Build();

        var component = RenderForm(model, config);

        var timePicker = component.FindComponent<MudTimePicker>();
        timePicker.Instance.Time.ShouldBeNull();

        // Act - pick a time
        await timePicker.InvokeAsync(() => timePicker.Instance.TimeChanged.InvokeAsync(new TimeSpan(14, 30, 0)));

        // Assert
        model.OptionalTimeOnly.ShouldBe(new TimeOnly(14, 30));

        // Act - clear the picker
        await timePicker.InvokeAsync(() => timePicker.Instance.TimeChanged.InvokeAsync(null));

        // Assert - null round-trips instead of TimeOnly.MinValue
        model.OptionalTimeOnly.ShouldBeNull();
    }

    private class TestModel
    {
        public int Age { get; set; }
        public int? OptionalAge { get; set; }
        public decimal? OptionalPrice { get; set; }
        public double? OptionalRating { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime? OptionalDate { get; set; }
        public DateOnly? OptionalDateOnly { get; set; }
        public TimeOnly? OptionalTimeOnly { get; set; }
    }
}
