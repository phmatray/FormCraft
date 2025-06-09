using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorFileUploadFieldComponent<TModel>
{
    private string _dragClass = "";
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
        return Task.CompletedTask;
    }

    private void SetDragClass()
    {
        _dragClass = "mud-border-primary";
    }

    private void ClearDragClass()
    {
        _dragClass = "";
    }
}