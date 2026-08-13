using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorFileUploadFieldComponent<TModel>
{
    private MudFileUpload<IBrowserFile>? _fileUpload;
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
    public bool AllowMultiple { get; set; }
    public long? MaxFileSize { get; set; }
    public int? MaxFiles { get; set; }
    public bool ShowPreview { get; set; }
    public bool EnableDragDrop { get; set; } = true;
    public FileUploadMode UploadMode { get; set; } = FileUploadMode.Immediate;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Accept = GetAttribute<string>("Accept");
        AllowMultiple = GetAttribute<bool>("AllowMultiple");
        MaxFileSize = GetAttribute<long?>("MaxFileSize");
        MaxFiles = GetAttribute<int?>("MaxFiles");
        ShowPreview = GetAttribute<bool>("ShowPreview");
        EnableDragDrop = GetAttribute("EnableDragDrop", true);
        UploadMode = GetAttribute("UploadMode", FileUploadMode.Immediate);
    }

    private Task OnFileChanged(IBrowserFile? file)
    {
        CurrentValue = file;
        ClearDragClass();
        StateHasChanged();
        return Task.CompletedTask;
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
        CurrentValue = null;

        if (_fileUpload is not null)
        {
            await _fileUpload.ClearAsync();
        }

        // Clear's own @if is now false, so the button the user activated has unmounted. Move focus
        // deliberately or it falls to <body> (#281).
        await FocusBrowseAsync();
    }

    private string GetHeight()
    {
        return ShowPreview && CurrentValue != null ? "200px" : "150px";
    }

    private static string FormatFileSize(long bytes)
    {
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
