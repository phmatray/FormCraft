namespace FormCraft.UnitTests.Extensions;

public class FormConfigurationExtensionsTests
{
    [Fact]
    public void GetRequiredFields_Should_Return_Only_Required_Field_Names()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
                .Required()
            .AddField(x => x.LastName)
                .WithLabel("Last Name")
                .Required()
            .AddField(x => x.Email)
                .WithLabel("Email")
                // Not required
            .AddField(x => x.Phone)
                .WithLabel("Phone")
                .Required()
            .Build();

        // Act
        var requiredFields = config.GetRequiredFields().ToList();

        // Assert
        requiredFields.Count.ShouldBe(3);
        requiredFields.ShouldContain("FirstName");
        requiredFields.ShouldContain("LastName");
        requiredFields.ShouldContain("Phone");
        requiredFields.ShouldNotContain("Email");
    }

    [Fact]
    public void GetRequiredFields_Should_Return_Empty_When_No_Required_Fields()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
            .AddField(x => x.Email)
                .WithLabel("Email")
            .Build();

        // Act
        var requiredFields = config.GetRequiredFields().ToList();

        // Assert
        requiredFields.ShouldBeEmpty();
    }

    [Fact]
    public void GetVisibleFields_Should_Return_All_Fields_When_No_Visibility_Conditions()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
            .AddField(x => x.LastName)
                .WithLabel("Last Name")
            .AddField(x => x.Email)
                .WithLabel("Email")
            .Build();

        var model = new TestModel();

        // Act
        var visibleFields = config.GetVisibleFields(model).ToList();

        // Assert
        visibleFields.Count.ShouldBe(3);
        visibleFields.ShouldContain("FirstName");
        visibleFields.ShouldContain("LastName");
        visibleFields.ShouldContain("Email");
    }

    [Fact]
    public void GetVisibleFields_Should_Respect_Visibility_Conditions()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
            .AddField(x => x.LastName)
                .WithLabel("Last Name")
                .VisibleWhen(m => m.ShowOptionalFields)
            .AddField(x => x.Email)
                .WithLabel("Email")
                .VisibleWhen(m => !string.IsNullOrEmpty(m.FirstName))
            .AddField(x => x.Phone)
                .WithLabel("Phone")
                .VisibleWhen(m => m.ShowOptionalFields && !string.IsNullOrEmpty(m.Email))
            .Build();

        // Test with model where ShowOptionalFields is false and FirstName is empty
        var model1 = new TestModel 
        { 
            ShowOptionalFields = false, 
            FirstName = "",
            Email = ""
        };

        // Act
        var visibleFields1 = config.GetVisibleFields(model1).ToList();

        // Assert
        visibleFields1.Count.ShouldBe(1);
        visibleFields1.ShouldContain("FirstName");
        visibleFields1.ShouldNotContain("LastName"); // ShowOptionalFields is false
        visibleFields1.ShouldNotContain("Email"); // FirstName is empty
        visibleFields1.ShouldNotContain("Phone"); // ShowOptionalFields is false

        // Test with model where conditions are met
        var model2 = new TestModel 
        { 
            ShowOptionalFields = true, 
            FirstName = "John",
            Email = "john@example.com"
        };

        // Act
        var visibleFields2 = config.GetVisibleFields(model2).ToList();

        // Assert
        visibleFields2.Count.ShouldBe(4);
        visibleFields2.ShouldContain("FirstName");
        visibleFields2.ShouldContain("LastName"); // ShowOptionalFields is true
        visibleFields2.ShouldContain("Email"); // FirstName is not empty
        visibleFields2.ShouldContain("Phone"); // ShowOptionalFields is true and Email is not empty
    }

    [Fact]
    public void GetVisibleFields_Should_Use_IsVisible_Property_When_No_Condition()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        
        // Add field and get direct access to configuration to set IsVisible
        var fieldBuilder = builder.AddField(x => x.FirstName)
            .WithLabel("First Name");
        
        builder.AddField(x => x.LastName)
            .WithLabel("Last Name");

        var config = builder.Build();
        
        // Manually set IsVisible to false for the first field
        config.Fields.First(f => f.FieldName == "FirstName").IsVisible = false;

        var model = new TestModel();

        // Act
        var visibleFields = config.GetVisibleFields(model).ToList();

        // Assert
        visibleFields.Count.ShouldBe(1);
        visibleFields.ShouldNotContain("FirstName"); // IsVisible is false
        visibleFields.ShouldContain("LastName"); // IsVisible is true by default
    }

    [Fact]
    public void GetVisibleFields_Should_Prioritize_Visibility_Condition_Over_IsVisible_Property()
    {
        // Arrange
        var builder = FormBuilder<TestModel>.Create();
        
        var fieldBuilder = builder.AddField(x => x.FirstName)
            .WithLabel("First Name")
            .VisibleWhen(m => true); // Always visible via condition

        var config = builder.Build();
        
        // Set IsVisible to false, but condition should override
        config.Fields.First(f => f.FieldName == "FirstName").IsVisible = false;

        var model = new TestModel();

        // Act
        var visibleFields = config.GetVisibleFields(model).ToList();

        // Assert
        visibleFields.Count.ShouldBe(1);
        visibleFields.ShouldContain("FirstName"); // Condition overrides IsVisible property
    }

    [Fact]
    public void GetVisibleFields_Should_Handle_Complex_Visibility_Logic()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.FirstName)
                .WithLabel("First Name")
            .AddField(x => x.LastName)
                .WithLabel("Last Name")
                .VisibleWhen(m => !string.IsNullOrEmpty(m.FirstName))
            .AddField(x => x.Email)
                .WithLabel("Email")
                .VisibleWhen(m => !string.IsNullOrEmpty(m.FirstName) && !string.IsNullOrEmpty(m.LastName))
            .Build();

        // Test progressive visibility
        var model = new TestModel();

        // Step 1: Only FirstName should be visible
        var step1 = config.GetVisibleFields(model).ToList();
        step1.Count.ShouldBe(1);
        step1.ShouldContain("FirstName");

        // Step 2: FirstName filled, LastName becomes visible
        model.FirstName = "John";
        var step2 = config.GetVisibleFields(model).ToList();
        step2.Count.ShouldBe(2);
        step2.ShouldContain("FirstName");
        step2.ShouldContain("LastName");

        // Step 3: Both names filled, Email becomes visible
        model.LastName = "Doe";
        var step3 = config.GetVisibleFields(model).ToList();
        step3.Count.ShouldBe(3);
        step3.ShouldContain("FirstName");
        step3.ShouldContain("LastName");
        step3.ShouldContain("Email");
    }

    public class TestModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool ShowOptionalFields { get; set; }
    }
}