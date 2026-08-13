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
    /// <summary>
    /// The literal characters of <c>(000) 000-0000</c>, the pattern every case here is written
    /// against — i.e. what <see cref="MaskedValueDiagnostic.LiteralsOf"/> returns for it.
    /// </summary>
    private const string PhoneLiterals = "() -";

    [Fact]
    public void Applies_Should_Be_True_When_A_NonBlank_Value_Masks_To_Blank()
    {
        // Arrange - the reported case: legacy data that predates the mask. The value survives in the
        // model, the field renders empty, and nothing says so.

        // Act
        var applies = MaskedValueDiagnostic.Applies("N/A", string.Empty, PhoneLiterals);

        // Assert
        applies.ShouldBeTrue();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Mask_Reformats_The_Value()
    {
        // Arrange - reformatting is the mask working as intended. Nothing was lost, so there is
        // nothing to report; warning here would make the diagnostic useless noise.

        // Act
        var applies = MaskedValueDiagnostic.Applies("5551234567", "(555) 123-4567", PhoneLiterals);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Value_Is_Empty()
    {
        // Arrange - an empty field masks to empty. There was nothing to lose, so this is the
        // overwhelmingly common case of a blank optional field and must stay silent.

        // Act
        var applies = MaskedValueDiagnostic.Applies(string.Empty, string.Empty, PhoneLiterals);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Value_Is_Null()
    {
        // Arrange - the unset-model case, reached before a user has typed anything.

        // Act
        var applies = MaskedValueDiagnostic.Applies(null, null, PhoneLiterals);

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
        var applies = MaskedValueDiagnostic.Applies("   ", string.Empty, PhoneLiterals);

        // Assert
        applies.ShouldBeFalse();
    }

    // ---------------------------------------------------------------------------------------------
    // #283(b) — the widened rule. A mask that discards PART of a value is reported too, because the
    // model then diverges from the display just as surely as when it blanks. It is arguably the worse
    // of the two: a blank field is visibly wrong, whereas "(155) 512-3456" is a plausible phone
    // number that simply is not the one on record.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// The full behaviour table: a discard is reported, a reformat is not.
    /// </summary>
    /// <remarks>
    /// The rule is "strip the mask's own literals from both sides and compare". A reformat only ever
    /// moves literals around, so the two sides reduce to the same characters; a discard loses
    /// characters that no amount of reformatting can restore.
    /// <para>
    /// Rows 5 and 6 are the ones that keep it honest, and neither is hypothetical. Stored data
    /// routinely carries its OWN separators — <c>555 123 4567</c>, <c>555-123-4567</c> — and a rule
    /// comparing raw strings, or testing whether the stored value survives as a subsequence of the
    /// rendered one, reports both as discards. They are pure reformats: the same ten digits go in and
    /// come out.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("N/A", "", true)]                             // total collapse — #266, unchanged
    [InlineData("+1 555 123 4567", "(155) 512-3456", true)]   // #283(b): shifted, a DIFFERENT number
    [InlineData("N/A5551234567", "(555) 123-4567", true)]     // #283(b): the letters were dropped
    [InlineData("5551234567", "(555) 123-4567", false)]       // reformat
    [InlineData("555 123 4567", "(555) 123-4567", false)]     // reformat, value had its own spaces
    [InlineData("(555) 123-4567", "(555) 123-4567", false)]   // already formatted — nothing happened
    public void Applies_Should_Report_A_Discard_But_Never_A_Reformat(
        string stored,
        string rendered,
        bool expected)
    {
        // Act
        var applies = MaskedValueDiagnostic.Applies(stored, rendered, PhoneLiterals);

        // Assert
        applies.ShouldBe(expected);
    }

    [Fact]
    public void Applies_Should_Ignore_CleanDelimiters()
    {
        // Arrange - the #265 edge case the spec flagged: a mask that strips its own literals must not
        // read as having discarded them. It cannot, because the rule removes those same literals from
        // BOTH sides before comparing -- so the verdict is the same whichever way CleanDelimiters is
        // set, even though the two produce different clean text. This is the property that made
        // LiteralsOf the right input and GetCleanText the wrong one.

        // Act - what the field renders is identical either way; only the model write-back differs.
        var reformat = MaskedValueDiagnostic.Applies("5551234567", "(555) 123-4567", PhoneLiterals);
        var discard = MaskedValueDiagnostic.Applies("N/A5551234567", "(555) 123-4567", PhoneLiterals);

        // Assert
        reformat.ShouldBeFalse();
        discard.ShouldBeTrue();
    }

    [Theory]
    [InlineData("N/A", "", true)]
    [InlineData("+1 555 123 4567", "(155) 512-3456", false)]
    public void Applies_Should_Fall_Back_To_Total_Collapse_When_The_Literals_Are_Unknown(
        string stored,
        string rendered,
        bool expected)
    {
        // Arrange - a null literal set means "no opinion", which LiteralsOf returns for a mask whose
        // decoration cannot be read off a pattern (a RegexMask from the #265 factory) or whose
        // Transformation rewrites characters as it consumes them. Guessing there would report every
        // value of a correctly configured field. Falling back to #266's rule keeps the blank case
        // covered and reports nothing it cannot justify.

        // Act
        var applies = MaskedValueDiagnostic.Applies(stored, rendered, maskLiterals: null);

        // Assert
        applies.ShouldBe(expected);
    }

    [Fact]
    public void Applies_Should_Report_A_Value_Made_Only_Of_Literals()
    {
        // Arrange - the regression this rule could plausibly have introduced, which is why the blank
        // case stays an explicit disjunct rather than being folded into the comparison. "()-" is
        // non-blank but contains nothing the mask keeps, so it renders empty AND both sides reduce to
        // "" -- meaning a pure strip-and-compare rule would call it a reformat and go silent, losing
        // a case #266 reported.

        // Act
        var applies = MaskedValueDiagnostic.Applies("()-", string.Empty, PhoneLiterals);

        // Assert
        applies.ShouldBeTrue();
    }

    [Fact]
    public void LiteralsOf_Should_Return_The_Patterns_NonPlaceholder_Characters()
    {
        // Act
        var literals = MaskedValueDiagnostic.LiteralsOf(new PatternMask("(000) 000-0000"));

        // Assert - distinct, in first-appearance order; the digits' placeholder '0' is not decoration.
        literals.ShouldBe(PhoneLiterals);
    }

    [Fact]
    public void LiteralsOf_Should_Be_Null_For_A_Mask_It_Cannot_Read()
    {
        // Arrange - a RegexMask reaches the diagnostic through the #265 factory. Its Mask is a
        // regular expression, so its non-placeholder characters are metacharacters rather than
        // decoration, and stripping them would be nonsense dressed up as a rule.

        // Act
        var literals = MaskedValueDiagnostic.LiteralsOf(new RegexMask(@"^\d{0,10}$"));

        // Assert
        literals.ShouldBeNull();
    }

    [Fact]
    public void LiteralsOf_Should_Be_Null_For_A_Transforming_Mask()
    {
        // Arrange - a Transformation rewrites characters as the mask consumes them, so the rendered
        // text legitimately differs from the stored value character for character and EVERY value
        // would read as a discard. Firing on every value of a correctly configured field is the
        // happy-path false positive the whole diagnostic is shaped to avoid.
        var mask = new PatternMask("aaa") { Transformation = char.ToUpperInvariant };

        // Act
        var literals = MaskedValueDiagnostic.LiteralsOf(mask);

        // Assert
        literals.ShouldBeNull();
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
    /// <c>GetCleanText()</c> tracks <c>CleanDelimiters</c> — it is not a stable "what survived".
    /// </summary>
    /// <remarks>
    /// The measurement that ruled out the issue's proposed implementation. With the default
    /// <c>CleanDelimiters = false</c> — what FormCraft configures unless <c>.WithMask(…, true)</c>
    /// says otherwise (#265) — <c>GetCleanText()</c> hands back the formatted <c>Text</c>, literals
    /// and all. Only with the flag set does it strip them.
    /// <para>
    /// So the same stored value would yield opposite verdicts under a <c>GetCleanText()</c>-based
    /// rule depending on a setting that has nothing to do with whether data was lost. The rule uses
    /// the mask's literal set instead, which is invariant across the flag — pinned from the rule's
    /// side by <c>Applies_Should_Ignore_CleanDelimiters</c>.
    /// </para>
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
