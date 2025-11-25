namespace FormCraft;

/// <summary>
/// Defines the contract for date/time field components across different UI frameworks.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
public interface IDateTimeFieldComponent<TModel> : IFieldComponent<TModel>
{
    /// <summary>
    /// Gets or sets the input mode for date/time selection.
    /// </summary>
    DateTimeInputMode InputMode { get; set; }

    /// <summary>
    /// Gets or sets the date/time format string.
    /// </summary>
    string? Format { get; set; }

    /// <summary>
    /// Gets or sets the minimum allowed date/time.
    /// </summary>
    DateTime? MinDate { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed date/time.
    /// </summary>
    DateTime? MaxDate { get; set; }

    /// <summary>
    /// Gets or sets whether to show a clear button.
    /// </summary>
    bool ShowClearButton { get; set; }

    /// <summary>
    /// Gets or sets whether the picker opens on field focus.
    /// </summary>
    bool OpenOnFocus { get; set; }
}

/// <summary>
/// Defines the input mode for date/time fields.
/// </summary>
public enum DateTimeInputMode
{
    /// <summary>
    /// Date picker only.
    /// </summary>
    Date,

    /// <summary>
    /// Time picker only.
    /// </summary>
    Time,

    /// <summary>
    /// Combined date and time picker.
    /// </summary>
    DateTime,

    /// <summary>
    /// Month and year picker only.
    /// </summary>
    Month,

    /// <summary>
    /// Year picker only.
    /// </summary>
    Year
}