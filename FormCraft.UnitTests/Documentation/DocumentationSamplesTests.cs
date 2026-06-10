using System.ComponentModel.DataAnnotations;
using ValidationResult = FormCraft.ValidationResult;

namespace FormCraft.UnitTests.Documentation;

/// <summary>
/// Compiles and validates the code samples published in README.md and the demo docs
/// (FormCraft.DemoBlazorApp/wwwroot/docs). Each test mirrors a documented sample so
/// the documentation cannot silently drift away from the real public API again.
/// When a sample in the docs changes, update the matching test here (and vice versa).
/// </summary>
public class DocumentationSamplesTests
{
    #region README - Quick Start

    [Fact]
    public void Readme_QuickStart_Sample_Should_Build()
    {
        // Mirrors README.md "Quick Start > 3. Build Your Form"
        var formConfig = FormBuilder<UserRegistration>.Create()
            .AddRequiredTextField(x => x.FirstName, "First Name")
            .AddRequiredTextField(x => x.LastName, "Last Name")
            .AddEmailField(x => x.Email)
            .AddNumericField(x => x.Age, "Age", min: 18, max: 120)
            .AddDropdownField(x => x.Country, "Country",
                ("us", "United States"),
                ("uk", "United Kingdom"),
                ("ca", "Canada"),
                ("au", "Australia"))
            .AddField(x => x.AcceptTerms, field => field
                .WithLabel("I accept the terms and conditions")
                .Required("You must accept the terms"))
            .Build();

        formConfig.Fields.Count.ShouldBe(6);
        formConfig.Fields.Single(f => f.FieldName == "Age").IsRequired.ShouldBeTrue();
        formConfig.Fields.Single(f => f.FieldName == "AcceptTerms").IsRequired.ShouldBeTrue();
        formConfig.Fields.Single(f => f.FieldName == "Country").Label.ShouldBe("Country");
    }

    #endregion

    #region README - Attribute-Based Forms

    [Fact]
    public void Readme_AttributeBasedForm_Sample_Should_Build()
    {
        // Mirrors README.md "Attribute-Based Forms"
        var formConfig = FormBuilder<AttributeUserRegistration>.Create()
            .AddFieldsFromAttributes()
            .Build();

        formConfig.Fields.Count.ShouldBe(8);
        formConfig.Fields.Single(f => f.FieldName == "FirstName").Label.ShouldBe("First Name");
        formConfig.Fields.Single(f => f.FieldName == "Country").ShouldNotBeNull();
    }

    [Fact]
    public void Readme_FluentApi_Comparison_Sample_Should_Build()
    {
        // Mirrors README.md "Comparison: Fluent API vs Attributes" (fluent side)
        var config = FormBuilder<ComparisonUser>.Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Full Name")
                .WithPlaceholder("Enter name")
                .Required("Name is required")
                .WithMinLength(2))
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithInputType("email")
                .Required())
            .Build();

        config.Fields.Count.ShouldBe(2);
        config.Fields.Single(f => f.FieldName == "Email").InputType.ShouldBe("email");
    }

    #endregion

    #region README - Dynamic Field Dependencies

    [Fact]
    public void Readme_DynamicFieldDependencies_Sample_Should_Build()
    {
        // Mirrors README.md "Dynamic Field Dependencies"
        var formConfig = FormBuilder<OrderForm>.Create()
            .AddDropdownField(x => x.ProductType, "Product Type",
                ("standard", "Standard"),
                ("premium", "Premium"))
            .AddField(x => x.ProductModel, field => field
                .WithLabel("Model")
                .WithOptions(
                    ("basic", "Basic Model"),
                    ("pro", "Pro Model"))
                // Reset the model whenever Product Type changes
                .DependsOn(x => x.ProductType, (model, productType) =>
                    model.ProductModel = string.Empty))
            .AddNumericField(x => x.Quantity, "Quantity", min: 1)
            .AddField(x => x.TotalPrice, field => field
                .WithLabel("Total Price")
                .ReadOnly()
                // Recalculate the total whenever Quantity changes
                .DependsOn(x => x.Quantity, (model, quantity) =>
                    model.TotalPrice = quantity * GetUnitPrice(model.ProductModel)))
            .Build();

        formConfig.Fields.Count.ShouldBe(4);

        var totalPrice = formConfig.Fields.Single(f => f.FieldName == "TotalPrice");
        totalPrice.IsReadOnly.ShouldBeTrue();
        totalPrice.Dependencies.Count.ShouldBe(1);
        totalPrice.Dependencies[0].DependentFieldName.ShouldBe("Quantity");

        // The dependency callback recalculates the total when the watched field changes
        var order = new OrderForm { ProductModel = "pro", Quantity = 3 };
        totalPrice.Dependencies[0].OnDependencyChanged(order);
        order.TotalPrice.ShouldBe(3 * GetUnitPrice("pro"));
    }

    private static decimal GetUnitPrice(string productModel) =>
        productModel == "pro" ? 19.99m : 9.99m;

    #endregion

    #region README - Custom Validation

    [Fact]
    public void Readme_CustomValidation_Sample_Should_Build()
    {
        // Mirrors README.md "Custom Validation"
        var forbiddenUsernames = new[] { "admin", "root" };

        var config = FormBuilder<ComparisonUser>.Create()
            .AddField(x => x.Name, field => field
                .WithValidator(
                    username => !forbiddenUsernames.Contains(username.ToLower()),
                    "This username is not available")
                .WithAsyncValidator(
                    async username => await IsUsernameAvailableAsync(username),
                    "Username is already taken"))
            .Build();

        config.Fields.Count.ShouldBe(1);
        config.Fields.Single().Validators.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Readme_ModelAware_Validator_Sample_Should_Resolve_Services()
    {
        // Mirrors README.md "Custom Validation" (IFieldValidator with DI services)
        var services = new ServiceCollection()
            .AddSingleton<IUserService>(new FakeUserService(isAvailable: false))
            .BuildServiceProvider();

        var validator = new UniqueUsernameValidator();
        var result = await validator.ValidateAsync(new ComparisonUser(), "taken", services);

        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Username is already taken");
    }

    private static Task<bool> IsUsernameAvailableAsync(string username) =>
        Task.FromResult(true);

    public interface IUserService
    {
        Task<bool> IsUsernameAvailableAsync(string username);
    }

    private sealed class FakeUserService(bool isAvailable) : IUserService
    {
        public Task<bool> IsUsernameAvailableAsync(string username) =>
            Task.FromResult(isAvailable);
    }

    public class UniqueUsernameValidator : IFieldValidator<ComparisonUser, string>
    {
        public string? ErrorMessage { get; set; } = "Username is already taken";

        public async Task<ValidationResult> ValidateAsync(
            ComparisonUser model, string value, IServiceProvider services)
        {
            var userService = services.GetRequiredService<IUserService>();
            return await userService.IsUsernameAvailableAsync(value)
                ? ValidationResult.Success()
                : ValidationResult.Failure("Username is already taken");
        }
    }

    #endregion

    #region README - Multiple Layouts

    [Theory]
    [InlineData(FormLayout.Vertical)]
    [InlineData(FormLayout.Horizontal)]
    [InlineData(FormLayout.Grid)]
    [InlineData(FormLayout.Inline)]
    public void Readme_MultipleLayouts_Sample_Should_Build(FormLayout layout)
    {
        // Mirrors README.md "Multiple Layouts"
        var config = FormBuilder<ComparisonUser>.Create()
            .WithLayout(layout)
            .AddField(x => x.Name, field => field.WithLabel("Name"))
            .Build();

        config.Layout.ShouldBe(layout);
    }

    [Fact]
    public void Readme_GroupColumns_Sample_Should_Build()
    {
        // Mirrors README.md "Multiple Layouts" (column counts via field groups)
        var config = FormBuilder<GroupedUserModel>.Create()
            .AddFieldGroup(group => group
                .WithGroupName("Address")
                .WithColumns(2)
                .AddField(x => x.City)
                .AddField(x => x.PostalCode))
            .Build();

        var grouped = config.ShouldBeAssignableTo<IGroupedFormConfiguration<GroupedUserModel>>()!;
        grouped.FieldGroups.Count.ShouldBe(1);
        grouped.FieldGroups[0].Columns.ShouldBe(2);
        grouped.FieldGroups[0].Name.ShouldBe("Address");
    }

    #endregion

    #region README - Advanced Field Types

    [Fact]
    public void Readme_AdvancedFieldTypes_Sample_Should_Build()
    {
        // Mirrors README.md "Advanced Field Types"
        var config = FormBuilder<RegistrationModel>.Create()
            // Password field with strength requirements
            .AddPasswordField(x => x.Password, "Password", minLength: 8, requireSpecialChars: true)
            // Password confirmation via a model-aware validator
            .AddField(x => x.ConfirmPassword, field => field
                .WithLabel("Confirm Password")
                .WithInputType("password")
                .Required("Please confirm your password")
                .WithValidator(new PasswordsMatchValidator()))
            // Date picker with validation
            .AddField(x => x.BirthDate, field => field
                .WithLabel("Date of Birth")
                .WithValidator(date => date <= DateTime.Today.AddYears(-18), "Must be 18 or older")
                .WithHelpText("Must be 18 or older"))
            // Multi-line text with character limit
            .AddField(x => x.Description, field => field
                .WithLabel("Description")
                .AsTextArea(lines: 5, maxLength: 500)
                .WithMaxLength(500, "Maximum 500 characters")
                .WithHelpText("Maximum 500 characters"))
            // File upload
            .AddFileUploadField(x => x.Resume, "Upload Resume",
                acceptedFileTypes: new[] { ".pdf", ".doc", ".docx" },
                maxFileSize: 5 * 1024 * 1024)
            // Multiple file upload
            .AddMultipleFileUploadField(x => x.Documents, "Upload Documents",
                maxFiles: 3,
                acceptedFileTypes: new[] { ".pdf", ".jpg", ".png" },
                maxFileSize: 10 * 1024 * 1024)
            .Build();

        config.Fields.Count.ShouldBe(6);
        config.Fields.Single(f => f.FieldName == "Password").InputType.ShouldBe("password");
        config.Fields.Single(f => f.FieldName == "Description")
            .AdditionalAttributes["Lines"].ShouldBe(5);
    }

    [Fact]
    public async Task Readme_PasswordsMatchValidator_Should_Fail_When_Passwords_Differ()
    {
        // Mirrors README.md "Advanced Field Types" (PasswordsMatchValidator)
        var validator = new PasswordsMatchValidator();
        var model = new RegistrationModel { Password = "S3cret!!" };

        var mismatch = await validator.ValidateAsync(model, "different", default!);
        var match = await validator.ValidateAsync(model, "S3cret!!", default!);

        mismatch.IsValid.ShouldBeFalse();
        mismatch.ErrorMessage.ShouldBe("Passwords do not match");
        match.IsValid.ShouldBeTrue();
    }

    public class PasswordsMatchValidator : IFieldValidator<RegistrationModel, string>
    {
        public string? ErrorMessage { get; set; } = "Passwords do not match";

        public Task<ValidationResult> ValidateAsync(
            RegistrationModel model, string value, IServiceProvider services)
            => Task.FromResult(value == model.Password
                ? ValidationResult.Success()
                : ValidationResult.Failure("Passwords do not match"));
    }

    #endregion

    #region README - Conditional Fields

    [Fact]
    public void Readme_ConditionalFields_Sample_Should_Build()
    {
        // Mirrors README.md "Conditional Fields"
        var config = FormBuilder<BusinessModel>.Create()
            .AddField(x => x.CompanyName, field => field
                .WithLabel("Company Name")
                .VisibleWhen(model => model.UserType == UserType.Business))
            .AddField(x => x.TaxId, field => field
                .WithLabel("Tax ID")
                .VisibleWhen(model => model.Country == "US")
                .DisabledWhen(model => model.IsLocked))
            .Build();

        var companyName = config.Fields.Single(f => f.FieldName == "CompanyName");
        companyName.VisibilityCondition.ShouldNotBeNull();
        companyName.VisibilityCondition!(new BusinessModel { UserType = UserType.Business }).ShouldBeTrue();
        companyName.VisibilityCondition!(new BusinessModel { UserType = UserType.Personal }).ShouldBeFalse();

        var taxId = config.Fields.Single(f => f.FieldName == "TaxId");
        taxId.DisabledCondition.ShouldNotBeNull();
        taxId.DisabledCondition!(new BusinessModel { IsLocked = true }).ShouldBeTrue();
    }

    [Fact]
    public async Task Readme_RequiredWhenUsValidator_Should_Fail_Only_For_US_Without_TaxId()
    {
        // Mirrors README.md "Conditional Fields" (conditional requiredness validator)
        var validator = new RequiredWhenUsValidator();

        var usWithoutTaxId = await validator.ValidateAsync(
            new BusinessModel { Country = "US" }, "", default!);
        var usWithTaxId = await validator.ValidateAsync(
            new BusinessModel { Country = "US" }, "12-3456789", default!);
        var nonUsWithoutTaxId = await validator.ValidateAsync(
            new BusinessModel { Country = "BE" }, "", default!);

        usWithoutTaxId.IsValid.ShouldBeFalse();
        usWithTaxId.IsValid.ShouldBeTrue();
        nonUsWithoutTaxId.IsValid.ShouldBeTrue();
    }

    public class RequiredWhenUsValidator : IFieldValidator<BusinessModel, string>
    {
        public string? ErrorMessage { get; set; } = "Tax ID is required for US companies";

        public Task<ValidationResult> ValidateAsync(
            BusinessModel model, string value, IServiceProvider services)
            => Task.FromResult(model.Country == "US" && string.IsNullOrWhiteSpace(value)
                ? ValidationResult.Failure("Tax ID is required for US companies")
                : ValidationResult.Success());
    }

    #endregion

    #region README - Field Groups

    [Fact]
    public void Readme_FieldGroups_Sample_Should_Build()
    {
        // Mirrors README.md "Field Groups"
        var formConfig = FormBuilder<GroupedUserModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Personal Information")
                .WithColumns(2)
                .ShowInCard(2)
                .AddField(x => x.FirstName, field => field
                    .WithLabel("First Name")
                    .Required())
                .AddField(x => x.LastName, field => field
                    .WithLabel("Last Name")
                    .Required())
                .AddField(x => x.DateOfBirth))
            .AddFieldGroup(group => group
                .WithGroupName("Contact Information")
                .WithColumns(3)
                .ShowInCard()
                .AddField(x => x.Email)
                .AddField(x => x.Phone)
                .AddField(x => x.Address))
            .Build();

        var grouped = formConfig.ShouldBeAssignableTo<IGroupedFormConfiguration<GroupedUserModel>>()!;
        grouped.FieldGroups.Count.ShouldBe(2);
        grouped.FieldGroups[0].Columns.ShouldBe(2);
        grouped.FieldGroups[1].Columns.ShouldBe(3);
        formConfig.Fields.Count.ShouldBe(6);
    }

    #endregion

    #region README - Security Features

    [Fact]
    public void Readme_SecurityFeatures_Sample_Should_Build()
    {
        // Mirrors README.md "Security Features (v2.0.0+)"
        var formConfig = FormBuilder<SecureForm>.Create()
            .AddField(x => x.SSN, field => field
                .WithLabel("Social Security Number")
                .WithPlaceholder("XXX-XX-XXXX"))
            .AddField(x => x.CreditCard, field => field
                .WithLabel("Credit Card")
                .WithPlaceholder("XXXX XXXX XXXX XXXX"))
            .WithSecurity(security => security
                .EncryptField(x => x.SSN)
                .EncryptField(x => x.CreditCard)
                .EnableCsrfProtection()
                .WithRateLimit(5, TimeSpan.FromMinutes(1))
                .EnableAuditLogging())
            .Build();

        formConfig.Security.ShouldNotBeNull();
        formConfig.Security!.EncryptedFields.ShouldContain("SSN");
        formConfig.Security.EncryptedFields.ShouldContain("CreditCard");
        formConfig.Security.IsCsrfProtectionEnabled.ShouldBeTrue();
        formConfig.Security.RateLimit.ShouldNotBeNull();
        formConfig.Security.RateLimit!.MaxAttempts.ShouldBe(5);
        formConfig.Security.RateLimit.TimeWindow.ShouldBe(TimeSpan.FromMinutes(1));
        formConfig.Security.IsAuditLoggingEnabled.ShouldBeTrue();
    }

    #endregion

    #region README / docs - Custom Field Renderers

    [Fact]
    public void Readme_CustomRenderer_Sample_Should_Build()
    {
        // Mirrors README.md "Custom Field Renderers" and docs/getting-started.md
        var config = FormBuilder<ProductModel>.Create()
            .AddField(x => x.Color, field => field
                .WithLabel("Product Color")
                .WithCustomRenderer<ProductModel, string, ColorPickerRenderer>()
                .WithHelpText("Select the primary color"))
            .Build();

        config.Fields.Single().CustomRendererType.ShouldBe(typeof(ColorPickerRenderer));
    }

    #endregion

    #region docs/examples.md - Contact Form

    [Fact]
    public void Docs_ContactForm_Sample_Should_Build()
    {
        // Mirrors docs/examples.md "Contact Form"
        var config = FormBuilder<ContactModel>
            .Create()
            .WithLayout(FormLayout.Horizontal)
            .AddRequiredTextField(x => x.FirstName, "First Name", minLength: 2)
            .AddRequiredTextField(x => x.LastName, "Last Name", minLength: 2)
            .AddEmailField(x => x.Email)
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithPlaceholder("(555) 123-4567"))
            .AddDropdownField(x => x.Country, "Country",
                ("US", "United States"),
                ("CA", "Canada"),
                ("UK", "United Kingdom"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .VisibleWhen(m => !string.IsNullOrEmpty(m.Country))
                .DependsOn(x => x.Country, (model, country) =>
                {
                    if (string.IsNullOrEmpty(country))
                    {
                        model.City = "";
                    }
                }))
            .AddCheckboxField(x => x.SubscribeToNewsletter, "Subscribe to newsletter")
            .Build();

        config.Fields.Count.ShouldBe(7);
        config.Layout.ShouldBe(FormLayout.Horizontal);
        config.Fields.Single(f => f.FieldName == "City").Dependencies.Count.ShouldBe(1);
    }

    #endregion

    #region docs/examples.md - Survey Form

    [Fact]
    public void Docs_SurveyForm_Sample_Should_Build()
    {
        // Mirrors docs/examples.md "Survey Form"
        var config = FormBuilder<SurveyModel>
            .Create()
            .AddRequiredTextField(x => x.Name, "Your Name")
            .AddNumericField(x => x.Satisfaction, "Satisfaction (1-10)", 1, 10)
            .AddCheckboxField(x => x.WouldRecommend, "Would you recommend us to others?")
            .AddField(x => x.Feedback, field => field
                .WithLabel("Additional Feedback")
                .WithPlaceholder("Tell us about your experience...")
                .AsTextArea(lines: 4))
            .AddField(x => x.ImprovementSuggestions, field => field
                .WithLabel("Suggestions for Improvement")
                .AsTextArea(lines: 3)
                .VisibleWhen(m => m.Satisfaction < 8))
            .Build();

        config.Fields.Count.ShouldBe(5);
        config.Fields.Single(f => f.FieldName == "Feedback")
            .AdditionalAttributes["Lines"].ShouldBe(4);
    }

    #endregion

    #region docs/examples.md - Auto-Generated Forms

    [Fact]
    public void Docs_AutoGeneratedForm_Sample_Should_Build()
    {
        // Mirrors docs/examples.md "Auto-Generated Forms (Zero Configuration)"
        var config = FormBuilder<AccountSignupModel>
            .Create()
            .AddFieldsAuto()
            .Build();

        config.Fields.Count.ShouldBe(7);
        config.Fields.Single(f => f.FieldName == "FirstName").Label.ShouldBe("First Name");
        config.Fields.Single(f => f.FieldName == "Email").InputType.ShouldBe("email");
        config.Fields.Single(f => f.FieldName == "Password").InputType.ShouldBe("password");
        config.Fields.Single(f => f.FieldName == "ExperienceLevel")
            .AdditionalAttributes.ShouldContainKey("Options");
    }

    [Fact]
    public void Docs_AutoGeneratedForm_Options_Sample_Should_Build()
    {
        // Mirrors docs/examples.md "Auto-Generated Forms" options callback sample
        var config = FormBuilder<AccountSignupModel>
            .Create()
            .AddFieldsAuto(options => options
                .Exclude(x => x.Password)
                .ConfigureField(x => x.FirstName, field => field
                    .WithLabel("Given Name")
                    .Required()))
            .Build();

        config.Fields.ShouldNotContain(f => f.FieldName == "Password");
        config.Fields.Single(f => f.FieldName == "FirstName").Label.ShouldBe("Given Name");
        config.Fields.Single(f => f.FieldName == "FirstName").IsRequired.ShouldBeTrue();
    }

    #endregion

    #region docs/examples.md - Master-Detail Form

    [Fact]
    public void Docs_MasterDetailForm_Sample_Should_Build()
    {
        // Mirrors docs/examples.md "Master-Detail Form (Invoice with Line Items)"
        var config = FormBuilder<InvoiceFormModel>
            .Create()
            .AddField(x => x.InvoiceNumber, field => field
                .WithLabel("Invoice Number")
                .Required())
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Line Items")
                .AllowAdd("Add Line")
                .AllowRemove()
                .WithMinItems(1)
                .WithItemForm(item => item
                    .AddField(x => x.Description, field => field.Required())
                    .AddField(x => x.Quantity, field => field.WithLabel("Quantity"))
                    .AddField(x => x.UnitPrice, field => field.WithLabel("Unit Price"))))
            .AddField(x => x.Subtotal, field => field.WithLabel("Subtotal").ReadOnly())
            .AddField(x => x.Total, field => field.WithLabel("Total").ReadOnly())
            .Build();

        config.Fields.Count.ShouldBe(3);

        var collectionConfig = (ICollectionFormConfiguration<InvoiceFormModel>)config;
        collectionConfig.CollectionFields.Count.ShouldBe(1);

        var items = collectionConfig.CollectionFields.Single();
        items.FieldName.ShouldBe("Items");
        items.MinItems.ShouldBe(1);
        items.CanAdd.ShouldBeTrue();
        items.CanRemove.ShouldBeTrue();

        // Computed totals derive from the line items
        var model = new InvoiceFormModel
        {
            Items = [new InvoiceLineModel { Description = "Consulting", Quantity = 2, UnitPrice = 100m }]
        };
        model.Subtotal.ShouldBe(200m);
        model.Total.ShouldBe(242m); // 21% tax
    }

    #endregion

    #region Test models

    public class UserRegistration
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
        public string Country { get; set; } = "";
        public bool AcceptTerms { get; set; }
    }

    public class AttributeUserRegistration
    {
        [TextField("First Name", "Enter your first name")]
        [Required(ErrorMessage = "First name is required")]
        [MinLength(2)]
        public string FirstName { get; set; } = string.Empty;

        [TextField("Last Name", "Enter your last name")]
        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [EmailField("Email Address")]
        [Required]
        public string Email { get; set; } = string.Empty;

        [NumberField("Age", "Your age")]
        [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
        public int Age { get; set; }

        [DateField("Date of Birth")]
        public DateTime BirthDate { get; set; }

        [SelectField("Country", "United States", "Canada", "United Kingdom", "Australia")]
        public string Country { get; set; } = string.Empty;

        [TextArea("Bio", "Tell us about yourself")]
        [MaxLength(500)]
        public string Bio { get; set; } = string.Empty;

        [CheckboxField("Newsletter", "Subscribe to our newsletter")]
        public bool SubscribeToNewsletter { get; set; }
    }

    public class ComparisonUser
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class OrderForm
    {
        public string ProductType { get; set; } = "";
        public string ProductModel { get; set; } = "";
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class RegistrationModel
    {
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public string Description { get; set; } = "";
        public IBrowserFile Resume { get; set; } = default!;
        public IReadOnlyList<IBrowserFile> Documents { get; set; } = [];
    }

    public enum UserType
    {
        Personal,
        Business
    }

    public class BusinessModel
    {
        public UserType UserType { get; set; }
        public string Country { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string TaxId { get; set; } = "";
        public bool IsLocked { get; set; }
    }

    public class GroupedUserModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string PostalCode { get; set; } = "";
    }

    public class SecureForm
    {
        public string SSN { get; set; } = "";
        public string CreditCard { get; set; } = "";
    }

    public class ProductModel
    {
        public string Color { get; set; } = "";
    }

    public class ContactModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        public bool SubscribeToNewsletter { get; set; }
    }

    public class SurveyModel
    {
        public string Name { get; set; } = "";
        public int Satisfaction { get; set; }
        public bool WouldRecommend { get; set; }
        public string Feedback { get; set; } = "";
        public string ImprovementSuggestions { get; set; } = "";
    }

    public enum ExperienceLevel
    {
        Junior,
        MidLevel,
        Senior
    }

    public class AccountSignupModel
    {
        public string FirstName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public int Age { get; set; }
        public ExperienceLevel ExperienceLevel { get; set; }
        public DateTime StartDate { get; set; }
        public bool AcceptUpdates { get; set; }
    }

    public class InvoiceFormModel
    {
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? InvoiceNumber { get; set; }
        public List<InvoiceLineModel> Items { get; set; } = [new InvoiceLineModel()];
        public decimal TaxRatePercent { get; set; } = 21m;

        // Computed totals stay live: the form re-renders on every line item change.
        public decimal Subtotal => Items.Sum(i => i.Quantity * i.UnitPrice);
        public decimal Total => Subtotal + Math.Round(Subtotal * TaxRatePercent / 100m, 2);
    }

    public class InvoiceLineModel
    {
        public string Description { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    #endregion
}
