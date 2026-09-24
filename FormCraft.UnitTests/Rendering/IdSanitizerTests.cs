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

}
