using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// Modal dialog for LOV (List of Values) selection with MudDataGrid.
/// Supports virtualization for large datasets, search, and multi-selection.
/// </summary>
/// <typeparam name="TItem">The type of items in the LOV.</typeparam>
/// <typeparam name="TValue">The type of the selected value.</typeparam>
public partial class LovSelectionDialog<TItem, TValue> : IDisposable
{
    private MudDataGrid<TItem>? _dataGrid;
    private HashSet<TItem> _selectedItemsSet = [];
    private string? _searchText;
    private bool _isLoading;
    private int _totalItems;
    private CancellationTokenSource? _searchCts;

    /// <summary>
    /// Gets or sets the dialog instance reference.
    /// </summary>
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>
    /// Gets or sets the LOV configuration.
    /// </summary>
    [Parameter]
    public ILovConfiguration<TItem, TValue> LovConfig { get; set; } = null!;

    /// <summary>
    /// Gets or sets the data provider for fetching items.
    /// </summary>
    [Parameter]
    public ILovDataProvider<TItem> DataProvider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the initially selected items.
    /// </summary>
    [Parameter]
    public List<TItem> SelectedItems { get; set; } = [];

    /// <summary>
    /// Gets or sets the service provider for resolving dependencies.
    /// </summary>
    [Parameter]
    public IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Gets whether multi-selection is enabled.
    /// </summary>
    protected bool IsMultiSelect => LovConfig.SelectionMode == LovSelectionMode.Multiple;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (SelectedItems.Count > 0)
        {
            _selectedItemsSet = [..SelectedItems];
        }
    }

    /// <summary>
    /// Server data callback for MudDataGrid virtualization.
    /// </summary>
    private async Task<GridData<TItem>> ServerDataFunc(GridState<TItem> gridState)
    {
        if (DataProvider == null)
        {
            return new GridData<TItem> { Items = [], TotalItems = 0 };
        }

        _isLoading = true;
        StateHasChanged();

        try
        {
            var query = BuildQuery(gridState);
            var result = await DataProvider.GetItemsAsync(query);

            _totalItems = result.TotalCount;

            return new GridData<TItem>
            {
                Items = result.Items,
                TotalItems = result.TotalCount
            };
        }
        catch (TaskCanceledException)
        {
            return new GridData<TItem> { Items = [], TotalItems = 0 };
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
            return new GridData<TItem> { Items = [], TotalItems = 0 };
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Builds a LovQuery from the MudDataGrid state.
    /// </summary>
    private LovQuery BuildQuery(GridState<TItem> gridState)
    {
        var query = new LovQuery
        {
            SearchText = _searchText,
            StartIndex = gridState.Page * gridState.PageSize,
            Count = gridState.PageSize
        };

        // Add sort definitions
        foreach (var sortDef in gridState.SortDefinitions)
        {
            query.SortDefinitions.Add(new LovSortDefinition
            {
                PropertyName = sortDef.SortBy ?? string.Empty,
                Descending = sortDef.Descending
            });
        }

        // Add filter definitions
        foreach (var filterDef in gridState.FilterDefinitions)
        {
            if (filterDef.Column?.PropertyName != null && filterDef.Value != null)
            {
                query.FilterDefinitions.Add(new LovFilterDefinition
                {
                    PropertyName = filterDef.Column.PropertyName,
                    Operator = filterDef.Operator ?? "contains",
                    Value = filterDef.Value
                });
            }
        }

        return query;
    }

    /// <summary>
    /// Handles search text changes with debouncing.
    /// </summary>
    private async Task OnSearchTextChanged()
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    /// <summary>
    /// Clears the search text and reloads data.
    /// </summary>
    private async Task ClearSearch()
    {
        _searchText = null;
        if (_dataGrid != null)
        {
            await _dataGrid.ReloadServerData();
        }
    }

    /// <summary>
    /// Handles selection changes.
    /// </summary>
    private void OnSelectedItemsChanged(HashSet<TItem> items)
    {
        _selectedItemsSet = items;
    }

    /// <summary>
    /// Clears all selections.
    /// </summary>
    private void ClearAllSelections()
    {
        _selectedItemsSet.Clear();
    }

    /// <summary>
    /// Gets the text for the confirm button.
    /// </summary>
    private string GetConfirmButtonText()
    {
        if (!_selectedItemsSet.Any()) return "Select";
        if (IsMultiSelect) return $"Select ({_selectedItemsSet.Count})";
        return "Select";
    }

    /// <summary>
    /// Gets the style for a column.
    /// </summary>
    private static string? GetColumnStyle(LovColumnDefinition<TItem> column)
    {
        return column.Width != null ? $"width: {column.Width}" : null;
    }

    /// <summary>
    /// Confirms the selection and closes the dialog.
    /// </summary>
    private void Confirm()
    {
        var result = new LovSelectionResult<TItem>
        {
            SelectedItems = _selectedItemsSet.ToList()
        };
        MudDialog.Close(DialogResult.Ok(result));
    }

    /// <summary>
    /// Cancels and closes the dialog.
    /// </summary>
    private void Cancel()
    {
        MudDialog.Cancel();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
