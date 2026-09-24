namespace FormCraft.UnitTests.Rendering;

public class IdSanitizerTests
{
    [Fact]
    public void ToCssSafeId_Should_Return_Input_Unchanged_When_No_Dot_Present()
    {
        // Act & Assert
        IdSanitizer.ToCssSafeId("Amount").ShouldBe("Amount");
    }

    [Fact]
    public void ToCssSafeId_Should_Replace_A_Single_Dot()
    {
        // Act & Assert
        IdSanitizer.ToCssSafeId("Billing.Amount").ShouldBe("Billing-Amount");
    }

    [Fact]
    public void ToCssSafeId_Should_Replace_Every_Dot_When_Multiple_Are_Present()
    {
        // Act & Assert
        IdSanitizer.ToCssSafeId("A.B.C").ShouldBe("A-B-C");
    }

    [Fact]
    public void ToCssSafeId_Should_Never_Collide_A_Literal_Dash_With_A_Sanitized_Dot()
    {
        // The type's own XML doc claims injectivity: "a.b" and a hypothetical literal "a-b" never
        // collide because '-' cannot appear in a C# identifier, so a field genuinely named with a
        // dash is not a real input this sanitizer needs to disambiguate against.
        IdSanitizer.ToCssSafeId("a.b").ShouldBe("a-b");
        IdSanitizer.ToCssSafeId("a.b").ShouldNotBe("a.b");
    }
}
