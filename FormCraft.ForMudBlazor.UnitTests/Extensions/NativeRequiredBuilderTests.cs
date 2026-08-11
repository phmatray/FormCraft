namespace FormCraft.ForMudBlazor.UnitTests.Extensions;

/// <summary>
/// Tests the typed builder method for MudBlazor's native required decoration (#204).
/// </summary>
/// <remarks>
/// #193 introduced the opt-in as a documented magic string — <c>.WithAttribute("Required", true)</c> —
/// which is undiscoverable, unchecked by the compiler, and one typo (<c>"required"</c>) away from
/// silently doing nothing. These tests pin the typed replacement and the fact that the raw string
/// keeps working, since this is additive rather than a breaking change.
/// </remarks>
public class NativeRequiredBuilderTests
{
    [Fact]
    public void WithNativeRequired_Should_Write_The_Required_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithNativeRequired())
            .Build();

        // Assert
        Attributes(config)["Required"].ShouldBe(true);
    }

    [Fact]
    public void WithNativeRequired_False_Should_Write_False()
    {
        // Arrange & Act - an explicit opt-out has to be expressible, or the method is a one-way door.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithNativeRequired(false))
            .Build();

        // Assert
        Attributes(config)["Required"].ShouldBe(false);
    }

    [Fact]
    public void WithNativeRequired_Should_Write_The_Same_Attribute_As_The_Raw_String()
    {
        // Arrange & Act - the typed method must be a pure alias, not a parallel mechanism: the raw
        // form is documented and in use, and two keys meaning "required" would be a new divergence.
        var typed = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithNativeRequired())
            .Build();

        var raw = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithAttribute("Required", true))
            .Build();

        // Assert
        Attributes(typed)["Required"].ShouldBe(Attributes(raw)["Required"]);
    }

    [Fact]
    public void WithNativeRequired_Should_Return_The_Same_Builder_For_Chaining()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create();
        FieldBuilder<TestModel, string>? captured = null;
        FieldBuilder<TestModel, string>? returned = null;

        // Act
        config.AddField(x => x.Name, f =>
        {
            captured = f;
            returned = f.WithNativeRequired();
        });

        // Assert - the fluent contract: With* configures and returns `this`, no side effects.
        returned.ShouldBeSameAs(captured);
    }

    [Fact]
    public void WithNativeRequired_Should_Be_Available_On_A_Numeric_Field()
    {
        // Arrange & Act - declared on the general TValue overload rather than string-only, so the
        // numeric and date item renderers can use it too. This would not compile if it were
        // constrained to string.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Quantity, f => f.WithNativeRequired())
            .Build();

        // Assert
        Attributes(config)["Required"].ShouldBe(true);
    }

    [Fact]
    public void Required_Alone_Should_Not_Write_The_Native_Attribute()
    {
        // Arrange & Act - the #190 invariant, restated here so a regression names itself: validation
        // is server-side, and `.Required(...)` must never start emitting the HTML5 decoration again.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.Required("Name is required"))
            .Build();

        // Assert
        Attributes(config).ContainsKey("Required").ShouldBeFalse();
    }

    private static IReadOnlyDictionary<string, object> Attributes(IFormConfiguration<TestModel> config) =>
        config.Fields[0].AdditionalAttributes;

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
