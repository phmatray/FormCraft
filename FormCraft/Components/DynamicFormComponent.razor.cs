using Microsoft.AspNetCore.Components;

namespace FormCraft;

public partial class DynamicFormComponent<TModel>
{
    /// <summary>
    /// Gets or sets the model instance that the form binds to.
    /// </summary>
    [Parameter]
    public TModel Model { get; set; } = new();

    /// <summary>
    /// Gets or sets the form configuration that defines the fields, layout, and validation rules.
    /// </summary>
    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    /// <summary>
    /// Gets or sets the callback to invoke when the form is submitted with valid data.
    /// </summary>
    [Parameter]
    public EventCallback<TModel> OnValidSubmit { get; set; }

    /// <summary>
    /// Gets or sets the callback to invoke when any field value changes.
    /// </summary>
    [Parameter]
    public EventCallback<(string fieldName, object? value)> OnFieldChanged { get; set; }

    /// <summary>
    /// Gets or sets whether to show the submit button. Default is true.
    /// </summary>
    [Parameter]
    public bool ShowSubmitButton { get; set; } = true;

    /// <summary>
    /// Gets or sets the text to display on the submit button. Default is "Submit".
    /// </summary>
    [Parameter]
    public string SubmitButtonText { get; set; } = "Submit";

    /// <summary>
    /// Gets or sets the text to display while the form is being submitted. Default is "Submitting...".
    /// </summary>
    [Parameter]
    public string SubmittingText { get; set; } = "Submitting...";

    /// <summary>
    /// Gets or sets the CSS class to apply to the submit button. Default is "ml-auto".
    /// </summary>
    [Parameter]
    public string SubmitButtonClass { get; set; } = "ml-auto";

    /// <summary>
    /// Gets or sets whether the form is currently being submitted, which disables the submit button.
    /// </summary>
    [Parameter]
    public bool IsSubmitting { get; set; }

    private bool ShouldShowField(IFieldConfiguration<TModel, object> field)
    {
        if (field.VisibilityCondition != null)
        {
            return field.VisibilityCondition(Model);
        }

        return field.IsVisible;
    }

    private async Task OnSubmit()
    {
        if (OnValidSubmit.HasDelegate)
        {
            await OnValidSubmit.InvokeAsync(Model);
        }
    }

    private async Task HandleFieldValueChanged(string fieldName, object? value)
    {
        SetFieldValue(fieldName, value);

        if (OnFieldChanged.HasDelegate)
        {
            await OnFieldChanged.InvokeAsync((fieldName, value));
        }

        StateHasChanged();
    }

    private Task HandleFieldDependencyChanged(string fieldName)
    {
        if (Configuration.FieldDependencies.TryGetValue(fieldName, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                dependency.OnDependencyChanged(Model);
            }
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    private void SetFieldValue(string fieldName, object? value)
    {
        var property = typeof(TModel).GetProperty(fieldName);
        property?.SetValue(Model, value);
    }

    private string GetFormLayoutClass()
    {
        return Configuration.Layout switch
        {
            FormLayout.Horizontal => "row",
            FormLayout.Grid => "row",
            FormLayout.Inline => "d-flex flex-wrap gap-3",
            _ => ""
        };
    }

    private string GetFieldLayoutClass(IFieldConfiguration<TModel, object> _)
    {
        return Configuration.Layout switch
        {
            FormLayout.Horizontal => "col-md-6",
            FormLayout.Grid => "col-lg-4",
            FormLayout.Inline => "flex-fill",
            _ => ""
        };
    }
}