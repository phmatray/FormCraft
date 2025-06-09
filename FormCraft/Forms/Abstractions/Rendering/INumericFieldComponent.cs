namespace FormCraft;

/// <summary>
/// Defines the contract for numeric field components across different UI frameworks.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
/// <typeparam name="TValue">The numeric type (int, decimal, double, etc.).</typeparam>
public interface INumericFieldComponent<TModel, TValue> : IFieldComponent<TModel>
    where TValue : struct
{
    /// <summary>
    /// Gets or sets the minimum allowed value.
    /// </summary>
    TValue? Min { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum allowed value.
    /// </summary>
    TValue? Max { get; set; }
    
    /// <summary>
    /// Gets or sets the step increment for the numeric input.
    /// </summary>
    TValue? Step { get; set; }
    
    /// <summary>
    /// Gets or sets the number format string.
    /// </summary>
    string? Format { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show spinner controls.
    /// </summary>
    bool ShowSpinButtons { get; set; }
}