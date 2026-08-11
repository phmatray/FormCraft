namespace FormCraft.ForMudBlazor.UnitTests.Extensions;

public class FieldBuilderExtensionsTests : MudBlazorTestBase
{

    [Fact]
    public void AsPassword_Should_Set_Password_InputType()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: false))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
    }

    [Fact]
    public void AsPassword_With_Toggle_Should_Add_Adornment()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .AsPassword(enableVisibilityToggle: true))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End);
        mudTextField.Instance.AdornmentIcon.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void WithAdornment_Should_Set_Start_Icon()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.Start);
        mudTextField.Instance.AdornmentIcon.ShouldBe(Icons.Material.Filled.Email);
    }

    [Fact]
    public void WithAdornment_Should_Set_End_Icon()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Name")
                .WithAdornment(Icons.Material.Filled.Person, Adornment.End))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End);
        mudTextField.Instance.AdornmentIcon.ShouldBe(Icons.Material.Filled.Person);
    }

    [Fact]
    public void WithAdornment_Should_Set_Color()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Email, Adornment.Start, Color.Secondary))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.AdornmentColor.ShouldBe(Color.Secondary);
    }

    [Fact]
    public void WithAdornment_Should_Keep_The_OnClick_Handler()
    {
        // Arrange - the handler is the one parameter of WithAdornment that was accepted,
        // documented and then discarded (#192). Configuration is where the loss happened.
        Action<string?> handler = _ => { };

        // Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: handler))
            .Build();

        // Assert - same instance, so the renderer can invoke exactly what the caller passed
        var attributes = config.Fields[0].AdditionalAttributes;
        attributes.ShouldContainKey("OnAdornmentClick");
        attributes["OnAdornmentClick"].ShouldBeSameAs(handler);
    }

    [Fact]
    public void WithAdornment_Without_An_OnClick_Handler_Should_Resolve_To_None()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start))
            .Build();

        // Assert - the entry may exist, but it must not resolve to a handler; both render paths
        // read it with `is Action<string?>`, which a null fails exactly as an absent key would
        config.Fields[0].AdditionalAttributes
            .GetValueOrDefault("OnAdornmentClick")
            .ShouldBeNull();
    }

    [Fact]
    public void WithAdornment_Called_Again_Without_A_Handler_Should_Clear_The_Earlier_One()
    {
        // Arrange - the documented "reusable field configurations" pattern: a helper configures a
        // searching adornment, and a caller re-configures the field with a plain decorative icon.
        // WithAdornment must overwrite ALL FOUR of its settings, not three of them — otherwise the
        // caller gets an icon that silently still runs the helper's handler.
        Action<string?> helperHandler = _ => { };

        // Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: helperHandler)
                .WithAdornment(Icons.Material.Filled.Email, Adornment.End))
            .Build();

        // Assert - the second call wins on every parameter, the handler included
        var attributes = config.Fields[0].AdditionalAttributes;
        attributes["Adornment"].ShouldBe(Adornment.End);
        attributes["AdornmentIcon"].ShouldBe(Icons.Material.Filled.Email);
        attributes.GetValueOrDefault("OnAdornmentClick").ShouldBeNull();
    }

    [Fact]
    public void WithAdornment_Called_Again_With_A_Handler_Should_Replace_The_Earlier_One()
    {
        // Arrange - the mirror case: last call wins, so the second handler is the live one
        Action<string?> first = _ => { };
        Action<string?> second = _ => { };

        // Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithAdornment(Icons.Material.Filled.Search, Adornment.Start, onClick: first)
                .WithAdornment(Icons.Material.Filled.Email, Adornment.End, onClick: second))
            .Build();

        // Assert
        config.Fields[0].AdditionalAttributes["OnAdornmentClick"].ShouldBeSameAs(second);
    }

    [Fact]
    public void WithAdornment_Should_Still_Write_The_Three_Presentation_Attributes()
    {
        // Arrange & Act - guards the addition above from regressing what #184 made work
        Action<string?> handler = _ => { };
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithAdornment(Icons.Material.Filled.Search, Adornment.End, Color.Secondary, handler))
            .Build();

        // Assert
        var attributes = config.Fields[0].AdditionalAttributes;
        attributes["Adornment"].ShouldBe(Adornment.End);
        attributes["AdornmentIcon"].ShouldBe(Icons.Material.Filled.Search);
        attributes["AdornmentColor"].ShouldBe(Color.Secondary);
    }

    [Fact]
    public void WithAdornment_Should_Set_The_Three_Attributes_On_A_Numeric_Field()
    {
        // Arrange - WithAdornment was declared on FieldBuilder<TModel, string> only, so a numeric
        // field could not call it at all and had to fall back to raw WithAttribute (#191).
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Quantity, field => field
                .WithLabel("Quantity")
                .WithAdornment(Icons.Material.Filled.Numbers, Adornment.End, Color.Primary))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var numeric = component.FindComponent<MudNumericField<int>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.End);
        numeric.AdornmentIcon.ShouldBe(Icons.Material.Filled.Numbers);
        numeric.AdornmentColor.ShouldBe(Color.Primary);
    }

    [Fact]
    public void WithAdornment_Should_Set_The_Three_Attributes_On_A_Nullable_Numeric_Field()
    {
        // Arrange - the `struct` constraint excludes nullable value types, so int?/decimal? need
        // their own overload rather than falling out of the non-nullable one.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Discount, field => field
                .WithLabel("Discount")
                .WithAdornment(Icons.Material.Filled.Percent, Adornment.Start, Color.Secondary))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var numeric = component.FindComponent<MudNumericField<decimal?>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.Start);
        numeric.AdornmentIcon.ShouldBe(Icons.Material.Filled.Percent);
        numeric.AdornmentColor.ShouldBe(Color.Secondary);
    }

    [Fact]
    public void WithAdornment_Should_Default_Its_Position_And_Colour_On_A_Numeric_Field()
    {
        // Arrange - same defaults as the string overload: Start, Color.Default.
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Rating, field => field
                .WithLabel("Rating")
                .WithAdornment(Icons.Material.Filled.Star))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var numeric = component.FindComponent<MudNumericField<double>>().Instance;
        numeric.Adornment.ShouldBe(Adornment.Start);
        numeric.AdornmentColor.ShouldBe(Color.Default);
    }

    [Fact]
    public void AsSlider_Should_Configure_Slider_Field()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Rating, field => field
                .WithLabel("Rating")
                .AsSlider(min: 0.0, max: 10.0, step: 1.0, showValueLabel: true))
            .Build();

        // Assert - Configuration should be built without error
        config.ShouldNotBeNull();
        var field = config.Fields.First(f => f.FieldName == "Rating");
        field.AdditionalAttributes["Min"].ShouldBe(0.0);
        field.AdditionalAttributes["Max"].ShouldBe(10.0);
        field.AdditionalAttributes["Step"].ShouldBe(1.0);
        field.AdditionalAttributes["ShowValueLabel"].ShouldBe(true);
    }

    [Fact]
    public void ChainedExtensions_Should_Work_Together()
    {
        // Arrange
        var model = new TestModel();
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithPlaceholder("Enter your password")
                .AsPassword(enableVisibilityToggle: true)
                .Required("Password is required"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Configuration, config));

        // Assert
        var mudTextField = component.FindComponent<MudTextField<string>>();
        mudTextField.Instance.Label.ShouldBe("Password");
        mudTextField.Instance.Placeholder.ShouldBe("Enter your password");
        mudTextField.Instance.InputType.ShouldBe(InputType.Password);
        mudTextField.Instance.Adornment.ShouldBe(Adornment.End);
    }

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int Quantity { get; set; }
        public decimal? Discount { get; set; }
    }
}
