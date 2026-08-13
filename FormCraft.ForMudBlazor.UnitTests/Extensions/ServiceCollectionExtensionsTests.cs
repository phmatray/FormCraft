using FormCraft.ForFluentUI.Extensions;
using FormCraft.ForMudBlazor.Extensions;

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

    [Fact]
    public void AddFormCraftMudBlazor_Should_Throw_When_FluentUI_Is_Already_Registered()
    {
        // Arrange - the order that silently succeeded until #279. The guard lived only in
        // AddFormCraftFluentUI(), so it caught Mud-then-Fluent and missed Fluent-then-Mud, which
        // produced a container rendering some fields Material and some Fluent with nothing to point
        // at. A rule that lives in one of the two packages needing it can only ever be one-directional.
        var services = new ServiceCollection();
        services.AddFormCraft();
        services.AddFormCraftFluentUI();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => services.AddFormCraftMudBlazor());

        ex.Message.ShouldContain("mutually exclusive");
        // Both adapters named, so the message is actionable from either direction.
        ex.Message.ShouldContain("FormCraft.ForFluentUI");
        ex.Message.ShouldContain("FormCraft.ForMudBlazor");
    }

    [Fact]
    public void AddFormCraftMudBlazor_Can_Be_Called_Twice_Without_Tripping_The_Adapter_Guard()
    {
        // Arrange - the guard must exclude the registering assembly, or re-registering the SAME
        // adapter reads as a conflict with itself.
        var services = new ServiceCollection();
        services.AddFormCraft();

        // Act & Assert - Should not throw
        services.AddFormCraftMudBlazor();
        services.AddFormCraftMudBlazor();

        services.BuildServiceProvider().GetService<IFieldRendererService>().ShouldNotBeNull();
    }
}
