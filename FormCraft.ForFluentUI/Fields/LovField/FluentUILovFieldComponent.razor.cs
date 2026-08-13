using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
    private int _loadTicket;
    private bool _isOpen;
    private bool _isLoading;
    private string _searchText = string.Empty;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>The resolved LOV configuration.</summary>
    private ILovConfiguration<TItem, TValue>? LovConfig { get; set; }

    /// <summary>The text shown in the read-only display.</summary>
    private string DisplayText { get; set; } = string.Empty;

    /// <summary>Whether the configuration asked for multiple selection.</summary>
    private bool IsMultiSelect => LovConfig?.SelectionMode == LovSelectionMode.Multiple;

    /// <summary>Whether the picker offers a search box.</summary>
    private bool SearchEnabled => LovConfig?.SearchOptions.Enabled ?? true;

    /// <summary>The search box's placeholder.</summary>
    private string SearchPlaceholder => LovConfig?.SearchOptions.Placeholder ?? "Search...";

    /// <summary>The grid's columns.</summary>
    private IReadOnlyList<LovColumnDefinition<TItem>> Columns => LovConfig?.Columns ?? [];

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#335). The hook runs on first render too,
    /// so this component needs no <c>OnInitialized</c> of its own.
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        // ⛔ Cleared before anything is rebuilt, and the SELECTION is the part that matters. It holds
        // rows drawn from the previous field's data source, and a subsequent pick appends to it — so
        // the display would read "old, old, new" and, worse, PublishSelectionAsync would write the
        // previous field's values into the NEW field's model property. The MudBlazor LOV clears the
        // same list for the same reason (#298); the two adapters drifting on this is exactly what
        // moving the hook into core is meant to stop.
        _selectedItems.Clear();
        _rows.Clear();
        DisplayText = string.Empty;
        _searchText = string.Empty;
        _isOpen = false;
        _isLoading = false;

        LovConfig = GetAttribute<ILovConfiguration<TItem, TValue>>("LovConfiguration")
            ?? throw new InvalidOperationException(
                "LovConfiguration is required. Use the .AsLov() extension method to configure the field.");

        if (CurrentValue is not null)
        {
            DisplayText = CurrentValue.ToString() ?? string.Empty;
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

    /// <summary>
    /// Selects a row from the keyboard, matching the pointer's behaviour on Enter and Space.
    /// </summary>
    private async Task HandleRowKeyDownAsync(KeyboardEventArgs args, TItem row)
    {
        if (args.Key is "Enter" or " " or "Spacebar")
        {
            await SelectRowAsync(row);
        }
    }

    private async Task LoadRowsAsync()
    {
        if (LovConfig is null)
        {
            return;
        }

        // Only the newest load publishes - see the lookup component for why an immediate,
        // undebounced search box otherwise lets a slower query overwrite a newer one.
        var ticket = ++_loadTicket;
        _isLoading = true;

        try
        {
            var query = new LovQuery { SearchText = _searchText, StartIndex = 0, Count = 50 };
            var result = await ResolveDataAsync(query);

            if (ticket != _loadTicket)
            {
                return;
            }

            _rows.Clear();
            _rows.AddRange(result.Items);
        }
        finally
        {
            if (ticket == _loadTicket)
            {
                _isLoading = false;
            }
        }
    }

    /// <summary>
    /// Fetches rows from whichever source the configuration named: an inline provider delegate, or
    /// an <see cref="ILovDataProvider{TItem}"/> resolved from DI.
    /// </summary>
    private async Task<LovDataResult<TItem>> ResolveDataAsync(LovQuery query)
    {
        if (LovConfig!.DataProvider is { } provider)
        {
            return await provider(query, CancellationToken.None);
        }

        if (LovConfig.DataProviderServiceType is { } serviceType &&
            ServiceProvider.GetService(serviceType) is ILovDataProvider<TItem> service)
        {
            return await service.GetItemsAsync(query, CancellationToken.None);
        }

        return new LovDataResult<TItem>();
    }

    private async Task SelectRowAsync(TItem row)
    {
        if (LovConfig is null)
        {
            return;
        }

        if (IsMultiSelect)
        {
            if (!_selectedItems.Contains(row))
            {
                _selectedItems.Add(row);
            }

            DisplayText = string.Join(", ", _selectedItems.Select(LovConfig.DisplaySelector));
        }
        else
        {
            _selectedItems.Clear();
            _selectedItems.Add(row);
            DisplayText = LovConfig.DisplaySelector(row);
            _isOpen = false;
        }

        await ApplyFieldMappingsAsync(row);
        await PublishSelectionAsync();
    }

    private async Task RemoveSelectedAsync(TItem row)
    {
        if (LovConfig is null)
        {
            return;
        }

        _selectedItems.Remove(row);
        DisplayText = string.Join(", ", _selectedItems.Select(LovConfig.DisplaySelector));

        await PublishSelectionAsync();
    }

    /// <summary>
    /// Writes the current selection to the model: the whole set in multi-select mode, the single
    /// chosen value otherwise.
    /// </summary>
    /// <remarks>
    /// Multi-select must publish <b>every</b> selected value, matching the MudBlazor component,
    /// which casts the value list to <c>TValue</c>. Publishing only the row that was just clicked -
    /// as an earlier draft did - showed N chips while the model held one value, so selecting A then
    /// B stored B alone and everything but the last pick was lost with nothing on screen saying so.
    /// <para>
    /// The cast is guarded rather than blind: multi-select is only meaningful when the bound
    /// property is itself a collection, and a configuration that turns it on over a scalar property
    /// would otherwise throw <see cref="InvalidCastException"/> from a click handler. Falling back
    /// to the first value keeps such a form working exactly as the single-select case does.
    /// </para>
    /// </remarks>
    private async Task PublishSelectionAsync()
    {
        var value = ResolveSelectionValue();
        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }

    private TValue? ResolveSelectionValue()
    {
        if (LovConfig is null || _selectedItems.Count == 0)
        {
            return default;
        }

        var values = _selectedItems.Select(LovConfig.ValueSelector).ToList();

        if (!IsMultiSelect)
        {
            return values[0];
        }

        return values is TValue typedList ? typedList : values[0];
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
        if (LovConfig is null || Context.Model is null)
        {
            return;
        }

        foreach (var mapping in LovConfig.FieldMappings)
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
