using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DynamicFormBlazor.Validators;

public class FluentValidationValidator<T> : ComponentBase
{
    private readonly IValidator<T> _validator;
    private ValidationMessageStore? _messageStore;

    [CascadingParameter] private EditContext? EditContext { get; set; }

    public FluentValidationValidator(IValidator<T> validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    protected override void OnInitialized()
    {
        if (EditContext == null)
        {
            throw new InvalidOperationException($"{nameof(FluentValidationValidator<T>)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(FluentValidationValidator<T>)} " +
                $"inside an EditForm.");
        }

        _messageStore = new ValidationMessageStore(EditContext);

        EditContext.OnValidationRequested += HandleValidationRequested;
        EditContext.OnFieldChanged += HandleFieldChanged;
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (EditContext?.Model is T model)
        {
            var propertyName = e.FieldIdentifier.FieldName;
            var validationContext = new ValidationContext<T>(model);
            validationContext.PropertyChain.Add(propertyName);

            var result = _validator.Validate(validationContext);

            _messageStore?.Clear(e.FieldIdentifier);

            foreach (var error in result.Errors.Where(e => e.PropertyName == propertyName))
            {
                _messageStore?.Add(e.FieldIdentifier, error.ErrorMessage);
            }

            EditContext?.NotifyValidationStateChanged();
        }
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        if (EditContext?.Model is T model)
        {
            var result = _validator.Validate(model);
            
            _messageStore?.Clear();

            foreach (var error in result.Errors)
            {
                var fieldIdentifier = new FieldIdentifier(model, error.PropertyName);
                _messageStore?.Add(fieldIdentifier, error.ErrorMessage);
            }

            EditContext?.NotifyValidationStateChanged();
        }
    }

    public void Dispose()
    {
        if (EditContext != null)
        {
            EditContext.OnValidationRequested -= HandleValidationRequested;
            EditContext.OnFieldChanged -= HandleFieldChanged;
        }
    }
}