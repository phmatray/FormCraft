using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the LOV (List of Values) field component.
/// Displays a text field with a lookup button that opens a modal table for selection.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
/// <typeparam name="TItem">The type of items in the LOV.</typeparam>
public partial class MudBlazorLovFieldComponent<TModel, TValue, TItem>
{
    private ILovConfiguration<TItem, TValue>? _lovConfig;
    private ILovDataProvider<TItem>? _dataProvider;
    private readonly List<TItem> _selectedItems = [];
    private string? _displayText;
    private bool _isLoading;

    /// <summary>
    /// Gets the LOV configuration.
    /// </summary>
    protected ILovConfiguration<TItem, TValue>? LovConfig => _lovConfig;

    /// <summary>
    /// Gets whether multiple selection is enabled.
    /// </summary>
    protected bool IsMultiSelect => _lovConfig?.SelectionMode == LovSelectionMode.Multiple;

    /// <summary>
    /// Gets the list of selected items.
    /// </summary>
    protected IReadOnlyList<TItem> SelectedItems => _selectedItems;

    /// <summary>
    /// Gets the display text for the field.
    /// </summary>
    protected string? DisplayText => _displayText;

    /// <summary>
    /// Gets the CSS class for the field.
    /// </summary>
    protected string? CssClass => Context.Field.CssClass;

    /// <summary>
    /// Gets the adornment icon based on current state.
    /// </summary>
    protected string AdornmentIcon
    {
        get
        {
            if (_isLoading) return Icons.Material.Filled.HourglassEmpty;
            if (CurrentValue != null && !IsMultiSelect) return Icons.Material.Filled.Clear;
            return Icons.Material.Filled.Search;
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _lovConfig = GetAttribute<ILovConfiguration<TItem, TValue>>("LovConfiguration");

        if (_lovConfig == null)
        {
            throw new InvalidOperationException(
                "LovConfiguration is required. Use .AsLov() extension method to configure the field.");
        }

        // Create data provider
        InitializeDataProvider();

        // Update display text based on current value
        UpdateDisplayText();
    }

    private void InitializeDataProvider()
    {
        if (_lovConfig == null) return;

        var factory = ServiceProvider.GetService(typeof(ILovDataProviderFactory)) as ILovDataProviderFactory;
        if (factory != null)
        {
            _dataProvider = factory.Create<TItem, TValue>(_lovConfig);
        }
        else if (_lovConfig.DataProvider != null)
        {
            _dataProvider = new LambdaLovDataProvider<TItem>(_lovConfig.DataProvider, null);
        }
    }

    private void UpdateDisplayText()
    {
        if (CurrentValue == null || EqualityComparer<TValue>.Default.Equals(CurrentValue, default))
        {
            _displayText = null;
            return;
        }

        if (IsMultiSelect)
        {
            _displayText = _selectedItems.Count > 0
                ? $"{_selectedItems.Count} item(s) selected"
                : null;
        }
        else if (_selectedItems.Count > 0)
        {
            _displayText = GetItemDisplayText(_selectedItems[0]);
        }
        else
        {
            // Try to get display text from cached value or show the value itself
            _displayText = CurrentValue?.ToString();
        }
    }

    /// <summary>
    /// Gets the display text for an item.
    /// </summary>
    protected string GetItemDisplayText(TItem item)
    {
        return _lovConfig?.DisplaySelector(item) ?? item?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Handles the adornment button click.
    /// </summary>
    private async Task HandleAdornmentClick()
    {
        if (IsDisabled || IsReadOnly) return;

        // If there's a value and it's single select, clear it
        if (CurrentValue != null && !IsMultiSelect)
        {
            await ClearSelection();
            return;
        }

        await OpenLovDialog();
    }

    /// <summary>
    /// Opens the LOV selection dialog.
    /// </summary>
    private async Task OpenLovDialog()
    {
        if (_lovConfig == null || _dataProvider == null) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            var parameters = new DialogParameters<LovSelectionDialog<TItem, TValue>>
            {
                { x => x.LovConfig, _lovConfig },
                { x => x.DataProvider, _dataProvider },
                { x => x.SelectedItems, _selectedItems.ToList() },
                { x => x.ServiceProvider, ServiceProvider }
            };

            var options = new DialogOptions
            {
                MaxWidth = ConvertModalSize(_lovConfig.ModalOptions.Size),
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = _lovConfig.ModalOptions.CloseOnEscapeKey,
                BackdropClick = _lovConfig.ModalOptions.CloseOnBackdropClick
            };

            var dialog = await DialogService.ShowAsync<LovSelectionDialog<TItem, TValue>>(
                _lovConfig.ModalOptions.Title,
                parameters,
                options);

            var result = await dialog.Result;

            if (result is { Canceled: false, Data: LovSelectionResult<TItem> selectionResult })
            {
                await ApplySelection(selectionResult.SelectedItems);
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Applies the selection from the dialog.
    /// </summary>
    private async Task ApplySelection(List<TItem> items)
    {
        _selectedItems.Clear();
        _selectedItems.AddRange(items);

        if (_lovConfig == null) return;

        if (IsMultiSelect)
        {
            // For multi-select, we'd need to handle IEnumerable<TValue>
            // This is a simplified implementation
            var values = items.Select(_lovConfig.ValueSelector).ToList();
            // Note: This cast may need adjustment based on actual TValue type
            await NotifyValueChangedAsync((TValue)(object)values);
        }
        else
        {
            var item = items.FirstOrDefault();
            if (item != null)
            {
                var value = _lovConfig.ValueSelector(item);
                await NotifyValueChangedAsync(value);

                // Apply field mappings
                await ApplyFieldMappings(item);
            }
            else
            {
                await NotifyValueChangedAsync(default);
            }
        }

        UpdateDisplayText();
    }

    /// <summary>
    /// Applies field mappings from the selected item to the model.
    /// </summary>
    private async Task ApplyFieldMappings(TItem item)
    {
        if (_lovConfig == null || Context.Model == null) return;

        // Apply each mapping directly
        foreach (var mapping in _lovConfig.FieldMappings)
        {
            if (mapping is IAsyncLovFieldMapping asyncMapping)
            {
                await asyncMapping.ApplyAsync(item!, Context.Model, ServiceProvider);
            }
            else
            {
                mapping.Apply(item!, Context.Model);
            }
        }

        // Trigger dependency changed to update related fields
        await Context.OnDependencyChanged.InvokeAsync();
    }

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    private async Task ClearSelection()
    {
        _selectedItems.Clear();
        _displayText = null;
        await NotifyValueChangedAsync(default);

        // Clear mapped fields by setting them to default values
        if (_lovConfig != null && Context.Model != null)
        {
            var modelType = Context.Model.GetType();
            foreach (var mapping in _lovConfig.FieldMappings)
            {
                var property = modelType.GetProperty(mapping.TargetProperty);
                if (property?.CanWrite == true)
                {
                    var defaultValue = property.PropertyType.IsValueType
                        ? Activator.CreateInstance(property.PropertyType)
                        : null;
                    property.SetValue(Context.Model, defaultValue);
                }
            }
        }
    }

    /// <summary>
    /// Handles chip close (removing an item from multi-select).
    /// </summary>
    private async Task HandleChipClose(MudChip<TItem> chip)
    {
        if (chip.Value != null)
        {
            _selectedItems.Remove(chip.Value);
            await ApplySelection(_selectedItems);
        }
    }

    /// <summary>
    /// Converts LOV modal size to MudBlazor MaxWidth.
    /// </summary>
    private static MaxWidth ConvertModalSize(LovModalSize size)
    {
        return size switch
        {
            LovModalSize.Small => MaxWidth.Small,
            LovModalSize.Medium => MaxWidth.Medium,
            LovModalSize.Large => MaxWidth.Large,
            LovModalSize.ExtraLarge => MaxWidth.ExtraLarge,
            LovModalSize.Fullscreen => MaxWidth.ExtraExtraLarge,
            _ => MaxWidth.Large
        };
    }
}

/// <summary>
/// Result from the LOV selection dialog.
/// </summary>
/// <typeparam name="TItem">The type of items.</typeparam>
public class LovSelectionResult<TItem>
{
    /// <summary>
    /// Gets or sets the list of selected items.
    /// </summary>
    public List<TItem> SelectedItems { get; set; } = [];
}
