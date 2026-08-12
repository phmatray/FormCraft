using FormCraft.ForFluentUI.UnitTests.Components;

namespace FormCraft.ForFluentUI.UnitTests.Fields;

/// <summary>
/// <c>.WithHelpText(...)</c> must reach the rendered form, on every field type, and be announced.
/// </summary>
/// <remarks>
/// <para>
/// Per-field instructions are the WCAG 2.1 <b>3.3.2</b> mechanism this adapter otherwise honours
/// through <c>aria-required</c>, so dropping them would undo half of that work.
/// </para>
/// <para>
/// The text is rendered by the container, not bound to each input's <c>Message</c> parameter.
/// Measured on v5 RC5: <c>FluentTextInput</c> and <c>FluentNumberInput</c> overwrite a
/// caller-supplied <c>Message</c> with their own validation string, while <c>FluentCheckbox</c>,
/// <c>FluentDatePicker</c> and <c>FluentSelect</c> leave it intact - so binding it would give help
/// text that works on three field types and silently vanishes on two. These tests assert the
/// uniform mechanism instead.
/// </para>
/// </remarks>
public class HelpTextTests : FluentUITestBase
{
    [Fact]
    public void Text_Field_Should_Render_Its_Help_Text()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Password")
                .WithHelpText("At least 12 characters"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find(".formcraft-field-help").TextContent.Trim()
            .ShouldBe("At least 12 characters");
    }

    [Fact]
    public void Numeric_Field_Should_Render_Its_Help_Text()
    {
        // Arrange
        var config = FormBuilder<NumericTestModel>.Create()
            .AddField(x => x.Quantity, f => f
                .WithLabel("Quantity")
                .WithHelpText("Whole units only"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<NumericTestModel>>(p => p
            .Add(c => c.Model, new NumericTestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find(".formcraft-field-help").TextContent.Trim().ShouldBe("Whole units only");
    }

    [Fact]
    public void Boolean_Field_Should_Render_Its_Help_Text()
    {
        // Arrange
        var config = FormBuilder<BooleanTestModel>.Create()
            .AddField(x => x.IsActive, f => f
                .WithLabel("Active")
                .WithHelpText("Inactive accounts are archived"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<BooleanTestModel>>(p => p
            .Add(c => c.Model, new BooleanTestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find(".formcraft-field-help").TextContent.Trim()
            .ShouldBe("Inactive accounts are archived");
    }

    [Fact]
    public void Date_Field_Should_Render_Its_Help_Text()
    {
        // Arrange
        var config = FormBuilder<DateTestModel>.Create()
            .AddField(x => x.BirthDate, f => f
                .WithLabel("Birth date")
                .WithHelpText("Used to verify eligibility"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<DateTestModel>>(p => p
            .Add(c => c.Model, new DateTestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find(".formcraft-field-help").TextContent.Trim()
            .ShouldBe("Used to verify eligibility");
    }

    [Fact]
    public void Select_Field_Should_Render_Its_Help_Text()
    {
        // Arrange
        var config = FormBuilder<SelectTestModel>.Create()
            .AddField(x => x.Country, f => f
                .WithLabel("Country")
                .WithOptions(("BE", "Belgium"))
                .WithHelpText("Where you are billed"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<SelectTestModel>>(p => p
            .Add(c => c.Model, new SelectTestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find(".formcraft-field-help").TextContent.Trim()
            .ShouldBe("Where you are billed");
    }

    [Fact]
    public void Help_Text_Should_Be_Announced_Via_Aria_Describedby()
    {
        // Arrange - visible but unannounced is the failure mode this adapter refuses elsewhere
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Password")
                .WithHelpText("At least 12 characters"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert - the input points at the element that actually holds the text
        var helpId = component.Find(".formcraft-field-help").GetAttribute("id");
        helpId.ShouldNotBeNullOrWhiteSpace();
        component.FindAll($"[aria-describedby='{helpId}']").ShouldNotBeEmpty();
    }

    [Fact]
    public void A_Field_Without_Help_Text_Should_Render_None()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert - no empty element, and nothing describing a description that is not there
        component.FindAll(".formcraft-field-help").ShouldBeEmpty();
        component.FindAll("[aria-describedby]").ShouldBeEmpty();
    }
}
