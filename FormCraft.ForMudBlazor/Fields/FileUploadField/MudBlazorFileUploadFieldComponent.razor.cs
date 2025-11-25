using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorFileUploadFieldComponent<TModel>
{
    private MudFileUpload<IReadOnlyList<IBrowserFile>>? _fileUpload;
    private IReadOnlyList<IBrowserFile>? _singleFileList;

    public IReadOnlyList<IBrowserFile>? SingleFileList
    {
        get => _singleFileList;
        set
        {
            if (!Equals(_singleFileList, value))
            {
                _singleFileList = value;
                CurrentValue = value?.FirstOrDefault();
                StateHasChanged();
            }
        }
    }
    private string _dragClass = DefaultDragClass;

    private const string DefaultDragClass = "relative rounded-lg border-2 border-dashed pa-4 mud-width-full mud-height-full d-flex justify-center align-center";

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

        // Initialize the single file list to sync with CurrentValue
        UpdateSingleFileList();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        UpdateSingleFileList();
    }

    private void UpdateSingleFileList()
    {
        if (CurrentValue != null)
        {
            _singleFileList = new List<IBrowserFile> { CurrentValue };
        }
        else
        {
            _singleFileList = new List<IBrowserFile>();
        }
    }

    private Task OnFilesChanged(IReadOnlyList<IBrowserFile>? files)
    {
        _singleFileList = files;
        CurrentValue = files?.FirstOrDefault();
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

    private Task ClearAsync()
    {
        CurrentValue = null;
        _singleFileList = new List<IBrowserFile>();
        return _fileUpload?.ClearAsync() ?? Task.CompletedTask;
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
                return $"{decimal.Divide(bytes, max):##.##} {order}";

            max /= scale;
        }
        return "0 Bytes";
    }
}