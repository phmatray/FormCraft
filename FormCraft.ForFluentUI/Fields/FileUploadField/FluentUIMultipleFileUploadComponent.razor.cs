using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a multiple-file <c>IBrowserFile</c> collection field as a Fluent UI file input.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
/// <remarks>
/// Bound as <c>IReadOnlyList&lt;IBrowserFile&gt;</c>, which is the type core's
/// <c>.AsMultipleFileUpload(...)</c> is declared on. Binding <c>List&lt;IBrowserFile&gt;</c>
/// instead - as an earlier draft did - left the canonical multi-upload field matching no renderer
/// at all, so it fell through to the service's "Unsupported field type" placeholder.
/// </remarks>
public partial class FluentUIMultipleFileUploadComponent<TModel>
{
    private readonly string _browseButtonId = $"formcraft-upload-{Guid.NewGuid():N}";
    private string? _tooManyFilesError;

    /// <summary>
    /// The id the hidden file input anchors to, so clicking the visible button opens the picker.
    /// </summary>
    private string BrowseButtonId => _browseButtonId;

    /// <summary>The chosen files, shown back to the user.</summary>
    private IReadOnlyList<IBrowserFile> SelectedFiles => CurrentValue ?? [];

    /// <summary>
    /// The message shown when the user picked more files than the field allows, or <c>null</c>.
    /// </summary>
    private string? TooManyFilesError => _tooManyFilesError;

    private async Task HandleFilesChangedAsync(InputFileChangeEventArgs args)
    {
        // GetMultipleFiles THROWS when the selection exceeds the cap rather than truncating, and an
        // exception out of an event handler is an unhandled Blazor error - a torn-down circuit on
        // Server, a dead page on WebAssembly - for something the user can simply be told about.
        // Checked up front so the limit reports as a message instead.
        if (args.FileCount > MaximumFileCount)
        {
            _tooManyFilesError = $"Select at most {MaximumFileCount} file{(MaximumFileCount == 1 ? "" : "s")}.";
            return;
        }

        _tooManyFilesError = null;
        IReadOnlyList<IBrowserFile> files = args.GetMultipleFiles(MaximumFileCount);

        SetValueWithoutNotification(files);
        await Context.OnValueChanged.InvokeAsync(files);
    }
}
