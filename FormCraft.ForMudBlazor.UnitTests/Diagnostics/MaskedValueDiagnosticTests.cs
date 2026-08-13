namespace FormCraft.ForMudBlazor.UnitTests.Diagnostics;

/// <summary>
/// Tests the rule behind the diagnostic that reports a mask blanking (#266) or partly discarding
/// (#283) a stored value.
/// <para>
/// What the rule must never do is fire on the happy path: a mask that reformats <c>5551234567</c>
/// into <c>(555) 123-4567</c> is doing its job, and warning about that would fire on every
/// correctly-masked field in every form until someone muted the category — taking the real signal
/// with it. So the signal is <b>loss</b>, not difference.
/// </para>
/// <para>
/// ⛔ <b>Do not write a new mask case against <c>(000) 000-0000</c> alone.</b> That pattern's
/// decoration — <c>"() -"</c> — happens to contain the space and hyphen that test values are
/// naturally punctuated with, so it cannot distinguish a rule that strips the mask's decoration from
/// one that strips punctuation generally. An earlier draft of #283 stripped only the mask's own
/// literals and passed this entire suite while warning on `000-00-0000` + `"123 45 6789"`, a value
/// nothing had been lost from. Pair every rule case with a pattern whose decoration does NOT match
/// the value's — see <see cref="Applies_Should_Not_Report_A_Value_Punctuated_Unlike_The_Mask"/>.
/// </para>
/// </summary>
public class MaskedValueDiagnosticTests
{
    /// <summary>
    /// What <see cref="MaskedValueDiagnostic.DecorationOf"/> returns for <c>(000) 000-0000</c>.
    /// </summary>
    private const string PhoneDecoration = "() -";

    [Fact]
    public void Applies_Should_Be_True_When_A_NonBlank_Value_Masks_To_Blank()
    {
        // Arrange - the reported case: legacy data that predates the mask. The value survives in the
        // model, the field renders empty, and nothing says so.

        // Act
        var applies = MaskedValueDiagnostic.Applies("N/A", string.Empty, PhoneDecoration);

        // Assert
        applies.ShouldBeTrue();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Mask_Reformats_The_Value()
    {
        // Arrange - reformatting is the mask working as intended. Nothing was lost, so there is
        // nothing to report; warning here would make the diagnostic useless noise.

        // Act
        var applies = MaskedValueDiagnostic.Applies("5551234567", "(555) 123-4567", PhoneDecoration);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Value_Is_Empty()
    {
        // Arrange - an empty field masks to empty. There was nothing to lose, so this is the
        // overwhelmingly common case of a blank optional field and must stay silent.

        // Act
        var applies = MaskedValueDiagnostic.Applies(string.Empty, string.Empty, PhoneDecoration);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Be_False_When_The_Value_Is_Null()
    {
        // Arrange - the unset-model case, reached before a user has typed anything.

        // Act
        var applies = MaskedValueDiagnostic.Applies(null, null, PhoneDecoration);

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
        var applies = MaskedValueDiagnostic.Applies("   ", string.Empty, PhoneDecoration);

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
    /// The rule reduces both sides to the characters that carry data — dropping punctuation and the
    /// mask's own decoration — and compares those. A reformat only rearranges decoration, so the two
    /// sides reduce to the same characters; a discard loses characters no reformatting can restore.
    /// <para>
    /// These rows all use <c>(000) 000-0000</c>, so they establish the behaviour table from the issue
    /// but <b>cannot</b> distinguish a decoration-only rule from a correct one — see the class remark.
    /// <see cref="Applies_Should_Not_Report_A_Value_Punctuated_Unlike_The_Mask"/> is what does that.
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
        var applies = MaskedValueDiagnostic.Applies(stored, rendered, PhoneDecoration);

        // Assert
        applies.ShouldBe(expected);
    }

    /// <summary>
    /// A value punctuated differently from the mask is a reformat, not a discard.
    /// </summary>
    /// <remarks>
    /// The regression test for the bug an earlier #283 draft shipped past a green suite. That draft
    /// stripped only the <b>mask's</b> literals from both sides, which leaves the stored value's own
    /// separators standing whenever they are not also the pattern's: <c>000-00-0000</c> over
    /// <c>"123 45 6789"</c> compared <c>"123 45 6789"</c> against <c>"123456789"</c> and reported a
    /// discard for an SSN with every digit intact. Legacy data is punctuated however whoever stored
    /// it felt like, and almost never the way the new mask is.
    /// <para>
    /// Every row pairs a pattern with a value punctuated some OTHER way, which is exactly what the
    /// <c>(000) 000-0000</c> cases above cannot do. The last row is the control: under the same mask
    /// as row 1, a value that really did lose characters still warns, so these are not passing
    /// because the rule went silent everywhere.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("000-00-0000", "123 45 6789", "123-45-6789", false)]                        // SSN, spaces stored
    [InlineData("000.000.0000", "555-123-4567", "555.123.4567", false)]                     // dots vs dashes
    [InlineData("0000 0000 0000 0000", "4111-1111-1111-1111", "4111 1111 1111 1111", false)] // card
    [InlineData("(000) 000-0000", "555.123.4567", "(555) 123-4567", false)]                 // dots stored
    [InlineData("0000000000", "555-123-4567", "5551234567", false)]                         // no literals at all
    [InlineData("000-00-0000", "N/A123456789", "123-45-6789", true)]                        // control: a real discard
    public void Applies_Should_Not_Report_A_Value_Punctuated_Unlike_The_Mask(
        string pattern,
        string stored,
        string rendered,
        bool expected)
    {
        // Arrange - decoration derived from the pattern rather than hand-written, so the test cannot
        // drift from what the production path actually computes.
        var decoration = MaskedValueDiagnostic.DecorationOf(new PatternMask(pattern));

        // Act
        var applies = MaskedValueDiagnostic.Applies(stored, rendered, decoration);

        // Assert
        applies.ShouldBe(expected);
    }

    [Fact]
    public void Applies_Should_Not_Report_A_Mask_Whose_Literal_Is_Itself_Alphanumeric()
    {
        // Arrange - the false positive in the OTHER direction, and the reason the rule is not simply
        // "strip non-alphanumerics". A pattern may spell a literal that is itself alphanumeric: the
        // "1" in "+1 000-0000" is decoration the mask contributes, so the rendered side holds a digit
        // the stored side never had. Stripping punctuation alone would compare "5551234" against
        // "15551234" and report a discard on a field that is working perfectly.
        var decoration = MaskedValueDiagnostic.DecorationOf(new PatternMask("+1 000-0000"));

        // Act
        var applies = MaskedValueDiagnostic.Applies("5551234", "+1 555-1234", decoration);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void Applies_Should_Not_Report_Placeholder_Padding()
    {
        // Arrange - a PatternMask with a Placeholder pads the positions a short value does not reach,
        // so the rendered text is LONGER than what was stored. Counting those pad characters as data
        // reports a discard for characters the mask added -- and would do so for every value shorter
        // than the pattern, on every render, which for a variable-length field is every value.
        var mask = new PatternMask("(000) 000-0000") { Placeholder = '_' };
        var decoration = MaskedValueDiagnostic.DecorationOf(mask);

        // Act
        var applies = MaskedValueDiagnostic.Applies("55512345", "(555) 123-45__", decoration);

        // Assert
        applies.ShouldBeFalse();
    }

    [Fact]
    public void DecorationOf_Should_Include_The_Placeholder()
    {
        // Arrange - the placeholder is decoration by the same definition the literals are: the mask
        // contributes it rather than taking it from the value.
        var mask = new PatternMask("(000) 000-0000") { Placeholder = '_' };

        // Act
        var decoration = MaskedValueDiagnostic.DecorationOf(mask);

        // Assert
        decoration.ShouldNotBeNull();
        decoration.ShouldContain('_');
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
        var reformat = MaskedValueDiagnostic.Applies("5551234567", "(555) 123-4567", PhoneDecoration);
        var discard = MaskedValueDiagnostic.Applies("N/A5551234567", "(555) 123-4567", PhoneDecoration);

        // Assert
        reformat.ShouldBeFalse();
        discard.ShouldBeTrue();
    }

    [Theory]
    [InlineData("N/A", "", true)]
    [InlineData("+1 555 123 4567", "(155) 512-3456", false)]
    public void Applies_Should_Fall_Back_To_Total_Collapse_When_The_Decoration_Is_Unknown(
        string stored,
        string rendered,
        bool expected)
    {
        // Arrange - null decoration means "no opinion", which DecorationOf returns for a mask whose
        // decoration cannot be read off a pattern (a RegexMask from the #265 factory) or whose
        // Transformation rewrites characters as it consumes them. Guessing there would report every
        // value of a correctly configured field. Falling back to #266's rule keeps the blank case
        // covered and reports nothing it cannot justify.

        // Act
        var applies = MaskedValueDiagnostic.Applies(stored, rendered, maskDecoration: null);

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
        var applies = MaskedValueDiagnostic.Applies("()-", string.Empty, PhoneDecoration);

        // Assert
        applies.ShouldBeTrue();
    }

    [Fact]
    public void DecorationOf_Should_Return_The_Patterns_NonPlaceholder_Characters()
    {
        // Act
        var literals = MaskedValueDiagnostic.DecorationOf(new PatternMask("(000) 000-0000"));

        // Assert - distinct, in first-appearance order; the digits' placeholder '0' is not decoration.
        literals.ShouldBe(PhoneDecoration);
    }

    [Fact]
    public void DecorationOf_Should_Be_Null_For_A_Mask_It_Cannot_Read()
    {
        // Arrange - a RegexMask reaches the diagnostic through the #265 factory. Its Mask is a
        // regular expression, so its non-placeholder characters are metacharacters rather than
        // decoration, and stripping them would be nonsense dressed up as a rule.

        // Act
        var literals = MaskedValueDiagnostic.DecorationOf(new RegexMask(@"^\d{0,10}$"));

        // Assert
        literals.ShouldBeNull();
    }

    [Fact]
    public void DecorationOf_Should_Be_Null_For_A_Transforming_Mask()
    {
        // Arrange - a Transformation rewrites characters as the mask consumes them, so the rendered
        // text legitimately differs from the stored value character for character and EVERY value
        // would read as a discard. Firing on every value of a correctly configured field is the
        // happy-path false positive the whole diagnostic is shaped to avoid.
        var mask = new PatternMask("aaa") { Transformation = char.ToUpperInvariant };

        // Act
        var literals = MaskedValueDiagnostic.DecorationOf(mask);

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
