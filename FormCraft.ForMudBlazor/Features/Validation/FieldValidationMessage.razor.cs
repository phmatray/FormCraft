using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForMudBlazor;

public partial class FieldValidationMessage : IDisposable
{
    [Parameter] public string FieldName { get; set; } = string.Empty;
    [CascadingParameter] private EditContext? EditContext { get; set; }

    private IEnumerable<string> ValidationErrors =>
        EditContext?.GetValidationMessages(EditContext.Field(FieldName)) ?? Enumerable.Empty<string>();

    private bool HasValidationErrors => ValidationErrors.Any();

    protected override void OnInitialized()
    {
        if (EditContext != null)
        {
            EditContext.OnValidationStateChanged += HandleValidationStateChanged;
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        if (EditContext != null)
        {
            EditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        }
    }
}