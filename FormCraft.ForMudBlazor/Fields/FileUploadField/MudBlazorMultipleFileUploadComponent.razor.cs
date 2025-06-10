using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorMultipleFileUploadComponent<TModel>
{
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
    private string _dragClass = DefaultDragClass;
    
    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mud-width-full mud-height-full d-flex justify-center align-center";
    
    public string? Accept { get; set; }
    public int MaxFiles { get; set; } = 10;
    public long? MaxFileSize { get; set; }
    public bool ShowPreview { get; set; } = true;
    public bool EnableDragDrop { get; set; } = true;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
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

    private Task ClearAsync()
    {
        CurrentValue = new List<IBrowserFile>();
        return _fileUpload?.ClearAsync() ?? Task.CompletedTask;
    }

    private void RemoveFile(IBrowserFile fileToRemove)
    {
        if (CurrentValue != null)
        {
            var fileList = CurrentValue.ToList();
            fileList.Remove(fileToRemove);
            CurrentValue = fileList;
        }
    }

    private string GetHeight()
    {
        return ShowPreview && CurrentValue?.Any() == true ? "250px" : "180px";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 Bytes";
        
        const int scale = 1024;
        string[] orders = { "GB", "MB", "KB", "Bytes" };
        long max = (long)Math.Pow(scale, orders.Length - 1);

        foreach (string order in orders)
        {
            if (bytes > max)
                return $"{decimal.Divide(bytes, max):##.##} {order}";

            max /= scale;
        }
        return "0 Bytes";
    }
}