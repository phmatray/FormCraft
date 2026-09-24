namespace FormCraft.UnitTests.Rendering;

public class DateTimeFieldRendererTests : CoreRendererTestBase
{
    private readonly DateTimeFieldRenderer _renderer;

    public DateTimeFieldRendererTests()
    {
        _renderer = new DateTimeFieldRenderer();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_DateTime_Type()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(DateTime), mockField);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_True_For_Nullable_DateTime_Type()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act
        var result = _renderer.CanRender(typeof(DateTime?), mockField);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanRender_Should_Return_False_For_Non_DateTime_Types()
    {
        // Arrange
        var mockField = A.Fake<IFieldConfiguration<object, object>>();

        // Act & Assert
        _renderer.CanRender(typeof(string), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(int), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(bool), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(DateOnly), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(TimeOnly), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(DateTimeOffset), mockField).ShouldBeFalse();
        _renderer.CanRender(typeof(object), mockField).ShouldBeFalse();
    }

    [Fact]
    public void Render_Should_Return_RenderFragment_For_DatePicker()
    {
        // Arrange
        var birthDate = new DateTime(1990, 1, 1);
        var model = new TestModel { BirthDate = birthDate };
        var field = CreateMockField("Birth Date");
        var context = CreateContext(model, field, birthDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Birth Date");
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("datetime-local");
        input.GetAttribute("value").ShouldBe(birthDate.ToString());
    }

    [Fact]
    public void Render_Should_Handle_Valid_DateTime_Value()
    {
        // Arrange
        var testDate = new DateTime(2023, 12, 25);
        var model = new TestModel { BirthDate = testDate };
        var field = CreateMockField("Event Date");
        var context = CreateContext(model, field, testDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(testDate.ToString());
        cut.Find("label").TextContent.ShouldBe("Event Date");
    }

    [Fact]
    public void Render_Should_Handle_DateTime_MinValue()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.MinValue };
        var field = CreateMockField("Start Date");
        var context = CreateContext(model, field, DateTime.MinValue);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(DateTime.MinValue.ToString());
        cut.Find("label").TextContent.ShouldBe("Start Date");
    }

    [Fact]
    public void Render_Should_Handle_DateTime_MaxValue()
    {
        // Arrange
        var model = new TestModel { BirthDate = DateTime.MaxValue };
        var field = CreateMockField("End Date");
        var context = CreateContext(model, field, DateTime.MaxValue);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(DateTime.MaxValue.ToString());
        cut.Find("label").TextContent.ShouldBe("End Date");
    }

    [Fact]
    public void Render_Should_Handle_Null_DateTime_Value()
    {
        // Arrange
        var model = new TestModel { BirthDate = null };
        var field = CreateMockField("Optional Date");
        var context = CreateContext(model, field, null);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert: a null DateTime renders no "value" attribute at all.
        cut.Find("input").HasAttribute("value").ShouldBeFalse();
        cut.Find("label").TextContent.ShouldBe("Optional Date");
    }

    [Fact]
    public void Render_Should_Handle_Nullable_DateTime_With_Value()
    {
        // Arrange
        var testDate = new DateTime(2024, 6, 15);
        var model = new TestModel { BirthDate = testDate };
        var field = CreateMockField("Birth Date");
        var context = CreateContext(model, field, (DateTime?)testDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(testDate.ToString());
        cut.Find("label").TextContent.ShouldBe("Birth Date");
    }

    [Fact]
    public void Render_Should_Handle_All_Field_Properties()
    {
        // Arrange
        var eventDate = DateTime.Today;
        var model = new TestModel { BirthDate = eventDate };
        var field = CreateMockField("Event Date", "Select the date of the event", true, true);
        var context = CreateContext(model, field, eventDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Event Date");
        var input = cut.Find("input");
        input.GetAttribute("value").ShouldBe(eventDate.ToString());
        input.HasAttribute("disabled").ShouldBeTrue();
        cut.Find(".help-text").TextContent.ShouldBe("Select the date of the event");
    }

    [Fact]
    public void Render_Should_Handle_Empty_HelpText()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField("Date", "");
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Date");
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("div.test-datetime-field").QuerySelector(".help-text").ShouldBeNull();
    }

    [Fact]
    public void Render_Should_Handle_Required_Field()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField("Required Date", isRequired: true);
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Required Date");
    }

    [Fact]
    public void Render_Should_Handle_Disabled_Field()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField("Disabled Date", isDisabled: true);
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Render_Should_Handle_Today_Date()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField("Today");
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Today");
    }

    [Fact]
    public void Render_Should_Handle_DateTime_With_Time_Component()
    {
        // Arrange
        var dateTimeWithTime = new DateTime(2024, 3, 15, 14, 30, 45);
        var model = new TestModel { BirthDate = dateTimeWithTime };
        var field = CreateMockField("Date with Time");
        var context = CreateContext(model, field, dateTimeWithTime);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(dateTimeWithTime.ToString());
        cut.Find("label").TextContent.ShouldBe("Date with Time");
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
        A.CallTo(() => field.IsRequired).Returns(isRequired);
        A.CallTo(() => field.IsDisabled).Returns(isDisabled);
        A.CallTo(() => field.DisabledCondition).Returns(null);
        A.CallTo(() => field.AdditionalAttributes).Returns(new Dictionary<string, object>());

        return field;
    }

    private IFieldRenderContext<TestModel> CreateContext(
        TestModel model,
        IFieldConfiguration<TestModel, object> field,
        object? currentValue)
    {
        var context = A.Fake<IFieldRenderContext<TestModel>>();

        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(currentValue?.GetType() ?? typeof(DateTime));
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));

        return context;
    }

    [Fact]
    public void Render_Should_Handle_DateOnly_Type()
    {
        // Arrange
        var dateValue = new DateTime(2024, 6, 15);
        var model = new TestModel { BirthDate = dateValue };
        var field = CreateMockField("Date Only");
        var context = CreateContextWithType(model, field, dateValue, typeof(DateOnly));

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - ActualFieldType only carries metadata; CurrentValue is still stringified as-is.
        cut.Find("input").GetAttribute("value").ShouldBe(dateValue.ToString());
        cut.Find("label").TextContent.ShouldBe("Date Only");
    }

    [Fact]
    public void Render_Should_Handle_Min_Date_Constraint()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Constrained Date", new Dictionary<string, object>
        {
            { "MinDate", new DateTime(2020, 1, 1) }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - this stub ignores AdditionalAttributes entirely; MinDate never reaches the DOM.
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Constrained Date");
    }

    [Fact]
    public void Render_Should_Handle_Max_Date_Constraint()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Max Date", new Dictionary<string, object>
        {
            { "MaxDate", new DateTime(2030, 12, 31) }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Max Date");
    }

    [Fact]
    public void Render_Should_Handle_Date_Format_Attribute()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Formatted Date", new Dictionary<string, object>
        {
            { "DateFormat", "yyyy-MM-dd" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - the stub always renders the raw ToString(), ignoring any DateFormat attribute.
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Formatted Date");
    }

    [Fact]
    public void Render_Should_Handle_Culture_Attribute()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Localized Date", new Dictionary<string, object>
        {
            { "Culture", "fr-FR" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Localized Date");
    }

    [Fact]
    public void Render_Should_Handle_Editable_Mode()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Editable Date", new Dictionary<string, object>
        {
            { "Editable", true }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Editable Date");
    }

    [Fact]
    public void Render_Should_Handle_ReadOnly_State()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("ReadOnly Date", new Dictionary<string, object>
        {
            { "ReadOnly", true }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("ReadOnly Date");
    }

    [Fact]
    public void Render_Should_Handle_Placeholder_Text()
    {
        // Arrange
        var model = new TestModel { BirthDate = null };
        var field = CreateMockFieldWithAttributes("Date with Placeholder", new Dictionary<string, object>
        {
            { "Placeholder", "Select a date..." }
        });
        var context = CreateContext(model, field, null);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - the placeholder comes from Field.Placeholder, not this AdditionalAttributes key;
        // that property is unconfigured (a fake default of "") so the rendered attribute is empty.
        var input = cut.Find("input");
        input.HasAttribute("value").ShouldBeFalse();
        input.GetAttribute("placeholder").ShouldBe(string.Empty);
        cut.Find("label").TextContent.ShouldBe("Date with Placeholder");
    }

    [Fact]
    public void Render_Should_Handle_String_Date_Value()
    {
        // Arrange
        var model = new TestModel { BirthDate = null };
        var field = CreateMockField("String Date");
        var context = CreateContext(model, field, "2024-06-15"); // String representation

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - the stub stringifies whatever it is given; it does not parse dates.
        cut.Find("input").GetAttribute("value").ShouldBe("2024-06-15");
        cut.Find("label").TextContent.ShouldBe("String Date");
    }

    [Fact]
    public void Render_Should_Handle_Invalid_Date_String()
    {
        // Arrange
        var model = new TestModel { BirthDate = null };
        var field = CreateMockField("Invalid Date");
        var context = CreateContext(model, field, "not a date");

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - no validation happens at render time.
        cut.Find("input").GetAttribute("value").ShouldBe("not a date");
        cut.Find("label").TextContent.ShouldBe("Invalid Date");
    }

    [Fact]
    public void Render_Should_Handle_Leap_Year_Date()
    {
        // Arrange
        var leapDate = new DateTime(2024, 2, 29); // Leap year date
        var model = new TestModel { BirthDate = leapDate };
        var field = CreateMockField("Leap Year Date");
        var context = CreateContext(model, field, leapDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(leapDate.ToString());
        cut.Find("label").TextContent.ShouldBe("Leap Year Date");
    }

    [Fact]
    public void Render_Should_Handle_Different_Time_Zones()
    {
        // Arrange
        var utcDate = DateTime.UtcNow;
        var model = new TestModel { BirthDate = utcDate };
        var field = CreateMockField("UTC Date");
        var context = CreateContext(model, field, utcDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(utcDate.ToString());
        cut.Find("label").TextContent.ShouldBe("UTC Date");
    }

    [Fact]
    public void Render_Should_Handle_DatePickerMode_Attribute()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Picker Mode", new Dictionary<string, object>
        {
            { "PickerMode", "Month" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Picker Mode");
    }

    [Fact]
    public void Render_Should_Handle_Clearable_Attribute()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Clearable Date", new Dictionary<string, object>
        {
            { "Clearable", true }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Clearable Date");
    }

    [Fact]
    public void Render_Should_Handle_CSS_Classes()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Styled Date", new Dictionary<string, object>
        {
            { "class", "date-picker-custom mt-4" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - the wrapper's class comes from the stub itself, not from AdditionalAttributes.
        cut.Find("div").ClassName.ShouldBe("test-datetime-field");
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
    }

    [Fact]
    public void Render_Should_Handle_ARIA_Attributes()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Accessible Date", new Dictionary<string, object>
        {
            { "aria-label", "Select event date" },
            { "aria-describedby", "date-help" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Accessible Date");
    }

    [Fact]
    public void Render_Should_Handle_Dense_Mode()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Dense Date", new Dictionary<string, object>
        {
            { "Dense", true }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Dense Date");
    }

    [Fact]
    public void Render_Should_Handle_Variant_Attribute()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Outlined Date", new Dictionary<string, object>
        {
            { "Variant", "Outlined" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Outlined Date");
    }

    [Fact]
    public void Render_Should_Handle_Margin_Attribute()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Date with Margin", new Dictionary<string, object>
        {
            { "Margin", "Dense" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Date with Margin");
    }

    [Fact]
    public void Render_Should_Handle_Multiple_Attributes()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Complex Date", new Dictionary<string, object>
        {
            { "MinDate", new DateTime(2020, 1, 1) },
            { "MaxDate", new DateTime(2030, 12, 31) },
            { "DateFormat", "dd/MM/yyyy" },
            { "Clearable", true },
            { "Dense", true },
            { "class", "custom-date" }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Complex Date");
    }

    [Fact]
    public void Render_Should_Handle_First_Day_Of_Week()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Week Start", new Dictionary<string, object>
        {
            { "FirstDayOfWeek", DayOfWeek.Monday }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Week Start");
    }

    [Fact]
    public void Render_Should_Handle_Disabled_Dates_Function()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockFieldWithAttributes("Disabled Dates", new Dictionary<string, object>
        {
            { "IsDateDisabled", new Func<DateTime, bool>(date => date.DayOfWeek == DayOfWeek.Sunday) }
        });
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - a Func-valued attribute doesn't break rendering; the stub ignores it.
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
        cut.Find("label").TextContent.ShouldBe("Disabled Dates");
    }

    [Fact]
    public void Render_Should_Handle_Year_1900()
    {
        // Arrange
        var oldDate = new DateTime(1900, 1, 1);
        var model = new TestModel { BirthDate = oldDate };
        var field = CreateMockField("Old Date");
        var context = CreateContext(model, field, oldDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(oldDate.ToString());
        cut.Find("label").TextContent.ShouldBe("Old Date");
    }

    [Fact]
    public void Render_Should_Handle_Year_9999()
    {
        // Arrange
        var futureDate = new DateTime(9999, 12, 31);
        var model = new TestModel { BirthDate = futureDate };
        var field = CreateMockField("Future Date");
        var context = CreateContext(model, field, futureDate);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe(futureDate.ToString());
        cut.Find("label").TextContent.ShouldBe("Future Date");
    }

    [Fact]
    public void Render_Should_Handle_Empty_Label()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField("");
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("");
        cut.Find("input").GetAttribute("value").ShouldBe(today.ToString());
    }

    [Fact]
    public void Render_Should_Handle_Very_Long_Label()
    {
        // Arrange
        var longLabel = new string('A', 500);
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField(longLabel);
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe(longLabel);
    }

    [Fact]
    public void Render_Should_Handle_Special_Characters_In_Label()
    {
        // Arrange
        var today = DateTime.Today;
        var model = new TestModel { BirthDate = today };
        var field = CreateMockField("Date <Field> & \"Time\" 'Selection'");
        var context = CreateContext(model, field, today);

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Date <Field> & \"Time\" 'Selection'");
    }

    [Fact]
    public void Render_Should_Handle_Non_DateTime_Value()
    {
        // Arrange
        var model = new TestModel { BirthDate = null };
        var field = CreateMockField("Test");
        var context = CreateContext(model, field, 12345); // Integer value

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert - no type validation happens; the value is stringified as given.
        cut.Find("input").GetAttribute("value").ShouldBe("12345");
        cut.Find("label").TextContent.ShouldBe("Test");
    }

    [Fact]
    public void Render_Should_Handle_Object_Value()
    {
        // Arrange
        var model = new TestModel { BirthDate = null };
        var field = CreateMockField("Test");
        var context = CreateContext(model, field, new object());

        // Act
        var cut = Render(_renderer.Render(context));

        // Assert
        cut.Find("input").GetAttribute("value").ShouldBe("System.Object");
        cut.Find("label").TextContent.ShouldBe("Test");
    }

    private IFieldConfiguration<TestModel, object> CreateMockFieldWithAttributes(
        string label,
        Dictionary<string, object> attributes)
    {
        var field = A.Fake<IFieldConfiguration<TestModel, object>>();

        A.CallTo(() => field.Label).Returns(label);
        A.CallTo(() => field.HelpText).Returns(string.Empty);
        A.CallTo(() => field.IsRequired).Returns(false);
        A.CallTo(() => field.IsDisabled).Returns(false);
        A.CallTo(() => field.DisabledCondition).Returns(null);
        A.CallTo(() => field.AdditionalAttributes).Returns(attributes);

        return field;
    }

    private IFieldRenderContext<TestModel> CreateContextWithType(
        TestModel model,
        IFieldConfiguration<TestModel, object> field,
        object? currentValue,
        Type fieldType)
    {
        var context = A.Fake<IFieldRenderContext<TestModel>>();

        A.CallTo(() => context.Model).Returns(model);
        A.CallTo(() => context.Field).Returns(field);
        A.CallTo(() => context.CurrentValue).Returns(currentValue);
        A.CallTo(() => context.ActualFieldType).Returns(fieldType);
        A.CallTo(() => context.OnValueChanged).Returns(EventCallback.Factory.Create<object?>(this, _ => { }));
        A.CallTo(() => context.OnDependencyChanged).Returns(EventCallback.Factory.Create(this, () => { }));

        return context;
    }

    [Fact]
    public void RenderField_Should_Render_Label_DateInput_And_HelpText_With_No_Adapter_Registered()
    {
        // Arrange - the standalone-consumer path: services.AddFormCraft() with no UI adapter.
        var model = new TestModel { BirthDate = new DateTime(2024, 5, 17) };
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.BirthDate, field => field
                .WithLabel("Birth Date")
                .WithHelpText("As shown on your ID"))
            .Build();
        var field = config.Fields.First(f => f.FieldName == "BirthDate");

        // Act
        var cut = Render(RendererService.RenderField(model, field, default, default));

        // Assert
        cut.Find("label").TextContent.ShouldBe("Birth Date");
        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("datetime-local");
        input.GetAttribute("value").ShouldBe(model.BirthDate!.Value.ToString());
        cut.Find(".help-text").TextContent.ShouldBe("As shown on your ID");
    }

    public class TestModel
    {
        public DateTime? BirthDate { get; set; }
    }
}
