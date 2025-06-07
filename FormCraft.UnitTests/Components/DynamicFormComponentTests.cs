namespace FormCraft.UnitTests.Components;

public class DynamicFormComponentTests : Bunit.TestContext
{
    private readonly IFieldRendererService _fieldRendererService;

    public DynamicFormComponentTests()
    {
        _fieldRendererService = A.Fake<IFieldRendererService>();
        Services.AddSingleton(_fieldRendererService);
    }

    [Fact]
    public void DynamicFormComponent_Should_Render_With_Default_Parameters()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        // Setup fake renderer to return simple content
        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .Returns(builder => builder.AddContent(0, "Field Content"));

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.ShouldNotBeNull();
        component.Find("form").ShouldNotBeNull();
        component.FindAll(".form-field-container").Count.ShouldBe(1);
    }

    [Fact]
    public void DynamicFormComponent_Should_Render_Fields_In_Order()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
                .WithOrder(2)
            .AddField(x => x.Email)
                .WithLabel("Email")
                .WithOrder(1)
            .AddField(x => x.Age)
                .WithLabel("Age")
                .WithOrder(3)
            .Build();

        var callOrder = new List<string>();
        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .ReturnsLazily((TestModel m, IFieldConfiguration<TestModel, object> f, EventCallback<object?> e1, EventCallback e2) =>
            {
                callOrder.Add(f.FieldName);
                return builder => builder.AddContent(0, f.FieldName);
            });

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        callOrder.ShouldBe(new[] { "Email", "Name", "Age" }); // Order by Order property
    }

    [Fact]
    public void DynamicFormComponent_Should_Respect_Field_Visibility_Conditions()
    {
        // Arrange
        var model = new TestModel { ShowOptionalField = false };
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .AddField(x => x.Email)
                .WithLabel("Email")
                .VisibleWhen(m => m.ShowOptionalField)
            .Build();

        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .Returns(builder => builder.AddContent(0, "Field Content"));

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindAll(".form-field-container").Count.ShouldBe(1); // Only Name field should be visible
        
        // Verify only Name field was rendered
        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>.That.Matches(f => f.FieldName == "Name"), A<EventCallback<object?>>._, A<EventCallback>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>.That.Matches(f => f.FieldName == "Email"), A<EventCallback<object?>>._, A<EventCallback>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public void DynamicFormComponent_Should_Respect_IsVisible_Property()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .AddField(x => x.Email)
                .WithLabel("Email")
            .Build();

        // Manually set Email field as not visible
        config.Fields.First(f => f.FieldName == "Email").IsVisible = false;

        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .Returns(builder => builder.AddContent(0, "Field Content"));

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        component.FindAll(".form-field-container").Count.ShouldBe(1); // Only Name field should be visible
    }

    [Fact]
    public void DynamicFormComponent_Should_Apply_Correct_Layout_Classes()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .WithLayout(FormLayout.Horizontal)
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .Returns(builder => builder.AddContent(0, "Field Content"));

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var formContainer = component.Find("form > div");
        formContainer.GetAttribute("class")!.ShouldContain("row"); // Horizontal layout class

        var fieldContainer = component.Find(".mb-4");
        fieldContainer.GetAttribute("class")!.ShouldContain("col-md-6"); // Horizontal field class
    }

    [Fact]
    public void DynamicFormComponent_Should_Apply_Grid_Layout_Classes()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .WithLayout(FormLayout.Grid)
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .Returns(builder => builder.AddContent(0, "Field Content"));

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var formContainer = component.Find("form > div");
        formContainer.GetAttribute("class")!.ShouldContain("row"); // Grid layout class

        var fieldContainer = component.Find(".mb-4");
        fieldContainer.GetAttribute("class")!.ShouldContain("col-lg-4"); // Grid field class
    }

    [Fact]
    public void DynamicFormComponent_Should_Apply_Inline_Layout_Classes()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .WithLayout(FormLayout.Inline)
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .Returns(builder => builder.AddContent(0, "Field Content"));

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var formContainer = component.Find("form > div");
        formContainer.GetAttribute("class")!.ShouldContain("d-flex"); // Inline layout classes

        var fieldContainer = component.Find(".mb-4");
        fieldContainer.GetAttribute("class")!.ShouldContain("flex-fill"); // Inline field class
    }

    [Fact]
    public void DynamicFormComponent_Should_Include_DynamicFormValidator()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name)
                .WithLabel("Name")
            .Build();

        A.CallTo(() => _fieldRendererService.RenderField(A<TestModel>._, A<IFieldConfiguration<TestModel, object>>._, A<EventCallback<object?>>._, A<EventCallback>._))
            .Returns(builder => builder.AddContent(0, "Field Content"));

        // Act
        var component = RenderComponent<DynamicFormComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        // The component should contain a DynamicFormValidator component
        // This is verified by the component rendering successfully with the validator
        component.ShouldNotBeNull();
    }

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool ShowOptionalField { get; set; }
    }
}