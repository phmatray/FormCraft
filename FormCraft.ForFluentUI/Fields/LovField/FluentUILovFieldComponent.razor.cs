using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a field configured with <c>.AsLov(...)</c>: a read-only display of the current selection
/// plus a browsable, searchable grid of candidate rows.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
/// <typeparam name="TItem">The type of the rows in the LOV.</typeparam>
/// <remarks>
/// The picker is an inline panel rather than a modal, for the reason spelled out on
/// <see cref="FluentUILookupFieldComponent{TModel, TValue}"/>: Fluent v5's dialog service renders
/// nothing without a <c>FluentDialogProvider</c> the host application must add, and a browse button
/// that silently does nothing is a worse outcome than a different presentation.
/// </remarks>
public partial class FluentUILovFieldComponent<TModel, TValue, TItem>
{
    private readonly List<TItem> _rows = [];
    private readonly List<TItem> _selectedItems = [];
    private ILovConfiguration<TItem, TValue>? _lovConfig;
    private bool _isOpen;
    private bool _isLoading;
    private string _searchText = string.Empty;
    private string _displayText = string.Empty;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>The resolved LOV configuration.</summary>
    private ILovConfiguration<TItem, TValue>? LovConfig => _lovConfig;

    /// <summary>The text shown in the read-only display.</summary>
    private string DisplayText => _displayText;

    /// <summary>Whether the configuration asked for multiple selection.</summary>
    private bool IsMultiSelect => _lovConfig?.SelectionMode == LovSelectionMode.Multiple;

    /// <summary>Whether the picker offers a search box.</summary>
    private bool SearchEnabled => _lovConfig?.SearchOptions.Enabled ?? true;

    /// <summary>The search box's placeholder.</summary>
    private string SearchPlaceholder => _lovConfig?.SearchOptions.Placeholder ?? "Search...";

    /// <summary>The grid's columns.</summary>
    private IReadOnlyList<LovColumnDefinition<TItem>> Columns => _lovConfig?.Columns ?? [];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _lovConfig = GetAttribute<ILovConfiguration<TItem, TValue>>("LovConfiguration")
            ?? throw new InvalidOperationException(
                "LovConfiguration is required. Use the .AsLov() extension method to configure the field.");

        if (CurrentValue is not null)
        {
            _displayText = CurrentValue.ToString() ?? string.Empty;
        }
    }

    private async Task TogglePickerAsync()
    {
        _isOpen = !_isOpen;

        if (_isOpen)
        {
            await LoadRowsAsync();
        }
    }

    private async Task OnSearchChangedAsync(string? text)
    {
        _searchText = text ?? string.Empty;
        await LoadRowsAsync();
    }

    private async Task LoadRowsAsync()
    {
        if (_lovConfig is null)
        {
            return;
        }

        _isLoading = true;
        _rows.Clear();

        try
        {
            var query = new LovQuery { SearchText = _searchText, StartIndex = 0, Count = 50 };
            var result = await ResolveDataAsync(query);
            _rows.AddRange(result.Items);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Fetches rows from whichever source the configuration named: an inline provider delegate, or
    /// an <see cref="ILovDataProvider{TItem}"/> resolved from DI.
    /// </summary>
    private async Task<LovDataResult<TItem>> ResolveDataAsync(LovQuery query)
    {
        if (_lovConfig!.DataProvider is { } provider)
        {
            return await provider(query, CancellationToken.None);
        }

        if (_lovConfig.DataProviderServiceType is { } serviceType &&
            ServiceProvider.GetService(serviceType) is ILovDataProvider<TItem> service)
        {
            return await service.GetItemsAsync(query, CancellationToken.None);
        }

        return new LovDataResult<TItem>();
    }

    private async Task SelectRowAsync(TItem row)
    {
        if (_lovConfig is null)
        {
            return;
        }

        var value = _lovConfig.ValueSelector(row);

        if (IsMultiSelect)
        {
            if (!_selectedItems.Contains(row))
            {
                _selectedItems.Add(row);
            }

            _displayText = string.Join(", ", _selectedItems.Select(_lovConfig.DisplaySelector));
        }
        else
        {
            _selectedItems.Clear();
            _selectedItems.Add(row);
            _displayText = _lovConfig.DisplaySelector(row);
            _isOpen = false;
        }

        await ApplyFieldMappingsAsync(row);

        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }

    private async Task RemoveSelectedAsync(TItem row)
    {
        if (_lovConfig is null)
        {
            return;
        }

        _selectedItems.Remove(row);
        _displayText = string.Join(", ", _selectedItems.Select(_lovConfig.DisplaySelector));

        var value = _selectedItems.Count > 0 ? _lovConfig.ValueSelector(_selectedItems[0]) : default;
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }

    /// <summary>
    /// Copies further model properties from the chosen row, as configured by <c>MapField(...)</c>
    /// and <c>MapFieldAsync(...)</c>.
    /// </summary>
    /// <remarks>
    /// The mappings apply themselves - this only dispatches on whether one needs the service
    /// provider. Reimplementing the property write here (reflecting over a target name) would
    /// duplicate logic the mapping already owns and would silently skip the async ones.
    /// </remarks>
    private async Task ApplyFieldMappingsAsync(TItem row)
    {
        if (_lovConfig is null || Context.Model is null)
        {
            return;
        }

        foreach (var mapping in _lovConfig.FieldMappings)
        {
            if (mapping is IAsyncLovFieldMapping asyncMapping)
            {
                await asyncMapping.ApplyAsync(row!, Context.Model, ServiceProvider);
            }
            else
            {
                mapping.Apply(row!, Context.Model);
            }
        }

        // Let fields that depend on this one recompute from the newly-mapped properties.
        await Context.OnDependencyChanged.InvokeAsync();
    }
}
