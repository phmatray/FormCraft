namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>Model covering the three temporal CLR types the adapter supports.</summary>
public class DateTestModel
{
    /// <summary>A <see cref="DateTime"/> field.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>A <see cref="DateOnly"/> field.</summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>A <see cref="TimeOnly"/> field.</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>A nullable <see cref="DateOnly"/> field.</summary>
    public DateOnly? ExpiresOn { get; set; }
}

/// <summary>
/// Each temporal CLR type must reach its own picker, closed over its own type - the renderers are
/// registered together, so a mis-scoped <c>CanRender</c> would silently hand one type's field to
/// another type's component.
/// </summary>
public class DateOnlyTimeOnlyFieldTests : FluentUITestBase
{
    private IRenderedComponent<FormCraftComponent<DateTestModel>> Render(
        DateTestModel model, IFormConfiguration<DateTestModel> config) =>
        Render<FormCraftComponent<DateTestModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

    [Fact]
    public void DateOnly_Field_Should_Render_A_DateOnly_Picker()
    {
        // Arrange
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.BirthDate, f => f.WithLabel("Birth date"))
            .Build();

        // Act
        var component = Render(new DateTestModel(), config);

        // Assert
        component.FindComponents<FluentDatePicker<DateOnly?>>().ShouldNotBeEmpty();
    }

    [Fact]
    public void DateTime_Field_Should_Render_A_DateTime_Picker()
    {
        // Arrange
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.Timestamp, f => f.WithLabel("Timestamp"))
            .Build();

        // Act
        var component = Render(new DateTestModel(), config);

        // Assert
        component.FindComponents<FluentDatePicker<DateTime?>>().ShouldNotBeEmpty();
    }

    [Fact]
    public void TimeOnly_Field_Should_Render_A_Time_Picker()
    {
        // Arrange
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.StartTime, f => f.WithLabel("Start"))
            .Build();

        // Act
        var component = Render(new DateTestModel(), config);

        // Assert
        component.FindComponents<FluentTimePicker<TimeOnly?>>().ShouldNotBeEmpty();
    }

    [Fact]
    public void Unset_Date_Should_Render_As_Empty_Rather_Than_Year_One()
    {
        // Arrange - default(DateOnly) is 0001-01-01, which no user meant to choose
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.BirthDate, f => f.WithLabel("Birth date"))
            .Build();

        // Act
        var component = Render(new DateTestModel(), config);

        // Assert
        component.FindComponent<FluentDatePicker<DateOnly?>>().Instance.Value.ShouldBeNull();
    }

    [Fact]
    public void Set_Date_Should_Load_From_The_Model()
    {
        // Arrange
        var birthDate = new DateOnly(1990, 6, 15);
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.BirthDate, f => f.WithLabel("Birth date"))
            .Build();

        // Act
        var component = Render(new DateTestModel { BirthDate = birthDate }, config);

        // Assert
        component.FindComponent<FluentDatePicker<DateOnly?>>().Instance.Value.ShouldBe(birthDate);
    }

    [Fact]
    public async Task Picking_A_Date_Should_Write_Back_To_The_Model()
    {
        // Arrange
        var model = new DateTestModel();
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.BirthDate, f => f.WithLabel("Birth date"))
            .Build();
        var component = Render(model, config);
        var picker = component.FindComponent<FluentDatePicker<DateOnly?>>();
        var chosen = new DateOnly(2020, 1, 2);

        // Act
        await component.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(chosen));

        // Assert
        model.BirthDate.ShouldBe(chosen);
    }

    [Fact]
    public void Nullable_DateOnly_Field_Should_Also_Reach_The_Date_Picker()
    {
        // Arrange - CanRender unwraps Nullable<>, so DateOnly? routes to the same component
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.ExpiresOn, f => f.WithLabel("Expires"))
            .Build();

        // Act
        var component = Render(new DateTestModel(), config);

        // Assert
        component.FindComponents<FluentDatePicker<DateOnly?>>().ShouldNotBeEmpty();
    }

    [Fact]
    public void Required_Date_Field_Should_Announce_Itself()
    {
        // Arrange
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.BirthDate, f => f.WithLabel("Birth date").Required("Required"))
            .Build();

        // Act
        var component = Render(new DateTestModel(), config);

        // Assert
        component.FindAll("[aria-required='true']").ShouldNotBeEmpty();
    }

    [Fact]
    public void Midnight_Should_Remain_Selectable_On_A_Time_Field()
    {
        // Arrange - default(TimeOnly) IS midnight, a value a user may legitimately pick, so the
        // time component deliberately does not map default to null the way the date ones do.
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.StartTime, f => f.WithLabel("Start"))
            .Build();

        // Act
        var component = Render(new DateTestModel { StartTime = TimeOnly.MinValue }, config);

        // Assert
        component.FindComponent<FluentTimePicker<TimeOnly?>>().Instance.Value
            .ShouldBe(TimeOnly.MinValue);
    }
}
