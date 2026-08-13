using Microsoft.AspNetCore.Components.Web;
using System.Collections;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a field configured with <c>.AsLookup(...)</c>: a read-only display of the current
/// selection plus a browsable grid of candidate rows.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
/// <typeparam name="TValue">The field's value type.</typeparam>
/// <remarks>
/// <para>
/// <b>The picker is an inline panel, not a modal</b> - a deliberate departure from the MudBlazor
/// adapter, which opens <c>MudBlazorLookupDialog</c> through <c>IDialogService</c>. Fluent UI v5's
/// dialog service renders nothing unless the host application places a <c>FluentDialogProvider</c>
/// in its layout, and FormCraft cannot verify that from inside a field component. A lookup field
/// whose button silently did nothing on an app that had not added the provider is precisely the
/// class of quiet failure this library refuses elsewhere (the #206 <c>novalidate</c> script, #262's
/// unreachable file input), so the picker is rendered inline where it cannot fail to appear.
/// </para>
/// <para>
/// The row type is never named statically. <c>.AsLookup(...)</c> closes its delegates over a
/// <c>TItem</c> this component's type parameters do not carry, so the provider, selectors and
/// columns arrive boxed and are invoked through <see cref="Delegate.DynamicInvoke"/> - the same
/// approach the MudBlazor component takes, for the same reason.
/// </para>
/// </remarks>
public partial class FluentUILookupFieldComponent<TModel, TValue>
{
    private readonly List<object> _rows = [];
    private List<LookupColumnView> _columns = [];
    private int _loadTicket;
    private bool _isOpen;
    private bool _isLoading;
    private string _searchText = string.Empty;
    private string _displayText = string.Empty;

    /// <summary>The text shown in the read-only display.</summary>
    private string DisplayText => _displayText;

    /// <summary>
    /// The grid's columns. Falls back to a single display-text column when the field configured
    /// none, so the picker is usable rather than an empty table.
    /// </summary>
    private IReadOnlyList<LookupColumnView> Columns => _columns;

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#335). The hook runs on first render too,
    /// so this component needs no <c>OnInitialized</c> of its own.
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        // Columns are read from the field's attributes, so they belong to the field rather than to
        // this instance — a different field gets its own.
        _columns = BuildColumns();

        // ⛔ Re-derived, not merely cleared. Nothing else in this component repopulates _displayText
        // from the model: unlike the MudBlazor lookup, which calls UpdateDisplayText() from
        // OnParametersSet on every render and would repair a blank on the same pass, here the only
        // other writer is a row selection. Clearing alone therefore left a field with a perfectly
        // good stored value rendering empty for ever — a worse bug than the staleness it replaced.
        _displayText = CurrentValue?.ToString() ?? string.Empty;

        // The picker belongs to the field that opened it. Its rows came from the PREVIOUS field's
        // LookupDataProvider, and _rows is a List<object> so nothing type-guards it: clicking one
        // after a swap DynamicInvokes the new field's value/display selectors against the old
        // field's row object, which throws out of a click handler when the item types differ.
        _isOpen = false;
        _rows.Clear();
        _searchText = string.Empty;
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
    private async Task HandleRowKeyDownAsync(KeyboardEventArgs args, object row)
    {
        if (args.Key is "Enter" or " " or "Spacebar")
        {
            await SelectRowAsync(row);
        }
    }

    private async Task LoadRowsAsync()
    {
        var dataProvider = GetAttribute<object>("LookupDataProvider");
        if (dataProvider is not Delegate providerDelegate)
        {
            return;
        }

        // Each load claims a ticket, and only the newest one is allowed to publish. The search box
        // is immediate and undebounced, so two keystrokes put two loads in flight; without this the
        // slower one could land last and leave the grid showing results for a query the user has
        // already moved past - and could clear the spinner while the newer load was still running.
        var ticket = ++_loadTicket;

        _isLoading = true;

        try
        {
            var query = new LookupQuery { SearchText = _searchText, Page = 0, PageSize = 50 };
            if (providerDelegate.DynamicInvoke(query) is not Task task)
            {
                return;
            }

            await task;

            if (ticket != _loadTicket)
            {
                return;
            }

            _rows.Clear();

            // LookupResult<TItem>.Items, reached without naming TItem.
            var result = task.GetType().GetProperty("Result")?.GetValue(task);
            if (result?.GetType().GetProperty("Items")?.GetValue(result) is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is not null)
                    {
                        _rows.Add(item);
                    }
                }
            }
        }
        finally
        {
            if (ticket == _loadTicket)
            {
                _isLoading = false;
            }
        }
    }

    private async Task SelectRowAsync(object row)
    {
        var valueSelector = GetAttribute<object>("LookupValueSelector") as Delegate;
        var displaySelector = GetAttribute<object>("LookupDisplaySelector") as Delegate;
        if (valueSelector is null || displaySelector is null)
        {
            return;
        }

        if (valueSelector.DynamicInvoke(row) is not TValue value)
        {
            return;
        }

        _displayText = displaySelector.DynamicInvoke(row)?.ToString() ?? string.Empty;
        _isOpen = false;

        // The multi-field mapping hook runs before the value change is announced, so a handler
        // that reads sibling properties sees the fully-populated model.
        if (GetAttribute<object>("LookupOnItemSelected") is Delegate onItemSelected)
        {
            onItemSelected.DynamicInvoke(Context.Model, row);
        }

        SetValueWithoutNotification(value);
        await Context.OnValueChanged.InvokeAsync(value);
    }

    /// <summary>
    /// Projects the configured <c>LookupColumn&lt;TItem&gt;</c> list onto row-type-agnostic views.
    /// </summary>
    private List<LookupColumnView> BuildColumns()
    {
        if (GetAttribute<object>("LookupColumns") is not IEnumerable configured)
        {
            return [new LookupColumnView(Label ?? "Value", row => DisplayFor(row))];
        }

        var views = new List<LookupColumnView>();
        foreach (var column in configured)
        {
            if (column is null)
            {
                continue;
            }

            var type = column.GetType();
            var title = type.GetProperty("Title")?.GetValue(column)?.ToString() ?? string.Empty;
            if (type.GetProperty("ValueSelector")?.GetValue(column) is not Delegate selector)
            {
                continue;
            }

            views.Add(new LookupColumnView(title, row => selector.DynamicInvoke(row)));
        }

        return views.Count > 0
            ? views
            : [new LookupColumnView(Label ?? "Value", row => DisplayFor(row))];
    }

    private object? DisplayFor(object row) =>
        (GetAttribute<object>("LookupDisplaySelector") as Delegate)?.DynamicInvoke(row) ?? row;

    /// <summary>One grid column, decoupled from the row type.</summary>
    /// <param name="Title">The column header.</param>
    /// <param name="ValueSelector">Extracts this column's cell value from a row.</param>
    private sealed record LookupColumnView(string Title, Func<object, object?> ValueSelector);
}
