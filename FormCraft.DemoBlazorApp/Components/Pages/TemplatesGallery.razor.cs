using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class TemplatesGallery
{
    private string _selectedTemplate = "";

    // Template models
    private LoginModel _loginModel = new();
    private RegistrationModel _registrationModel = new();
    private ContactFormModel _contactModel = new();
    private ProfileModel _profileModel = new();
    private CheckoutModel _checkoutModel = new();
    private FeedbackModel _feedbackModel = new();

    // Template configurations
    private IFormConfiguration<LoginModel>? _loginConfig;
    private IFormConfiguration<RegistrationModel>? _registrationConfig;
    private IFormConfiguration<ContactFormModel>? _contactConfig;
    private IFormConfiguration<ProfileModel>? _profileConfig;
    private IFormConfiguration<CheckoutModel>? _checkoutConfig;
    private IFormConfiguration<FeedbackModel>? _feedbackConfig;

    private readonly List<TemplateInfo> _templates =
    [
        new()
        {
            Id = "login",
            Name = "Login Form",
            Category = "Authentication",
            Description = "Simple login form with email and password fields, remember me option.",
            Icon = Icons.Material.Filled.Login,
            Color = Color.Primary,
            Tags = ["auth", "security", "basic"]
        },
        new()
        {
            Id = "registration",
            Name = "Registration Form",
            Category = "Authentication",
            Description = "User registration with password confirmation, terms acceptance, and validation.",
            Icon = Icons.Material.Filled.PersonAdd,
            Color = Color.Secondary,
            Tags = ["auth", "onboarding", "validation"]
        },
        new()
        {
            Id = "contact",
            Name = "Contact Form",
            Category = "Communication",
            Description = "Standard contact form with name, email, subject, and message fields.",
            Icon = Icons.Material.Filled.ContactMail,
            Color = Color.Tertiary,
            Tags = ["communication", "basic", "email"]
        },
        new()
        {
            Id = "profile",
            Name = "User Profile",
            Category = "User Management",
            Description = "Complete user profile form with personal info, bio, and preferences.",
            Icon = Icons.Material.Filled.Person,
            Color = Color.Info,
            Tags = ["profile", "settings", "preferences"]
        },
        new()
        {
            Id = "checkout",
            Name = "Checkout Form",
            Category = "E-Commerce",
            Description = "Checkout form with shipping address, billing info, and payment details.",
            Icon = Icons.Material.Filled.ShoppingCart,
            Color = Color.Success,
            Tags = ["e-commerce", "payment", "address"]
        },
        new()
        {
            Id = "feedback",
            Name = "Feedback Form",
            Category = "Communication",
            Description = "Customer feedback form with rating, category selection, and comments.",
            Icon = Icons.Material.Filled.Feedback,
            Color = Color.Warning,
            Tags = ["feedback", "survey", "rating"]
        }
    ];

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "templates-gallery",
        Title = "Form Templates Gallery",
        Description = "Ready-to-use form templates for common scenarios. Copy and customize these templates to accelerate your development.",
        Icon = Icons.Material.Filled.Dashboard,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Dashboard, Color = Color.Primary, Text = "Pre-built templates for common use cases" },
            new() { Icon = Icons.Material.Filled.ContentCopy, Color = Color.Secondary, Text = "Copy-paste ready code examples" },
            new() { Icon = Icons.Material.Filled.Tune, Color = Color.Tertiary, Text = "Customizable and extendable" },
            new() { Icon = Icons.Material.Filled.Verified, Color = Color.Info, Text = "Best practices built-in" },
            new() { Icon = Icons.Material.Filled.Speed, Color = Color.Success, Text = "Accelerate development" },
            new() { Icon = Icons.Material.Filled.Security, Color = Color.Warning, Text = "Validation included" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "Template Pattern", Usage = "Create static factory methods for reusable templates", Example = "public static IFormConfiguration<T> CreateLoginForm<T>()" },
            new() { Feature = "Generic Templates", Usage = "Use generics for flexible templates", Example = "public static FormBuilder<T> AddAddressFields<T>(...)" },
            new() { Feature = "Extension Methods", Usage = "Extend FormBuilder for common patterns", Example = "builder.AddEmailField(x => x.Email)" },
            new() { Feature = "Composable Templates", Usage = "Combine smaller templates into larger forms", Example = "builder.AddPersonalInfo().AddContactInfo()" },
            new() { Feature = "Configuration Options", Usage = "Allow customization through parameters", Example = "CreateForm(required: true, showHelp: true)" }
        ],
        CodeExamples =
        [
            new() { Title = "Creating Reusable Templates", Language = "csharp", CodeProvider = GetTemplatePatternCodeStatic }
        ],
        WhenToUse = "Use pre-built templates when you need common form patterns quickly. Templates provide a solid starting point with validation, proper field configuration, and best practices already implemented. Customize them to fit your specific requirements.",
        CommonPitfalls =
        [
            "Don't use templates without understanding the validation rules - customize as needed",
            "Remember to match your model properties with the template expectations",
            "Consider accessibility requirements when modifying templates",
            "Test templates with your specific data requirements"
        ],
        RelatedDemoIds = ["fluent", "improved", "field-groups"]
    };

    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        // Initialize all template configurations
        _loginConfig = CreateLoginFormConfig();
        _registrationConfig = CreateRegistrationFormConfig();
        _contactConfig = CreateContactFormConfig();
        _profileConfig = CreateProfileFormConfig();
        _checkoutConfig = CreateCheckoutFormConfig();
        _feedbackConfig = CreateFeedbackFormConfig();
    }

    private void SelectTemplate(string templateId)
    {
        _selectedTemplate = templateId;
        StateHasChanged();
    }

    private static IFormConfiguration<LoginModel> CreateLoginFormConfig()
    {
        return FormBuilder<LoginModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email Address")
                .WithPlaceholder("your@email.com")
                .WithEmailValidation("Please enter a valid email address")
                .Required("Email is required"))
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithInputType("password")
                .Required("Password is required"))
            .AddField(x => x.RememberMe, field => field
                .WithLabel("Remember me"))
            .Build();
    }

    private static IFormConfiguration<RegistrationModel> CreateRegistrationFormConfig()
    {
        return FormBuilder<RegistrationModel>
            .Create()
            .AddField(x => x.FullName, field => field
                .WithLabel("Full Name")
                .WithPlaceholder("John Doe")
                .Required("Name is required")
                .WithMinLength(2, "Name must be at least 2 characters"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email Address")
                .WithPlaceholder("your@email.com")
                .WithEmailValidation()
                .Required("Email is required"))
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithInputType("password")
                .Required("Password is required")
                .WithMinLength(8, "Password must be at least 8 characters"))
            .AddField(x => x.ConfirmPassword, field => field
                .WithLabel("Confirm Password")
                .WithInputType("password")
                .Required("Please confirm your password"))
            .AddField(x => x.AcceptTerms, field => field
                .WithLabel("I accept the terms and conditions")
                .WithValidator(v => v, "You must accept the terms to continue"))
            .Build();
    }

    private static IFormConfiguration<ContactFormModel> CreateContactFormConfig()
    {
        return FormBuilder<ContactFormModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Your Name")
                .WithPlaceholder("John Doe")
                .Required("Name is required"))
            .AddField(x => x.Email, field => field
                .WithLabel("Email Address")
                .WithPlaceholder("your@email.com")
                .WithEmailValidation()
                .Required("Email is required"))
            .AddField(x => x.Subject, field => field
                .WithLabel("Subject")
                .WithSelectOptions([
                    new("general", "General Inquiry"),
                    new("support", "Technical Support"),
                    new("sales", "Sales Question"),
                    new("feedback", "Feedback")
                ])
                .Required("Please select a subject"))
            .AddField(x => x.Message, field => field
                .WithLabel("Message")
                .WithPlaceholder("Your message here...")
                .AsTextArea(lines: 5)
                .Required("Message is required")
                .WithMinLength(10, "Please provide more detail (at least 10 characters)"))
            .Build();
    }

    private static IFormConfiguration<ProfileModel> CreateProfileFormConfig()
    {
        return FormBuilder<ProfileModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Personal Information")
                .WithColumns(2)
                .ShowInCard()
                .AddField(x => x.FirstName, f => f
                    .WithLabel("First Name")
                    .Required())
                .AddField(x => x.LastName, f => f
                    .WithLabel("Last Name")
                    .Required()))
            .AddField(x => x.Bio, field => field
                .WithLabel("Biography")
                .WithPlaceholder("Tell us about yourself...")
                .AsTextArea(lines: 3)
                .WithMaxLength(500, "Bio cannot exceed 500 characters"))
            .AddField(x => x.Website, field => field
                .WithLabel("Website")
                .WithPlaceholder("https://example.com"))
            .AddField(x => x.NotificationPreference, field => field
                .WithLabel("Email Notifications")
                .WithSelectOptions([
                    new("all", "All Notifications"),
                    new("important", "Important Only"),
                    new("none", "No Notifications")
                ]))
            .Build();
    }

    private static IFormConfiguration<CheckoutModel> CreateCheckoutFormConfig()
    {
        return FormBuilder<CheckoutModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Shipping Address")
                .WithColumns(2)
                .ShowInCard()
                .AddField(x => x.StreetAddress, f => f
                    .WithLabel("Street Address")
                    .Required())
                .AddField(x => x.City, f => f
                    .WithLabel("City")
                    .Required())
                .AddField(x => x.State, f => f
                    .WithLabel("State/Province")
                    .Required())
                .AddField(x => x.PostalCode, f => f
                    .WithLabel("Postal Code")
                    .Required()))
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .WithSelectOptions([
                    new("US", "United States"),
                    new("CA", "Canada"),
                    new("UK", "United Kingdom"),
                    new("AU", "Australia")
                ])
                .Required("Please select a country"))
            .AddField(x => x.SameAsBilling, field => field
                .WithLabel("Billing address same as shipping"))
            .Build();
    }

    private static IFormConfiguration<FeedbackModel> CreateFeedbackFormConfig()
    {
        return FormBuilder<FeedbackModel>
            .Create()
            .AddField(x => x.Category, field => field
                .WithLabel("Feedback Category")
                .WithSelectOptions([
                    new("product", "Product"),
                    new("service", "Customer Service"),
                    new("website", "Website"),
                    new("other", "Other")
                ])
                .Required("Please select a category"))
            .AddField(x => x.Rating, field => field
                .WithLabel("Overall Rating (1-5)")
                .Required("Please provide a rating")
                .WithRange(1, 5, "Rating must be between 1 and 5"))
            .AddField(x => x.WouldRecommend, field => field
                .WithLabel("Would you recommend us to others?"))
            .AddField(x => x.Comments, field => field
                .WithLabel("Additional Comments")
                .WithPlaceholder("Share your thoughts...")
                .AsTextArea(lines: 4)
                .WithHelpText("Your feedback helps us improve"))
            .Build();
    }

    private string GetSelectedTemplateCode()
    {
        return _selectedTemplate switch
        {
            "login" => GetLoginTemplateCode(),
            "registration" => GetRegistrationTemplateCode(),
            "contact" => GetContactTemplateCode(),
            "profile" => GetProfileTemplateCode(),
            "checkout" => GetCheckoutTemplateCode(),
            "feedback" => GetFeedbackTemplateCode(),
            _ => "Select a template to view its code."
        };
    }

    private static string GetLoginTemplateCode() => """
        public class LoginModel
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public bool RememberMe { get; set; }
        }

        var config = FormBuilder<LoginModel>
            .Create()
            .AddField(x => x.Email, field => field
                .WithLabel("Email Address")
                .WithPlaceholder("your@email.com")
                .WithEmailValidation("Please enter a valid email")
                .Required("Email is required"))
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithInputType("password")
                .Required("Password is required"))
            .AddField(x => x.RememberMe, field => field
                .WithLabel("Remember me"))
            .Build();
        """;

    private static string GetRegistrationTemplateCode() => """
        public class RegistrationModel
        {
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
            public bool AcceptTerms { get; set; }
        }

        var config = FormBuilder<RegistrationModel>
            .Create()
            .AddField(x => x.FullName, field => field
                .WithLabel("Full Name")
                .Required()
                .WithMinLength(2))
            .AddField(x => x.Email, field => field
                .WithLabel("Email Address")
                .WithEmailValidation()
                .Required())
            .AddField(x => x.Password, field => field
                .WithLabel("Password")
                .WithInputType("password")
                .Required()
                .WithMinLength(8, "At least 8 characters"))
            .AddField(x => x.ConfirmPassword, field => field
                .WithLabel("Confirm Password")
                .WithInputType("password")
                .Required())
            .AddField(x => x.AcceptTerms, field => field
                .WithLabel("I accept the terms")
                .WithValidator(v => v, "You must accept terms"))
            .Build();
        """;

    private static string GetContactTemplateCode() => """
        public class ContactFormModel
        {
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string Subject { get; set; } = "";
            public string Message { get; set; } = "";
        }

        var config = FormBuilder<ContactFormModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Your Name")
                .Required())
            .AddField(x => x.Email, field => field
                .WithLabel("Email Address")
                .WithEmailValidation()
                .Required())
            .AddField(x => x.Subject, field => field
                .WithLabel("Subject")
                .WithSelectOptions([
                    new("general", "General Inquiry"),
                    new("support", "Technical Support"),
                    new("sales", "Sales Question")
                ])
                .Required())
            .AddField(x => x.Message, field => field
                .WithLabel("Message")
                .AsTextArea(lines: 5)
                .Required()
                .WithMinLength(10))
            .Build();
        """;

    private static string GetProfileTemplateCode() => """
        public class ProfileModel
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Bio { get; set; } = "";
            public string Website { get; set; } = "";
            public string NotificationPreference { get; set; } = "all";
        }

        var config = FormBuilder<ProfileModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Personal Information")
                .WithColumns(2)
                .ShowInCard()
                .AddField(x => x.FirstName, f => f
                    .WithLabel("First Name")
                    .Required())
                .AddField(x => x.LastName, f => f
                    .WithLabel("Last Name")
                    .Required()))
            .AddField(x => x.Bio, field => field
                .WithLabel("Biography")
                .AsTextArea(lines: 3)
                .WithMaxLength(500))
            .AddField(x => x.Website, field => field
                .WithLabel("Website"))
            .AddField(x => x.NotificationPreference, field => field
                .WithLabel("Email Notifications")
                .WithSelectOptions([...]))
            .Build();
        """;

    private static string GetCheckoutTemplateCode() => """
        public class CheckoutModel
        {
            public string StreetAddress { get; set; } = "";
            public string City { get; set; } = "";
            public string State { get; set; } = "";
            public string PostalCode { get; set; } = "";
            public string Country { get; set; } = "";
            public bool SameAsBilling { get; set; } = true;
        }

        var config = FormBuilder<CheckoutModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Shipping Address")
                .WithColumns(2)
                .ShowInCard()
                .AddField(x => x.StreetAddress, f => f
                    .WithLabel("Street Address")
                    .Required())
                .AddField(x => x.City, f => f.WithLabel("City").Required())
                .AddField(x => x.State, f => f.WithLabel("State").Required())
                .AddField(x => x.PostalCode, f => f
                    .WithLabel("Postal Code")
                    .Required()))
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .WithSelectOptions([...])
                .Required())
            .AddField(x => x.SameAsBilling, field => field
                .WithLabel("Billing same as shipping"))
            .Build();
        """;

    private static string GetFeedbackTemplateCode() => """
        public class FeedbackModel
        {
            public string Category { get; set; } = "";
            public int Rating { get; set; }
            public bool WouldRecommend { get; set; }
            public string Comments { get; set; } = "";
        }

        var config = FormBuilder<FeedbackModel>
            .Create()
            .AddField(x => x.Category, field => field
                .WithLabel("Feedback Category")
                .WithSelectOptions([
                    new("product", "Product"),
                    new("service", "Customer Service"),
                    new("website", "Website")
                ])
                .Required())
            .AddField(x => x.Rating, field => field
                .WithLabel("Overall Rating (1-5)")
                .Required()
                .WithRange(1, 5))
            .AddField(x => x.WouldRecommend, field => field
                .WithLabel("Would you recommend us?"))
            .AddField(x => x.Comments, field => field
                .WithLabel("Additional Comments")
                .AsTextArea(lines: 4))
            .Build();
        """;

    private string GetTemplatePatternCode() => GetTemplatePatternCodeStatic();

    private static string GetTemplatePatternCodeStatic() => """
        // Create reusable template classes
        public static class FormTemplates
        {
            // Simple factory method
            public static IFormConfiguration<LoginModel> CreateLoginForm()
            {
                return FormBuilder<LoginModel>
                    .Create()
                    .AddField(x => x.Email, f => f
                        .WithLabel("Email")
                        .WithEmailValidation()
                        .Required())
                    .AddField(x => x.Password, f => f
                        .WithLabel("Password")
                        .WithInputType("password")
                        .Required())
                    .Build();
            }

            // Parameterized template
            public static IFormConfiguration<T> CreateAddressForm<T>(
                Expression<Func<T, string>> streetExpr,
                Expression<Func<T, string>> cityExpr,
                Expression<Func<T, string>> zipExpr,
                bool required = true) where T : new()
            {
                var builder = FormBuilder<T>.Create();

                builder.AddField(streetExpr, f => {
                    f.WithLabel("Street Address");
                    if (required) f.Required();
                });
                builder.AddField(cityExpr, f => {
                    f.WithLabel("City");
                    if (required) f.Required();
                });
                builder.AddField(zipExpr, f => {
                    f.WithLabel("ZIP Code");
                    if (required) f.Required();
                });

                return builder.Build();
            }
        }

        // Usage
        var loginForm = FormTemplates.CreateLoginForm();
        var addressForm = FormTemplates.CreateAddressForm<CustomerModel>(
            x => x.Street, x => x.City, x => x.Zip);
        """;

    // Submit handlers
    private async Task HandleLoginSubmit(LoginModel model)
    {
        await Task.Delay(1000);
        _loginModel = new LoginModel();
    }

    private async Task HandleRegistrationSubmit(RegistrationModel model)
    {
        await Task.Delay(1000);
        _registrationModel = new RegistrationModel();
    }

    private async Task HandleContactSubmit(ContactFormModel model)
    {
        await Task.Delay(1000);
        _contactModel = new ContactFormModel();
    }

    private async Task HandleProfileSubmit(ProfileModel model)
    {
        await Task.Delay(1000);
        _profileModel = new ProfileModel();
    }

    private async Task HandleCheckoutSubmit(CheckoutModel model)
    {
        await Task.Delay(1000);
        _checkoutModel = new CheckoutModel();
    }

    private async Task HandleFeedbackSubmit(FeedbackModel model)
    {
        await Task.Delay(1000);
        _feedbackModel = new FeedbackModel();
    }

    // Template info record
    private record TemplateInfo
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Category { get; init; }
        public required string Description { get; init; }
        public required string Icon { get; init; }
        public required Color Color { get; init; }
        public required string[] Tags { get; init; }
    }

    // Model classes
    public class LoginModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public class RegistrationModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public bool AcceptTerms { get; set; }
    }

    public class ContactFormModel
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class ProfileModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Bio { get; set; } = "";
        public string Website { get; set; } = "";
        public string NotificationPreference { get; set; } = "all";
    }

    public class CheckoutModel
    {
        public string StreetAddress { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string PostalCode { get; set; } = "";
        public string Country { get; set; } = "";
        public bool SameAsBilling { get; set; } = true;
    }

    public class FeedbackModel
    {
        public string Category { get; set; } = "";
        public int Rating { get; set; }
        public bool WouldRecommend { get; set; }
        public string Comments { get; set; } = "";
    }
}
