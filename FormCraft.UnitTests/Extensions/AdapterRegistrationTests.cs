namespace FormCraft.UnitTests.Extensions;

public class AdapterRegistrationTests
{
    [Fact]
    public void IsAdapterRegistered_Should_Return_False_On_A_Fresh_ServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        AdapterRegistration.IsAdapterRegistered(services).ShouldBeFalse();
    }

    [Fact]
    public void IsAdapterRegistered_Should_Return_True_After_EnsureSingleAdapter_Runs()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        AdapterRegistration.EnsureSingleAdapter(services, "SomeAdapter");

        // Assert
        AdapterRegistration.IsAdapterRegistered(services).ShouldBeTrue();
    }

    [Fact]
    public void EnsureSingleAdapter_Should_Be_Idempotent_For_The_Same_Adapter()
    {
        // Arrange
        var services = new ServiceCollection();
        AdapterRegistration.EnsureSingleAdapter(services, "SomeAdapter");

        // Act & Assert - registering the same adapter twice must not throw
        Should.NotThrow(() => AdapterRegistration.EnsureSingleAdapter(services, "SomeAdapter"));
        AdapterRegistration.IsAdapterRegistered(services).ShouldBeTrue();
    }
}
