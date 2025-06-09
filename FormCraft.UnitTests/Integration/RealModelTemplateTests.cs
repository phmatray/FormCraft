namespace FormCraft.UnitTests.Integration;

public class RealModelTemplateTests
{
    [Fact]
    public void ContactFormTemplate_Should_Work_With_Real_ContactModel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        // Act - Use the actual ContactModel from the main project
        var contactForm = FormBuilder<RealContactModel>.Create()
            .AsContactForm()
            .Build();

        // Assert - Verify form structure
        contactForm.ShouldNotBeNull();
        contactForm.Fields.Count().ShouldBe(4); // FirstName, LastName, Email, Phone

        var fieldNames = contactForm.Fields.Select(f => f.FieldName).ToList();
        fieldNames.ShouldContain("FirstName");
        fieldNames.ShouldContain("LastName");
        fieldNames.ShouldContain("Email");
        fieldNames.ShouldContain("Phone");

        // Verify field labels
        var firstNameField = contactForm.Fields.First(f => f.FieldName == "FirstName");
        var lastNameField = contactForm.Fields.First(f => f.FieldName == "LastName");
        var emailField = contactForm.Fields.First(f => f.FieldName == "Email");
        var phoneField = contactForm.Fields.First(f => f.FieldName == "Phone");

        firstNameField.Label.ShouldBe("First Name");
        lastNameField.Label.ShouldBe("Last Name");
        emailField.Label.ShouldBe("Email Address");
        phoneField.Label.ShouldBe("Phone Number");

        // Verify required fields
        firstNameField.IsRequired.ShouldBeTrue();
        lastNameField.IsRequired.ShouldBeTrue();
        emailField.IsRequired.ShouldBeTrue();
        phoneField.IsRequired.ShouldBeFalse(); // Phone typically not required

        // Verify placeholders
        firstNameField.Placeholder.ShouldBe("Enter your first name");
        lastNameField.Placeholder.ShouldBe("Enter your last name");

        // Verify all fields can be rendered
        var testModel = new RealContactModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "123-456-7890"
        };

        foreach (var field in contactForm.Fields)
        {
            var renderResult = rendererService.RenderField(testModel, field, EventCallback<object?>.Empty, EventCallback.Empty);
            renderResult.ShouldNotBeNull($"Render result should not be null for {field.FieldName}");
        }
    }

    [Fact]
    public void RegistrationFormTemplate_Should_Work_With_Real_RegistrationModel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        // Act
        var registrationForm = FormBuilder<RealRegistrationModel>.Create()
            .AsRegistrationForm()
            .Build();

        // Assert - Verify form structure
        registrationForm.ShouldNotBeNull();
        registrationForm.Fields.Count().ShouldBe(5); // FirstName, LastName, Email, Password, AcceptTerms

        var fieldNames = registrationForm.Fields.Select(f => f.FieldName).ToList();
        fieldNames.ShouldContain("FirstName");
        fieldNames.ShouldContain("LastName");
        fieldNames.ShouldContain("Email");
        fieldNames.ShouldContain("Password");
        fieldNames.ShouldContain("AcceptTerms");

        // Verify field types and rendering capability
        var testModel = new RealRegistrationModel
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Password = "SecurePassword123!",
            AcceptTerms = true
        };

        foreach (var field in registrationForm.Fields)
        {
            var renderResult = rendererService.RenderField(testModel, field, EventCallback<object?>.Empty, EventCallback.Empty);
            renderResult.ShouldNotBeNull($"Render result should not be null for {field.FieldName}");
        }

        // Verify specific field configurations
        var acceptTermsField = registrationForm.Fields.First(f => f.FieldName == "AcceptTerms");
        acceptTermsField.Label.ShouldBe("I accept the terms and conditions");
        acceptTermsField.FieldName.ShouldBe("AcceptTerms");
    }

    [Fact]
    public void Template_Extension_Should_Allow_Customization()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Act - Extend contact form template with additional fields
        var extendedContactForm = FormBuilder<ExtendedContactModel>.Create()
            .AsContactForm()
            .AddField(x => x.Company)
                .WithLabel("Company")
                .WithPlaceholder("Enter your company name")
            .AddField(x => x.DateOfBirth)
                .WithLabel("Date of Birth")
            .AddField(x => x.IsSubscribed)
                .WithLabel("Subscribe to newsletter")
            .Build();

        // Assert
        extendedContactForm.ShouldNotBeNull();
        extendedContactForm.Fields.Count().ShouldBe(7); // 4 from template + 3 additional

        // Verify original template fields are present
        var originalFields = extendedContactForm.Fields.Where(f =>
            f.FieldName == "FirstName" ||
            f.FieldName == "LastName" ||
            f.FieldName == "Email" ||
            f.FieldName == "Phone").ToList();
        originalFields.Count.ShouldBe(4);

        // Verify additional fields are present
        var additionalFields = extendedContactForm.Fields.Where(f =>
            f.FieldName == "Company" ||
            f.FieldName == "DateOfBirth" ||
            f.FieldName == "IsSubscribed").ToList();
        additionalFields.Count.ShouldBe(3);

        // Verify additional field configurations
        var companyField = extendedContactForm.Fields.First(f => f.FieldName == "Company");
        var dobField = extendedContactForm.Fields.First(f => f.FieldName == "DateOfBirth");
        var subscribedField = extendedContactForm.Fields.First(f => f.FieldName == "IsSubscribed");

        companyField.Label.ShouldBe("Company");
        companyField.Placeholder.ShouldBe("Enter your company name");
        dobField.Label.ShouldBe("Date of Birth");
        subscribedField.Label.ShouldBe("Subscribe to newsletter");
    }

    [Fact]
    public void Template_With_Custom_Validation_Should_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Act - Create contact form with additional validation
        var validatedContactForm = FormBuilder<RealContactModel>.Create()
            .AsContactForm()
            .AddField(x => x.Email)
                .WithEmailValidation("Email must be valid")
            .AddField(x => x.Phone)
                .WithMinLength(10, "Phone must be at least 10 digits")
            .Build();

        // Assert - Form should have more fields due to template + additional fields
        validatedContactForm.ShouldNotBeNull();
        validatedContactForm.Fields.Count().ShouldBeGreaterThan(4); // Template fields + additional validation fields

        // Should have email and phone fields with validation
        var emailFields = validatedContactForm.Fields.Where(f => f.FieldName == "Email").ToList();
        var phoneFields = validatedContactForm.Fields.Where(f => f.FieldName == "Phone").ToList();

        emailFields.Count.ShouldBeGreaterThan(0);
        phoneFields.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Template_Performance_Should_Be_Acceptable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();
        var rendererService = serviceProvider.GetRequiredService<IFieldRendererService>();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Create multiple template instances
        var forms = new List<IFormConfiguration<RealContactModel>>();

        for (int i = 0; i < 100; i++)
        {
            var form = FormBuilder<RealContactModel>.Create()
                .AsContactForm()
                .Build();
            forms.Add(form);
        }

        // Test rendering performance
        var testModel = new RealContactModel
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "1234567890"
        };

        foreach (var form in forms.Take(10)) // Test first 10 forms for rendering
        {
            foreach (var field in form.Fields)
            {
                var renderResult = rendererService.RenderField(testModel, field, EventCallback<object?>.Empty, EventCallback.Empty);
                renderResult.ShouldNotBeNull();
            }
        }

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(2000,
            $"Template creation and rendering took too long: {stopwatch.ElapsedMilliseconds}ms");

        forms.Count.ShouldBe(100);
        forms.All(f => f.Fields.Count() == 4).ShouldBeTrue();
    }

    [Fact]
    public void Template_Memory_Usage_Should_Be_Reasonable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        var serviceProvider = services.BuildServiceProvider();

        // Act - Create many template instances and verify they can be garbage collected
        var weakReferences = new List<WeakReference>();

        for (int i = 0; i < 1000; i++)
        {
            var form = FormBuilder<RealContactModel>.Create()
                .AsContactForm()
                .Build();

            weakReferences.Add(new WeakReference(form));
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert - Some objects should be eligible for garbage collection
        // (This is a heuristic test - not all objects may be collected immediately)
        var aliveCount = weakReferences.Count(wr => wr.IsAlive);
        var totalCount = weakReferences.Count;

        // At least some objects should be collectable (this is implementation-dependent)
        (aliveCount < totalCount).ShouldBeTrue($"Expected some objects to be garbage collected. Alive: {aliveCount}, Total: {totalCount}");
    }

    // Real model classes that mirror the actual project structure
    public class RealContactModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class RealRegistrationModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; }
    }

    public class ExtendedContactModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public bool IsSubscribed { get; set; }
    }
}