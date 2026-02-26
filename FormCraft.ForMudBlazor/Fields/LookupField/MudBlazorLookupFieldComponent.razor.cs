using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorLookupFieldComponent<TModel, TValue>
{
    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    private string _displayText = string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateDisplayText();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateDisplayText();
    }

    private void UpdateDisplayText()
    {
        // Try to get the display selector and use it to show current value
        var displaySelector = GetAttribute<object>("LookupDisplaySelector");
        if (displaySelector != null && CurrentValue != null)
        {
            // We can't directly call the display selector since it takes TItem, not TValue.
            // The display text is stored after selection. If the field has a current value but
            // no stored display text, show the value's ToString.
            if (string.IsNullOrEmpty(_displayText))
            {
                _displayText = CurrentValue?.ToString() ?? string.Empty;
            }
        }
    }

    private async Task OpenLookupDialog()
    {
        if (IsReadOnly || IsDisabled)
            return;

        var dataProvider = GetAttribute<object>("LookupDataProvider");
        var valueSelector = GetAttribute<object>("LookupValueSelector");
        var displaySelector = GetAttribute<object>("LookupDisplaySelector");
        var columns = GetAttribute<object>("LookupColumns");
        var onItemSelected = GetAttribute<object>("LookupOnItemSelected");

        if (dataProvider == null || valueSelector == null || displaySelector == null)
            return;

        var parameters = new DialogParameters
        {
            { "DataProvider", dataProvider },
            { "ValueSelector", valueSelector },
            { "DisplaySelector", displaySelector },
            { "Columns", columns },
            { "FieldLabel", Label ?? "Select" }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<MudBlazorLookupDialog>(
            Label ?? "Lookup",
            parameters,
            options);

        var result = await dialog.Result;

        if (result is { Canceled: false, Data: not null })
        {
            // result.Data is the selected item (as object)
            var selectedItem = result.Data;

            // Extract value using the value selector
            try
            {
                var value = ((Delegate)valueSelector).DynamicInvoke(selectedItem);
                if (value is TValue typedValue)
                {
                    CurrentValue = typedValue;
                }

                // Extract display text
                var display = ((Delegate)displaySelector).DynamicInvoke(selectedItem);
                _displayText = display?.ToString() ?? string.Empty;

                // Invoke onItemSelected callback for multi-field mapping
                if (onItemSelected != null)
                {
                    ((Delegate)onItemSelected).DynamicInvoke(Context.Model, selectedItem);
                }

                StateHasChanged();
            }
            catch
            {
                // Silently handle type mismatches
            }
        }
    }
}
