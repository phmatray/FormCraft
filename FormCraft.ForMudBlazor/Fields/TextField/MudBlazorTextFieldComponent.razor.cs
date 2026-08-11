using MudBlazor;

namespace FormCraft.ForMudBlazor;

public partial class MudBlazorTextFieldComponent<TModel>
{
    private bool _passwordVisible;
    private string? _localValue;

    public int Lines { get; set; } = 1;
    public int? MaxLength { get; set; }
    public string InputType { get; set; } = "text";
    public string? Autocomplete { get; set; }
    public string? Mask { get; set; }
    public Adornment? Adornment { get; set; }
    public string? AdornmentIcon { get; set; }
    public Color AdornmentColor { get; set; } = Color.Default;
    public Action<string?>? OnAdornmentClick { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Initialize local value from CurrentValue
        _localValue = CurrentValue;

        // Load configuration - prioritize field.InputType over AdditionalAttributes
        Lines = GetAttribute("Lines", 1);
        MaxLength = GetAttribute<int?>("MaxLength");
        InputType = Context.Field.InputType ?? GetAttribute("InputType", "text") ?? "text";
        Autocomplete = GetAttribute<string?>("autocomplete");
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

            // Stays null when WithAdornment got no handler, which leaves HandleAdornmentClick a
            // no-op rather than a throw (#192). The password branch above deliberately keeps its
            // own toggle: it owns the adornment slot, so a configured handler must not displace it.
            OnAdornmentClick = GetAttribute<Action<string?>?>(
                MudBlazorFieldBuilderExtensions.AdornmentClickAttribute);
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync local value when model changes externally (e.g., form reset)
        // Only update if the value actually changed to avoid breaking user input
        if (CurrentValue != _localValue)
        {
            _localValue = CurrentValue;
        }
    }

    private InputType GetInputType()
    {
        // If password toggle is enabled and password is visible, show as text
        if (_passwordVisible && InputType.ToLowerInvariant() == "password")
        {
            return MudBlazor.InputType.Text;
        }

        return TextInputTypeMap.Resolve(InputType);
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

    private async Task OnLocalValueChanged()
    {
        // Sync to base class
        SetValueWithoutNotification(_localValue);

        // Notify parent to update the model
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}

/// <summary>
/// The single implementation of FormCraft's input-type string to <see cref="InputType"/> mapping,
/// shared by the component render path (<see cref="MudBlazorTextFieldComponent{TModel}"/>) and the
/// imperative RenderTreeBuilder path used for collection item fields.
/// </summary>
/// <remarks>
/// Extracted in #189. The collection path previously emitted no input type at all, so a
/// <c>.AsPassword()</c> field inside <c>.WithItemForm(...)</c> rendered its characters in clear
/// text. Duplicating the mapping to fix that would have set up the next divergence, so both paths
/// resolve through here instead.
/// </remarks>
internal static class TextInputTypeMap
{
    /// <summary>The input type a field renders with when it configures none.</summary>
    internal const string Default = "text";

    /// <summary>
    /// Maps a configured input-type string onto MudBlazor's enum, falling back to
    /// <see cref="InputType.Text"/> for null and for any value this library does not recognise.
    /// </summary>
    /// <remarks>
    /// The recognised set is carried over unchanged from the component path, so that #189 moved the
    /// mapping without changing what it renders. It is **not** complete: <c>"number"</c>,
    /// <c>"date"</c> and <c>"time"</c> fall through to <see cref="InputType.Text"/> even though
    /// MudBlazor has arms for them and FormCraft's own auto-builders emit those strings — a field
    /// so configured loses its mobile keypad and native picker, silently. Widening this changes
    /// what the component path renders too, so it is tracked separately rather than smuggled into
    /// a parity fix.
    /// </remarks>
    internal static InputType Resolve(string? inputType) =>
        (inputType ?? Default).ToLowerInvariant() switch
        {
            "email" => InputType.Email,
            "password" => InputType.Password,
            "tel" or "telephone" => InputType.Telephone,
            "url" => InputType.Url,
            "search" => InputType.Search,
            _ => InputType.Text,
        };
}
