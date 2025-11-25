namespace FormCraft.ForMudBlazor.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFormCraft_Should_Register_Required_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IFieldRendererService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddFormCraft_Should_Register_Field_Renderers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        // Assert
        rendererService.ShouldNotBeNull();
    }

    [Fact]
    public void AddFormCraft_Should_Be_Chainable()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFormCraft();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddFormCraft_Can_Be_Called_Multiple_Times()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - Should not throw
        services.AddFormCraft();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<IFieldRendererService>().ShouldNotBeNull();
    }
}
