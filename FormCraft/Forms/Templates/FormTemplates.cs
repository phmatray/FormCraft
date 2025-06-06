namespace FormCraft;

/// <summary>
/// Provides pre-built form templates for common use cases, reducing boilerplate code for standard forms.
/// </summary>
public static class FormTemplates
{
    /// <summary>
    /// Creates a basic contact form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    /// <example>
    /// <code>
    /// var contactFormConfig = FormTemplates.ContactForm&lt;ContactModel&gt;();
    /// // Then extend with specific fields using the builder pattern
    /// </code>
    /// </example>
    public static IFormConfiguration<T> ContactForm<T>() where T : new()
    {
        // This would need to be implemented with reflection or specific model binding
        // For now, return a placeholder
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates a registration form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    public static IFormConfiguration<T> RegistrationForm<T>() where T : new()
    {
        // This would need to be implemented with reflection or specific model binding
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates a login form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    public static IFormConfiguration<T> LoginForm<T>() where T : new()
    {
        return FormBuilder<T>.Create().Build();
    }

    /// <summary>
    /// Creates an address form template with placeholder configuration.
    /// This is a generic template that requires specific field configuration for your model.
    /// </summary>
    /// <typeparam name="T">The model type that the form will bind to. Must have a parameterless constructor.</typeparam>
    /// <returns>A basic form configuration that can be extended with specific fields.</returns>
    public static IFormConfiguration<T> AddressForm<T>() where T : new()
    {
        return FormBuilder<T>.Create().Build();
    }
}

