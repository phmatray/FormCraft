using System.Reflection;

namespace FormCraft;

/// <summary>
/// Helper class for handling encrypted field operations.
/// </summary>
public static class EncryptedFieldHelper
{
    /// <summary>
    /// Encrypts all fields marked for encryption in the model, including nested ones listed by their
    /// full dotted path (<c>"Address.City"</c>).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A listed field cannot be resolved to a writable <see cref="string"/> property — checked for every
    /// field before any is modified, so the model is never left half-encrypted.
    /// </exception>
    public static void EncryptFields<TModel>(TModel model, IFormSecurity security, IEncryptionService encryptionService)
        where TModel : new()
        => TransformFields(model, security, value => encryptionService.Encrypt(value));

    /// <summary>
    /// Decrypts all fields marked for encryption in the model, including nested ones listed by their
    /// full dotted path (<c>"Address.City"</c>).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// A listed field cannot be resolved to a writable <see cref="string"/> property.
    /// </exception>
    public static void DecryptFields<TModel>(TModel model, IFormSecurity security, IEncryptionService encryptionService)
        where TModel : new()
        => TransformFields(model, security, value => encryptionService.Decrypt(value));

    private static void TransformFields<TModel>(TModel model, IFormSecurity security, Func<string, string?> transform)
    {
        if (model == null || security?.EncryptedFields == null || security.EncryptedFields.Count == 0)
        {
            return;
        }

        // Resolve every path before writing anything: a bad entry must fail closed without leaving
        // some fields encrypted and others not.
        var chains = security.EncryptedFields
            .Select(path => ResolveWritable(typeof(TModel), path))
            .ToList();

        foreach (var chain in chains)
        {
            if (!MemberPathResolver.TryGetOwner(model, chain, out var owner))
            {
                continue; // A null intermediate holds no value to transform.
            }

            var property = chain[^1];
            if (property.GetValue(owner) is string { Length: > 0 } value)
            {
                property.SetValue(owner, transform(value));
            }
        }
    }

    private static PropertyInfo[] ResolveWritable(Type modelType, string path)
    {
        var chain = MemberPathResolver.ResolveStringProperty(modelType, path);

        // A write through a value-type intermediate lands on a boxed copy and is lost, which would
        // leave the plaintext in place just as silently as an unwritable property.
        if (!chain[^1].CanWrite || chain[..^1].Any(p => p.PropertyType.IsValueType))
        {
            throw new ArgumentException(
                $"Encrypted field '{path}' on {modelType.Name} cannot be written in place: its property must have a setter and every intermediate must be a reference type.");
        }

        return chain;
    }

    /// <summary>
    /// Creates a clone of the model with encrypted fields decrypted for display.
    /// </summary>
    public static TModel CreateDecryptedCopy<TModel>(TModel model, IFormSecurity security, IEncryptionService encryptionService)
        where TModel : new()
    {
        if (model == null)
        {
            return new TModel();
        }

        // Create a shallow copy
        var copy = (TModel)Activator.CreateInstance(typeof(TModel))!;

        // Copy all properties
        foreach (var property in typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanRead && property.CanWrite)
            {
                property.SetValue(copy, property.GetValue(model));
            }
        }

        // Decrypt encrypted fields in the copy
        DecryptFields(copy, security, encryptionService);

        return copy;
    }
}
