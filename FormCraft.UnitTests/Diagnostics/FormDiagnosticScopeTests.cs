using FormCraft.Diagnostics;

namespace FormCraft.UnitTests.Diagnostics;

/// <summary>
/// Direct tests for <see cref="FormDiagnosticScope"/> (moved into core from
/// <c>FormCraft.ForMudBlazor.FormDiagnosticScope</c> under #398). The MudBlazor and Fluent adapters'
/// own suites (<c>DiagnosticLatchTests</c>, <c>CustomTemplateTests</c>) cover this latch rendered
/// through a real form; this suite pins the latch itself.
/// </summary>
public class FormDiagnosticScopeTests
{
    [Fact]
    public void ShouldWarnOnce_Should_Return_True_The_First_Time_A_Pair_Is_Presented()
    {
        // Arrange
        var scope = new FormDiagnosticScope();

        // Act
        var result = scope.ShouldWarnOnce("Category", "Field");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldWarnOnce_Should_Return_False_The_Second_Time_The_Same_Pair_Is_Presented()
    {
        // Arrange
        var scope = new FormDiagnosticScope();
        scope.ShouldWarnOnce("Category", "Field");

        // Act
        var result = scope.ShouldWarnOnce("Category", "Field");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldWarnOnce_Should_Return_True_Again_For_A_Different_Key_Under_The_Same_Category()
    {
        // Arrange - the guard against over-latching: a key too coarse would silence a second field
        // entirely once the first has reported.
        var scope = new FormDiagnosticScope();
        scope.ShouldWarnOnce("Category", "FieldA");

        // Act
        var result = scope.ShouldWarnOnce("Category", "FieldB");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void ShouldWarnOnce_Should_Return_True_Again_For_The_Same_Key_Under_A_Different_Category()
    {
        // Arrange - the category is part of the key, not decoration: one field can legitimately
        // trip several diagnostics, and latching them together would report only the first (#274).
        var scope = new FormDiagnosticScope();
        scope.ShouldWarnOnce("CategoryA", "Field");

        // Act
        var result = scope.ShouldWarnOnce("CategoryB", "Field");

        // Assert
        result.ShouldBeTrue();
    }
}
