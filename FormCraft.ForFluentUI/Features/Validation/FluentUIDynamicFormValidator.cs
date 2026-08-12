using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft.ForFluentUI;

/// <summary>
/// Runs FormCraft's configured field validators against Blazor's <see cref="EditContext"/>, so a
/// <c>.Required(...)</c> or <c>.WithValidator(...)</c> rule produces a real validation message.
/// Place it inside an <see cref="EditForm"/>; <see cref="FormCraftComponent{TModel}"/> does.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>This duplicates the non-collection half of
/// <c>FormCraft.ForMudBlazor.DynamicFormValidator&lt;TModel&gt;</c>, and the two must agree.</b>
/// That type is entirely UI-framework-agnostic - it references nothing from MudBlazor - and lives in
/// the MudBlazor package only by history. The right shape is one copy in <c>FormCraft</c> core that
/// both adapters use, but moving it there changes its namespace and so breaks the MudBlazor
/// package's public API, which is the owner's call rather than this issue's. Tracked as a follow-up
/// on #260.
/// </para>
/// <para>
/// The collection half is deliberately absent: collection/item-form fields are not implemented for
/// Fluent yet (blocked on #203), so validating them here would report on fields this adapter never
/// renders.
/// </para>
/// </remarks>
/// <typeparam name="TModel">The form's model type.</typeparam>
public class FluentUIDynamicFormValidator<TModel> : ComponentBase, IDisposable where TModel : new()
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the form configuration containing field definitions and validation rules.
    /// </summary>
    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    [CascadingParameter]
    private EditContext CascadedEditContext { get; set; } = default!;

    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var editContext = CascadedEditContext ?? throw new InvalidOperationException(
            $"{nameof(FluentUIDynamicFormValidator<TModel>)} requires a cascading parameter of type " +
            $"{nameof(EditContext)}. For example, use it inside an {nameof(EditForm)}.");

        _editContext = editContext;
        _messageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += HandleValidationRequested;
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    /// <summary>
    /// Runs every configured validator for visible fields, updates the validation message store,
    /// and reports whether the model is valid. Unlike <see cref="EditContext.Validate"/>, this
    /// awaits asynchronous validators before returning.
    /// </summary>
    public async Task<bool> ValidateModelAsync()
    {
        var model = (TModel)_editContext!.Model;

        _messageStore!.Clear();

        foreach (var field in Configuration.Fields)
        {
            // Hidden fields must not block submission with errors nobody can see or correct.
            if (!IsFieldVisible(field, model))
            {
                continue;
            }

            var value = field.ValueExpression.Compile()(model);

            foreach (var validator in field.Validators)
            {
                var result = await validator.ValidateAsync(model, value, ServiceProvider);
                if (!result.IsValid)
                {
                    _messageStore.Add(_editContext.Field(field.FieldName), result.ErrorMessage!);
                }
            }
        }

        _editContext.NotifyValidationStateChanged();
        return !_editContext.GetValidationMessages().Any();
    }

    private static bool IsFieldVisible(IFieldConfiguration<TModel, object> field, TModel model) =>
        field.VisibilityCondition?.Invoke(model) ?? field.IsVisible;

    private async void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        // EditContext.Validate() is synchronous and returns before this handler's first await
        // completes, so it cannot gate submission when async validators are configured.
        // FormCraftComponent awaits ValidateModelAsync() directly on submit; this handler only
        // keeps EditContext.Validate() callers working for synchronously-completing validators.
        try
        {
            await ValidateModelAsync();
        }
        catch
        {
            // An exception escaping an async void handler would take the circuit down with it.
        }
    }

    private async void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        try
        {
            var fieldConfig = Configuration.Fields
                .FirstOrDefault(f => f.FieldName == e.FieldIdentifier.FieldName);
            if (fieldConfig is null)
            {
                return;
            }

            var model = (TModel)_editContext!.Model;
            var value = fieldConfig.ValueExpression.Compile()(model);

            // Clear this field's messages only, so correcting one field does not wipe another's.
            _messageStore!.Clear(e.FieldIdentifier);

            foreach (var validator in fieldConfig.Validators)
            {
                var result = await validator.ValidateAsync(model, value, ServiceProvider);
                if (!result.IsValid)
                {
                    _messageStore.Add(e.FieldIdentifier, result.ErrorMessage!);
                }
            }

            _editContext.NotifyValidationStateChanged();
        }
        catch
        {
            // As above: an async void handler must not throw.
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_editContext is not null)
        {
            _editContext.OnValidationRequested -= HandleValidationRequested;
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }

        GC.SuppressFinalize(this);
    }
}
