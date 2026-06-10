using FormCraft.ForMudBlazor.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Regression tests for DateOnly/TimeOnly support: the DateTime renderer used to
/// claim these types but its component hardcoded DateTime, so values never loaded
/// and editing crashed.
/// </summary>
public class DateOnlyTimeOnlyFieldTests : MudBlazorTestBase
{
    public DateOnlyTimeOnlyFieldTests()
    {
        ((IServiceCollection)Services).AddFormCraftMudBlazor();
    }

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
    public void DateOnlyField_Should_Render_DatePicker_With_Value()
    {
        // Arrange
        var model = new TestModel { BirthDate = new DateOnly(1990, 6, 15) };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, field => field.WithLabel("Birth Date"))
            .Build();

        // Act
        var component = RenderForm(model, config);

        // Assert
        var picker = component.FindComponent<MudDatePicker>();
        picker.Instance.Date.ShouldBe(new DateTime(1990, 6, 15));
    }

    [Fact]
    public async Task DateOnlyField_Should_Update_Model_On_Change()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.BirthDate, field => field.WithLabel("Birth Date"))
            .Build();

        var component = RenderForm(model, config);

        var picker = component.FindComponent<MudDatePicker>();

        // Act
        await component.InvokeAsync(() => picker.Instance.DateChanged.InvokeAsync(new DateTime(2001, 2, 3)));

        // Assert
        model.BirthDate.ShouldBe(new DateOnly(2001, 2, 3));
    }

    [Fact]
    public void NullableDateOnlyField_Should_Render_Without_Crashing()
    {
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.AnniversaryDate, field => field.WithLabel("Anniversary"))
            .Build();

        var component = RenderForm(model, config);

        component.FindComponents<MudDatePicker>().ShouldNotBeEmpty();
    }

    [Fact]
    public void TimeOnlyField_Should_Render_TimePicker_With_Value()
    {
        // Arrange
        var model = new TestModel { StartTime = new TimeOnly(9, 30) };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.StartTime, field => field.WithLabel("Start Time"))
            .Build();

        // Act
        var component = RenderForm(model, config);

        // Assert
        var picker = component.FindComponent<MudTimePicker>();
        picker.Instance.Time.ShouldBe(new TimeSpan(9, 30, 0));
    }

    [Fact]
    public async Task TimeOnlyField_Should_Update_Model_On_Change()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.StartTime, field => field.WithLabel("Start Time"))
            .Build();

        var component = RenderForm(model, config);

        var picker = component.FindComponent<MudTimePicker>();

        // Act
        await component.InvokeAsync(() => picker.Instance.TimeChanged.InvokeAsync(new TimeSpan(14, 45, 0)));

        // Assert
        model.StartTime.ShouldBe(new TimeOnly(14, 45));
    }

    private class TestModel
    {
        public DateOnly BirthDate { get; set; }
        public DateOnly? AnniversaryDate { get; set; }
        public TimeOnly StartTime { get; set; }
    }
}
