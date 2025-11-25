using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.ForMudBlazor;

/// <summary>
/// MudBlazor implementation of the UI framework adapter.
/// </summary>
public class MudBlazorUIFrameworkAdapter : IUIFrameworkAdapter
{
    /// <inheritdoc />
    public string FrameworkName => "MudBlazor";

    /// <inheritdoc />
    public IFieldRenderer TextFieldRenderer => new MudBlazorTextFieldRenderer();

    /// <inheritdoc />
    public IFieldRenderer NumericFieldRenderer => new MudBlazorNumericFieldRenderer();

    /// <inheritdoc />
    public IFieldRenderer BooleanFieldRenderer => new MudBlazorBooleanFieldRenderer();

    /// <inheritdoc />
    public IFieldRenderer DateTimeFieldRenderer => new MudBlazorDateTimeFieldRenderer();

    /// <inheritdoc />
    public IFieldRenderer SelectFieldRenderer => new MudBlazorSelectFieldRenderer();

    /// <inheritdoc />
    public IFieldRenderer FileUploadFieldRenderer => new MudBlazorFileUploadFieldRenderer();

    /// <inheritdoc />
    public RenderFragment RenderForm(RenderFragment content, string? cssClass = null)
    {
        return builder =>
        {
            var sequence = 0;
            builder.OpenComponent<MudForm>(sequence++);

            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                builder.AddAttribute(sequence++, "Class", cssClass);
            }

            builder.AddAttribute(sequence, "ChildContent", content);
            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public RenderFragment RenderFieldGroup(string? title, RenderFragment content, string? cssClass = null)
    {
        return builder =>
        {
            var sequence = 0;
            builder.OpenComponent<MudPaper>(sequence++);
            builder.AddAttribute(sequence++, "Class", $"pa-4 mb-4 {cssClass}");
            builder.AddAttribute(sequence++, "Elevation", 0);
            builder.AddAttribute(sequence++, "Outlined", true);

            builder.AddAttribute(sequence, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                var innerSeq = 0;

                if (!string.IsNullOrWhiteSpace(title))
                {
                    groupBuilder.OpenComponent<MudText>(innerSeq++);
                    groupBuilder.AddAttribute(innerSeq++, "Typo", Typo.h6);
                    groupBuilder.AddAttribute(innerSeq++, "Class", "mb-3");
                    groupBuilder.AddAttribute(innerSeq++, "ChildContent", (RenderFragment)(b => b.AddContent(0, title)));
                    groupBuilder.CloseComponent();
                }

                groupBuilder.AddContent(innerSeq, content);
            }));

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public RenderFragment RenderSubmitButton(string text, bool isSubmitting, EventCallback onClick, string? cssClass = null)
    {
        return builder =>
        {
            var sequence = 0;
            builder.OpenComponent<MudButton>(sequence++);
            builder.AddAttribute(sequence++, "ButtonType", ButtonType.Submit);
            builder.AddAttribute(sequence++, "Variant", Variant.Filled);
            builder.AddAttribute(sequence++, "Color", Color.Primary);
            builder.AddAttribute(sequence++, "Disabled", isSubmitting);
            builder.AddAttribute(sequence++, "OnClick", onClick);

            if (!string.IsNullOrWhiteSpace(cssClass))
            {
                builder.AddAttribute(sequence++, "Class", cssClass);
            }

            builder.AddAttribute(sequence, "ChildContent", (RenderFragment)(buttonBuilder =>
            {
                if (isSubmitting)
                {
                    buttonBuilder.OpenComponent<MudProgressCircular>(0);
                    buttonBuilder.AddAttribute(1, "Size", Size.Small);
                    buttonBuilder.AddAttribute(2, "Indeterminate", true);
                    buttonBuilder.CloseComponent();
                    buttonBuilder.AddContent(3, " ");
                }
                buttonBuilder.AddContent(4, text);
            }));

            builder.CloseComponent();
        };
    }

    /// <inheritdoc />
    public RenderFragment RenderValidationMessages(IEnumerable<string> messages)
    {
        return builder =>
        {
            var sequence = 0;
            var messagesList = messages.ToList();

            if (messagesList.Any())
            {
                foreach (var message in messagesList)
                {
                    builder.OpenElement(sequence++, "div");
                    builder.AddAttribute(sequence++, "class", "mud-input-error");
                    builder.AddContent(sequence++, message);
                    builder.CloseElement();
                }
            }
        };
    }
}