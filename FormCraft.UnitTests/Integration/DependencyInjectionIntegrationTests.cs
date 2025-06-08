namespace FormCraft.UnitTests.Integration;

public class DependencyInjectionIntegrationTests
{
    [Fact]
    public void Full_DI_Container_Integration_Should_Work_End_To_End()
    {
        // Arrange - Create service collection and register FormCraft services
        var services = new ServiceCollection();
        services.AddDynamicForms();
        
        // Add some additional services to test non-interference
        services.AddSingleton<ITestDependency, TestDependency>();
        services.AddScoped<ITestScopedService, TestScopedService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Test complete service resolution chain
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Should be able to resolve IFieldRendererService
        var rendererService = scopedProvider.GetRequiredService<IFieldRendererService>();
        rendererService.ShouldNotBeNull();
        rendererService.ShouldBeOfType<FieldRendererService>();

        // Should be able to resolve all individual renderers
        var allRenderers = scopedProvider.GetServices<IFieldRenderer>().ToList();
        allRenderers.Count.ShouldBe(6); // String, Int, Decimal, Double, Bool, DateTime

        // Should be able to resolve specific renderer types
        var stringRenderer = allRenderers.OfType<StringFieldRenderer>().FirstOrDefault();
        var intRenderer = allRenderers.OfType<IntFieldRenderer>().FirstOrDefault();
        var decimalRenderer = allRenderers.OfType<DecimalFieldRenderer>().FirstOrDefault();
        var doubleRenderer = allRenderers.OfType<DoubleFieldRenderer>().FirstOrDefault();
        var boolRenderer = allRenderers.OfType<BoolFieldRenderer>().FirstOrDefault();
        var dateTimeRenderer = allRenderers.OfType<DateTimeFieldRenderer>().FirstOrDefault();

        stringRenderer.ShouldNotBeNull();
        intRenderer.ShouldNotBeNull();
        decimalRenderer.ShouldNotBeNull();
        doubleRenderer.ShouldNotBeNull();
        boolRenderer.ShouldNotBeNull();
        dateTimeRenderer.ShouldNotBeNull();

        // Additional services should still work
        var testDependency = scopedProvider.GetRequiredService<ITestDependency>();
        testDependency.ShouldNotBeNull();
        testDependency.GetValue().ShouldBe("Test Value");

        var testScopedService = scopedProvider.GetRequiredService<ITestScopedService>();
        testScopedService.ShouldNotBeNull();
    }

    [Fact]
    public void Service_Lifetime_Verification_Should_Behave_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Test scoped lifetime behavior
        IFieldRendererService service1, service2, service3;
        
        using (var scope1 = serviceProvider.CreateScope())
        {
            service1 = scope1.ServiceProvider.GetRequiredService<IFieldRendererService>();
            service2 = scope1.ServiceProvider.GetRequiredService<IFieldRendererService>();
            
            // Same instance within same scope
            service1.ShouldBeSameAs(service2);
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            service3 = scope2.ServiceProvider.GetRequiredService<IFieldRendererService>();
            
            // Different instance in different scope
            service1.ShouldNotBeSameAs(service3);
        }

        // Test field renderer lifetime (should be scoped)
        IFieldRenderer renderer1, renderer2, renderer3;
        
        using (var scope1 = serviceProvider.CreateScope())
        {
            var renderers = scope1.ServiceProvider.GetServices<IFieldRenderer>().ToList();
            renderer1 = renderers.First(r => r is StringFieldRenderer);
            
            var renderersAgain = scope1.ServiceProvider.GetServices<IFieldRenderer>().ToList();
            renderer2 = renderersAgain.First(r => r is StringFieldRenderer);
            
            // Same instance within same scope
            renderer1.ShouldBeSameAs(renderer2);
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            var renderers = scope2.ServiceProvider.GetServices<IFieldRenderer>().ToList();
            renderer3 = renderers.First(r => r is StringFieldRenderer);
            
            // Different instance in different scope
            renderer1.ShouldNotBeSameAs(renderer3);
        }
    }

    [Fact]
    public void Custom_Renderer_Registration_Should_Work_With_Built_In_Renderers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        
        // Add a custom renderer
        services.AddScoped<IFieldRenderer, CustomTestRenderer>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var allRenderers = serviceProvider.GetServices<IFieldRenderer>().ToList();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        // Assert
        allRenderers.Count.ShouldBe(7); // 6 built-in + 1 custom
        allRenderers.ShouldContain(r => r.GetType() == typeof(CustomTestRenderer));
        allRenderers.ShouldContain(r => r.GetType() == typeof(StringFieldRenderer));
        allRenderers.ShouldContain(r => r.GetType() == typeof(IntFieldRenderer));
        allRenderers.ShouldContain(r => r.GetType() == typeof(DecimalFieldRenderer));
        allRenderers.ShouldContain(r => r.GetType() == typeof(DoubleFieldRenderer));
        allRenderers.ShouldContain(r => r.GetType() == typeof(BoolFieldRenderer));
        allRenderers.ShouldContain(r => r.GetType() == typeof(DateTimeFieldRenderer));

        // Custom renderer should be available to the service
        var customRenderer = allRenderers.OfType<CustomTestRenderer>().First();
        customRenderer.ShouldNotBeNull();
    }

    [Fact]
    public void Service_Provider_Access_In_Validators_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        services.AddSingleton<IValidationService, ValidationService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Create a validator that uses service provider
        var validator = new ServiceDependentValidator(serviceProvider);

        // Act & Assert
        var model = new TestModel { Name = "test" };
        var validationTask = validator.ValidateAsync(model, "test value", serviceProvider);
        
        validationTask.ShouldNotBeNull();
        // The validator should be able to access the validation service
        var result = validationTask.Result;
        result.ShouldNotBeNull();
        result.IsValid.ShouldBeTrue(); // Our test validation service always returns true
    }

    [Fact]
    public void Multiple_Service_Provider_Scopes_Should_Not_Interfere()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        services.AddScoped<ITestScopedService, TestScopedService>();
        
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Create multiple scopes simultaneously
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();
        using var scope3 = serviceProvider.CreateScope();

        var rendererService1 = scope1.ServiceProvider.GetRequiredService<IFieldRendererService>();
        var rendererService2 = scope2.ServiceProvider.GetRequiredService<IFieldRendererService>();
        var rendererService3 = scope3.ServiceProvider.GetRequiredService<IFieldRendererService>();

        var scopedService1 = scope1.ServiceProvider.GetRequiredService<ITestScopedService>();
        var scopedService2 = scope2.ServiceProvider.GetRequiredService<ITestScopedService>();
        var scopedService3 = scope3.ServiceProvider.GetRequiredService<ITestScopedService>();

        // All services should be distinct instances
        rendererService1.ShouldNotBeSameAs(rendererService2);
        rendererService1.ShouldNotBeSameAs(rendererService3);
        rendererService2.ShouldNotBeSameAs(rendererService3);

        scopedService1.ShouldNotBeSameAs(scopedService2);
        scopedService1.ShouldNotBeSameAs(scopedService3);
        scopedService2.ShouldNotBeSameAs(scopedService3);

        // All services should be functional
        rendererService1.ShouldNotBeNull();
        rendererService2.ShouldNotBeNull();
        rendererService3.ShouldNotBeNull();
        
        scopedService1.ShouldNotBeNull();
        scopedService2.ShouldNotBeNull();
        scopedService3.ShouldNotBeNull();
    }

    [Fact]
    public void Service_Registration_Order_Should_Not_Matter()
    {
        // Arrange - Register services in different orders
        var services1 = new ServiceCollection();
        services1.AddSingleton<ITestDependency, TestDependency>();
        services1.AddDynamicForms();
        services1.AddScoped<ITestScopedService, TestScopedService>();

        var services2 = new ServiceCollection();
        services2.AddDynamicForms();
        services2.AddSingleton<ITestDependency, TestDependency>();
        services2.AddScoped<ITestScopedService, TestScopedService>();

        var services3 = new ServiceCollection();
        services3.AddScoped<ITestScopedService, TestScopedService>();
        services3.AddSingleton<ITestDependency, TestDependency>();
        services3.AddDynamicForms();

        // Act & Assert
        var serviceProvider1 = services1.BuildServiceProvider();
        var serviceProvider2 = services2.BuildServiceProvider();
        var serviceProvider3 = services3.BuildServiceProvider();

        // All should resolve services correctly regardless of registration order
        var rendererService1 = serviceProvider1.GetRequiredService<IFieldRendererService>();
        var rendererService2 = serviceProvider2.GetRequiredService<IFieldRendererService>();
        var rendererService3 = serviceProvider3.GetRequiredService<IFieldRendererService>();

        rendererService1.ShouldNotBeNull();
        rendererService2.ShouldNotBeNull();
        rendererService3.ShouldNotBeNull();

        var testDep1 = serviceProvider1.GetRequiredService<ITestDependency>();
        var testDep2 = serviceProvider2.GetRequiredService<ITestDependency>();
        var testDep3 = serviceProvider3.GetRequiredService<ITestDependency>();

        testDep1.GetValue().ShouldBe("Test Value");
        testDep2.GetValue().ShouldBe("Test Value");
        testDep3.GetValue().ShouldBe("Test Value");
    }

    [Fact]
    public void Service_Disposal_Should_Work_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDynamicForms();
        var serviceProvider = services.BuildServiceProvider();

        IFieldRendererService? rendererService = null;
        IEnumerable<IFieldRenderer>? renderers = null;

        // Act - Create and dispose scope
        using (var scope = serviceProvider.CreateScope())
        {
            rendererService = scope.ServiceProvider.GetRequiredService<IFieldRendererService>();
            renderers = scope.ServiceProvider.GetServices<IFieldRenderer>();
            
            rendererService.ShouldNotBeNull();
            renderers.ShouldNotBeNull();
            renderers.Count().ShouldBe(6); // String, Int, Decimal, Double, Bool, DateTime
        }

        // Assert - After scope disposal, we should be able to create new instances
        using (var newScope = serviceProvider.CreateScope())
        {
            var newRendererService = newScope.ServiceProvider.GetRequiredService<IFieldRendererService>();
            var newRenderers = newScope.ServiceProvider.GetServices<IFieldRenderer>();
            
            newRendererService.ShouldNotBeNull();
            newRenderers.ShouldNotBeNull();
            newRenderers.Count().ShouldBe(6); // String, Int, Decimal, Double, Bool, DateTime
            
            // Should be different instances
            newRendererService.ShouldNotBeSameAs(rendererService);
        }

        // Dispose the service provider
        serviceProvider.Dispose();
    }

    // Test interfaces and implementations
    public interface ITestDependency
    {
        string GetValue();
    }

    public class TestDependency : ITestDependency
    {
        public string GetValue() => "Test Value";
    }

    public interface ITestScopedService
    {
        Guid Id { get; }
    }

    public class TestScopedService : ITestScopedService
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public interface IValidationService
    {
        bool IsValid(string value);
    }

    public class ValidationService : IValidationService
    {
        public bool IsValid(string value) => true; // Always valid for testing
    }

    public class CustomTestRenderer : IFieldRenderer
    {
        public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field) => fieldType == typeof(string);

        public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
        {
            return builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, "Custom field renderer for: " + context.Field.FieldName);
                builder.CloseElement();
            };
        }
    }

    public class ServiceDependentValidator : IFieldValidator<TestModel, string>
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceDependentValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string? ErrorMessage { get; set; }

        public async Task<ValidationResult> ValidateAsync(TestModel model, string? value, IServiceProvider services)
        {
            // Use the validation service from DI
            var validationService = services.GetRequiredService<IValidationService>();
            var isValid = validationService.IsValid(value ?? "");
            
            return await Task.FromResult(isValid 
                ? ValidationResult.Success() 
                : ValidationResult.Failure("Validation failed"));
        }
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
    }
}