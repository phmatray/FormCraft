namespace FormCraft.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDynamicForms_Should_Register_All_Required_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddDynamicForms();

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
    public void AddDynamicForms_Should_Allow_Service_Resolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
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
    public void AddDynamicForms_Should_Create_Scoped_Instances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
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
    public void AddDynamicForms_Should_Not_Interfere_With_Existing_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestService>();

        // Act
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var testService = serviceProvider.GetService<ITestService>();
        testService.ShouldNotBeNull();
        testService.ShouldBeOfType<TestService>();

        var fieldRendererService = serviceProvider.GetService<IFieldRendererService>();
        fieldRendererService.ShouldNotBeNull();
    }

    [Fact]
    public void AddDynamicForms_Should_Be_Idempotent()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDynamicForms();
        services.AddDynamicForms(); // Add again
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Should still work and have multiple registrations for IFieldRenderer
        var fieldRenderers = serviceProvider.GetServices<IFieldRenderer>().ToList();
        fieldRenderers.Count.ShouldBe(14); // 7 renderers x 2 registrations

        var fieldRendererService = serviceProvider.GetService<IFieldRendererService>();
        fieldRendererService.ShouldNotBeNull();
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