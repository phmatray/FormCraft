namespace FormCraft.UnitTests.Builders;

public class FieldConfigurationWrapperTests
{
    [Fact]
    public void FieldConfigurationWrapper_Should_Initialize_With_Inner_Configuration()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.FieldName).Returns("TestField");

        // Act
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Assert
        wrapper.ShouldNotBeNull();
        wrapper.TypedConfiguration.ShouldBe(innerConfig);
    }

    [Fact]
    public void FieldName_Should_Return_Inner_FieldName()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.FieldName).Returns("TestField");
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var fieldName = wrapper.FieldName;

        // Assert
        fieldName.ShouldBe("TestField");
        A.CallTo(() => innerConfig.FieldName).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void ValueExpression_Should_Convert_To_Object_Expression()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        Expression<Func<TestModel, string>> originalExpression = x => x.Name;
        A.CallTo(() => innerConfig.ValueExpression).Returns(originalExpression);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var valueExpression = wrapper.ValueExpression;

        // Assert
        valueExpression.ShouldNotBeNull();
        valueExpression.ReturnType.ShouldBe(typeof(object));
        valueExpression.Parameters.Count.ShouldBe(1);
        valueExpression.Parameters[0].Type.ShouldBe(typeof(TestModel));
    }

    [Fact]
    public void Label_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.Label;

        // Assert - Get
        A.CallTo(() => innerConfig.Label).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.Label = "Test Label";

        // Assert - Set
        A.CallToSet(() => innerConfig.Label).To("Test Label").MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Placeholder_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.Placeholder;

        // Assert - Get
        A.CallTo(() => innerConfig.Placeholder).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.Placeholder = "Test Placeholder";

        // Assert - Set
        A.CallToSet(() => innerConfig.Placeholder).To("Test Placeholder").MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void HelpText_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.HelpText;

        // Assert - Get
        A.CallTo(() => innerConfig.HelpText).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.HelpText = "Test Help";

        // Assert - Set
        A.CallToSet(() => innerConfig.HelpText).To("Test Help").MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CssClass_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.CssClass;

        // Assert - Get
        A.CallTo(() => innerConfig.CssClass).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.CssClass = "test-class";

        // Assert - Set
        A.CallToSet(() => innerConfig.CssClass).To("test-class").MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void IsRequired_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.IsRequired;

        // Assert - Get
        A.CallTo(() => innerConfig.IsRequired).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.IsRequired = true;

        // Assert - Set
        A.CallToSet(() => innerConfig.IsRequired).To(true).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void IsVisible_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.IsVisible;

        // Assert - Get
        A.CallTo(() => innerConfig.IsVisible).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.IsVisible = false;

        // Assert - Set
        A.CallToSet(() => innerConfig.IsVisible).To(false).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void IsDisabled_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.IsDisabled;

        // Assert - Get
        A.CallTo(() => innerConfig.IsDisabled).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.IsDisabled = true;

        // Assert - Set
        A.CallToSet(() => innerConfig.IsDisabled).To(true).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void IsReadOnly_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.IsReadOnly;

        // Assert - Get
        A.CallTo(() => innerConfig.IsReadOnly).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.IsReadOnly = true;

        // Assert - Set
        A.CallToSet(() => innerConfig.IsReadOnly).To(true).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Order_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.Order;

        // Assert - Get
        A.CallTo(() => innerConfig.Order).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.Order = 5;

        // Assert - Set
        A.CallToSet(() => innerConfig.Order).To(5).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void AdditionalAttributes_Should_Return_Inner_AdditionalAttributes()
    {
        // Arrange
        var attributes = new Dictionary<string, object> { { "data-test", "value" } };
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.AdditionalAttributes).Returns(attributes);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var result = wrapper.AdditionalAttributes;

        // Assert
        result.ShouldBe(attributes);
        A.CallTo(() => innerConfig.AdditionalAttributes).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Validators_Should_Wrap_Inner_Validators()
    {
        // Arrange
        var innerValidator = A.Fake<IFieldValidator<TestModel, string>>();
        var validators = new List<IFieldValidator<TestModel, string>> { innerValidator };
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Validators).Returns(validators);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var result = wrapper.Validators;

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void Validators_Should_Return_Same_Cached_Instance_On_Repeated_Reads()
    {
        // Arrange
        var validators = new List<IFieldValidator<TestModel, string>> { A.Fake<IFieldValidator<TestModel, string>>() };
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Validators).Returns(validators);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var first = wrapper.Validators;
        var second = wrapper.Validators;

        // Assert - repeated reads must not hand out throwaway copies (issue #151)
        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void AddValidator_Should_Be_Retained_Across_Reads()
    {
        // Arrange - `config.Fields[i].Validators.Add(...)` used to compile and silently drop the
        // validator (#151). #151 made the view cache so the mutation at least survived; #155 removed
        // the mutation instead — `Validators` is IReadOnlyList and that line no longer compiles, so
        // AddValidator is the only way in and it writes through to the underlying typed config.
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Validators).Returns(new List<IFieldValidator<TestModel, string>>());
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);
        var addedValidator = A.Fake<IFieldValidator<TestModel, object>>();

        // Act
        wrapper.AddValidator(addedValidator);

        // Assert - the caller's own instance is what comes back, and it survives a re-read.
        wrapper.Validators.ShouldContain(addedValidator);
        A.CallTo(() => innerConfig.AddValidator(A<IFieldValidator<TestModel, string>>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Validators_Should_Surface_Typed_Validators_Added_After_First_Read()
    {
        // Arrange
        var typedValidators = new List<IFieldValidator<TestModel, string>>();
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Validators).Returns(typedValidators);
        // Make the fake behave like a real configuration: AddValidator is the mutation path since
        // #155, so a fake that only returns a list would swallow the write and these assertions
        // would be measuring the stub rather than the wrapper.
        A.CallTo(() => innerConfig.AddValidator(A<IFieldValidator<TestModel, string>>._))
            .Invokes((IFieldValidator<TestModel, string> v) => typedValidators.Add(v));
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        var cached = wrapper.Validators;
        cached.ShouldBeEmpty();

        // Act - a typed validator added through the builder API after the first read
        typedValidators.Add(A.Fake<IFieldValidator<TestModel, string>>());

        // Assert - the cached view picks it up on the next read
        wrapper.Validators.Count.ShouldBe(1);
        wrapper.Validators[0].ShouldBeOfType<ValidatorWrapper<TestModel, string>>();
    }

    [Fact]
    public void AddValidator_Should_Forward_To_Inner_Typed_List()
    {
        // Arrange
        var typedValidators = new List<IFieldValidator<TestModel, string>>();
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Validators).Returns(typedValidators);
        // Make the fake behave like a real configuration: AddValidator is the mutation path since
        // #155, so a fake that only returns a list would swallow the write and these assertions
        // would be measuring the stub rather than the wrapper.
        A.CallTo(() => innerConfig.AddValidator(A<IFieldValidator<TestModel, string>>._))
            .Invokes((IFieldValidator<TestModel, string> v) => typedValidators.Add(v));
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);
        var objectValidator = A.Fake<IFieldValidator<TestModel, object>>();

        // Act
        wrapper.AddValidator(objectValidator);

        // Assert - registered against the underlying typed configuration AND visible
        // through the object-typed view
        typedValidators.Count.ShouldBe(1);
        wrapper.Validators.ShouldContain(objectValidator);
    }

    [Fact]
    public void AddValidator_Should_Unwrap_ValidatorWrapper_Into_Inner_Typed_List()
    {
        // Arrange
        var typedValidators = new List<IFieldValidator<TestModel, string>>();
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Validators).Returns(typedValidators);
        // Make the fake behave like a real configuration: AddValidator is the mutation path since
        // #155, so a fake that only returns a list would swallow the write and these assertions
        // would be measuring the stub rather than the wrapper.
        A.CallTo(() => innerConfig.AddValidator(A<IFieldValidator<TestModel, string>>._))
            .Invokes((IFieldValidator<TestModel, string> v) => typedValidators.Add(v));
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        var typedValidator = A.Fake<IFieldValidator<TestModel, string>>();
        var wrappedValidator = new ValidatorWrapper<TestModel, string>(typedValidator);

        // Act
        wrapper.AddValidator(wrappedValidator);

        // Assert - the original typed validator lands in the inner list, not a double wrapper
        typedValidators.ShouldContain(typedValidator);
    }

    [Fact]
    public async Task AddValidator_Adapter_Should_Delegate_Validation_To_Object_Validator()
    {
        // Arrange
        var typedValidators = new List<IFieldValidator<TestModel, string>>();
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Validators).Returns(typedValidators);
        // Make the fake behave like a real configuration: AddValidator is the mutation path since
        // #155, so a fake that only returns a list would swallow the write and these assertions
        // would be measuring the stub rather than the wrapper.
        A.CallTo(() => innerConfig.AddValidator(A<IFieldValidator<TestModel, string>>._))
            .Invokes((IFieldValidator<TestModel, string> v) => typedValidators.Add(v));
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        var objectValidator = A.Fake<IFieldValidator<TestModel, object>>();
        A.CallTo(() => objectValidator.ValidateAsync(A<TestModel>._, A<object>._, A<IServiceProvider>._))
            .Returns(Task.FromResult(ValidationResult.Failure("nope")));

        var model = new TestModel();
        var services = A.Fake<IServiceProvider>();

        // Act
        wrapper.AddValidator(objectValidator);
        var result = await typedValidators.Single().ValidateAsync(model, "value", services);

        // Assert - the typed adapter forwards to the original object-typed validator
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("nope");
        A.CallTo(() => objectValidator.ValidateAsync(model, "value", services)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Validators_Added_Through_Built_Configuration_Should_Be_Retained()
    {
        // Arrange - end-to-end shape of the bug report, now through the supported path. The original
        // `config.Fields[0].Validators.Add(...)` no longer compiles (#155), which is the fix: it used
        // to compile, run, and mutate a snapshot that validation never read.
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();
        var addedValidator = A.Fake<IFieldValidator<TestModel, object>>();

        // Act
        config.Fields[0].AddValidator(addedValidator);

        // Assert - the validator is still there on subsequent reads (it will run during validation)
        config.Fields[0].Validators.ShouldContain(addedValidator);
    }

    [Fact]
    public void Dependencies_Should_Return_Inner_Dependencies()
    {
        // Arrange
        var dependency = A.Fake<IFieldDependency<TestModel>>();
        var dependencies = new List<IFieldDependency<TestModel>> { dependency };
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Dependencies).Returns(dependencies);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var result = wrapper.Dependencies;

        // Assert
        result.ShouldBe(dependencies);
        A.CallTo(() => innerConfig.Dependencies).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void VisibilityCondition_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var condition = new Func<TestModel, bool>(x => x.Name.Length > 0);
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.VisibilityCondition;

        // Assert - Get
        A.CallTo(() => innerConfig.VisibilityCondition).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.VisibilityCondition = condition;

        // Assert - Set
        A.CallToSet(() => innerConfig.VisibilityCondition).To(condition).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void DisabledCondition_Should_Get_And_Set_From_Inner()
    {
        // Arrange
        var condition = new Func<TestModel, bool>(x => x.Name.Length == 0);
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get
        var _ = wrapper.DisabledCondition;

        // Assert - Get
        A.CallTo(() => innerConfig.DisabledCondition).MustHaveHappenedOnceExactly();

        // Act - Set
        wrapper.DisabledCondition = condition;

        // Assert - Set
        A.CallToSet(() => innerConfig.DisabledCondition).To(condition).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void CustomTemplate_Should_Allow_Get_And_Set()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.CustomTemplate).Returns(null);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act - Get (should be null when neither wrapper nor inner has a template)
        var result = wrapper.CustomTemplate;

        // Assert - Get
        result.ShouldBeNull();

        // Act - Set
        RenderFragment<IFieldContext<TestModel, object>> template = _ => builder => { };
        wrapper.CustomTemplate = template;

        // Assert - Set
        wrapper.CustomTemplate.ShouldBe(template);
    }

    [Fact]
    public void CustomTemplate_Should_Adapt_Typed_Template_From_Inner_Configuration()
    {
        // Arrange - a template configured through the typed builder API must
        // surface through the object-typed wrapper instead of being dropped
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        RenderFragment<IFieldContext<TestModel, string>> typedTemplate = _ => builder => { };
        A.CallTo(() => innerConfig.CustomTemplate).Returns(typedTemplate);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var result = wrapper.CustomTemplate;

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public void GetActualFieldType_Should_Return_TValue_Type()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act
        var result = wrapper.GetActualFieldType();

        // Assert
        result.ShouldBe(typeof(string));
    }

    [Fact]
    public void GetActualFieldType_Should_Return_Correct_Type_For_Different_Types()
    {
        // Arrange & Act & Assert for int
        var intConfig = A.Fake<IFieldConfiguration<TestModel, int>>();
        var intWrapper = new FieldConfigurationWrapper<TestModel, int>(intConfig);
        intWrapper.GetActualFieldType().ShouldBe(typeof(int));

        // Arrange & Act & Assert for bool
        var boolConfig = A.Fake<IFieldConfiguration<TestModel, bool>>();
        var boolWrapper = new FieldConfigurationWrapper<TestModel, bool>(boolConfig);
        boolWrapper.GetActualFieldType().ShouldBe(typeof(bool));

        // Arrange & Act & Assert for DateTime
        var dateConfig = A.Fake<IFieldConfiguration<TestModel, DateTime>>();
        var dateWrapper = new FieldConfigurationWrapper<TestModel, DateTime>(dateConfig);
        dateWrapper.GetActualFieldType().ShouldBe(typeof(DateTime));
    }

    [Fact]
    public void ValueExpression_Should_Execute_Correctly()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        Expression<Func<TestModel, string>> originalExpression = x => x.Name;
        A.CallTo(() => innerConfig.ValueExpression).Returns(originalExpression);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);
        var model = new TestModel { Name = "Test Value" };

        // Act
        var valueExpression = wrapper.ValueExpression;
        var compiledExpression = valueExpression.Compile();
        var result = compiledExpression(model);

        // Assert
        result.ShouldBe("Test Value");
        result.ShouldBeOfType<string>();
    }

    [Fact]
    public void Wrapper_Should_Implement_Correct_Interface()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();

        // Act
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Assert
        wrapper.ShouldBeAssignableTo<IFieldConfiguration<TestModel, object>>();
    }

    [Fact]
    public void Wrapper_Should_Handle_Null_Values_Gracefully()
    {
        // Arrange
        var innerConfig = A.Fake<IFieldConfiguration<TestModel, string>>();
        A.CallTo(() => innerConfig.Placeholder).Returns(null);
        A.CallTo(() => innerConfig.HelpText).Returns(null);
        A.CallTo(() => innerConfig.CssClass).Returns(null);
        A.CallTo(() => innerConfig.VisibilityCondition).Returns(null);
        A.CallTo(() => innerConfig.DisabledCondition).Returns(null);
        var wrapper = new FieldConfigurationWrapper<TestModel, string>(innerConfig);

        // Act & Assert
        wrapper.Placeholder.ShouldBeNull();
        wrapper.HelpText.ShouldBeNull();
        wrapper.CssClass.ShouldBeNull();
        wrapper.VisibilityCondition.ShouldBeNull();
        wrapper.DisabledCondition.ShouldBeNull();
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}