namespace FormCraft;

/// <summary>
/// Defines the contract for file upload field components across different UI frameworks.
/// </summary>
/// <typeparam name="TModel">The type of the model containing the field.</typeparam>
public interface IFileUploadFieldComponent<TModel> : IFieldComponent<TModel>
{
    /// <summary>
    /// Gets or sets the accepted file types (MIME types or extensions).
    /// </summary>
    string? Accept { get; set; }

    /// <summary>
    /// Gets or sets whether multiple file selection is allowed.
    /// </summary>
    bool AllowMultiple { get; set; }

    /// <summary>
    /// Gets or sets the maximum file size in bytes.
    /// </summary>
    long? MaxFileSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of files that can be uploaded.
    /// </summary>
    int? MaxFiles { get; set; }

    /// <summary>
    /// Gets or sets whether to show file preview.
    /// </summary>
    bool ShowPreview { get; set; }

    /// <summary>
    /// Gets or sets whether drag and drop is enabled.
    /// </summary>
    bool EnableDragDrop { get; set; }

    /// <summary>
    /// Gets or sets the upload mode.
    /// </summary>
    FileUploadMode UploadMode { get; set; }
}

/// <summary>
/// Defines how files should be uploaded.
/// </summary>
public enum FileUploadMode
{
    /// <summary>
    /// Upload immediately after selection.
    /// </summary>
    Immediate,

    /// <summary>
    /// Upload when form is submitted.
    /// </summary>
    OnSubmit,

    /// <summary>
    /// Manual upload triggered by user action.
    /// </summary>
    Manual
}