using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a <c>List&lt;IBrowserFile&gt;</c> field as a multiple-file Fluent UI file input.
/// </summary>
/// <typeparam name="TModel">The form's model type.</typeparam>
public partial class FluentUIMultipleFileUploadComponent<TModel>
{
    private readonly string _browseButtonId = $"formcraft-upload-{Guid.NewGuid():N}";

    /// <summary>
    /// The id the hidden file input anchors to, so clicking the visible button opens the picker.
    /// </summary>
    private string BrowseButtonId => _browseButtonId;

    /// <summary>The accepted file types, from <c>.WithAttribute("Accept", ...)</c>.</summary>
    private string? Accept => GetAttribute<string>("Accept");

    /// <summary>The largest accepted file, in bytes. Defaults to 10 MB, as MudBlazor's does.</summary>
    private long MaximumFileSize => GetAttribute("MaximumFileSize", 10L * 1024 * 1024);

    /// <summary>The most files that may be chosen at once.</summary>
    private int MaximumFileCount => GetAttribute("MaximumFileCount", 10);

    /// <summary>The chosen files, shown back to the user.</summary>
    private IReadOnlyList<IBrowserFile> SelectedFiles => CurrentValue ?? [];

    private async Task HandleFilesChangedAsync(InputFileChangeEventArgs args)
    {
        var files = args.GetMultipleFiles(MaximumFileCount).ToList();

        SetValueWithoutNotification(files);
        await Context.OnValueChanged.InvokeAsync(files);
    }
}
