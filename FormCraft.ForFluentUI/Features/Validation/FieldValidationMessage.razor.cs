using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Displays the validation messages the <see cref="EditContext"/> currently holds for one field.
/// </summary>
public partial class FieldValidationMessage : ComponentBase, IDisposable
{
    /// <summary>The name of the field whose messages this component displays.</summary>
    [Parameter]
    public string FieldName { get; set; } = string.Empty;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    private EditContext? _subscribed;

    private IEnumerable<string> ValidationErrors =>
        CurrentEditContext is null || string.IsNullOrEmpty(FieldName)
            ? []
            : CurrentEditContext.GetValidationMessages(CurrentEditContext.Field(FieldName));

    private bool HasValidationErrors => ValidationErrors.Any();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Re-render when the validation state changes, otherwise a message added after this
        // component's last render would never appear.
        if (ReferenceEquals(_subscribed, CurrentEditContext))
        {
            return;
        }

        Unsubscribe();
        _subscribed = CurrentEditContext;
        if (_subscribed is not null)
        {
            _subscribed.OnValidationStateChanged += HandleValidationStateChanged;
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        => StateHasChanged();

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.OnValidationStateChanged -= HandleValidationStateChanged;
            _subscribed = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Unsubscribe();
        GC.SuppressFinalize(this);
    }
}
