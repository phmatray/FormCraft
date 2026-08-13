using FormCraft.ForMudBlazor.Extensions;

namespace FormCraft.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFormCraft_Should_Register_All_Required_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFormCraft();

        // Assert
        result.ShouldBeSameAs(services);

        // Verify all expected services are registered
        services.ShouldContain(s => s.ServiceType == typeof(IFieldRendererService) && s.Lifetime == ServiceLifetime.Scoped);

        // Verify field renderers are registered as IFieldRenderer
        var fieldRendererRegistrations = services.Where(s => s.ServiceType == typeof(IFieldRenderer)).ToList();
        fieldRendererRegistrations.Count.ShouldBe(7);
        fieldRendererRegistrations.ShouldContain(s => s.ImplementationType == typeof(StringFieldRenderer));
        fieldRendererRegistrations.ShouldContain(s => s.ImplementationType == typeof(IntFieldRenderer));
        fieldRendererRegistrations.ShouldContain(s => s.ImplementationType == typeof(DecimalFieldRenderer));
        fieldRendererRegistrations.ShouldContain(s => s.ImplementationType == typeof(DoubleFieldRenderer));
        fieldRendererRegistrations.ShouldContain(s => s.ImplementationType == typeof(BoolFieldRenderer));
        fieldRendererRegistrations.ShouldContain(s => s.ImplementationType == typeof(DateTimeFieldRenderer));
        fieldRendererRegistrations.ShouldContain(s => s.ImplementationType == typeof(FileUploadFieldRenderer));

        // Verify all field renderers are scoped
        foreach (var registration in fieldRendererRegistrations)
        {
            registration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        }
    }

    [Fact]
    public void AddFormCraft_Should_Allow_Service_Resolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Should be able to resolve IFieldRendererService
        var fieldRendererService = serviceProvider.GetService<IFieldRendererService>();
        fieldRendererService.ShouldNotBeNull();
        fieldRendererService.ShouldBeOfType<FieldRendererService>();

        // Act & Assert - Should be able to resolve all field renderers
        var fieldRenderers = serviceProvider.GetServices<IFieldRenderer>().ToList();
        fieldRenderers.Count.ShouldBe(7);
        fieldRenderers.ShouldContain(r => r.GetType() == typeof(StringFieldRenderer));
        fieldRenderers.ShouldContain(r => r.GetType() == typeof(IntFieldRenderer));
        fieldRenderers.ShouldContain(r => r.GetType() == typeof(DecimalFieldRenderer));
        fieldRenderers.ShouldContain(r => r.GetType() == typeof(DoubleFieldRenderer));
        fieldRenderers.ShouldContain(r => r.GetType() == typeof(BoolFieldRenderer));
        fieldRenderers.ShouldContain(r => r.GetType() == typeof(DateTimeFieldRenderer));
        fieldRenderers.ShouldContain(r => r.GetType() == typeof(FileUploadFieldRenderer));
    }

    [Fact]
    public void AddFormCraft_Should_Create_Scoped_Instances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var service1_1 = scope1.ServiceProvider.GetRequiredService<IFieldRendererService>();
        var service1_2 = scope1.ServiceProvider.GetRequiredService<IFieldRendererService>();
        var service2_1 = scope2.ServiceProvider.GetRequiredService<IFieldRendererService>();

        // Assert
        // Same instance within the same scope
        service1_1.ShouldBeSameAs(service1_2);

        // Different instances across different scopes
        service1_1.ShouldNotBeSameAs(service2_1);
    }

    [Fact]
    public void AddFormCraft_Should_Not_Interfere_With_Existing_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestService>();

        // Act
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var testService = serviceProvider.GetService<ITestService>();
        testService.ShouldNotBeNull();
        testService.ShouldBeOfType<TestService>();

        var fieldRendererService = serviceProvider.GetService<IFieldRendererService>();
        fieldRendererService.ShouldNotBeNull();
    }

    [Fact]
    public void AddFormCraft_Should_Be_Idempotent()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFormCraft();
        services.AddFormCraft(); // Add again
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Should still work and have multiple registrations for IFieldRenderer
        var fieldRenderers = serviceProvider.GetServices<IFieldRenderer>().ToList();
        fieldRenderers.Count.ShouldBe(14); // 7 renderers x 2 registrations

        var fieldRendererService = serviceProvider.GetService<IFieldRendererService>();
        fieldRendererService.ShouldNotBeNull();
    }

    [Fact]
    public void AddFormCraft_Alone_Should_Register_The_Built_In_Renderers()
    {
        // Arrange & Act - with no adapter in the container, core's own renderers are what render
        // the form, so they must be registered.
        var services = new ServiceCollection();
        services.AddFormCraft();

        // Assert
        CoreRendererCount(services).ShouldBe(7);
    }

    [Fact]
    public void AddFormCraft_Then_An_Adapter_Should_Leave_No_Core_Renderers()
    {
        // Arrange & Act - the ordinary order. The adapter strips core's defaults so its own take
        // precedence; renderer selection is first-match-wins and core's would otherwise win.
        var services = new ServiceCollection();
        services.AddFormCraft();
        services.AddFormCraftMudBlazor();

        // Assert
        CoreRendererCount(services).ShouldBe(0);
    }

    [Fact]
    public void An_Adapter_Then_AddFormCraft_Should_Not_Register_The_Core_Renderers()
    {
        // Arrange & Act - the order the guard in AddFormCraft() exists for. #279 replaced the
        // IUIFrameworkAdapter-presence test that used to answer this question with an explicit
        // adapter marker; the interface is gone, this behaviour must not be.
        var services = new ServiceCollection();
        services.AddFormCraftMudBlazor();
        services.AddFormCraft();

        // Assert - core's renderers are never added, rather than added and removed.
        CoreRendererCount(services).ShouldBe(0);
    }

    [Fact]
    public void AddFormCraft_Without_An_Adapter_Should_Resolve_Its_Own_Renderers()
    {
        // The registration counts above are structural; this one proves the container actually
        // builds, so a marker change cannot pass by leaving descriptors that fail to resolve.
        var services = new ServiceCollection();
        services.AddFormCraft();

        using var provider = services.BuildServiceProvider();

        provider.GetServices<IFieldRenderer>().ShouldNotBeEmpty();
    }

    private static int CoreRendererCount(IServiceCollection services)
    {
        var coreAssembly = typeof(IFieldRenderer).Assembly;
        return services.Count(s =>
            s.ServiceType == typeof(IFieldRenderer) &&
            s.ImplementationType?.Assembly == coreAssembly);
    }

    // Test interface and implementation for additional service testing
    public interface ITestService
    {
        string GetMessage();
    }

    public class TestService : ITestService
    {
        public string GetMessage() => "Test message";
    }
}