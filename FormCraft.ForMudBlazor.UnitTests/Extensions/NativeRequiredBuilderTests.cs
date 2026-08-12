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
        // Arrange & Act - `.Required(...)` writes a VALIDATOR, not an attribute. The distinction
        // survives #199 and is what this pins: the decoration is now inferred from IsRequired at
        // RENDER time, so the attribute bag must stay clean for `.WithNativeRequired(...)` to remain
        // distinguishable from it. If `.Required(...)` started writing "Required" into the bag, the
        // explicit override could no longer be told apart from the inference and
        // `.WithNativeRequired(false)` would stop working.
        //
        // ⚠️ Not the #190 invariant any more. That was "`.Required(...)` must never emit the HTML5
        // decoration", which #199 deliberately reversed — see AriaRequiredTests, which asserts the
        // decoration IS emitted. Only the builder-level fact is pinned here.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.Required("Name is required"))
            .Build();

        // Assert
        Attributes(config).ContainsKey("Required").ShouldBeFalse();
    }

    [Fact]
    public void WithNativeRequired_Should_Be_Honoured_On_The_Component_Path()
    {
        // Arrange - #204 Task 3, decided rather than left open. Before this the opt-in reached the
        // collection item path only, so `.WithNativeRequired()` on an ordinary field silently did
        // nothing. An escape hatch that works on one render path and not the other is the exact
        // divergence class this library keeps re-filing (#146, #177, #184, #189).
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name").WithNativeRequired())
            .Build();

        // Act
        var component = new TestHost().Render(config);

        // Assert - the component property, the class MudBlazor's asterisk hangs off, and the
        // attribute on the element the user's browser and screen reader actually see.
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeTrue();
        component.FindAll(".mud-input-required").ShouldNotBeEmpty();
        component.Find("input").HasAttribute("required").ShouldBeTrue();
    }

    [Fact]
    public void A_Field_With_Neither_Required_Nor_The_Opt_In_Should_Not_Be_Decorated_On_The_Component_Path()
    {
        // Arrange - the guard on the guard: a field that asked for nothing must be untouched. This
        // is the overwhelmingly common case, and it is what keeps #199's inference from becoming
        // "decorate everything".
        //
        // This test used to configure `.Required("…")` here and assert the same emptiness, pinning
        // the #190 invariant. #199 reverses that deliberately — a required field is now announced to
        // assistive technology — so the `.Required(...)` case moved to AriaRequiredTests, which
        // asserts the opposite, and this one narrowed to the genuinely unconfigured field.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = new TestHost().Render(config);

        // Assert
        component.FindComponent<MudTextField<string>>().Instance.Required.ShouldBeFalse();
        component.Find("input").HasAttribute("required").ShouldBeFalse();
        component.FindAll(".mud-input-required").ShouldBeEmpty();
    }

    /// <summary>
    /// bUnit host for the two component-path cases above. The rest of this suite asserts on the
    /// built configuration and needs no renderer, so the bUnit dependency is confined here.
    /// </summary>
    private sealed class TestHost : MudBlazorTestBase
    {
        public IRenderedComponent<FormCraftComponent<TestModel>> Render(
            IFormConfiguration<TestModel> config) =>
            Render<FormCraftComponent<TestModel>>(parameters => parameters
                .Add(p => p.Model, new TestModel())
                .Add(p => p.Configuration, config));
    }

    private static IReadOnlyDictionary<string, object> Attributes(IFormConfiguration<TestModel> config) =>
        config.Fields[0].AdditionalAttributes;

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
