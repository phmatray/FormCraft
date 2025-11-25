using System.Reflection;

namespace FormCraft;

/// <summary>
/// Helper class for handling encrypted field operations.
/// </summary>
public static class EncryptedFieldHelper
{
    /// <summary>
    /// Encrypts all fields marked for encryption in the model.
    /// </summary>
    public static void EncryptFields<TModel>(TModel model, IFormSecurity security, IEncryptionService encryptionService)
        where TModel : new()
    {
        if (model == null || security?.EncryptedFields == null || !security.EncryptedFields.Any())
            return;

        foreach (var fieldName in security.EncryptedFields)
        {
            var property = typeof(TModel).GetProperty(fieldName);
            if (property?.PropertyType == typeof(string) && property is { CanRead: true, CanWrite: true })
            {
                var value = property.GetValue(model) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    var encryptedValue = encryptionService.Encrypt(value);
                    property.SetValue(model, encryptedValue);
                }
            }
        }
    }

    /// <summary>
    /// Decrypts all fields marked for encryption in the model.
    /// </summary>
    public static void DecryptFields<TModel>(TModel model, IFormSecurity security, IEncryptionService encryptionService)
        where TModel : new()
    {
        if (model == null || security?.EncryptedFields == null || !security.EncryptedFields.Any())
            return;

        foreach (var fieldName in security.EncryptedFields)
        {
            var property = typeof(TModel).GetProperty(fieldName);
            if (property?.PropertyType == typeof(string) && property.CanRead && property.CanWrite)
            {
                var encryptedValue = property.GetValue(model) as string;
                if (!string.IsNullOrEmpty(encryptedValue))
                {
                    var decryptedValue = encryptionService.Decrypt(encryptedValue);
                    property.SetValue(model, decryptedValue);
                }
            }
        }
    }

    /// <summary>
    /// Creates a clone of the model with encrypted fields decrypted for display.
    /// </summary>
    public static TModel CreateDecryptedCopy<TModel>(TModel model, IFormSecurity security, IEncryptionService encryptionService)
        where TModel : new()
    {
        if (model == null)
            return new TModel();

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