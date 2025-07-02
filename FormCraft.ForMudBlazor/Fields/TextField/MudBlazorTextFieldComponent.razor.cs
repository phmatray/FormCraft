using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorTextFieldComponent<TModel>
{
    public int Lines { get; set; } = 1;
    public int? MaxLength { get; set; }
    public string InputType { get; set; } = "text";
    public string? Mask { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Load configuration from additional attributes
        Lines = GetAttribute("Lines", 1);
        MaxLength = GetAttribute<int?>("MaxLength");
        InputType = GetAttribute("InputType", "text") ?? "text";
        Mask = GetAttribute<string?>("Mask");
    }

    private InputType GetInputType()
    {
        return InputType.ToLowerInvariant() switch
        {
            "email" => MudBlazor.InputType.Email,
            "password" => MudBlazor.InputType.Password,
            "tel" or "telephone" => MudBlazor.InputType.Telephone,
            "url" => MudBlazor.InputType.Url,
            "search" => MudBlazor.InputType.Search,
            _ => MudBlazor.InputType.Text
        };
    }

    private IMask? GetMask()
    {
        if (string.IsNullOrEmpty(Mask))
            return null;

        // For now, return null. In a real implementation,
        // you would parse the mask string and create appropriate IMask
        return null;
    }
}