using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class LovFieldDemo
{
    private OrderFormModel _model = new();
    private IFormConfiguration<OrderFormModel>? _formConfiguration;
    private bool _isSubmitting;
    private bool _isSubmitted;

    // Sample customer data for the LOV
    private static readonly List<CustomerModel> _customers =
    [
        new() { Id = 1, Code = "CUST001", Name = "Acme Corporation", Email = "contact@acme.com", Phone = "+1-555-0101", City = "New York", Country = "USA", CreditLimit = 50000, IsActive = true },
        new() { Id = 2, Code = "CUST002", Name = "TechStart Inc", Email = "info@techstart.io", Phone = "+1-555-0102", City = "San Francisco", Country = "USA", CreditLimit = 75000, IsActive = true },
        new() { Id = 3, Code = "CUST003", Name = "Global Supplies Ltd", Email = "sales@globalsupplies.co.uk", Phone = "+44-20-5550103", City = "London", Country = "UK", CreditLimit = 100000, IsActive = true },
        new() { Id = 4, Code = "CUST004", Name = "Nordic Solutions AB", Email = "hello@nordicsolutions.se", Phone = "+46-8-5550104", City = "Stockholm", Country = "Sweden", CreditLimit = 60000, IsActive = true },
        new() { Id = 5, Code = "CUST005", Name = "Pacific Trading Co", Email = "orders@pacifictrading.com.au", Phone = "+61-2-5550105", City = "Sydney", Country = "Australia", CreditLimit = 80000, IsActive = true },
        new() { Id = 6, Code = "CUST006", Name = "Berlin Tech GmbH", Email = "kontakt@berlintech.de", Phone = "+49-30-5550106", City = "Berlin", Country = "Germany", CreditLimit = 90000, IsActive = true },
        new() { Id = 7, Code = "CUST007", Name = "Tokyo Industries", Email = "info@tokyoind.jp", Phone = "+81-3-5550107", City = "Tokyo", Country = "Japan", CreditLimit = 120000, IsActive = true },
        new() { Id = 8, Code = "CUST008", Name = "Maple Consulting", Email = "support@mapleconsulting.ca", Phone = "+1-416-5550108", City = "Toronto", Country = "Canada", CreditLimit = 45000, IsActive = true },
        new() { Id = 9, Code = "CUST009", Name = "Paris Designs SARL", Email = "contact@parisdesigns.fr", Phone = "+33-1-5550109", City = "Paris", Country = "France", CreditLimit = 55000, IsActive = true },
        new() { Id = 10, Code = "CUST010", Name = "Mumbai Exports Pvt", Email = "sales@mumbaiexports.in", Phone = "+91-22-5550110", City = "Mumbai", Country = "India", CreditLimit = 35000, IsActive = true },
        new() { Id = 11, Code = "CUST011", Name = "Rio Services Ltda", Email = "atendimento@rioservices.com.br", Phone = "+55-21-5550111", City = "Rio de Janeiro", Country = "Brazil", CreditLimit = 40000, IsActive = true },
        new() { Id = 12, Code = "CUST012", Name = "Singapore Trading Pte", Email = "enquiry@sgtrading.sg", Phone = "+65-6555-0112", City = "Singapore", Country = "Singapore", CreditLimit = 85000, IsActive = true },
        new() { Id = 13, Code = "CUST013", Name = "Dubai Ventures LLC", Email = "info@dubaiventures.ae", Phone = "+971-4-5550113", City = "Dubai", Country = "UAE", CreditLimit = 150000, IsActive = true },
        new() { Id = 14, Code = "CUST014", Name = "Amsterdam Digital BV", Email = "hello@amsterdamdigital.nl", Phone = "+31-20-5550114", City = "Amsterdam", Country = "Netherlands", CreditLimit = 70000, IsActive = true },
        new() { Id = 15, Code = "CUST015", Name = "Seoul Systems Co", Email = "support@seoulsystems.kr", Phone = "+82-2-5550115", City = "Seoul", Country = "South Korea", CreditLimit = 95000, IsActive = true }
    ];

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "AsLov<TModel, TValue, TItem>",
            Usage = "Configure a field as a LOV selector",
            Example = ".AsLov<OrderModel, int, Customer>(lov => ...)"
        },
        new()
        {
            Feature = "WithDataSource",
            Usage = "Provide data from a collection or async function",
            Example = ".WithDataSource(() => customers)"
        },
        new()
        {
            Feature = "WithKey / WithDisplay",
            Usage = "Configure which properties to use for value and display",
            Example = ".WithKey(c => c.Id).WithDisplay(c => c.Name)"
        },
        new()
        {
            Feature = "AddColumn",
            Usage = "Add columns to the selection table with optional configuration",
            Example = ".AddColumn(c => c.Name, \"Name\", col => col.Width(\"100px\"))"
        },
        new()
        {
            Feature = "MapField",
            Usage = "Auto-populate form fields from selection",
            Example = ".MapField(c => c.Email, m => m.CustomerEmail)"
        },
        new()
        {
            Feature = "WithModalTitle / WithModalSize",
            Usage = "Configure the modal dialog appearance",
            Example = ".WithModalTitle(\"Select\").WithModalSize(LovModalSize.Large)"
        },
        new()
        {
            Feature = "WithDataService<T>",
            Usage = "Use a DI-registered service for data",
            Example = ".WithDataService<ICustomerLovService>()"
        }
    ];

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new() { Text = "Modal table picker for large datasets", Icon = Icons.Material.Filled.TableChart },
        new() { Text = "Built-in search and pagination", Icon = Icons.Material.Filled.Search },
        new() { Text = "Field mapping for auto-population", Icon = Icons.Material.Filled.Link },
        new() { Text = "Customizable columns and display", Icon = Icons.Material.Filled.ViewColumn },
        new() { Text = "Supports single and multi-select modes", Icon = Icons.Material.Filled.CheckBox },
        new() { Text = "Works with any data source", Icon = Icons.Material.Filled.Storage }
    ];

    protected override void OnInitialized()
    {
        _formConfiguration = FormBuilder<OrderFormModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Customer Selection")
                .WithColumns(1)
                .ShowInCard()
                .AddField(x => x.CustomerId, field => field
                    .WithLabel("Customer")
                    .Required()
                    .WithPlaceholder("Click search to select a customer")
                    .AsLov<OrderFormModel, int?, CustomerModel>(lov => lov
                        .WithDataSource(() => _customers)
                        .WithKey(c => c.Id)
                        .WithDisplay((Func<CustomerModel, string>)(c => $"{c.Code} - {c.Name}"))
                        .AddColumn(c => c.Code, "Code", col => col.Width("100px"))
                        .AddColumn(c => c.Name, "Company Name", col => col.Width("200px"))
                        .AddColumn(c => c.City, "City")
                        .AddColumn(c => c.Country, "Country")
                        .AddColumn(c => c.CreditLimit, "Credit Limit", col => col.Template(c => c.CreditLimit.ToString("C0")))
                        .MapField(c => c.Name, m => m.CustomerName)
                        .MapField(c => c.Email, m => m.CustomerEmail)
                        .MapField(c => c.Phone, m => m.CustomerPhone)
                        .MapField(c => c.City, m => m.CustomerCity)
                        .WithModalTitle("Select Customer")
                        .WithModalSize(LovModalSize.Large))))
            .AddFieldGroup(group => group
                .WithGroupName("Customer Details (Auto-filled)")
                .WithColumns(2)
                .AddField(x => x.CustomerName, field => field
                    .WithLabel("Customer Name")
                    .ReadOnly()
                    .WithPlaceholder("Auto-filled from selection"))
                .AddField(x => x.CustomerEmail, field => field
                    .WithLabel("Email")
                    .ReadOnly()
                    .WithPlaceholder("Auto-filled from selection"))
                .AddField(x => x.CustomerPhone, field => field
                    .WithLabel("Phone")
                    .ReadOnly()
                    .WithPlaceholder("Auto-filled from selection"))
                .AddField(x => x.CustomerCity, field => field
                    .WithLabel("City")
                    .ReadOnly()
                    .WithPlaceholder("Auto-filled from selection")))
            .AddFieldGroup(group => group
                .WithGroupName("Order Details")
                .WithColumns(2)
                .AddField(x => x.OrderReference, field => field
                    .WithLabel("Order Reference")
                    .WithPlaceholder("e.g., ORD-2024-001"))
                .AddField(x => x.OrderDate, field => field
                    .WithLabel("Order Date")))
            .AddField(x => x.Notes, field => field
                .AsTextArea(lines: 3)
                .WithLabel("Notes")
                .WithPlaceholder("Additional order notes..."))
            .Build();
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        StateHasChanged();

        // Simulate API call
        await Task.Delay(1500);

        _isSubmitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new OrderFormModel();
        _isSubmitted = false;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Customer ID", Value = _model.CustomerId?.ToString() ?? "Not selected" },
            new() { Label = "Customer Name", Value = _model.CustomerName ?? "-" },
            new() { Label = "Customer Email", Value = _model.CustomerEmail ?? "-" },
            new() { Label = "Customer Phone", Value = _model.CustomerPhone ?? "-" },
            new() { Label = "Customer City", Value = _model.CustomerCity ?? "-" },
            new() { Label = "Order Reference", Value = _model.OrderReference ?? "-" },
            new() { Label = "Order Date", Value = _model.OrderDate.ToShortDateString() },
            new() { Label = "Notes", Value = string.IsNullOrWhiteSpace(_model.Notes) ? "-" : _model.Notes }
        ];
    }

    private static string GetBasicExampleCode()
    {
        return """
            // Basic LOV field configuration
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .Required()
                .AsLov<OrderFormModel, int?, CustomerModel>(lov => lov
                    .WithDataSource(() => customers)  // Provide data source
                    .WithKey(c => c.Id)               // Value to store
                    .WithDisplay(c => c.Name)         // Text to display
                    .AddColumn(c => c.Code, "Code")   // Table columns
                    .AddColumn(c => c.Name, "Name")
                    .AddColumn(c => c.City, "City")))
            """;
    }

    private static string GetFieldMappingExampleCode()
    {
        return """
            // LOV with field mapping to auto-populate related fields
            .AsLov<OrderFormModel, int?, CustomerModel>(lov => lov
                .WithDataSource(() => customers)
                .WithKey(c => c.Id)
                .WithDisplay((Func<CustomerModel, string>)(c => $"{c.Code} - {c.Name}"))

                // Configure table columns with width and formatting
                .AddColumn(c => c.Code, "Code", col => col.Width("100px"))
                .AddColumn(c => c.Name, "Company Name", col => col.Width("200px"))
                .AddColumn(c => c.City, "City")
                .AddColumn(c => c.Country, "Country")
                .AddColumn(c => c.CreditLimit, "Credit Limit",
                    col => col.Template(c => c.CreditLimit.ToString("C0")))

                // Map selected item properties to form fields
                .MapField(c => c.Name, m => m.CustomerName)
                .MapField(c => c.Email, m => m.CustomerEmail)
                .MapField(c => c.Phone, m => m.CustomerPhone)
                .MapField(c => c.City, m => m.CustomerCity)

                // Modal dialog options
                .WithModalTitle("Select Customer")
                .WithModalSize(LovModalSize.Large)))
            """;
    }
}
