using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FormCraft;

/// <summary>
/// A validation component that integrates Dynamic Form validation with Blazor's EditContext.
/// This component handles both form-level and field-level validation using the configured validators.
/// Add this component inside an EditForm to enable dynamic validation.
/// </summary>
/// <remarks>
/// UI-framework-agnostic — it references only <c>Microsoft.AspNetCore.Components</c>. It shipped
/// inside <c>FormCraft.ForMudBlazor</c> until #279, which is why the second adapter had to write its
/// own copy of the non-collection half rather than reuse this one. Every adapter's form container
/// now renders this component.
/// </remarks>
/// <typeparam name="TModel">The form's model type.</typeparam>
public class DynamicFormValidator<TModel> : ComponentBase, IDisposable where TModel : new()
{
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the form configuration containing field definitions and validation rules.
    /// </summary>
    [Parameter]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;

    /// <summary>
    /// Whether collection fields are validated. Defaults to <c>true</c>; set it to <c>false</c> in
    /// an adapter whose container does not render collection/item-form fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ <b>Not a preference — a correctness requirement for such an adapter.</b> A collection
    /// error is attached to the collection's own field identifier, so a container that renders no
    /// control for it also renders no <c>ValidationMessage</c> for it. The form then reports
    /// invalid with nothing on screen to explain why and no input the user could correct, and since
    /// submit is gated on the result, the submit button silently stops working.
    /// </para>
    /// <para>
    /// This is why <c>FluentUIDynamicFormValidator</c> omitted the collection half while it existed
    /// (#260), and the flag is how that decision survives sharing one implementation (#279). Turn it
    /// back on for an adapter once that adapter renders collection fields — not before.
    /// </para>
    /// </remarks>
    [Parameter]
    public bool ValidateCollections { get; set; } = true;

    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;

    protected override void OnInitialized()
    {
        var editContext = CascadedEditContext ?? throw new InvalidOperationException(
            $"{nameof(DynamicFormValidator<TModel>)} requires a cascading parameter of type {nameof(EditContext)}. " +
            $"For example, you can use {nameof(DynamicFormValidator<TModel>)} inside an {nameof(EditForm)}.");

        _editContext = editContext;
        _messageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += HandleValidationRequested;
        _editContext.OnFieldChanged += HandleFieldChanged;
    }

    [CascadingParameter] private EditContext CascadedEditContext { get; set; } = default!;

    private async void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        // EditContext.Validate() is synchronous and returns before this handler's
        // first await completes, so it cannot reliably gate submission when async
        // validators are configured. FormCraftComponent awaits ValidateModelAsync()
        // directly on submit; this handler only keeps EditContext.Validate() callers
        // working for synchronously-completing validators.
        try
        {
            await ValidateModelAsync();
        }
        catch
        {
            // Exceptions escaping an async void handler would crash the circuit.
        }
    }

    /// <summary>
    /// Runs all configured validators for visible fields and collection fields,
    /// updates the validation message store, and returns whether the model is valid.
    /// Unlike <see cref="EditContext.Validate"/>, this method awaits asynchronous
    /// validators before reporting the result.
    /// </summary>
    public async Task<bool> ValidateModelAsync()
    {
        var model = (TModel)_editContext!.Model;

        // Clear all existing custom validation messages
        _messageStore!.Clear();

        foreach (var field in Configuration.Fields)
        {
            // Hidden fields must not block submission with invisible errors
            if (!IsFieldVisible(field, model))
            {
                continue;
            }

            var getter = FieldValueGetterCache<TModel>.GetOrCompile(field);
            var value = getter(model);

            foreach (var validator in field.Validators)
            {
                var result = await validator.ValidateAsync(model, value, ServiceProvider);
                if (!result.IsValid)
                {
                    _messageStore.Add(_editContext.Field(field.FieldName), result.ErrorMessage!);
                }
            }
        }

        // Validate collection fields. FormConfiguration<TModel> always implements the interface, so
        // ValidateCollections is what actually decides this for an adapter that renders no
        // collection UI - see the parameter's remarks.
        if (ValidateCollections && Configuration is ICollectionFormConfiguration<TModel> collectionConfig)
        {
            foreach (var collectionField in collectionConfig.CollectionFields)
            {
                var errors = await ValidateCollectionFieldAsync(model, collectionField);
                foreach (var error in errors)
                {
                    _messageStore.Add(_editContext.Field(collectionField.FieldName), error);
                }

                // Additionally attach per-item errors to nested field identifiers
                // (e.g. Items[0].ProductName) so ValidationMessage/ValidationSummary
                // and FieldValidationMessage can display them natively.
                var itemErrors = await ValidateCollectionItemsAsync(model, collectionField);
                foreach (var itemError in itemErrors)
                {
                    _messageStore.Add(
                        CreateCollectionItemFieldIdentifier(collectionField.FieldName, itemError.ItemIndex, itemError.FieldName),
                        itemError.Message);
                }
            }
        }

        _editContext.NotifyValidationStateChanged();
        return !_editContext.GetValidationMessages().Any();
    }

    private static bool IsFieldVisible(IFieldConfiguration<TModel, object> field, TModel model)
    {
        if (field.VisibilityCondition != null)
        {
            return field.VisibilityCondition(model);
        }

        return field.IsVisible;
    }

    private async Task<List<string>> ValidateCollectionFieldAsync(TModel model, ICollectionFieldConfigurationBase collectionField)
    {
        // Use reflection to create the typed validator and invoke it
        var validatorType = typeof(CollectionFieldValidator<,>).MakeGenericType(typeof(TModel), collectionField.ItemType);
        var validator = Activator.CreateInstance(validatorType, collectionField);

        var validateMethod = validatorType.GetMethod("ValidateAsync");
        if (validateMethod == null)
        {
            return new List<string>();
        }

        var task = (Task<List<string>>)validateMethod.Invoke(validator, new object[] { model!, ServiceProvider })!;
        return await task;
    }

    private async Task<List<CollectionItemError>> ValidateCollectionItemsAsync(TModel model, ICollectionFieldConfigurationBase collectionField)
    {
        // Use reflection to create the typed validator and invoke it
        var validatorType = typeof(CollectionFieldValidator<,>).MakeGenericType(typeof(TModel), collectionField.ItemType);
        var validator = Activator.CreateInstance(validatorType, collectionField);

        var validateMethod = validatorType.GetMethod("ValidateItemsAsync");
        if (validateMethod == null)
        {
            return new List<CollectionItemError>();
        }

        var task = (Task<List<CollectionItemError>>)validateMethod.Invoke(validator, new object[] { model!, ServiceProvider })!;
        return await task;
    }

    private FieldIdentifier CreateCollectionItemFieldIdentifier(string collectionFieldName, int itemIndex, string itemFieldName)
        => new(_editContext!.Model, $"{collectionFieldName}[{itemIndex}].{itemFieldName}");

    // Matches nested collection item field names such as "Items[0].ProductName".
    private static readonly System.Text.RegularExpressions.Regex CollectionItemFieldPattern =
        new(@"^(?<collection>[A-Za-z_]\w*)\[(?<index>\d+)\]\.(?<field>[A-Za-z_]\w*)$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private async void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        try
        {
            // Nested collection item identifiers (Items[0].ProductName) are validated
            // against the owning collection field's item form configuration.
            var nestedMatch = CollectionItemFieldPattern.Match(e.FieldIdentifier.FieldName);
            if (ValidateCollections && nestedMatch.Success)
            {
                await ValidateCollectionItemFieldAsync(e.FieldIdentifier, nestedMatch);
                return;
            }

            // Find the field configuration for the changed field
            var fieldConfig = Configuration.Fields.FirstOrDefault(f => f.FieldName == e.FieldIdentifier.FieldName);
            if (fieldConfig == null)
            {
                return;
            }

            var model = (TModel)_editContext!.Model;
            var getter = FieldValueGetterCache<TModel>.GetOrCompile(fieldConfig);
            var value = getter(model);

            // Clear existing messages for this field only
            _messageStore!.Clear(e.FieldIdentifier);

            // Validate the specific field
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
            // Exceptions escaping an async void handler would crash the circuit.
        }
    }

    private async Task ValidateCollectionItemFieldAsync(FieldIdentifier fieldIdentifier, System.Text.RegularExpressions.Match nestedMatch)
    {
        if (Configuration is not ICollectionFormConfiguration<TModel> collectionConfig)
        {
            return;
        }

        var collectionFieldName = nestedMatch.Groups["collection"].Value;
        var itemIndex = int.Parse(nestedMatch.Groups["index"].Value);
        var itemFieldName = nestedMatch.Groups["field"].Value;

        var collectionField = collectionConfig.CollectionFields
            .FirstOrDefault(f => f.FieldName == collectionFieldName);
        if (collectionField == null)
        {
            return;
        }

        var model = (TModel)_editContext!.Model;

        // Clear existing messages for this nested field only, then re-validate it
        // so stale errors disappear as soon as the user corrects the value.
        _messageStore!.Clear(fieldIdentifier);

        var itemErrors = await ValidateCollectionItemsAsync(model, collectionField);
        foreach (var itemError in itemErrors)
        {
            if (itemError.ItemIndex == itemIndex && itemError.FieldName == itemFieldName)
            {
                _messageStore.Add(fieldIdentifier, itemError.Message);
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    public void Dispose()
    {
        if (_editContext != null)
        {
            _editContext.OnValidationRequested -= HandleValidationRequested;
            _editContext.OnFieldChanged -= HandleFieldChanged;
        }
    }
}
