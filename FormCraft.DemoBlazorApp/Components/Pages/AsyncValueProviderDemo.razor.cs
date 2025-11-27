using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class AsyncValueProviderDemo : ComponentBase
{
    private AddressModel _model = new();
    private IFormConfiguration<AddressModel>? _formConfig;
    private bool _submitted;
    private bool _isSubmitting;

    // Loading state tracking
    private bool _countriesLoaded;
    private bool _statesLoaded;
    private bool _citiesLoaded;
    private bool _loadingStates;
    private bool _loadingCities;
    private int _stateCount;
    private int _cityCount;

    // Simulated data store
    private static readonly Dictionary<string, List<string>> StatesByCountry = new()
    {
        ["United States"] = ["California", "New York", "Texas", "Florida", "Washington"],
        ["Canada"] = ["Ontario", "Quebec", "British Columbia", "Alberta", "Manitoba"],
        ["United Kingdom"] = ["England", "Scotland", "Wales", "Northern Ireland"],
        ["Germany"] = ["Bavaria", "Berlin", "Hamburg", "Saxony", "Hesse"],
        ["Australia"] = ["New South Wales", "Victoria", "Queensland", "Western Australia"]
    };

    private static readonly Dictionary<string, List<string>> CitiesByState = new()
    {
        // US States
        ["California"] = ["Los Angeles", "San Francisco", "San Diego", "San Jose"],
        ["New York"] = ["New York City", "Buffalo", "Rochester", "Albany"],
        ["Texas"] = ["Houston", "Dallas", "Austin", "San Antonio"],
        ["Florida"] = ["Miami", "Orlando", "Tampa", "Jacksonville"],
        ["Washington"] = ["Seattle", "Spokane", "Tacoma", "Vancouver"],
        // Canada
        ["Ontario"] = ["Toronto", "Ottawa", "Mississauga", "Hamilton"],
        ["Quebec"] = ["Montreal", "Quebec City", "Laval", "Gatineau"],
        ["British Columbia"] = ["Vancouver", "Victoria", "Surrey", "Burnaby"],
        // UK
        ["England"] = ["London", "Manchester", "Birmingham", "Liverpool"],
        ["Scotland"] = ["Edinburgh", "Glasgow", "Aberdeen", "Dundee"],
        // Germany
        ["Bavaria"] = ["Munich", "Nuremberg", "Augsburg", "Regensburg"],
        ["Berlin"] = ["Berlin"],
        // Australia
        ["New South Wales"] = ["Sydney", "Newcastle", "Wollongong", "Central Coast"],
        ["Victoria"] = ["Melbourne", "Geelong", "Ballarat", "Bendigo"]
    };

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "async-value-provider",
        Title = "Async Value Providers",
        Description = "Load field values and options asynchronously from APIs, databases, or other data sources. Perfect for dynamic dropdowns, auto-complete fields, and data that needs to be fetched on demand. Demonstrates cascading dropdowns where selecting one field triggers async loading of options for dependent fields.",
        Icon = Icons.Material.Filled.CloudSync,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.CloudDownload, Color = Color.Primary, Text = "Load options from API" },
            new() { Icon = Icons.Material.Filled.AccountTree, Color = Color.Secondary, Text = "Cascading async updates" },
            new() { Icon = Icons.Material.Filled.Cached, Color = Color.Tertiary, Text = "Caching support for performance" },
            new() { Icon = Icons.Material.Filled.HourglassEmpty, Color = Color.Info, Text = "Loading state indicators" },
            new() { Icon = Icons.Material.Filled.Verified, Color = Color.Success, Text = "Async validation with APIs" },
            new() { Icon = Icons.Material.Filled.ErrorOutline, Color = Color.Warning, Text = "Error handling for failed requests" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "WithAsyncOptionsProvider()", Usage = "Load dropdown options asynchronously", Example = ".WithAsyncOptionsProvider(async (m, s) => await api.GetOptions())" },
            new() { Feature = "WithAsyncValueProvider()", Usage = "Compute value from async source", Example = ".WithAsyncValueProvider(async (m, s) => await api.GetDefault())" },
            new() { Feature = "WithAsyncValidator()", Usage = "Validate against external service", Example = ".WithAsyncValidator(async (v, s) => await api.Validate(v))" },
            new() { Feature = "DependsOn with Async", Usage = "Trigger async load on dependency change", Example = ".DependsOn(x => x.Country, async (m, c) => await LoadStates(c))" },
            new() { Feature = "Loading States", Usage = "Show loading indicators", Example = "Track IsLoading property in component" },
            new() { Feature = "Error Handling", Usage = "Handle API failures gracefully", Example = "try/catch with fallback options" }
        ],
        CodeExamples =
        [
            new() { Title = "Async Options Loading", Language = "csharp", CodeProvider = GetAsyncOptionsCodeStatic },
            new() { Title = "Async Validation", Language = "csharp", CodeProvider = GetAsyncValidationCodeStatic }
        ],
        WhenToUse = "Use async value providers when field options or values must be loaded from external sources like REST APIs, databases, or file systems. Common scenarios include country/state/city cascading dropdowns, dynamic category selection, user search with auto-complete, loading configuration from remote services, and validating unique values against a database. The DependsOn() pattern ensures dependent fields reload when their dependencies change, creating responsive cascading forms.",
        CommonPitfalls =
        [
            "Not handling null/empty selections in dependent fields - always clear dependent values when parent changes",
            "Forgetting to add loading state indicators - users need visual feedback during async operations",
            "Not implementing error handling for failed API calls - network failures should be graceful",
            "Creating circular dependencies between async fields that load each other",
            "Missing StateHasChanged() calls after async operations - UI may not update properly",
            "Not debouncing expensive async validators - can cause performance issues with frequent calls"
        ],
        RelatedDemoIds = ["field-dependencies", "fluent", "cross-field-validation", "async-validation"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<FormGuidelines.GuidelineItem> _sidebarFeatures => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    protected override async Task OnInitializedAsync()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        // Simulate loading countries from API
        await Task.Delay(800);
        _countriesLoaded = true;
        StateHasChanged();

        _formConfig = FormBuilder<AddressModel>
            .Create()
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .WithPlaceholder("Select a country")
                .WithSelectOptions([
                    new("United States", "United States"),
                    new("Canada", "Canada"),
                    new("United Kingdom", "United Kingdom"),
                    new("Germany", "Germany"),
                    new("Australia", "Australia")
                ])
                .Required("Please select a country"))
            .AddField(x => x.State, field => field
                .WithLabel("State/Region")
                .WithPlaceholder("Select a state or region")
                .DependsOn(x => x.Country, async (model, country) =>
                {
                    model.State = "";
                    model.City = "";
                    _statesLoaded = false;
                    _citiesLoaded = false;
                    _cityCount = 0;

                    if (!string.IsNullOrEmpty(country))
                    {
                        _loadingStates = true;
                        StateHasChanged();

                        // Simulate API call
                        await Task.Delay(500);

                        _stateCount = StatesByCountry.GetValueOrDefault(country)?.Count ?? 0;
                        _statesLoaded = true;
                        _loadingStates = false;
                        StateHasChanged();
                    }
                })
                .Required("Please select a state or region"))
            .AddField(x => x.City, field => field
                .WithLabel("City")
                .WithPlaceholder("Select a city")
                .DependsOn(x => x.State, async (model, state) =>
                {
                    model.City = "";
                    _citiesLoaded = false;

                    if (!string.IsNullOrEmpty(state))
                    {
                        _loadingCities = true;
                        StateHasChanged();

                        // Simulate API call
                        await Task.Delay(400);

                        _cityCount = CitiesByState.GetValueOrDefault(state)?.Count ?? 0;
                        _citiesLoaded = true;
                        _loadingCities = false;
                        StateHasChanged();
                    }
                })
                .Required("Please select a city"))
            .AddField(x => x.StreetAddress, field => field
                .WithLabel("Street Address")
                .WithPlaceholder("123 Main Street")
                .Required("Please enter street address"))
            .AddField(x => x.PostalCode, field => field
                .WithLabel("Postal/ZIP Code")
                .WithPlaceholder("Enter postal code")
                .DependsOn(x => x.City, (_, _) => { })
                .WithAsyncValidator(ValidatePostalCodeAsync, "Invalid postal code format")
                .Required("Please enter postal code"))
            .Build();
    }

    private async Task<bool> ValidatePostalCodeAsync(string postalCode)
    {
        // Simulate async validation against external service
        await Task.Delay(300);

        if (string.IsNullOrEmpty(postalCode))
            return true;

        // Simple validation based on country
        return _model.Country switch
        {
            "United States" => postalCode.Length == 5 && postalCode.All(char.IsDigit),
            "Canada" => postalCode.Length >= 6,
            "United Kingdom" => postalCode.Length >= 5,
            _ => postalCode.Length >= 4
        };
    }

    private async Task HandleValidSubmit(AddressModel model)
    {
        _isSubmitting = true;
        StateHasChanged();

        await Task.Delay(1500);

        _submitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new AddressModel();
        _submitted = false;
        _statesLoaded = false;
        _citiesLoaded = false;
        _stateCount = 0;
        _cityCount = 0;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Country", Value = _model.Country },
            new() { Label = "State/Region", Value = _model.State },
            new() { Label = "City", Value = _model.City },
            new() { Label = "Street Address", Value = _model.StreetAddress },
            new() { Label = "Postal Code", Value = _model.PostalCode }
        ];
    }

    // Code Examples
    private string GetAsyncOptionsCodeExample() => GetAsyncOptionsCodeStatic();

    private static string GetAsyncOptionsCodeStatic()
    {
        return """
            // Load dropdown options asynchronously
            .AddField(x => x.Country, field => field
                .WithLabel("Country")
                .WithAsyncOptionsProvider(async (model, services) =>
                {
                    var api = services.GetRequiredService<ICountryApi>();
                    var countries = await api.GetCountriesAsync();
                    return countries.Select(c => new SelectOption(c.Code, c.Name));
                }))

            // Cascade: Load states when country changes
            .AddField(x => x.State, field => field
                .WithLabel("State")
                .DependsOn(x => x.Country, async (model, country) =>
                {
                    // Clear dependent fields
                    model.State = "";
                    model.City = "";

                    // Load states for selected country
                    if (!string.IsNullOrEmpty(country))
                    {
                        var states = await LoadStatesAsync(country);
                        // Options will be refreshed
                    }
                })
                .WithAsyncOptionsProvider(async (model, services) =>
                {
                    if (string.IsNullOrEmpty(model.Country))
                        return [];

                    var api = services.GetRequiredService<ILocationApi>();
                    return await api.GetStatesAsync(model.Country);
                }))
            """;
    }

    private string GetAsyncValidationCodeExample() => GetAsyncValidationCodeStatic();

    private static string GetAsyncValidationCodeStatic()
    {
        return """
            // Async validation with external API
            .AddField(x => x.PostalCode, field => field
                .WithLabel("Postal Code")
                .WithAsyncValidator(async (postalCode, services) =>
                {
                    var api = services.GetRequiredService<IPostalCodeApi>();
                    var result = await api.ValidateAsync(postalCode);
                    return result.IsValid
                        ? ValidationResult.Success()
                        : ValidationResult.Error(result.Message);
                }))

            // Async validation with debounce for expensive calls
            .AddField(x => x.Username, field => field
                .WithLabel("Username")
                .WithAsyncValidator(async (username, services) =>
                {
                    // Check uniqueness against database
                    var userService = services.GetRequiredService<IUserService>();
                    var isAvailable = await userService.CheckUsernameAsync(username);
                    return isAvailable
                        ? ValidationResult.Success()
                        : ValidationResult.Error("Username already taken");
                })
                .WithDebounce(500)) // Wait 500ms before validating
            """;
    }

    // Model class
    public class AddressModel
    {
        public string Country { get; set; } = "";
        public string State { get; set; } = "";
        public string City { get; set; } = "";
        public string StreetAddress { get; set; } = "";
        public string PostalCode { get; set; } = "";
    }
}
