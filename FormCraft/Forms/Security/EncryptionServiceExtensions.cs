namespace FormCraft;

/// <summary>
/// Convenience extensions for <see cref="IEncryptionService"/> that work with the
/// fields marked via <c>.WithSecurity(s =&gt; s.EncryptField(...))</c>.
/// </summary>
/// <remarks>
/// Encryption remains an application concern: FormCraft never mutates the bound model.
/// Use these helpers in your submit handler to obtain the encrypted values of the
/// configured fields in one call before persisting them.
/// </remarks>
public static class EncryptionServiceExtensions
{
    /// <summary>
    /// Returns the values of all fields configured for encryption on
    /// <paramref name="security"/>, encrypted via the service. The model itself
    /// is not modified.
    /// </summary>
    /// <typeparam name="TModel">The form model type.</typeparam>
    /// <param name="encryptionService">The encryption service to use.</param>
    /// <param name="model">The model holding the plaintext values.</param>
    /// <param name="security">
    /// The form security configuration (typically <c>configuration.Security</c>).
    /// May be null, in which case an empty dictionary is returned.
    /// </param>
    /// <returns>
    /// A dictionary keyed by field name containing the encrypted values. Only string
    /// properties listed in <see cref="IFormSecurity.EncryptedFields"/> are included;
    /// null or empty values are passed through unencrypted.
    /// </returns>
    /// <example>
    /// <code>
    /// var encrypted = encryptionService.EncryptConfiguredFields(model, configuration.Security);
    /// await repository.SaveAsync(model with { SSN = encrypted["SSN"] });
    /// </code>
    /// </example>
    public static IReadOnlyDictionary<string, string?> EncryptConfiguredFields<TModel>(
        this IEncryptionService encryptionService,
        TModel model,
        IFormSecurity? security)
        where TModel : new()
    {
        ArgumentNullException.ThrowIfNull(encryptionService);

        var result = new Dictionary<string, string?>();
        if (model is null || security is not { EncryptedFields.Count: > 0 })
        {
            return result;
        }

        foreach (var fieldName in security.EncryptedFields)
        {
            var property = typeof(TModel).GetProperty(fieldName);
            if (property?.PropertyType == typeof(string) && property.CanRead)
            {
                var value = property.GetValue(model) as string;
                result[fieldName] = string.IsNullOrEmpty(value)
                    ? value
                    : encryptionService.Encrypt(value);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the values of all fields configured for encryption on the form
    /// configuration, encrypted via the service. The model itself is not modified.
    /// </summary>
    /// <typeparam name="TModel">The form model type.</typeparam>
    /// <param name="encryptionService">The encryption service to use.</param>
    /// <param name="model">The model holding the plaintext values.</param>
    /// <param name="configuration">The form configuration whose <see cref="IFormConfiguration{TModel}.Security"/> settings are used.</param>
    /// <returns>A dictionary keyed by field name containing the encrypted values.</returns>
    public static IReadOnlyDictionary<string, string?> EncryptConfiguredFields<TModel>(
        this IEncryptionService encryptionService,
        TModel model,
        IFormConfiguration<TModel> configuration)
        where TModel : new()
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return encryptionService.EncryptConfiguredFields(model, configuration.Security);
    }
}
