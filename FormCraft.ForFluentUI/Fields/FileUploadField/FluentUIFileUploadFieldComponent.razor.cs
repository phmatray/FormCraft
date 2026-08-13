using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a single-file <c>IBrowserFile</c> field as a Fluent UI file input.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
public partial class FluentUIFileUploadFieldComponent<TModel>
{
    private readonly string _browseButtonId = $"formcraft-upload-{Guid.NewGuid():N}";

    /// <summary>
    /// The id the hidden file input anchors to, so clicking the visible button opens the picker.
    /// Unique per instance for the same reason the hint id is.
    /// </summary>
    private string BrowseButtonId => _browseButtonId;

    /// <summary>The accepted file types, from <c>.WithAttribute("Accept", ...)</c>.</summary>
    private string? Accept => GetAttribute<string>("Accept");

    /// <summary>The largest accepted file, in bytes. Defaults to 10 MB, as MudBlazor's does.</summary>
    private long MaximumFileSize => GetAttribute("MaximumFileSize", 10L * 1024 * 1024);

    /// <summary>The chosen file's name, shown back to the user once one is picked.</summary>
    private string? SelectedFileName => CurrentValue?.Name;

    private async Task HandleFilesChangedAsync(InputFileChangeEventArgs args)
    {
        var file = args.FileCount > 0 ? args.File : null;

        SetValueWithoutNotification(file);
        await Context.OnValueChanged.InvokeAsync(file);
    }
}
