using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// A dialog component that displays a searchable, paginated table for lookup field selection.
/// </summary>
public partial class MudBlazorLookupDialog : ComponentBase
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    /// <summary>
    /// The async data provider delegate (typed as object to handle generic TItem).
    /// </summary>
    [Parameter]
    public object DataProvider { get; set; } = default!;

    /// <summary>
    /// The value selector delegate.
    /// </summary>
    [Parameter]
    public object ValueSelector { get; set; } = default!;

    /// <summary>
    /// The display selector delegate.
    /// </summary>
    [Parameter]
    public object DisplaySelector { get; set; } = default!;

    /// <summary>
    /// The column definitions (as object to handle generic LookupColumn list).
    /// </summary>
    [Parameter]
    public object? Columns { get; set; }

    /// <summary>
    /// The label for the field.
    /// </summary>
    [Parameter]
    public string FieldLabel { get; set; } = "Select";

    private MudTable<object> _table = default!;
    private string _searchText = string.Empty;
    private object? _selectedItem;
    private bool _loading;
    private List<ColumnDef> _columnDefinitions = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ExtractColumnDefinitions();
    }

    private void ExtractColumnDefinitions()
    {
        if (Columns == null) return;

        // The columns parameter is a List<LookupColumn<TItem>> stored as object.
        // We use reflection to extract column info.
        var columnsType = Columns.GetType();
        if (!columnsType.IsGenericType) return;

        var enumerableInterface = columnsType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerableInterface == null) return;

        foreach (var col in (System.Collections.IEnumerable)Columns)
        {
            var colType = col.GetType();
            var titleProp = colType.GetProperty("Title");
            var valueSelectorProp = colType.GetProperty("ValueSelector");

            if (titleProp != null && valueSelectorProp != null)
            {
                var title = titleProp.GetValue(col)?.ToString() ?? "";
                var valueFunc = valueSelectorProp.GetValue(col);

                _columnDefinitions.Add(new ColumnDef
                {
                    Title = title,
                    ValueFunc = valueFunc as Delegate
                });
            }
        }
    }

    private async Task<TableData<object>> LoadServerData(TableState state, CancellationToken cancellationToken)
    {
        _loading = true;

        try
        {
            var query = new LookupQuery
            {
                SearchText = _searchText,
                Page = state.Page,
                PageSize = state.PageSize,
                SortField = state.SortLabel,
                SortDescending = state.SortDirection == SortDirection.Descending
            };

            // Invoke the data provider delegate
            var task = ((Delegate)DataProvider).DynamicInvoke(query) as Task;
            if (task == null) return new TableData<object> { Items = Array.Empty<object>(), TotalItems = 0 };

            await task;

            // Get the result from the completed task
            var resultProp = task.GetType().GetProperty("Result");
            var result = resultProp?.GetValue(task);
            if (result == null) return new TableData<object> { Items = Array.Empty<object>(), TotalItems = 0 };

            // Extract Items and TotalCount from LookupResult<TItem>
            var itemsProp = result.GetType().GetProperty("Items");
            var totalCountProp = result.GetType().GetProperty("TotalCount");

            var items = itemsProp?.GetValue(result) as System.Collections.IEnumerable;
            var totalCount = totalCountProp?.GetValue(result) is int count ? count : 0;

            var objectItems = items?.Cast<object>().ToList() ?? new List<object>();

            return new TableData<object>
            {
                Items = objectItems,
                TotalItems = totalCount
            };
        }
        finally
        {
            _loading = false;
        }
    }

    private string GetDisplayText(object item)
    {
        try
        {
            var result = ((Delegate)DisplaySelector).DynamicInvoke(item);
            return result?.ToString() ?? string.Empty;
        }
        catch
        {
            return item.ToString() ?? string.Empty;
        }
    }

    private async Task OnSearchTextChanged(string text)
    {
        _searchText = text;
        _selectedItem = null;
        await _table.ReloadServerData();
    }

    private void OnRowClicked(TableRowClickEventArgs<object> args)
    {
        _selectedItem = args.Item;
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }

    private void Submit()
    {
        if (_selectedItem != null)
        {
            MudDialog.Close(DialogResult.Ok(_selectedItem));
        }
    }

    /// <summary>
    /// Internal column definition used for rendering.
    /// </summary>
    private class ColumnDef
    {
        public string Title { get; set; } = string.Empty;
        public Delegate? ValueFunc { get; set; }

        public object? GetValue(object item)
        {
            try
            {
                return ValueFunc?.DynamicInvoke(item);
            }
            catch
            {
                return null;
            }
        }
    }
}
