namespace FormCraft.ForMudBlazor.UnitTests.Diagnostics;

/// <summary>
/// Tests the rule behind the diagnostic that reports a mask blanking a stored value (#266).
/// <para>
/// The rule is deliberately narrow, and the narrowness is the whole design: a mask that reformats
/// <c>5551234567</c> into <c>(555) 123-4567</c> is doing its job, and warning about it would fire on
/// every correctly-masked field in every form until someone muted the category. Only total collapse
/// — a value went in, nothing came out — means the mask rejected the value outright, which is the
/// case where the user sees a blank field and the model quietly keeps the original.
/// </para>
/// </summary>
public class MaskedValueDiagnosticTests
{
    [Fact]
    public void Applies_Should_Be_True_When_A_NonBlank_Value_Masks_To_Blank()
    {
        // Arrange - the reported case: legacy data that predates the mask. The value survives in the
        // model, the field renders empty, and nothing says so.

        // Act
        var applies = MaskedValueDiagnostic.Applies("N/A", string.Empty);

        // Assert
        applies.ShouldBeTrue();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Mask_Reformats_The_Value()
    {
        // Arrange - reformatting is the mask working as intended. Nothing was lost, so there is
        // nothing to report; warning here would make the diagnostic useless noise.

        // Act
        var applies = MaskedValueDiagnostic.Applies("5551234567", "(555) 123-4567");

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Value_Is_Empty()
    {
        // Arrange - an empty field masks to empty. There was nothing to lose, so this is the
        // overwhelmingly common case of a blank optional field and must stay silent.

        // Act
        var applies = MaskedValueDiagnostic.Applies(string.Empty, string.Empty);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Value_Is_Null()
    {
        // Arrange - the unset-model case, reached before a user has typed anything.

        // Act
        var applies = MaskedValueDiagnostic.Applies(null, null);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Value_Is_Only_Whitespace()
    {
        // Arrange - "   " is a blank field wearing a different hat, and it is what a trimmed-to-blank
        // setting or a padded database column produces. Treating it as a value worth warning about
        // would fire the diagnostic on fields the developer has already left empty. Matches how
        // TextMaskMap.Resolve reads a whitespace-only PATTERN as "no mask configured".

        // Act
        var applies = MaskedValueDiagnostic.Applies("   ", string.Empty);

        // Assert
        applies.ShouldBeFalse();
    }
}
