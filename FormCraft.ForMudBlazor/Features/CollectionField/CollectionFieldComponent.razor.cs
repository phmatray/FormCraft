using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// A MudBlazor component that renders a collection (one-to-many) field with add, remove, reorder capabilities.
/// Each item in the collection is rendered as a sub-form using the configured item form fields.
/// </summary>
/// <typeparam name="TModel">The parent model type.</typeparam>
/// <typeparam name="TItem">The type of items in the collection.</typeparam>
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

    private List<TItem> Items => Configuration.CollectionAccessor(Model);

    private bool HasReachedMax => Configuration.MaxItems > 0 && Items.Count >= Configuration.MaxItems;

    private bool HasReachedMin => Configuration.MinItems > 0 && Items.Count <= Configuration.MinItems;

    private List<string> ValidationErrors { get; set; } = new();

    private async Task AddItem()
    {
        if (HasReachedMax) return;

        Items.Add(new TItem());
        await NotifyCollectionChanged();
    }

    private async Task RemoveItem(int index)
    {
        if (HasReachedMin) return;
        if (index < 0 || index >= Items.Count) return;

        Items.RemoveAt(index);
        await NotifyCollectionChanged();
    }

    private async Task MoveItemUp(int index)
    {
        if (index <= 0 || index >= Items.Count) return;

        (Items[index], Items[index - 1]) = (Items[index - 1], Items[index]);
        await NotifyCollectionChanged();
    }

    private async Task MoveItemDown(int index)
    {
        if (index < 0 || index >= Items.Count - 1) return;

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
        if (itemIndex < 0 || itemIndex >= Items.Count) return;

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
        }

        await NotifyCollectionChanged();
    }

    private RenderFragment RenderItemFields(int itemIndex)
    {
        return builder =>
        {
            if (Configuration.ItemFormConfiguration == null) return;

            var item = Items[itemIndex];

            foreach (var field in Configuration.ItemFormConfiguration.Fields.OrderBy(f => f.Order))
            {
                var capturedIndex = itemIndex;
                var capturedFieldName = field.FieldName;

                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "mb-3");
                builder.AddContent(2, RenderItemField(item, field, capturedIndex));
                builder.CloseElement();
            }
        };
    }

    private RenderFragment RenderItemField(TItem item, IFieldConfiguration<TItem, object> field, int itemIndex)
    {
        return builder =>
        {
            var property = typeof(TItem).GetProperty(field.FieldName);
            if (property == null) return;

            var fieldType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            var value = property.GetValue(item);

            if (fieldType == typeof(string))
            {
                RenderTextField(builder, field, value as string, itemIndex);
            }
            else if (underlyingType == typeof(int))
            {
                RenderNumericField<int>(builder, field, (int)(value ?? 0), itemIndex);
            }
            else if (underlyingType == typeof(decimal))
            {
                RenderNumericField<decimal>(builder, field, (decimal)(value ?? 0m), itemIndex);
            }
            else if (underlyingType == typeof(double))
            {
                RenderNumericField<double>(builder, field, (double)(value ?? 0.0), itemIndex);
            }
            else if (underlyingType == typeof(bool))
            {
                RenderBooleanField(builder, field, value ?? false, itemIndex);
            }
            else if (underlyingType == typeof(DateTime))
            {
                RenderDateTimeField(builder, field, value as DateTime?, itemIndex);
            }
        };
    }

    private void RenderTextField(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, string? value, int itemIndex)
    {
        builder.OpenComponent<MudTextField<string>>(0);
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<string>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.AddAttribute(4, "Immediate", true);
        builder.CloseComponent();
    }

    private void RenderNumericField<T>(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, T value, int itemIndex)
        where T : struct
    {
        builder.OpenComponent(0, typeof(MudNumericField<>).MakeGenericType(typeof(T)));
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<T>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.AddAttribute(4, "Immediate", true);
        builder.AddAttribute(5, "Culture", System.Globalization.CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(decimal))
        {
            builder.AddAttribute(6, "Pattern", "[0-9]+([.,][0-9]+)?");
        }
        builder.CloseComponent();
    }

    private void RenderBooleanField(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, object value, int itemIndex)
    {
        builder.OpenComponent<MudCheckBox<bool>>(0);
        builder.AddAttribute(1, "Label", field.Label);
        builder.AddAttribute(2, "Value", value);
        builder.AddAttribute(3, "ValueChanged",
            EventCallback.Factory.Create<bool>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.AddAttribute(4, "ReadOnly", field.IsReadOnly);
        builder.AddAttribute(5, "Disabled", field.IsDisabled);
        builder.CloseComponent();
    }

    private void RenderDateTimeField(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, DateTime? value, int itemIndex)
    {
        builder.OpenComponent<MudDatePicker>(0);
        AddCommonFieldAttributes(builder, field, 1);
        builder.AddAttribute(2, "Date", value);
        builder.AddAttribute(3, "DateChanged",
            EventCallback.Factory.Create<DateTime?>(this,
                newValue => UpdateItemFieldValue(itemIndex, field.FieldName, newValue)));
        builder.CloseComponent();
    }

    private void AddCommonFieldAttributes(RenderTreeBuilder builder, IFieldConfiguration<TItem, object> field, int startIndex)
    {
        builder.AddAttribute(startIndex++, "Label", field.Label);
        builder.AddAttribute(startIndex++, "Placeholder", field.Placeholder);
        builder.AddAttribute(startIndex++, "HelperText", field.HelpText);
        builder.AddAttribute(startIndex++, "Required", field.IsRequired);
        builder.AddAttribute(startIndex++, "ReadOnly", field.IsReadOnly);
        builder.AddAttribute(startIndex++, "Disabled", field.IsDisabled);
        builder.AddAttribute(startIndex++, "Variant", Variant.Outlined);
        builder.AddAttribute(startIndex++, "Margin", Margin.Dense);
        builder.AddAttribute(startIndex, "ShrinkLabel", true);
    }
}
