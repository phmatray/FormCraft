using Microsoft.FluentUI.AspNetCore.Components;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Renders a <see cref="string"/> field as a Fluent UI text input, or as a text area when the field
/// configures more than one line.
/// </summary>
public partial class FluentUITextFieldComponent<TModel>
{
    private string? _localValue;

    /// <summary>The number of lines the field renders with. More than one selects a text area.</summary>
    private int Lines { get; set; } = 1;

    /// <summary>The Fluent input type resolved from the field's configured input-type string.</summary>
    private TextInputType InputType { get; set; } = TextInputType.Text;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        _localValue = CurrentValue;

        var configuredInputType = Context.Field.InputType ?? GetAttribute("InputType", "text") ?? "text";
        InputType = FluentTextInputTypeMap.Resolve(configuredInputType);

        // A masked field is forced back to a single line, for the same reason the MudBlazor adapter
        // does it (#207): a text area has no `type` attribute and therefore cannot mask, so
        // honouring Lines on a password field would render the credential in clear text.
        Lines = FluentTextInputTypeMap.EffectiveLines(InputType, GetAttribute("Lines", 1));
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Sync the local value when the model changes externally (dependency callbacks, form reset).
        // Guarded so an in-flight edit is not wiped mid-keystroke.
        if (CurrentValue != _localValue)
        {
            _localValue = CurrentValue;
        }
    }

    private async Task OnLocalValueChanged()
    {
        SetValueWithoutNotification(_localValue);
        await Context.OnValueChanged.InvokeAsync(_localValue);
    }
}

/// <summary>
/// Maps FormCraft's input-type string onto Fluent UI's <see cref="TextInputType"/>.
/// </summary>
/// <remarks>
/// The Fluent counterpart of <c>TextInputTypeMap</c> in the MudBlazor adapter. It is deliberately a
/// separate map rather than a shared one: the two enums are different types from different packages,
/// and the core project must stay free of both (a shared map would have to live somewhere that
/// references neither).
/// </remarks>
internal static class FluentTextInputTypeMap
{
    /// <summary>The input type a field renders with when it configures none.</summary>
    internal const string Default = "text";

    /// <summary>
    /// Maps a configured input-type string onto Fluent's enum, falling back to
    /// <see cref="TextInputType.Text"/> for null and for any value this library does not recognise.
    /// </summary>
    /// <remarks>
    /// <c>date</c> and <c>time</c> deliberately fall through to <see cref="TextInputType.Text"/>:
    /// Fluent's enum has no member for either, because it renders those through the dedicated
    /// <c>FluentDatePicker</c>/<c>FluentTimePicker</c> components instead. A <see cref="string"/>
    /// field asking for them is asking for something this input cannot be.
    /// </remarks>
    internal static TextInputType Resolve(string? inputType) =>
        (inputType ?? Default).ToLowerInvariant() switch
        {
            "email" => TextInputType.Email,
            "password" => TextInputType.Password,
            "tel" or "telephone" => TextInputType.Telephone,
            "url" => TextInputType.Url,
            "search" => TextInputType.Search,
            "number" => TextInputType.Number,
            "color" => TextInputType.Color,
            _ => TextInputType.Text,
        };

    /// <summary>
    /// The number of lines a field actually renders with: always 1 for a password field, otherwise
    /// whatever was configured.
    /// </summary>
    internal static int EffectiveLines(TextInputType resolved, int configuredLines) =>
        resolved == TextInputType.Password ? 1 : configuredLines;
}
