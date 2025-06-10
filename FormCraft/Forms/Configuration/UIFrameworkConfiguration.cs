namespace FormCraft;

/// <summary>
/// Configuration for UI framework selection and customization.
/// </summary>
public class UIFrameworkConfiguration
{
    /// <summary>
    /// Gets or sets the current UI framework adapter.
    /// </summary>
    public IUIFrameworkAdapter CurrentFramework { get; set; } = default!;
    
    /// <summary>
    /// Gets the registered UI framework adapters.
    /// </summary>
    public Dictionary<string, IUIFrameworkAdapter> RegisteredFrameworks { get; } = new();
    
    /// <summary>
    /// Registers a UI framework adapter.
    /// </summary>
    /// <param name="adapter">The adapter to register.</param>
    public void RegisterFramework(IUIFrameworkAdapter adapter)
    {
        if (adapter == null) throw new ArgumentNullException(nameof(adapter));
        RegisteredFrameworks[adapter.FrameworkName] = adapter;
        
        // Set as current if it's the first one
        CurrentFramework ??= adapter;
    }
    
    /// <summary>
    /// Sets the current UI framework by name.
    /// </summary>
    /// <param name="frameworkName">The name of the framework to use.</param>
    public void UseFramework(string frameworkName)
    {
        if (!RegisteredFrameworks.TryGetValue(frameworkName, out var adapter))
        {
            throw new InvalidOperationException($"UI framework '{frameworkName}' is not registered.");
        }
        
        CurrentFramework = adapter;
    }
    
    /// <summary>
    /// Gets the field renderer for the specified field type.
    /// </summary>
    /// <param name="fieldType">The field type.</param>
    /// <returns>The appropriate field renderer.</returns>
    public IFieldRenderer GetFieldRenderer(Type fieldType)
    {
        if (CurrentFramework == null)
        {
            throw new InvalidOperationException("No UI framework is configured.");
        }

        if (fieldType is { } t1 && t1 == typeof(string))
            return CurrentFramework.TextFieldRenderer;
        
        if (fieldType is { } t2 && IsNumericType(t2))
            return CurrentFramework.NumericFieldRenderer;
        
        if (fieldType is { } t3 && (t3 == typeof(bool) || t3 == typeof(bool?)))
            return CurrentFramework.BooleanFieldRenderer;
        
        if (fieldType is { } t4 && (t4 == typeof(DateTime) || t4 == typeof(DateTime?) || t4 == typeof(DateOnly) || t4 == typeof(DateOnly?)))
            return CurrentFramework.DateTimeFieldRenderer;
        
        if (fieldType is { } t5 && IsFileUploadType(t5))
            return CurrentFramework.FileUploadFieldRenderer;
        
        throw new NotSupportedException($"No renderer available for type {fieldType}");
    }
    
    private static bool IsNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType == typeof(int) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte);
    }
    
    private static bool IsFileUploadType(Type type)
    {
        // Check for common file upload types
        return type.Name.Contains("IBrowserFile") || 
               type.Name.Contains("IFormFile") ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && 
                type.GetGenericArguments()[0].Name.Contains("IBrowserFile"));
    }
}