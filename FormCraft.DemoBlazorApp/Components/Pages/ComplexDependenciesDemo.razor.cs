using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class ComplexDependenciesDemo : ComponentBase
{
    private OrderModel _model = new();
    private IFormConfiguration<OrderModel>? _formConfig;
    private bool _submitted;
    private bool _isSubmitting;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "complex-dependencies",
        Title = "Complex Field Dependencies",
        Description = "Build sophisticated forms with cascading updates, conditional visibility, and computed values. Learn how to create dynamic forms where fields react to changes in other fields through a complex dependency chain. This demo shows product ordering with real-time price calculations, coupon validation, and shipping cost updates.",
        Icon = Icons.Material.Filled.AccountTree,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.AccountTree, Color = Color.Primary, Text = "Cascading field updates" },
            new() { Icon = Icons.Material.Filled.Visibility, Color = Color.Secondary, Text = "Conditional field visibility" },
            new() { Icon = Icons.Material.Filled.Calculate, Color = Color.Tertiary, Text = "Auto-computed field values" },
            new() { Icon = Icons.Material.Filled.FilterList, Color = Color.Info, Text = "Dynamic dropdown options" },
            new() { Icon = Icons.Material.Filled.Refresh, Color = Color.Success, Text = "Real-time recalculations" },
            new() { Icon = Icons.Material.Filled.Code, Color = Color.Warning, Text = "Type-safe dependency chain" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "DependsOn()", Usage = "React to another field's changes", Example = ".DependsOn(x => x.Category, (m, v) => UpdateProducts(m, v))" },
            new() { Feature = "WithValueProvider()", Usage = "Compute field value from model", Example = ".WithValueProvider((m, _) => m.Quantity * m.UnitPrice)" },
            new() { Feature = "WithVisibilityProvider()", Usage = "Show/hide based on conditions", Example = ".WithVisibilityProvider(m => m.Category != null)" },
            new() { Feature = "WithOptionsProvider()", Usage = "Dynamic dropdown options", Example = ".WithOptionsProvider((m, _) => GetProductsForCategory(m))" },
            new() { Feature = "Chain Dependencies", Usage = "Multiple DependsOn calls", Example = ".DependsOn(x => x.A, ...).DependsOn(x => x.B, ...)" },
            new() { Feature = "ReadOnly()", Usage = "Prevent editing computed fields", Example = ".ReadOnly() // For calculated totals" }
        ],
        CodeExamples =
        [
            new() { Title = "Cascading Dependencies", Language = "csharp", CodeProvider = GetCascadingCodeExampleStatic },
            new() { Title = "Computed Values", Language = "csharp", CodeProvider = GetComputedValuesCodeExampleStatic }
        ],
        WhenToUse = "Use complex dependencies when building forms with interconnected fields that need to update each other. Perfect for e-commerce order forms (product selection updates price, quantity updates total), financial calculators (interest rate changes update payment amounts), booking systems (room type affects available amenities), and configuration forms (selecting a category filters available options). The DependsOn() pattern ensures fields stay synchronized without manual event wiring.",
        CommonPitfalls =
        [
            "Circular dependencies - Field A depends on B, B depends on C, C depends on A creates infinite loops",
            "Forgetting to call StateHasChanged() in async dependency handlers - UI won't update",
            "Not resetting dependent fields when parent changes - leads to invalid state",
            "Over-complicating dependency chains - keep them simple and unidirectional when possible",
            "Missing null checks in value providers - can cause runtime errors when fields are cleared"
        ],
        RelatedDemoIds = ["async-value-provider", "field-dependencies", "fluent-validation", "dynamic-forms"]
    };

    // Product catalog
    private static readonly Dictionary<string, List<(string Name, decimal Price)>> ProductsByCategory = new()
    {
        ["Electronics"] =
        [
            ("Laptop", 999.99m),
            ("Smartphone", 699.99m),
            ("Tablet", 449.99m),
            ("Headphones", 149.99m)
        ],
        ["Clothing"] =
        [
            ("T-Shirt", 29.99m),
            ("Jeans", 59.99m),
            ("Jacket", 89.99m),
            ("Sneakers", 79.99m)
        ],
        ["Books"] =
        [
            ("Fiction Novel", 14.99m),
            ("Technical Guide", 49.99m),
            ("Biography", 24.99m),
            ("Cookbook", 34.99m)
        ]
    };

    // Coupon codes
    private static readonly Dictionary<string, int> CouponCodes = new()
    {
        ["SAVE10"] = 10,
        ["SAVE20"] = 20,
        ["HALFOFF"] = 50
    };

    // Shipping methods
    private static readonly Dictionary<string, decimal> ShippingMethods = new()
    {
        ["Standard"] = 5.99m,
        ["Express"] = 12.99m,
        ["Overnight"] = 24.99m,
        ["Free Pickup"] = 0m
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<FormGuidelines.GuidelineItem> _sidebarFeatures => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);

        _formConfig = FormBuilder<OrderModel>
            .Create()
            .AddField(x => x.Category, field => field
                .WithLabel("Product Category")
                .WithPlaceholder("Select a category")
                .WithSelectOptions([
                    new("Electronics", "Electronics"),
                    new("Clothing", "Clothing"),
                    new("Books", "Books")
                ])
                .Required("Please select a category"))
            .AddField(x => x.Product, field => field
                .WithLabel("Product")
                .WithPlaceholder("Select a product")
                .DependsOn(x => x.Category, (model, category) =>
                {
                    model.Product = "";
                    model.UnitPrice = 0;
                    RecalculateTotals();
                })
                .Required("Please select a product"))
            .AddField(x => x.Quantity, field => field
                .WithLabel("Quantity")
                .WithPlaceholder("1")
                .DependsOn(x => x.Product, (model, product) =>
                {
                    if (!string.IsNullOrEmpty(product) && !string.IsNullOrEmpty(model.Category))
                    {
                        var products = ProductsByCategory.GetValueOrDefault(model.Category);
                        var selectedProduct = products?.FirstOrDefault(p => p.Name == product);
                        if (selectedProduct.HasValue)
                        {
                            model.UnitPrice = selectedProduct.Value.Price;
                        }
                    }
                    RecalculateTotals();
                })
                .Required("Please enter quantity"))
            .AddField(x => x.CouponCode, field => field
                .WithLabel("Coupon Code (optional)")
                .WithPlaceholder("Enter coupon code")
                .DependsOn(x => x.Quantity, (model, _) => RecalculateTotals())
                .WithHelpText("Try: SAVE10, SAVE20, or HALFOFF"))
            .AddField(x => x.ShippingMethod, field => field
                .WithLabel("Shipping Method")
                .WithSelectOptions([
                    new("Standard", "Standard - $5.99"),
                    new("Express", "Express - $12.99"),
                    new("Overnight", "Overnight - $24.99"),
                    new("Free Pickup", "Free Pickup - $0.00")
                ])
                .DependsOn(x => x.CouponCode, (model, coupon) =>
                {
                    if (!string.IsNullOrEmpty(coupon) && CouponCodes.TryGetValue(coupon.ToUpper(), out var discount))
                    {
                        model.DiscountPercent = discount;
                    }
                    else
                    {
                        model.DiscountPercent = 0;
                    }
                    RecalculateTotals();
                })
                .Required("Please select shipping method"))
            .AddField(x => x.Notes, field => field
                .WithLabel("Order Notes (optional)")
                .WithPlaceholder("Any special instructions?")
                .DependsOn(x => x.ShippingMethod, (model, shipping) =>
                {
                    model.ShippingCost = ShippingMethods.GetValueOrDefault(shipping, 0);
                    RecalculateTotals();
                })
                .WithAttribute("Lines", 3))
            .Build();
    }

    private void RecalculateTotals()
    {
        _model.Subtotal = _model.UnitPrice * _model.Quantity;
        var discountAmount = _model.Subtotal * _model.DiscountPercent / 100;
        _model.Total = _model.Subtotal - discountAmount + _model.ShippingCost;
        StateHasChanged();
    }

    private async Task HandleValidSubmit(OrderModel model)
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
        _model = new OrderModel();
        _submitted = false;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Category", Value = _model.Category },
            new() { Label = "Product", Value = _model.Product },
            new() { Label = "Quantity", Value = _model.Quantity.ToString() },
            new() { Label = "Unit Price", Value = _model.UnitPrice.ToString("C") },
            new() { Label = "Subtotal", Value = _model.Subtotal.ToString("C") },
            new() { Label = "Discount", Value = $"{_model.DiscountPercent}%" },
            new() { Label = "Shipping", Value = $"{_model.ShippingMethod} ({_model.ShippingCost:C})" },
            new() { Label = "Total", Value = _model.Total.ToString("C") }
        ];
    }

    // Code Examples (instance methods for backward compatibility)
    private string GetCascadingCodeExample() => GetCascadingCodeExampleStatic();
    private string GetComputedValuesCodeExample() => GetComputedValuesCodeExampleStatic();

    // Static code providers for Documentation
    private static string GetCascadingCodeExampleStatic()
    {
        return """
            // Cascading dependencies - each field reacts to the previous
            var formConfig = FormBuilder<OrderModel>
                .Create()
                .AddField(x => x.Category, field => field
                    .WithLabel("Category")
                    .WithOptions(categories))
                .AddField(x => x.Product, field => field
                    .WithLabel("Product")
                    // When Category changes, reset Product and update options
                    .DependsOn(x => x.Category, (model, category) => {
                        model.Product = "";
                        model.UnitPrice = 0;
                    }))
                .AddField(x => x.Quantity, field => field
                    .WithLabel("Quantity")
                    // When Product changes, set the unit price
                    .DependsOn(x => x.Product, (model, product) => {
                        var price = GetPriceForProduct(model.Category, product);
                        model.UnitPrice = price;
                    }))
                .Build();
            """;
    }

    private static string GetComputedValuesCodeExampleStatic()
    {
        return """
            // Computed values that update automatically
            .AddField(x => x.Subtotal, field => field
                .WithLabel("Subtotal")
                .DependsOn(x => x.Quantity, (model, qty) => {
                    model.Subtotal = model.UnitPrice * qty;
                })
                .ReadOnly()) // Prevent manual editing

            .AddField(x => x.Total, field => field
                .WithLabel("Total")
                .DependsOn(x => x.Subtotal, (model, sub) => {
                    var discount = sub * model.DiscountPercent / 100;
                    model.Total = sub - discount + model.ShippingCost;
                })
                .ReadOnly())

            // Apply coupon codes dynamically
            .AddField(x => x.CouponCode, field => field
                .WithLabel("Coupon")
                .DependsOn(x => x.CouponCode, (model, code) => {
                    if (ValidCoupons.TryGetValue(code, out var discount))
                        model.DiscountPercent = discount;
                }))
            """;
    }

    // Model class
    public class OrderModel
    {
        public string Category { get; set; } = "";
        public string Product { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string CouponCode { get; set; } = "";
        public int DiscountPercent { get; set; }
        public string ShippingMethod { get; set; } = "Standard";
        public decimal ShippingCost { get; set; } = 5.99m;
        public string Notes { get; set; } = "";

        // Computed properties
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; } = 5.99m;
    }
}
