using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// A MudBlazor component that renders a collection (one-to-many) field with add, remove, reorder capabilities.
/// Each item in the collection is rendered as a sub-form using the configured item form fields.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
/// <remarks>
/// Item fields render through <see cref="IFieldRendererService"/>, exactly as ordinary fields do
/// (#203). This component used to build them by hand with a <c>RenderTreeBuilder</c> instead — a
/// second implementation of "render a field" that had to be taught every presentation attribute
/// separately, and silently lacked whichever one had most recently been added to the components.
/// That produced #146 (Variant), #177 (ShrinkLabel), #184 (adornments) and #190 (Required), each
/// found from a bug report rather than a test, and left a documented list of attributes the two
/// paths were known to disagree on. Deleting the second implementation is what closed the class.
/// </remarks>
public partial class CollectionFieldComponent<TModel, TItem>
    where TModel : new()
    where TItem : new()
{
    /// <summary>
    /// Gets or sets the parent model instance.
    /// </summary>
    [Parameter]
    public TModel Model { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collection field configuration.
    /// </summary>
    [Parameter]
    public ICollectionFieldConfiguration<TModel, TItem> Configuration { get; set; } = default!;

    /// <summary>
    /// Gets or sets the callback invoked when the collection changes (items added, removed, or reordered).
    /// </summary>
    [Parameter]
    public EventCallback OnCollectionChanged { get; set; }

    /// <summary>
    /// Gets or sets the parent form's EditContext, cascaded from the surrounding EditForm.
    /// When present, item field changes raise <see cref="EditContext.NotifyFieldChanged(in FieldIdentifier)"/>
    /// with a nested field identifier (e.g. <c>Items[0].ProductName</c>) on the root model, so
    /// modification tracking and Blazor's validation infrastructure see collection item edits.
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>
    /// The nested context cascaded to every item field this collection renders (#203).
    /// </summary>
    /// <remarks>
    /// Created once and reused: it carries a once-per-field diagnostic latch, and a scope rebuilt
    /// on each render would reset that latch and reintroduce the per-row warning flood it exists to
    /// prevent. Rebuilt only if the collection is repointed at a different field, which would make
    /// the old name — and the keys derived from it — wrong.
    /// </remarks>
    private CollectionItemFieldScope? _itemFieldScope;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (_itemFieldScope is null || _itemFieldScope.CollectionName != Configuration.FieldName)
        {
            _itemFieldScope = new CollectionItemFieldScope(Configuration.FieldName);
        }
    }

    private List<TItem> Items => Configuration.CollectionAccessor(Model);

    private bool HasReachedMax => Configuration.MaxItems > 0 && Items.Count >= Configuration.MaxItems;

    private bool HasReachedMin => Configuration.MinItems > 0 && Items.Count <= Configuration.MinItems;

    private List<string> ValidationErrors { get; set; } = new();

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
        if (HasReachedMin)
        {
            return;
        }

        if (index < 0 || index >= Items.Count)
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
                    // If conversion fails, use the value as-is
                }
            }

            property.SetValue(item, convertedValue);

            // Notify the parent EditContext with a nested field identifier
            // (Blazor convention: model stays the root model, the field name
            // encodes the collection path, e.g. "Items[0].ProductName") so
            // IsModified tracking and validation messages work natively.
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
                builder.AddAttribute(1, "class", "mb-3");
                builder.AddContent(2, RenderItemField(item, field, capturedIndex));
                // Surface validation messages attached to the nested field identifier
                // (e.g. Items[0].ProductName) next to the item field input.
                builder.OpenComponent<FieldValidationMessage>(3);
                builder.AddAttribute(4, "FieldName", $"{Configuration.FieldName}[{capturedIndex}].{capturedFieldName}");
                builder.CloseComponent();
                builder.CloseElement();
            }
        };
    }

    /// <summary>
    /// Renders one item field through <see cref="IFieldRendererService"/> — the same selector, and
    /// therefore the same per-type component, that an ordinary field renders through (#203).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the whole of the convergence, and the reason the file below it is gone. What stood
    /// here before was a type switch over <c>string</c>/<c>int</c>/<c>decimal</c>/… dispatching to
    /// hand-written <c>RenderTreeBuilder</c> methods, each re-deriving the presentation attributes
    /// that <c>MudBlazorFieldComponentBase</c> already resolves. A missing arm did not degrade
    /// gracefully either: it emitted no frames at all, which is how four numeric types rendered
    /// nothing whatsoever until #209.
    /// </para>
    /// <para>
    /// No nested-model machinery is needed to do this: the service is generic over the model and an
    /// item field is already configured as <c>IFieldConfiguration&lt;TItem, object&gt;</c>, so the
    /// ITEM is passed as the model and the selected component binds against
    /// <typeparamref name="TItem"/> directly.
    /// </para>
    /// <para>
    /// What the item does not carry is the parent's identity, which is why the value callback still
    /// goes through <see cref="UpdateItemFieldValue"/>: that writes the property on the item and then
    /// notifies the parent <see cref="EditContext"/> under the nested
    /// <c>&lt;collection&gt;[i].&lt;field&gt;</c> identifier (#91). Binding against the item while
    /// reporting against the parent is the entire "nested context" this path requires; the rest of
    /// it — which collection a field belongs to, and whether it has already warned — is cascaded as
    /// a <see cref="CollectionItemFieldScope"/>.
    /// </para>
    /// </remarks>
    private RenderFragment RenderItemField(TItem item, IFieldConfiguration<TItem, object> field, int itemIndex)
        => FieldRendererService.RenderField(
            item,
            field,
            EventCallback.Factory.Create<object?>(
                this, value => UpdateItemFieldValue(itemIndex, field.FieldName, value)),
            EventCallback.Factory.Create(this, NotifyCollectionChanged));
}
