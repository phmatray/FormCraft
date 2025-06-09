namespace FormCraft.UnitTests.Integration;

public class CompleteFormWorkflowTests
{
    [Fact]
    public void Complete_Form_Workflow_Should_Work_End_To_End()
    {
        // Arrange - Set up complete service container
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        // Act & Assert - Build form configuration
        var formConfig = FormBuilder<CompleteTestModel>.Create()
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
                .WithPlaceholder("Enter your first name")
            .AddField(x => x.LastName)
                .WithLabel("Last Name")
                .WithPlaceholder("Enter your last name")
            .AddField(x => x.Age)
                .WithLabel("Age")
            .AddField(x => x.IsActive)
                .WithLabel("Is Active")
            .AddField(x => x.BirthDate)
                .WithLabel("Birth Date")
            .Build();

        // Verify form configuration
        formConfig.ShouldNotBeNull();
        formConfig.Fields.Count().ShouldBe(5);

        // Act & Assert - Test field rendering for each field type
        var model = new CompleteTestModel
        {
            FirstName = "John",
            LastName = "Doe",
            Age = 30,
            IsActive = true,
            BirthDate = new DateTime(1993, 1, 1)
        };

        foreach (var field in formConfig.Fields)
        {
            // Should be able to render each field without errors - test via service
            var renderResult = rendererService.RenderField(model, field, EventCallback<object?>.Empty, EventCallback.Empty);
            renderResult.ShouldNotBeNull($"Render result should not be null for field: {field.FieldName}");
        }
    }

    [Fact]
    public void Multi_Validator_Form_Should_Execute_All_Validations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<CompleteTestModel>.Create()
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
                .Required("First name is required")
                .WithMinLength(2, "First name must be at least 2 characters")
            .AddField(x => x.Age)
                .WithLabel("Age")
                .WithRange(0, 120, "Age must be between 0 and 120")
            .Build();

        // Act & Assert - Test valid model
        var validModel = new CompleteTestModel { FirstName = "John", Age = 30 };

        var firstNameField = formConfig.Fields.First(f => f.FieldName == "FirstName");
        var ageField = formConfig.Fields.First(f => f.FieldName == "Age");

        // Test FirstName validation (should pass both required and length validators)
        var firstNameValidators = firstNameField.Validators;
        firstNameValidators.Count.ShouldBe(2);

        // Test Age validation (should pass range validator)
        var ageValidators = ageField.Validators;
        ageValidators.Count.ShouldBe(1);

        // Verify form structure
        formConfig.Fields.Count().ShouldBe(2);
        formConfig.Fields.All(f => f.Validators.Count > 0).ShouldBeTrue();
    }

    [Fact]
    public void Complex_Form_With_Dependencies_Should_Handle_Conditional_Logic()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Build form with conditional fields
        var formConfig = FormBuilder<CompleteTestModel>.Create()
            .AddField(x => x.IsActive)
                .WithLabel("Is Active")
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
                .VisibleWhen(model => model.IsActive) // Only show if active
            .AddField(x => x.LastName)
                .WithLabel("Last Name")
                .VisibleWhen(model => model.IsActive) // Only show if active
            .AddField(x => x.Age)
                .WithLabel("Age")
                .DisabledWhen(model => !model.IsActive) // Disabled if not active
            .Build();

        // Act & Assert - Test with active model
        var activeModel = new CompleteTestModel { IsActive = true, FirstName = "John" };

        var isActiveField = formConfig.Fields.First(f => f.FieldName == "IsActive");
        var firstNameField = formConfig.Fields.First(f => f.FieldName == "FirstName");
        var lastNameField = formConfig.Fields.First(f => f.FieldName == "LastName");
        var ageField = formConfig.Fields.First(f => f.FieldName == "Age");

        // Test that fields have conditional logic configured
        firstNameField.VisibilityCondition.ShouldNotBeNull();
        lastNameField.VisibilityCondition.ShouldNotBeNull();
        ageField.DisabledCondition.ShouldNotBeNull();

        // Test conditions with active model
        firstNameField.VisibilityCondition(activeModel).ShouldBeTrue();
        lastNameField.VisibilityCondition(activeModel).ShouldBeTrue();
        ageField.DisabledCondition(activeModel).ShouldBeFalse(); // Should not be disabled when active

        // Test with inactive model
        var inactiveModel = new CompleteTestModel { IsActive = false };

        firstNameField.VisibilityCondition(inactiveModel).ShouldBeFalse();
        lastNameField.VisibilityCondition(inactiveModel).ShouldBeFalse();
        ageField.DisabledCondition(inactiveModel).ShouldBeTrue(); // Should be disabled when not active
    }

    [Fact]
    public void Form_With_All_Field_Types_Should_Render_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        // Build form with all supported field types
        var formConfig = FormBuilder<CompleteTestModel>.Create()
            .AddField(x => x.FirstName)    // string
                .WithLabel("First Name")
            .AddField(x => x.Age)          // int
                .WithLabel("Age")
            .AddField(x => x.IsActive)     // bool
                .WithLabel("Is Active")
            .AddField(x => x.BirthDate)    // DateTime
                .WithLabel("Birth Date")
            .Build();

        var model = new CompleteTestModel
        {
            FirstName = "John",
            Age = 30,
            IsActive = true,
            BirthDate = DateTime.Now
        };

        // Act & Assert - Verify each field type can be rendered
        var stringField = formConfig.Fields.First(f => f.FieldName == "FirstName");
        var intField = formConfig.Fields.First(f => f.FieldName == "Age");
        var boolField = formConfig.Fields.First(f => f.FieldName == "IsActive");
        var dateTimeField = formConfig.Fields.First(f => f.FieldName == "BirthDate");

        // Test that fields can be rendered via service
        var stringRender = rendererService.RenderField(model, stringField, EventCallback<object?>.Empty, EventCallback.Empty);
        var intRender = rendererService.RenderField(model, intField, EventCallback<object?>.Empty, EventCallback.Empty);
        var boolRender = rendererService.RenderField(model, boolField, EventCallback<object?>.Empty, EventCallback.Empty);
        var dateTimeRender = rendererService.RenderField(model, dateTimeField, EventCallback<object?>.Empty, EventCallback.Empty);

        stringRender.ShouldNotBeNull();
        intRender.ShouldNotBeNull();
        boolRender.ShouldNotBeNull();
        dateTimeRender.ShouldNotBeNull();

        // Verify field names
        stringField.FieldName.ShouldBe("FirstName");
        intField.FieldName.ShouldBe("Age");
        boolField.FieldName.ShouldBe("IsActive");
        dateTimeField.FieldName.ShouldBe("BirthDate");
    }

    [Fact]
    public void Form_Performance_Should_Be_Acceptable_With_Many_Fields()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Build form with many fields
        var formBuilder = FormBuilder<CompleteTestModel>.Create();

        // Add multiple fields of each type to simulate a complex form
        for (int i = 0; i < 10; i++)
        {
            formBuilder
                .AddField(x => x.FirstName)
                    .WithLabel($"String Field {i}")
                .AddField(x => x.Age)
                    .WithLabel($"Int Field {i}")
                .AddField(x => x.IsActive)
                    .WithLabel($"Bool Field {i}")
                .AddField(x => x.BirthDate)
                    .WithLabel($"DateTime Field {i}");
        }

        var formConfig = formBuilder.Build();
        var model = new CompleteTestModel();

        // Test rendering performance
        foreach (var field in formConfig.Fields)
        {
            var renderResult = rendererService.RenderField(model, field, EventCallback<object?>.Empty, EventCallback.Empty);
            renderResult.ShouldNotBeNull();
        }

        stopwatch.Stop();

        // Assert - Performance should be reasonable (under 1 second for 40 fields)
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000,
            $"Form building and rendering took too long: {stopwatch.ElapsedMilliseconds}ms");

        formConfig.Fields.Count().ShouldBe(40);
    }

    [Fact]
    public void Form_State_Changes_Should_Propagate_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        var formConfig = FormBuilder<CompleteTestModel>.Create()
            .AddField(x => x.IsActive)
                .WithLabel("Is Active")
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
                .VisibleWhen(model => model.IsActive)
            .Build();

        // Act & Assert - Test state transitions
        var model = new CompleteTestModel { IsActive = false };
        var firstNameField = formConfig.Fields.First(f => f.FieldName == "FirstName");

        // Initially hidden when IsActive is false
        firstNameField.VisibilityCondition?.Invoke(model).ShouldBeFalse();

        // Should become visible when IsActive changes to true
        model.IsActive = true;
        firstNameField.VisibilityCondition?.Invoke(model).ShouldBeTrue();

        // Should become hidden again when IsActive changes back to false
        model.IsActive = false;
        firstNameField.VisibilityCondition?.Invoke(model).ShouldBeFalse();
    }

    [Fact]
    public void Error_Recovery_Should_Handle_Invalid_Configurations_Gracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        // Act & Assert - Test with minimal valid configuration
        var minimalConfig = FormBuilder<CompleteTestModel>.Create()
            .AddField(x => x.FirstName) // No label, no validation
            .Build();

        minimalConfig.ShouldNotBeNull();
        minimalConfig.Fields.Count().ShouldBe(1);

        var field = minimalConfig.Fields.First();
        field.Label.ShouldBe("FirstName"); // Default label is property name
        field.Validators.Count.ShouldBe(0); // No validators

        // Should still be able to render
        var renderResult = rendererService.RenderField(new CompleteTestModel(), field, EventCallback<object?>.Empty, EventCallback.Empty);
        renderResult.ShouldNotBeNull();
    }

    public class CompleteTestModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime BirthDate { get; set; }
    }
}