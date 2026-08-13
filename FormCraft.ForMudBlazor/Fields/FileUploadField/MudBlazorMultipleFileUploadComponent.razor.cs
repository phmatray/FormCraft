using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorMultipleFileUploadComponent<TModel>
{
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
    private string _dragClass = DefaultDragClass;

    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mud-width-full mud-height-full d-flex justify-center align-center";

    /// <summary>
    /// Variant applied to the Browse/Clear buttons. Honors the field-level "Variant"
    /// attribute (set via .WithVariant(...)); defaults to Filled to preserve the
    /// historical button styling. The form-level default variant targets input
    /// fields and intentionally does not restyle these buttons.
    /// </summary>
    protected Variant ButtonVariant => GetAttribute<Variant?>("Variant") ?? Variant.Filled;

    public string? Accept { get; set; }
    public int MaxFiles { get; set; } = 10;
    public long? MaxFileSize { get; set; }
    public bool ShowPreview { get; set; } = true;
    public bool EnableDragDrop { get; set; } = true;

    /// <inheritdoc />
    /// <remarks>
    /// Moved off <c>OnInitialized</c> so a component instance handed a different field re-reads it
    /// rather than rendering the previous field's settings (#298).
    /// </remarks>
    protected override void OnFieldConfigurationChanged()
    {
        base.OnFieldConfigurationChanged();

        // Get configuration from FileUploadConfiguration if available
        var config = GetAttribute<FileUploadConfiguration>("FileUploadConfiguration");
        if (config != null)
        {
            Accept = string.Join(",", config.AcceptedFileTypes ?? Array.Empty<string>());
            MaxFiles = config.MaxFiles;
            MaxFileSize = config.MaxFileSize;
            ShowPreview = config.ShowPreview;
            EnableDragDrop = config.EnableDragDrop;
        }
        else
        {
            // Fallback to individual attributes
            Accept = GetAttribute<string>("Accept");
            MaxFiles = GetAttribute("MaxFiles", 10);
            MaxFileSize = GetAttribute<long?>("MaxFileSize");
            ShowPreview = GetAttribute("ShowPreview", true);
            EnableDragDrop = GetAttribute("EnableDragDrop", true);
        }
    }

    private void SetDragClass()
    {
        _dragClass = $"{DefaultDragClass} mud-border-primary";
    }

    private void ClearDragClass()
    {
        _dragClass = DefaultDragClass;
    }

    private Task OpenFilePickerAsync()
        => _fileUpload?.OpenFilePickerAsync() ?? Task.CompletedTask;

    private async Task ClearAsync()
    {
        CurrentValue = new List<IBrowserFile>();

        if (_fileUpload is not null)
        {
            await _fileUpload.ClearAsync();
        }

        // Clear All's own @if is now false, so the button the user activated has unmounted. Move
        // focus deliberately or it falls to <body> (#281). Reuses the shared base member so this
        // component and the single-file one cannot drift.
        await FocusBrowseAsync();
    }

    private async Task RemoveFile(IBrowserFile fileToRemove)
    {
        if (CurrentValue != null)
        {
            var fileList = CurrentValue.ToList();
            fileList.Remove(fileToRemove);
            CurrentValue = fileList;
        }

        // ONLY when that was the last file. The chip loop is keyless, so with files still left the
        // diff *retains* the close button the user activated — it simply becomes the next file's —
        // and focus was never lost. Moving it to Browse anyway would make removing three files mean
        // tabbing back into the chip stack twice. It is the empty case that unmounts the whole chip
        // stack and "Clear All" together, leaving Browse as the only survivor (#318).
        if (CurrentValue?.Any() != true)
        {
            await FocusBrowseAsync();
        }
    }

    private string GetHeight()
    {
        return ShowPreview && CurrentValue?.Any() == true ? "250px" : "180px";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0)
        {
            return "0 Bytes";
        }

        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
            {
                return $"{decimal.Divide(bytes, max):##.##} {order}";
            }

            max /= scale;
        }
        return "0 Bytes";
    }
}
