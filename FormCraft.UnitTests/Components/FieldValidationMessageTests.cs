using System.Reflection;

namespace FormCraft.UnitTests.Components;

public class FieldValidationMessageTests
{
    [Fact]
    public void FieldValidationMessage_Should_Initialize_With_Empty_FieldName()
    {
        // Arrange & Act
        var component = new FieldValidationMessage();

        // Assert
        component.FieldName.ShouldBe(string.Empty);
    }

    [Fact]
    public void FieldValidationMessage_Should_Set_FieldName_Property()
    {
        // Arrange
        var component = new FieldValidationMessage();

        // Act
        component.FieldName = "TestField";

        // Assert
        component.FieldName.ShouldBe("TestField");
    }

    [Fact]
    public void ValidationErrors_Should_Return_Empty_When_No_EditContext()
    {
        // Arrange
        var component = new FieldValidationMessage();
        component.FieldName = "TestField";

        // Act - Use reflection to access private property
        var validationErrorsProperty = typeof(FieldValidationMessage)
            .GetProperty("ValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance);
        var validationErrors = (IEnumerable<string>)validationErrorsProperty!.GetValue(component)!;

        // Assert
        validationErrors.ShouldNotBeNull();
        validationErrors.Count().ShouldBe(0);
    }

    [Fact]
    public void HasValidationErrors_Should_Return_False_When_No_EditContext()
    {
        // Arrange
        var component = new FieldValidationMessage();
        component.FieldName = "TestField";

        // Act - Use reflection to access private property
        var hasValidationErrorsProperty = typeof(FieldValidationMessage)
            .GetProperty("HasValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance);
        var hasValidationErrors = (bool)hasValidationErrorsProperty!.GetValue(component)!;

        // Assert
        hasValidationErrors.ShouldBeFalse();
    }

    [Fact]
    public void ValidationErrors_Should_Return_Messages_When_EditContext_Has_Errors()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = editContext.Field("Name");
        var validationMessageStore = new ValidationMessageStore(editContext);
        validationMessageStore.Add(fieldIdentifier, "Name is required");
        validationMessageStore.Add(fieldIdentifier, "Name must be at least 2 characters");

        var component = new FieldValidationMessage();
        component.FieldName = "Name";

        // Set the cascading parameter using reflection
        var editContextProperty = typeof(FieldValidationMessage)
            .GetProperty("EditContext", BindingFlags.NonPublic | BindingFlags.Instance);
        editContextProperty!.SetValue(component, editContext);

        // Act - Use reflection to access private property
        var validationErrorsProperty = typeof(FieldValidationMessage)
            .GetProperty("ValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance);
        var validationErrors = (IEnumerable<string>)validationErrorsProperty!.GetValue(component)!;

        // Assert
        validationErrors.Count().ShouldBe(2);
        validationErrors.ShouldContain("Name is required");
        validationErrors.ShouldContain("Name must be at least 2 characters");
    }

    [Fact]
    public void HasValidationErrors_Should_Return_True_When_EditContext_Has_Errors()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = editContext.Field("Name");
        var validationMessageStore = new ValidationMessageStore(editContext);
        validationMessageStore.Add(fieldIdentifier, "Name is required");

        var component = new FieldValidationMessage();
        component.FieldName = "Name";

        // Set the cascading parameter using reflection
        var editContextProperty = typeof(FieldValidationMessage)
            .GetProperty("EditContext", BindingFlags.NonPublic | BindingFlags.Instance);
        editContextProperty!.SetValue(component, editContext);

        // Act - Use reflection to access private property
        var hasValidationErrorsProperty = typeof(FieldValidationMessage)
            .GetProperty("HasValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance);
        var hasValidationErrors = (bool)hasValidationErrorsProperty!.GetValue(component)!;

        // Assert
        hasValidationErrors.ShouldBeTrue();
    }

    [Fact]
    public void ValidationErrors_Should_Return_Empty_For_Nonexistent_Field()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = editContext.Field("Name");
        var validationMessageStore = new ValidationMessageStore(editContext);
        validationMessageStore.Add(fieldIdentifier, "Name is required");

        var component = new FieldValidationMessage();
        component.FieldName = "NonexistentField";

        // Set the cascading parameter using reflection
        var editContextProperty = typeof(FieldValidationMessage)
            .GetProperty("EditContext", BindingFlags.NonPublic | BindingFlags.Instance);
        editContextProperty!.SetValue(component, editContext);

        // Act - Use reflection to access private property
        var validationErrorsProperty = typeof(FieldValidationMessage)
            .GetProperty("ValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance);
        var validationErrors = (IEnumerable<string>)validationErrorsProperty!.GetValue(component)!;

        // Assert
        validationErrors.Count().ShouldBe(0);
    }

    [Fact]
    public void ValidationErrors_Should_Return_Empty_For_Empty_Field_Name()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var fieldIdentifier = editContext.Field("Name");
        var validationMessageStore = new ValidationMessageStore(editContext);
        validationMessageStore.Add(fieldIdentifier, "Name is required");

        var component = new FieldValidationMessage();
        component.FieldName = "";

        // Set the cascading parameter using reflection
        var editContextProperty = typeof(FieldValidationMessage)
            .GetProperty("EditContext", BindingFlags.NonPublic | BindingFlags.Instance);
        editContextProperty!.SetValue(component, editContext);

        // Act - Use reflection to access private property
        var validationErrorsProperty = typeof(FieldValidationMessage)
            .GetProperty("ValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance);
        var validationErrors = (IEnumerable<string>)validationErrorsProperty!.GetValue(component)!;

        // Assert
        validationErrors.Count().ShouldBe(0);
    }

    [Fact]
    public void OnInitialized_Should_Not_Throw_When_EditContext_Present()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var component = new FieldValidationMessage();

        // Set the cascading parameter using reflection
        var editContextProperty = typeof(FieldValidationMessage)
            .GetProperty("EditContext", BindingFlags.NonPublic | BindingFlags.Instance);
        editContextProperty!.SetValue(component, editContext);

        // Act & Assert - Call OnInitialized using reflection should not throw
        var onInitializedMethod = typeof(FieldValidationMessage)
            .GetMethod("OnInitialized", BindingFlags.NonPublic | BindingFlags.Instance);

        Should.NotThrow(() => onInitializedMethod!.Invoke(component, null));
    }

    [Fact]
    public void OnInitialized_Should_Not_Throw_When_EditContext_Is_Null()
    {
        // Arrange
        var component = new FieldValidationMessage();
        // EditContext remains null

        // Act & Assert - Should not throw
        var onInitializedMethod = typeof(FieldValidationMessage)
            .GetMethod("OnInitialized", BindingFlags.NonPublic | BindingFlags.Instance);

        Should.NotThrow(() => onInitializedMethod!.Invoke(component, null));
    }

    [Fact]
    public void Dispose_Should_Unsubscribe_From_ValidationStateChanged_When_EditContext_Present()
    {
        // Arrange
        var model = new TestModel();
        var editContext = new EditContext(model);
        var component = new FieldValidationMessage();

        // Set the cascading parameter using reflection
        var editContextProperty = typeof(FieldValidationMessage)
            .GetProperty("EditContext", BindingFlags.NonPublic | BindingFlags.Instance);
        editContextProperty!.SetValue(component, editContext);

        // Initialize the component first
        var onInitializedMethod = typeof(FieldValidationMessage)
            .GetMethod("OnInitialized", BindingFlags.NonPublic | BindingFlags.Instance);
        onInitializedMethod!.Invoke(component, null);

        // Act - Dispose the component
        component.Dispose();

        // Assert - Should not throw when validation state changes after disposal
        Should.NotThrow(() => editContext.NotifyValidationStateChanged());
    }

    [Fact]
    public void Dispose_Should_Not_Throw_When_EditContext_Is_Null()
    {
        // Arrange
        var component = new FieldValidationMessage();
        // EditContext remains null

        // Act & Assert - Should not throw
        Should.NotThrow(() => component.Dispose());
    }

    [Fact]
    public void FieldValidationMessage_Should_Have_Handle_Method()
    {
        // Act - Get the handle method using reflection
        var handleMethod = typeof(FieldValidationMessage)
            .GetMethod("HandleValidationStateChanged", BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert - Method should exist
        handleMethod.ShouldNotBeNull();
        handleMethod.Name.ShouldBe("HandleValidationStateChanged");
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}