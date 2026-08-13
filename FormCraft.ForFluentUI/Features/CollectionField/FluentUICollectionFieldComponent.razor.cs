using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a collection (one-to-many) field as a Fluent card with add, remove and reorder controls.
/// Each item is rendered as a sub-form from the configured item form fields.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <remarks>
/// Item fields render through <see cref="IFieldRendererService"/>, exactly as ordinary fields do
/// (#203, #278). That is the whole of the implementation: this component owns the collection
/// chrome - the card, the add/remove/reorder buttons, the empty state - and knows nothing about
/// field types. A field type the adapter registers works inside a collection by construction.
/// <para>
/// ⛔ Do not add a type switch here that builds item fields with a <c>RenderTreeBuilder</c>. The
/// MudBlazor adapter carried one until #203, and every presentation attribute had to be taught to
/// it separately; #146 (Variant), #177 (ShrinkLabel), #184 (adornments), #190 (Required) and #209
/// (four numeric types rendering nothing at all) were each found from a bug report rather than a
/// test. Starting a second one in this adapter would restart that sequence from zero.
/// </para>
/// </remarks>
public partial class FluentUICollectionFieldComponent<TModel, TItem>
    where TModel : new()
    where TItem : new()
{
    /// <summary>The parent model instance.</summary>
    [Parameter]
    public TModel Model { get; set; } = default!;

    /// <summary>The collection field configuration.</summary>
    [Parameter]
    public ICollectionFieldConfiguration<TModel, TItem> Configuration { get; set; } = default!;

    /// <summary>Invoked when the collection changes (items added, removed or reordered).</summary>
    [Parameter]
    public EventCallback OnCollectionChanged { get; set; }

    /// <summary>
    /// The parent form's <see cref="EditContext"/>, cascaded from the surrounding EditForm. When
    /// present, item field changes raise <see cref="EditContext.NotifyFieldChanged(in FieldIdentifier)"/>
    /// with a nested identifier (e.g. <c>Lines[0].Product</c>) on the root model, so modification
    /// tracking and Blazor's validation infrastructure see collection item edits (#91).
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    private List<TItem> Items => Configuration.CollectionAccessor(Model);

    private bool HasReachedMax => Configuration.MaxItems > 0 && Items.Count >= Configuration.MaxItems;

    private bool HasReachedMin => Configuration.MinItems > 0 && Items.Count <= Configuration.MinItems;

    private async Task AddItem()
    {
        if (HasReachedMax)
        {
            return;
        }

        Items.Add(new TItem());
        await NotifyCollectionChanged();
    }

    private async Task RemoveItem(int index)
    {
        if (HasReachedMin || index < 0 || index >= Items.Count)
        {
            return;
        }

        Items.RemoveAt(index);
        await NotifyCollectionChanged();
    }

    private async Task MoveItemUp(int index)
    {
        if (index <= 0 || index >= Items.Count)
        {
            return;
        }

        (Items[index], Items[index - 1]) = (Items[index - 1], Items[index]);
        await NotifyCollectionChanged();
    }

    private async Task MoveItemDown(int index)
    {
        if (index < 0 || index >= Items.Count - 1)
        {
            return;
        }

        (Items[index], Items[index + 1]) = (Items[index + 1], Items[index]);
        await NotifyCollectionChanged();
    }

    private async Task NotifyCollectionChanged()
    {
        if (OnCollectionChanged.HasDelegate)
        {
            await OnCollectionChanged.InvokeAsync();
        }

        StateHasChanged();
    }

    private async Task UpdateItemFieldValue(int itemIndex, string fieldName, object? value)
    {
        if (itemIndex < 0 || itemIndex >= Items.Count)
        {
            return;
        }

        var item = Items[itemIndex];
        var property = typeof(TItem).GetProperty(fieldName);
        if (property != null)
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var convertedValue = value;

            if (value != null && value.GetType() != targetType)
            {
                try
                {
                    convertedValue = Convert.ChangeType(value, targetType);
                }
                catch
                {
                    // Conversion failed - hand the value over as-is and let validation report it.
                }
            }

            property.SetValue(item, convertedValue);

            // Notify the parent EditContext with a nested field identifier (Blazor convention: the
            // model stays the root model, the field name encodes the collection path, e.g.
            // "Lines[0].Product") so IsModified tracking and validation messages work natively.
            if (EditContext != null && Model is not null)
            {
                EditContext.NotifyFieldChanged(GetItemFieldIdentifier(itemIndex, fieldName));
            }
        }

        await NotifyCollectionChanged();
    }

    private FieldIdentifier GetItemFieldIdentifier(int itemIndex, string fieldName)
        => new(Model!, $"{Configuration.FieldName}[{itemIndex}].{fieldName}");

    private RenderFragment RenderItemFields(int itemIndex)
    {
        return builder =>
        {
            if (Configuration.ItemFormConfiguration == null)
            {
                return;
            }

            var item = Items[itemIndex];

            foreach (var field in Configuration.ItemFormConfiguration.Fields.OrderBy(f => f.Order))
            {
                var capturedIndex = itemIndex;
                var capturedFieldName = field.FieldName;

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "formcraft-collection-item-field");
                builder.AddContent(2, RenderItemField(item, field, capturedIndex));
                builder.OpenComponent<FieldHelpText>(3);
                builder.AddAttribute(4, "Text", field.HelpText);
                builder.AddAttribute(5, "Id", FieldHelpText.IdFor(capturedFieldName));
                builder.CloseComponent();
                // Surface validation messages attached to the nested field identifier
                // (e.g. Lines[0].Product) next to the item field input.
                builder.OpenComponent<FieldValidationMessage>(6);
                builder.AddAttribute(7, "FieldName", $"{Configuration.FieldName}[{capturedIndex}].{capturedFieldName}");
                builder.CloseComponent();
                builder.CloseElement();
            }
        };
    }

    /// <summary>
    /// Renders one item field through <see cref="IFieldRendererService"/> - the same selector, and
    /// therefore the same per-type component, an ordinary field renders through (#203).
    /// </summary>
    /// <remarks>
    /// No nested-model machinery is needed: the service is generic over the model and an item field
    /// is already configured as <c>IFieldConfiguration&lt;TItem, object&gt;</c>, so the ITEM is
    /// passed as the model and the selected component binds against <typeparamref name="TItem"/>
    /// directly. What the item does not carry is the parent's identity, which is why the value
    /// callback goes through <see cref="UpdateItemFieldValue"/> - it writes the property on the item
    /// and then notifies the parent <see cref="EditContext"/> under the nested identifier (#91).
    /// </remarks>
    private RenderFragment RenderItemField(TItem item, IFieldConfiguration<TItem, object> field, int itemIndex)
        => FieldRendererService.RenderField(
            item,
            field,
            EventCallback.Factory.Create<object?>(
                this, value => UpdateItemFieldValue(itemIndex, field.FieldName, value)),
            EventCallback.Factory.Create(this, NotifyCollectionChanged));
}
