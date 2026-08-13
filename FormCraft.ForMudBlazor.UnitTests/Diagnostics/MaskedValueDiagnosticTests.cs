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

    // ---------------------------------------------------------------------------------------------
    // #283 Task 2 — characterisation. Ground truth before the rule, because the rule in Task 3 hangs
    // entirely on whether MudBlazor gives us something that can tell a DISCARD apart from a
    // REFORMAT. These tests record what PatternMask 9.8.0 actually does rather than what the issue
    // assumed it does; if a MudBlazor upgrade changes any of it, these fail first and name the
    // reason, instead of the rule quietly going wrong.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// What <c>PatternMask("(000) 000-0000")</c> produces for the four values in the issue's table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Text</c> is what renders. <c>GetCleanText()</c> is what the mask considers the significant
    /// characters — the value with the pattern's literals stripped — and is what <c>CleanDelimiters</c>
    /// makes the mask report back to the model (#265).
    /// </para>
    /// <para>
    /// First finding: the mask CONSUMES characters positionally and silently drops whatever does not
    /// fit. <c>"+1 555 123 4567"</c> keeps only the first ten digits — <i>a different phone
    /// number</i> — and <c>"N/A5551234567"</c> drops the letters and keeps the digits. Neither
    /// blanks, so #266's total-collapse rule cannot see either.
    /// </para>
    /// <para>
    /// Second finding, and the one that changed the design: <b><c>GetCleanText()</c> is not "what
    /// survived".</b> The issue assumed it returns the mask-significant characters; measured against
    /// 9.8.0 it returns <c>Text</c> verbatim whenever <c>CleanDelimiters</c> is <c>false</c> — which
    /// is FormCraft's default and therefore the normal case. Pinned by
    /// <see cref="PatternMask_CleanText_Should_Follow_CleanDelimiters"/>. A rule written on
    /// <c>GetCleanText()</c> would have compared <c>"(555) 123-4567"</c> against the raw stored value
    /// and fired on every correctly-masked field — the exact happy-path false positive #266 was
    /// designed to avoid.
    /// </para>
    /// <para>
    /// So Task 3 takes the fallback the spec named: compare the two sides with the MASK'S OWN
    /// LITERALS removed from each. <c>"555 123 4567"</c> and <c>"(555) 123-4567"</c> both reduce to
    /// <c>5551234567</c> and stay silent, while <c>"+1 555 123 4567"</c> reduces to
    /// <c>+15551234567</c> against the rendered <c>1555123456</c> and warns. The literal set is
    /// derivable because the mask exposes its placeholder alphabet — see
    /// <see cref="PatternMask_Should_Expose_Its_Placeholder_Alphabet"/>.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("N/A", "", "")]
    [InlineData("+1 555 123 4567", "(155) 512-3456", "(155) 512-3456")]
    [InlineData("N/A5551234567", "(555) 123-4567", "(555) 123-4567")]
    [InlineData("5551234567", "(555) 123-4567", "(555) 123-4567")]
    public void PatternMask_Should_Produce_The_Recorded_Text_And_CleanText(
        string stored,
        string expectedText,
        string expectedCleanText)
    {
        // Arrange
        var mask = new PatternMask("(000) 000-0000");

        // Act
        mask.SetText(stored);

        // Assert
        mask.Text.ShouldBe(expectedText);
        mask.GetCleanText().ShouldBe(expectedCleanText);
    }

    /// <summary>
    /// <c>GetCleanText()</c> reports the significant characters regardless of <c>CleanDelimiters</c>.
    /// </summary>
    /// <remarks>
    /// The edge case the spec flagged (#265): a mask that strips its own literals must not read as
    /// having discarded them. This records that <c>CleanDelimiters</c> changes only what the mask
    /// reports to the MODEL, not what <c>GetCleanText()</c> returns — so a rule written on
    /// <c>GetCleanText()</c> is unaffected by the setting, and the same stored value yields the same
    /// verdict either way. Without this, Task 3's rule would need a special case it does not need.
    /// </remarks>
    [Theory]
    [InlineData(false, "(555) 123-4567")]
    [InlineData(true, "5551234567")]
    public void PatternMask_CleanText_Should_Follow_CleanDelimiters(
        bool cleanDelimiters,
        string expectedCleanText)
    {
        // Arrange
        var mask = new PatternMask("(000) 000-0000") { CleanDelimiters = cleanDelimiters };

        // Act
        mask.SetText("5551234567");

        // Assert - Text is the same either way; only GetCleanText() moves.
        mask.Text.ShouldBe("(555) 123-4567");
        mask.GetCleanText().ShouldBe(expectedCleanText);
    }

    /// <summary>
    /// The pattern's placeholder alphabet is readable off the mask, so its literals are derivable.
    /// </summary>
    /// <remarks>
    /// This is the fallback the spec named, and measuring it is what makes the fallback usable rather
    /// than hypothetical. <c>MaskChars</c> carries the placeholder characters — <c>0</c>, <c>a</c>,
    /// <c>*</c> by default — so "a literal" is precisely "a pattern character that is not one of
    /// these", computed from the mask in hand rather than from a hardcoded list that a caller
    /// supplying custom <c>MaskChars</c> would silently invalidate.
    /// </remarks>
    [Fact]
    public void PatternMask_Should_Expose_Its_Placeholder_Alphabet()
    {
        // Arrange
        var mask = new PatternMask("(000) 000-0000");

        // Act
        var placeholders = mask.MaskChars.Select(m => m.Char).ToArray();

        // Assert
        placeholders.ShouldBe(['0', 'a', '*'], ignoreOrder: true);
    }
}
