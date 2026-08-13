using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class MasterDetailDemo
{
    private InvoiceFormModel _model = new();
    private bool _isSubmitted;
    private bool _isSubmitting;
    private IFormConfiguration<InvoiceFormModel> _formConfiguration = null!;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "master-detail",
        Title = "Master-Detail Form",
        Description = "Demonstrates a realistic master-detail data-entry form: an invoice (master) whose customer is selected through a LOV lookup with auto-filled fields, line items (detail) managed with AddCollectionField including add/remove/reorder, and totals that recalculate live - computed model properties for collection-driven values and DependsOn() for scalar-driven ones. Submitting shows the full aggregate.",
        Icon = Icons.Material.Filled.TableView,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.PersonSearch, Color = Color.Primary, Text = "Customer LOV lookup with MapField auto-fill" },
            new() { Icon = Icons.Material.Filled.PlaylistAdd, Color = Color.Secondary, Text = "Line items with add, remove, and reorder" },
            new() { Icon = Icons.Material.Filled.Calculate, Color = Color.Tertiary, Text = "Live subtotal, tax, and total computation" },
            new() { Icon = Icons.Material.Filled.Link, Color = Color.Info, Text = "DependsOn() for reactive deposit calculation" },
            new() { Icon = Icons.Material.Filled.Rule, Color = Color.Success, Text = "Minimum item count enforced (at least 1 line)" },
            new() { Icon = Icons.Material.Filled.Receipt, Color = Color.Warning, Text = "Submit produces the complete invoice aggregate" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "AddCollectionField()", Usage = "Manage a one-to-many detail collection inside the form", Example = ".AddCollectionField(x => x.Items, c => c.AllowAdd().AllowRemove())" },
            new() { Feature = "WithItemForm()", Usage = "Configure the sub-form rendered for each detail row", Example = ".WithItemForm(item => item.AddField(x => x.Description))" },
            new() { Feature = "WithMinItems() / WithMaxItems()", Usage = "Constrain how many detail rows are allowed", Example = ".WithMinItems(1).WithMaxItems(20)" },
            new() { Feature = "AsLov()", Usage = "Select the master's parent entity from a large dataset", Example = ".AsLov<InvoiceFormModel, int?, CustomerModel>(lov => ...)" },
            new() { Feature = "MapField()", Usage = "Auto-populate master fields from the LOV selection", Example = ".MapField(c => c.Name, m => m.CustomerName)" },
            new() { Feature = "DependsOn()", Usage = "Recalculate a field when another scalar field changes", Example = ".DependsOn(x => x.DepositPercent, (m, pct) => m.DepositDue = ...)" },
            new() { Feature = "Computed Properties", Usage = "Collection-driven totals as get-only model properties stay live on every item edit", Example = "public decimal Subtotal => Items.Sum(i => i.Quantity * i.UnitPrice);" }
        ],
        CodeExamples =
        [
            new() { Title = "Master: Customer LOV With Field Mapping", Language = "csharp", CodeProvider = GetMasterExampleCodeStatic },
            new() { Title = "Detail: Line Items With AddCollectionField", Language = "csharp", CodeProvider = GetDetailExampleCodeStatic },
            new() { Title = "Computed Totals and DependsOn", Language = "csharp", CodeProvider = GetTotalsExampleCodeStatic }
        ],
        WhenToUse = "Use this pattern for any parent-child data entry: invoices with line items, orders with order lines, score sheets with per-player entries, or surveys with repeatable sections. The master section references existing entities (via LOV or select), the detail section manages the child collection, and totals or summaries derive from both.",
        CommonPitfalls =
        [
            "DependsOn() fires when a scalar form field changes - it does not fire when collection items are edited. Derive collection-driven values (like Subtotal) from computed get-only model properties instead; the form re-renders on every collection change so they stay current",
            "Bind computed properties as ReadOnly() fields - they have no setter, and users should not edit derived values",
            "Collection item sub-forms support common scalar types (string, int, decimal, double, bool, DateTime) - keep detail rows simple",
            "Use WithMinItems(1) when an aggregate without children is meaningless; the remove button disables automatically at the minimum",
            "Initialize the collection in the model (e.g. with one empty row) so users see the detail section immediately"
        ],
        RelatedDemoIds = ["lov-field", "complex-dependencies", "field-groups", "auto-form"]
    };

    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    private List<FormGuidelines.GuidelineItem> _sidebarFeatures => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    // Sample customer data for the LOV (in a real app this would come from a service)
    private static readonly List<CustomerModel> _customers =
    [
        new() { Id = 1, Code = "CUST001", Name = "Acme Corporation", Email = "contact@acme.com", Phone = "+1-555-0101", City = "New York", Country = "USA", CreditLimit = 50000, IsActive = true },
        new() { Id = 2, Code = "CUST002", Name = "TechStart Inc", Email = "info@techstart.io", Phone = "+1-555-0102", City = "San Francisco", Country = "USA", CreditLimit = 75000, IsActive = true },
        new() { Id = 3, Code = "CUST003", Name = "Global Supplies Ltd", Email = "sales@globalsupplies.co.uk", Phone = "+44-20-5550103", City = "London", Country = "UK", CreditLimit = 100000, IsActive = true },
        new() { Id = 4, Code = "CUST004", Name = "Nordic Solutions AB", Email = "hello@nordicsolutions.se", Phone = "+46-8-5550104", City = "Stockholm", Country = "Sweden", CreditLimit = 60000, IsActive = true },
        new() { Id = 5, Code = "CUST005", Name = "Pacific Trading Co", Email = "orders@pacifictrading.com.au", Phone = "+61-2-5550105", City = "Sydney", Country = "Australia", CreditLimit = 80000, IsActive = true },
        new() { Id = 6, Code = "CUST006", Name = "Berlin Tech GmbH", Email = "kontakt@berlintech.de", Phone = "+49-30-5550106", City = "Berlin", Country = "Germany", CreditLimit = 90000, IsActive = true },
        new() { Id = 7, Code = "CUST007", Name = "Tokyo Industries", Email = "info@tokyoind.jp", Phone = "+81-3-5550107", City = "Tokyo", Country = "Japan", CreditLimit = 120000, IsActive = true },
        new() { Id = 8, Code = "CUST008", Name = "Maple Consulting", Email = "support@mapleconsulting.ca", Phone = "+1-416-5550108", City = "Toronto", Country = "Canada", CreditLimit = 45000, IsActive = true }
    ];

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        RecalculateDeposit(_model);

        _formConfiguration = FormBuilder<InvoiceFormModel>
            .Create()
            // ===== Master: customer lookup =====
            .AddFieldGroup(group => group
                .WithGroupName("Customer")
                .WithColumns(1)
                .ShowInCard()
                .AddField(x => x.CustomerId, field => field
                    .WithLabel("Customer")
                    .Required("Please select a customer")
                    .WithPlaceholder("Click search to select a customer")
                    .AsLov<InvoiceFormModel, int?, CustomerModel>(lov => lov
                        .WithDataSource(() => _customers)
                        .WithKey(c => c.Id)
                        .WithDisplay(c => $"{c.Code} - {c.Name}")
                        .AddColumn(c => c.Code, "Code", col => col.Width("100px"))
                        .AddColumn(c => c.Name, "Company Name", col => col.Width("200px"))
                        .AddColumn(c => c.City, "City")
                        .AddColumn(c => c.Country, "Country")
                        .MapField(c => c.Name, m => m.CustomerName)
                        .MapField(c => c.Email, m => m.CustomerEmail)
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
                    .WithPlaceholder("Auto-filled from selection")))
            // ===== Invoice header =====
            .AddFieldGroup(group => group
                .WithGroupName("Invoice Details")
                .WithColumns(3)
                .ShowInCard()
                .AddField(x => x.InvoiceNumber, field => field
                    .WithLabel("Invoice Number")
                    .Required("Invoice number is required")
                    .WithPlaceholder("e.g., INV-2026-001"))
                .AddField(x => x.InvoiceDate, field => field
                    .WithLabel("Invoice Date"))
                .AddField(x => x.TaxRatePercent, field => field
                    .WithLabel("Tax Rate (%)")
                    .WithHelpText("Applied to the subtotal")))
            // ===== Detail: line items =====
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Line Items")
                .AllowAdd("Add Line")
                .AllowRemove()
                .AllowReorder()
                .WithMinItems(1)
                .WithMaxItems(20)
                .WithEmptyText("No line items yet - add the first one.")
                .WithItemForm(item => item
                    .AddField(x => x.Description, field => field
                        .WithLabel("Description")
                        .Required("Description is required")
                        .WithPlaceholder("What was delivered?"))
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity"))
                    .AddField(x => x.UnitPrice, field => field
                        .WithLabel("Unit Price"))))
            // ===== Totals =====
            .AddFieldGroup(group => group
                .WithGroupName("Totals")
                .WithColumns(3)
                .ShowInCard()
                .AddField(x => x.Subtotal, field => field
                    .WithLabel("Subtotal")
                    .ReadOnly()
                    .WithHelpText("Sum of quantity × unit price"))
                .AddField(x => x.TaxAmount, field => field
                    .WithLabel("Tax")
                    .ReadOnly())
                .AddField(x => x.Total, field => field
                    .WithLabel("Total")
                    .ReadOnly()))
            .AddFieldGroup(group => group
                .WithGroupName("Deposit")
                .WithColumns(2)
                .AddField(x => x.DepositPercent, field => field
                    .WithLabel("Deposit (%)")
                    .WithHelpText("Change me - Deposit Due reacts via DependsOn"))
                .AddField(x => x.DepositDue, field => field
                    .WithLabel("Deposit Due")
                    .ReadOnly()
                    .DependsOn(x => x.DepositPercent, (model, _) => RecalculateDeposit(model))
                    .DependsOn(x => x.TaxRatePercent, (model, _) => RecalculateDeposit(model))))
            .Build();
    }

    private static void RecalculateDeposit(InvoiceFormModel model)
    {
        model.DepositDue = Math.Round(model.Total * model.DepositPercent / 100m, 2);
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        StateHasChanged();

        // Simulate API call persisting the whole aggregate (header + lines)
        if (!await DelayAsync(1500))
        {
            return;
        }


        _isSubmitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new InvoiceFormModel();
        RecalculateDeposit(_model);
        _isSubmitted = false;
        StateHasChanged();
    }

    // Demo prices are in USD; format explicitly so the summary matches regardless
    // of the runtime culture
    private static string Usd(decimal value) =>
        value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US"));

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        var items = new List<FormSuccessDisplay.DataDisplayItem>
        {
            new() { Label = "Customer", Value = $"{_model.CustomerName ?? "-"} ({_model.CustomerEmail ?? "-"})" },
            new() { Label = "Invoice", Value = $"{_model.InvoiceNumber ?? "-"} on {_model.InvoiceDate.ToShortDateString()}" },
            new() { Label = "Line Items", Value = _model.Items.Count.ToString() }
        };

        for (var i = 0; i < _model.Items.Count; i++)
        {
            var line = _model.Items[i];
            items.Add(new()
            {
                Label = $"  Line {i + 1}",
                Value = $"{line.Description} - {line.Quantity} × {Usd(line.UnitPrice)} = {Usd(line.Quantity * line.UnitPrice)}"
            });
        }

        items.Add(new() { Label = "Subtotal", Value = Usd(_model.Subtotal) });
        items.Add(new() { Label = $"Tax ({_model.TaxRatePercent}%)", Value = Usd(_model.TaxAmount) });
        items.Add(new() { Label = "Total", Value = Usd(_model.Total) });
        items.Add(new() { Label = $"Deposit Due ({_model.DepositPercent}%)", Value = Usd(_model.DepositDue) });

        return items;
    }

    private string GetMasterExampleCode() => GetMasterExampleCodeStatic();
    private string GetDetailExampleCode() => GetDetailExampleCodeStatic();
    private string GetTotalsExampleCode() => GetTotalsExampleCodeStatic();

    private static string GetMasterExampleCodeStatic()
    {
        return """
            // Master: the invoice references an existing customer via a LOV lookup
            .AddField(x => x.CustomerId, field => field
                .WithLabel("Customer")
                .Required("Please select a customer")
                .AsLov<InvoiceFormModel, int?, CustomerModel>(lov => lov
                    .WithDataSource(() => customers)
                    .WithKey(c => c.Id)
                    .WithDisplay(c => $"{c.Code} - {c.Name}")
                    .AddColumn(c => c.Code, "Code")
                    .AddColumn(c => c.Name, "Company Name")
                    .AddColumn(c => c.City, "City")
                    // Auto-fill master fields from the selected customer
                    .MapField(c => c.Name, m => m.CustomerName)
                    .MapField(c => c.Email, m => m.CustomerEmail)
                    .WithModalTitle("Select Customer")))
            """;
    }

    private static string GetDetailExampleCodeStatic()
    {
        return """
            // Detail: line items are a one-to-many collection with its own sub-form
            .AddCollectionField(x => x.Items, collection => collection
                .WithLabel("Line Items")
                .AllowAdd("Add Line")
                .AllowRemove()
                .AllowReorder()
                .WithMinItems(1)
                .WithMaxItems(20)
                .WithItemForm(item => item
                    .AddField(x => x.Description, field => field
                        .WithLabel("Description")
                        .Required("Description is required"))
                    .AddField(x => x.Quantity, field => field
                        .WithLabel("Quantity"))
                    .AddField(x => x.UnitPrice, field => field
                        .WithLabel("Unit Price"))))
            """;
    }

    private static string GetTotalsExampleCodeStatic()
    {
        return """
            // Collection-driven totals: computed get-only properties on the model.
            // The form re-renders on every line item change, so these stay live.
            public class InvoiceFormModel
            {
                public List<InvoiceLineModel> Items { get; set; } = [new InvoiceLineModel()];
                public decimal TaxRatePercent { get; set; } = 21m;

                public decimal Subtotal => Items.Sum(i => i.Quantity * i.UnitPrice);
                public decimal TaxAmount => Math.Round(Subtotal * TaxRatePercent / 100m, 2);
                public decimal Total => Subtotal + TaxAmount;

                public decimal DepositPercent { get; set; } = 30m;
                public decimal DepositDue { get; set; }
            }

            // Bind them as ReadOnly fields:
            .AddField(x => x.Subtotal, field => field.WithLabel("Subtotal").ReadOnly())
            .AddField(x => x.Total, field => field.WithLabel("Total").ReadOnly())

            // Scalar-driven values: DependsOn() reacts to other form fields.
            .AddField(x => x.DepositDue, field => field
                .WithLabel("Deposit Due")
                .ReadOnly()
                .DependsOn(x => x.DepositPercent, (model, _) =>
                    model.DepositDue = Math.Round(model.Total * model.DepositPercent / 100m, 2)))
            """;
    }
}
