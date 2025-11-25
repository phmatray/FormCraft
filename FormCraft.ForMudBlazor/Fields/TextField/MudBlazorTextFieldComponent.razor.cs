using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorTextFieldComponent<TModel>
{
    private bool _passwordVisible;

    public int Lines { get; set; } = 1;
    public int? MaxLength { get; set; }
    public string InputType { get; set; } = "text";
    public string? Mask { get; set; }
    public Adornment? Adornment { get; set; }
    public string? AdornmentIcon { get; set; }
    public Color AdornmentColor { get; set; } = Color.Default;
    public Action<string?>? OnAdornmentClick { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Load configuration - prioritize field.InputType over AdditionalAttributes
        Lines = GetAttribute("Lines", 1);
        MaxLength = GetAttribute<int?>("MaxLength");
        InputType = Context.Field.InputType ?? GetAttribute("InputType", "text") ?? "text";
        Mask = GetAttribute<string?>("Mask");

        // Load adornment configuration
        var customAdornment = GetAttribute<Adornment?>("Adornment");
        var customAdornmentIcon = GetAttribute<string?>("AdornmentIcon");
        var customAdornmentColor = GetAttribute("AdornmentColor", Color.Default);

        // Check for password visibility toggle
        var enablePasswordToggle = GetAttribute("EnablePasswordToggle", false);
        if (enablePasswordToggle && InputType.ToLowerInvariant() == "password")
        {
            // Password toggle always goes at the end
            Adornment = MudBlazor.Adornment.End;
            AdornmentIcon = Icons.Material.Filled.Visibility;
            AdornmentColor = Color.Default;
            OnAdornmentClick = TogglePasswordVisibility;
        }
        else if (customAdornment.HasValue)
        {
            // Use custom adornment if no password toggle
            Adornment = customAdornment;
            AdornmentIcon = customAdornmentIcon;
            AdornmentColor = customAdornmentColor;
        }
    }

    private InputType GetInputType()
    {
        // If password toggle is enabled and password is visible, show as text
        if (_passwordVisible && InputType.ToLowerInvariant() == "password")
        {
            return MudBlazor.InputType.Text;
        }

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

    private void TogglePasswordVisibility(string? value = null)
    {
        _passwordVisible = !_passwordVisible;
        AdornmentIcon = _passwordVisible ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility;
        StateHasChanged();
    }

    private void HandleAdornmentClick()
    {
        OnAdornmentClick?.Invoke(CurrentValue);
    }

    private async Task HandleValueChanged(string value)
    {
        // Directly invoke the parent callback to update the model and await it
        // This ensures the async operation completes before we continue
        await Context.OnValueChanged.InvokeAsync(value);

        // Update the base class's CurrentValue to keep the local state in sync
        // Note: This will trigger another async notification but since the
        // value in the model is already updated, it's effectively a no-op
        CurrentValue = value;
    }
}