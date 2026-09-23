namespace FormCraft.ForMudBlazor.UnitTests.Fields;

/// <summary>
/// Tests <see cref="TextMaskMap.Resolve"/>'s factory branch (#317): a factory-produced <see
/// cref="IMask"/> whose <c>Mask</c> string is computed lazily — <c>BlockMask</c> being the headline
/// case — must be bound as the factory produced it. The blank-pattern rejection stays scoped to
/// <see cref="PatternMask"/>, whose pattern is eager and whose empty pattern genuinely means "no
/// mask"; it is not a general property of every <see cref="IMask"/>.
/// </summary>
public class TextMaskMapTests : MudBlazorTestBase
{
    [Fact]
    public void A_Fresh_BlockMask_Should_Report_A_Null_Pattern_Before_First_Use()
    {
        // Arrange & Act & Assert - states the mechanism the bug relies on, not just the symptom:
        // BlockMask computes its Mask string lazily, on first SetText, not at construction. Resolve
        // inspects Mask at construction time - the one moment it is guaranteed to be empty - which
        // is why the old, un-narrowed blank guard rejected a perfectly functional mask.
        var mask = new BlockMask(new Block('0', 1, 4));

        mask.Mask.ShouldBeNull();
    }

    [Fact]
    public void Resolve_Should_Bind_A_Factory_Produced_BlockMask_Despite_Its_Lazy_Pattern()
    {
        // Act
        var resolved = TextMaskMap.Resolve(null, false, () => new BlockMask(new Block('0', 1, 4)));

        // Assert
        resolved.ShouldBeOfType<BlockMask>();
    }

    [Fact]
    public void Resolve_Should_Still_Bind_A_Factory_Produced_RegexMask()
    {
        // Act - RegexMask's pattern is eager, so this passed before the fix too; pinned as a
        // regression guard alongside the cases the fix actually changes.
        var resolved = TextMaskMap.Resolve(null, false, () => new RegexMask(@"^\d{0,4}$"));

        // Assert
        var regexMask = resolved.ShouldBeOfType<RegexMask>();
        regexMask.Mask.ShouldBe(@"^\d{0,4}$");
    }

    [Fact]
    public void Resolve_Should_Still_Bind_A_Factory_Produced_PatternMask_With_A_Pattern()
    {
        // Act
        var resolved = TextMaskMap.Resolve(null, false, () => new PatternMask("(000)"));

        // Assert
        var patternMask = resolved.ShouldBeOfType<PatternMask>();
        patternMask.Mask.ShouldBe("(000)");
    }

    [Fact]
    public void Resolve_Should_Still_Refuse_A_Factory_Produced_PatternMask_With_A_Blank_Pattern()
    {
        // Act - the case the blank guard exists for: a PatternMask("") must still route the field
        // through the unmasked path rather than into MudMask, which would drop MaxLines with it.
        var resolved = TextMaskMap.Resolve(null, false, () => new PatternMask(""));

        // Assert
        resolved.ShouldBeNull();
    }

    [Fact]
    public void Resolve_Should_Still_Refuse_A_Null_Factory_Result()
    {
        // Act
        var resolved = TextMaskMap.Resolve(null, false, () => null!);

        // Assert
        resolved.ShouldBeNull();
    }

    [Fact]
    public void A_Field_Configured_With_A_Factory_Produced_BlockMask_Should_Render_Masked()
    {
        // Arrange - proves the fix reaches the component, not just the helper: before it, this field
        // rendered completely unmasked because Resolve silently discarded the BlockMask.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Serial")
                .WithMask(() => new BlockMask(new Block('0', 1, 4))))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Mask.ShouldBeOfType<BlockMask>();
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
