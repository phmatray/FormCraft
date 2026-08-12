namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>Model whose temporal fields are all nullable.</summary>
public class NullableDateModel
{
    /// <summary>A nullable date.</summary>
    public DateOnly? ExpiresOn { get; set; }

    /// <summary>A nullable timestamp.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>A nullable time.</summary>
    public TimeOnly? Reminder { get; set; }

    /// <summary>A non-nullable date, for the contrast case.</summary>
    public DateOnly BirthDate { get; set; }
}

/// <summary>
/// Clearing a nullable date or time field must write <c>null</c> back to the model, not
/// <c>default</c>.
/// </summary>
/// <remarks>
/// This is the #150 guarantee applied to temporal fields. Writing <c>default</c> instead is not a
/// cosmetic slip: <c>0001-01-01</c> satisfies a <c>Required</c> validator, so a cleared mandatory
/// field passes validation, and it sits outside the SQL <c>datetime</c> range, so the mistake
/// surfaces at persistence rather than at the point the user made it.
/// </remarks>
public class NullableClearingTests : FluentUITestBase
{
    private IRenderedComponent<FormCraftComponent<NullableDateModel>> Render(
        NullableDateModel model, IFormConfiguration<NullableDateModel> config) =>
        Render<FormCraftComponent<NullableDateModel>>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Configuration, config));

    [Fact]
    public async Task Clearing_A_Nullable_DateOnly_Should_Write_Null()
    {
        // Arrange
        var model = new NullableDateModel { ExpiresOn = new DateOnly(2030, 1, 1) };
        var config = FormBuilder<NullableDateModel>.Create()
            .AddField(x => x.ExpiresOn, f => f.WithLabel("Expires"))
            .Build();
        var component = Render(model, config);
        var picker = component.FindComponent<FluentDatePicker<DateOnly?>>();

        // Act - the user clears the picker
        await component.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(null));

        // Assert
        model.ExpiresOn.ShouldBeNull();
    }

    [Fact]
    public async Task Clearing_A_Nullable_DateTime_Should_Write_Null()
    {
        // Arrange
        var model = new NullableDateModel { SignedAt = new DateTime(2030, 1, 1, 12, 0, 0) };
        var config = FormBuilder<NullableDateModel>.Create()
            .AddField(x => x.SignedAt, f => f.WithLabel("Signed"))
            .Build();
        var component = Render(model, config);
        var picker = component.FindComponent<FluentDatePicker<DateTime?>>();

        // Act
        await component.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(null));

        // Assert
        model.SignedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Clearing_A_Nullable_TimeOnly_Should_Write_Null()
    {
        // Arrange
        var model = new NullableDateModel { Reminder = new TimeOnly(9, 30) };
        var config = FormBuilder<NullableDateModel>.Create()
            .AddField(x => x.Reminder, f => f.WithLabel("Reminder"))
            .Build();
        var component = Render(model, config);
        var picker = component.FindComponent<FluentTimePicker<TimeOnly?>>();

        // Act
        await component.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(null));

        // Assert
        model.Reminder.ShouldBeNull();
    }

    [Fact]
    public async Task Clearing_A_NonNullable_Date_Should_Write_Default_Not_Throw()
    {
        // Arrange - a non-nullable property has nowhere to put null, so default is the only
        // answer available; it must not throw or leave the previous value in place.
        var model = new NullableDateModel { BirthDate = new DateOnly(1990, 6, 15) };
        var config = FormBuilder<NullableDateModel>.Create()
            .AddField(x => x.BirthDate, f => f.WithLabel("Birth date"))
            .Build();
        var component = Render(model, config);
        var picker = component.FindComponent<FluentDatePicker<DateOnly?>>();

        // Act
        await component.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(null));

        // Assert
        model.BirthDate.ShouldBe(default);
    }
}
