namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Rendering-level coverage for the configurable ShrinkLabel (#177).
/// <para>
/// ShrinkLabelConfigurationTests asserts the value FormCraft passes *into* the MudBlazor
/// component. That is necessary but not sufficient: MudBlazor ORs ShrinkLabel together with
/// several other conditions before emitting the "mud-shrink" class, so a field can receive
/// ShrinkLabel=false and still render a pinned label. These tests assert the rendered
/// outcome instead, and pin down exactly when the setting is and is not observable.
/// </para>
/// </summary>
public class ShrinkLabelRenderingTests : MudBlazorTestBase
{
    private const string ShrinkClass = "mud-shrink";

    [Fact]
    public void WithShrinkLabel_False_Should_Unpin_The_Label_When_No_Placeholder()
    {
        // Arrange - the scenario #177 was filed for: Variant.Text with a floating label
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Name")
                .WithVariant(Variant.Text)
                .WithShrinkLabel(false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Markup.ShouldNotContain(ShrinkClass);
    }

    [Fact]
    public void Default_Should_Keep_The_Label_Pinned()
    {
        // Arrange
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Markup.ShouldContain(ShrinkClass);
    }

    [Fact]
    public void WithShrinkLabel_False_Should_Not_Unpin_The_Label_When_A_Placeholder_Is_Set()
    {
        // Arrange - MudBlazor's MudInput ORs ShrinkLabel with the presence of a placeholder,
        // a start adornment and a non-empty value, so ShrinkLabel=false cannot win against
        // any of them. Documented on WithShrinkLabel and in the README; asserted here so the
        // limitation is a known, tested fact rather than a surprise in the field.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Name")
                .WithPlaceholder("john@example.com")
                .WithVariant(Variant.Text)
                .WithShrinkLabel(false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Markup.ShouldContain(ShrinkClass);
    }

    [Fact]
    public void WithShrinkLabel_False_Should_Not_Unpin_The_Label_When_The_Field_Has_A_Value()
    {
        // Arrange - a populated field always shows its label shrunk, whatever ShrinkLabel says
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f
                .WithLabel("Name")
                .WithVariant(Variant.Text)
                .WithShrinkLabel(false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel { Name = "Philippe" })
            .Add(c => c.Configuration, config));

        // Assert
        component.Markup.ShouldContain(ShrinkClass);
    }

    [Fact]
    public void LovField_Label_Is_Always_Pinned_Regardless_Of_ShrinkLabel()
    {
        // Arrange - the LOV input hardcodes Placeholder="@(Placeholder ?? "Click to select...")",
        // which is never empty, so MudBlazor's placeholder rule pins the label on its own and
        // ShrinkLabel cannot influence the rendering either way. Asserted so nobody documents
        // a LOV-visible ShrinkLabel behaviour that does not exist.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.CityId, f => f
                .WithLabel("City")
                .AsLov<TestModel, int, CityDto>(lov => lov
                    .WithDataSource(() => new[] { new CityDto { Id = 1, Name = "Paris" } })
                    .WithKey(c => c.Id)
                    .WithDisplay((Expression<Func<CityDto, string>>)(c => c.Name)))
                .WithShrinkLabel(false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert - pinned despite ShrinkLabel=false
        component.Markup.ShouldContain(ShrinkClass);
    }

    private class CityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int CityId { get; set; }
    }
}
