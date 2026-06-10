namespace FormCraft.DemoBlazorApp.Models;

/// <summary>
/// Master entity for the master-detail demo: an invoice with a customer lookup (LOV),
/// a collection of line items, and computed totals.
/// </summary>
public class InvoiceFormModel
{
    // Master: customer selection via LOV
    public int? CustomerId { get; set; }

    // Auto-populated from the customer selection (MapField)
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }

    // Invoice header
    public string? InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.Today;

    // Detail: line items managed by AddCollectionField
    public List<InvoiceLineModel> Items { get; set; } = [new InvoiceLineModel()];

    // Scalar input that influences the totals
    public decimal TaxRatePercent { get; set; } = 21m;

    // Computed totals: get-only properties stay in sync with the line items
    // because the form re-renders whenever the collection changes.
    public decimal Subtotal => Items.Sum(i => i.Quantity * i.UnitPrice);
    public decimal TaxAmount => Math.Round(Subtotal * TaxRatePercent / 100m, 2);
    public decimal Total => Subtotal + TaxAmount;

    // Deposit: DepositDue is recalculated via DependsOn when DepositPercent changes.
    public decimal DepositPercent { get; set; } = 30m;
    public decimal DepositDue { get; set; }
}

/// <summary>
/// Detail entity for the master-detail demo: a single invoice line item.
/// </summary>
public class InvoiceLineModel
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
