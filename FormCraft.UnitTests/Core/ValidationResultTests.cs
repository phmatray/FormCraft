namespace FormCraft.UnitTests.Core;

public class ValidationResultTests
{
    [Fact]
    public void Success_Should_Create_Valid_Result()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Failure_Should_Create_Invalid_Result_With_Message()
    {
        // Arrange
        const string errorMessage = "This field is required";

        // Act
        var result = ValidationResult.Failure(errorMessage);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(errorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Error message")]
    public void Failure_Should_Accept_Any_String_Message(string errorMessage)
    {
        // Act
        var result = ValidationResult.Failure(errorMessage);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void Failure_With_Null_Message_Should_Create_Invalid_Result()
    {
        // Act
        var result = ValidationResult.Failure(null!);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBeNull();
    }
}