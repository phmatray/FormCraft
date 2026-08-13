namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Tests the native-required rule now that it lives in core (#279).
/// </summary>
/// <remarks>
/// The rule was <c>internal</c> to <c>FormCraft.ForMudBlazor</c> until #279, which is why
/// <c>FormCraft.ForFluentUI</c> had to hand-copy the expression into
/// <c>FluentUIFieldComponentBase.EffectiveNativeRequired</c> — the exact copy-paste drift the type's
/// own XML doc was written to prevent. These tests exercise the shared public helper directly, so
/// the rule is pinned once rather than once per adapter suite.
/// </remarks>
public class NativeRequiredTests
{
    [Fact]
    public void AttributeName_Should_Be_The_Documented_Required_Key()
    {
        // The raw ".WithAttribute("Required", true)" form is documented and in use, so the constant
        // and the string consumers type have to stay the same key.
        NativeRequired.AttributeName.ShouldBe("Required");
    }

    [Fact]
    public void Resolve_Should_Fall_Back_To_IsRequired_When_Unconfigured()
    {
        // Arrange - nothing configured: the field's own required flag answers.
        var attributes = new Dictionary<string, object>();

        // Act & Assert
        NativeRequired.Resolve(attributes, true).ShouldBeTrue();
        NativeRequired.Resolve(attributes, false).ShouldBeFalse();
    }

    [Fact]
    public void Resolve_Should_Let_An_Explicit_True_Opt_In_A_Field_That_Is_Not_Required()
    {
        // Arrange - the opt-in direction: decoration without .Required(...).
        var attributes = new Dictionary<string, object> { [NativeRequired.AttributeName] = true };

        // Act & Assert
        NativeRequired.Resolve(attributes, false).ShouldBeTrue();
    }

    [Fact]
    public void Resolve_Should_Let_An_Explicit_False_Opt_Out_A_Required_Field()
    {
        // Arrange - the opt-out direction, and the reason presence is tested separately from value:
        // a plain get-with-default collapses "not configured" and "configured false" into one
        // fallback, which would silently re-acquire the decoration .WithNativeRequired(false) was
        // written to suppress.
        var attributes = new Dictionary<string, object> { [NativeRequired.AttributeName] = false };

        // Act & Assert
        NativeRequired.Resolve(attributes, true).ShouldBeFalse();
    }

    [Fact]
    public void Resolve_Should_Ignore_A_Non_Boolean_Value_And_Fall_Back()
    {
        // Arrange - the attribute bag is untyped, so a string "true" is reachable. It is not an
        // opt-in: only a real bool configures the rule, everything else falls through.
        var attributes = new Dictionary<string, object> { [NativeRequired.AttributeName] = "true" };

        // Act & Assert
        NativeRequired.Resolve(attributes, false).ShouldBeFalse();
        NativeRequired.Resolve(attributes, true).ShouldBeTrue();
    }
}
