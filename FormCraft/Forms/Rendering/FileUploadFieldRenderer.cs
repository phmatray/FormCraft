using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;

namespace FormCraft;

/// <summary>
/// Renderer for file upload fields using MudBlazor's file upload component.
/// </summary>
public class FileUploadFieldRenderer : IFieldRenderer
{
    /// <inheritdoc />
    public bool CanRender(Type fieldType, IFieldConfiguration<object, object> field)
    {
        return fieldType == typeof(IBrowserFile) || 
               fieldType == typeof(IReadOnlyList<IBrowserFile>) ||
               fieldType == typeof(IBrowserFile[]) ||
               fieldType == typeof(List<IBrowserFile>);
    }
    
    /// <inheritdoc />
    public RenderFragment Render<TModel>(IFieldRenderContext<TModel> context)
    {
        return builder =>
        {
            var uploadConfig = context.Field.AdditionalAttributes.GetValueOrDefault("FileUploadConfiguration") as FileUploadConfiguration 
                               ?? new FileUploadConfiguration();
            
            builder.OpenComponent<MudFileUpload<IBrowserFile>>(0);
            
            // Basic properties
            builder.AddAttribute(1, "Label", context.Field.Label);
            // Note: For parameter is not set as it expects an Expression, not a string
            builder.AddAttribute(2, "Variant", Variant.Outlined);
            builder.AddAttribute(3, "Margin", Margin.Dense);
            builder.AddAttribute(4, "FullWidth", true);
            
            // File constraints
            if (!string.IsNullOrEmpty(uploadConfig.Accept))
                builder.AddAttribute(5, "Accept", uploadConfig.Accept);
                
            builder.AddAttribute(6, "MaximumFileCount", uploadConfig.MaxFiles);
            builder.AddAttribute(7, "Multiple", uploadConfig.Multiple);
            
            // Drag and drop
            if (uploadConfig.EnableDragDrop)
            {
                builder.AddAttribute(8, "Class", "drag-drop-zone");
                builder.AddAttribute(9, "InputClass", "absolute mud-width-full mud-height-full overflow-hidden z-20");
                builder.AddAttribute(10, "InputStyle", "opacity:0");
            }
            
            // Help text with constraints
            var helpText = context.Field.HelpText;
            var constraints = uploadConfig.GetConstraintsDescription();
            if (!string.IsNullOrEmpty(constraints))
            {
                helpText = string.IsNullOrEmpty(helpText) 
                    ? constraints 
                    : $"{helpText} • {constraints}";
            }
            if (!string.IsNullOrEmpty(helpText))
                builder.AddAttribute(11, "HelperText", helpText);
            
            // Error handling - simplified for now
            // TODO: Get validation messages from context when available
            
            // Required indicator
            if (context.Field.IsRequired)
                builder.AddAttribute(12, "Required", true);
            
            // Disabled state
            if (context.Field.IsReadOnly || context.Field.IsDisabled)
                builder.AddAttribute(13, "Disabled", true);
                
            // File selection handler
            builder.AddAttribute(14, "OnFilesChanged", EventCallback.Factory.Create<InputFileChangeEventArgs>(
                this, async (args) => await HandleFileSelectionAsync(args, context, uploadConfig)));
            
            // Child content for custom upload button
            builder.AddAttribute(15, "ChildContent", (RenderFragment)(contentBuilder =>
            {
                if (uploadConfig.EnableDragDrop)
                {
                    contentBuilder.OpenElement(0, "div");
                    contentBuilder.AddAttribute(1, "class", "d-flex flex-column align-center justify-center mud-height-full");
                    
                    contentBuilder.OpenComponent<MudIcon>(2);
                    contentBuilder.AddAttribute(3, "Icon", Icons.Material.Filled.CloudUpload);
                    contentBuilder.AddAttribute(4, "Size", Size.Large);
                    contentBuilder.AddAttribute(5, "Color", Color.Default);
                    contentBuilder.CloseComponent();
                    
                    contentBuilder.OpenComponent<MudText>(6);
                    contentBuilder.AddAttribute(7, "Typo", Typo.body1);
                    contentBuilder.AddAttribute(8, "Class", "mt-2");
                    contentBuilder.AddContent(9, "Drag and drop files here or click to browse");
                    contentBuilder.CloseComponent();
                    
                    if (!string.IsNullOrEmpty(constraints))
                    {
                        contentBuilder.OpenComponent<MudText>(10);
                        contentBuilder.AddAttribute(11, "Typo", Typo.caption);
                        contentBuilder.AddAttribute(12, "Class", "mt-1 mud-text-secondary");
                        contentBuilder.AddContent(13, constraints);
                        contentBuilder.CloseComponent();
                    }
                    
                    contentBuilder.CloseElement();
                }
                else
                {
                    contentBuilder.OpenComponent<MudButton>(0);
                    contentBuilder.AddAttribute(1, "HtmlTag", "label");
                    contentBuilder.AddAttribute(2, "Variant", Variant.Filled);
                    contentBuilder.AddAttribute(3, "Color", Color.Primary);
                    contentBuilder.AddAttribute(4, "StartIcon", Icons.Material.Filled.CloudUpload);
                    contentBuilder.AddContent(5, uploadConfig.Multiple ? "Upload Files" : "Upload File");
                    contentBuilder.CloseComponent();
                }
            }));
            
            builder.CloseComponent();
            
            // Show selected files
            var currentValue = context.CurrentValue;
            if (currentValue != null)
            {
                builder.OpenElement(19, "div");
                builder.AddAttribute(20, "class", "mt-2");
                
                if (currentValue is IBrowserFile singleFile)
                {
                    RenderFileItem(builder, singleFile, uploadConfig, 21);
                }
                else if (currentValue is IReadOnlyList<IBrowserFile> fileList)
                {
                    var startIndex = 21;
                    foreach (var file in fileList)
                    {
                        RenderFileItem(builder, file, uploadConfig, startIndex);
                        startIndex += 10;
                    }
                }
                
                builder.CloseElement();
            }
        };
    }
    
    private async Task HandleFileSelectionAsync<TModel>(
        InputFileChangeEventArgs args, 
        IFieldRenderContext<TModel> context,
        FileUploadConfiguration config)
    {
        try
        {
            var files = args.GetMultipleFiles(config.MaxFiles);
            var validationErrors = new List<string>();
            var validFiles = new List<IBrowserFile>();
            
            foreach (var file in files)
            {
                var validationResult = config.ValidateFile(file);
                if (validationResult.IsValid)
                {
                    validFiles.Add(file);
                }
                else if (validationResult.ErrorMessage != null)
                {
                    validationErrors.Add(validationResult.ErrorMessage);
                }
            }
            
            // Update the field value
            if (config.Multiple)
            {
                await context.OnValueChanged.InvokeAsync(validFiles.AsReadOnly());
            }
            else if (validFiles.Any())
            {
                await context.OnValueChanged.InvokeAsync(validFiles.First());
            }
            
            // Notify about validation errors
            if (validationErrors.Any())
            {
                // TODO: Show validation errors to the user
                // This would require enhancing the context to support adding validation messages
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions gracefully
            Console.WriteLine($"Error handling file selection: {ex.Message}");
        }
    }
    
    private static void RenderFileItem(
        RenderTreeBuilder builder, 
        IBrowserFile file,
        FileUploadConfiguration config,
        int startIndex)
    {
        builder.OpenComponent<MudChip<string>>(startIndex);
        builder.AddAttribute(startIndex + 1, "Icon", Icons.Material.Filled.AttachFile);
        builder.AddAttribute(startIndex + 2, "Color", Color.Primary);
        builder.AddAttribute(startIndex + 3, "Size", Size.Small);
        
        var fileSize = file.Size / 1024.0; // KB
        var sizeText = fileSize < 1024 ? $"{fileSize:F1} KB" : $"{fileSize / 1024.0:F1} MB";
        builder.AddAttribute(startIndex + 4, "Text", $"{file.Name} ({sizeText})");
        
        builder.CloseComponent();
        
        // Show image preview if enabled and file is an image
        if (config.ShowPreview && IsImageFile(file))
        {
            builder.OpenElement(startIndex + 6, "div");
            builder.AddAttribute(startIndex + 7, "class", "mt-2");
            
            // Note: Actual image preview would require reading the file content
            // For now, just show a placeholder
            builder.OpenComponent<MudText>(startIndex + 8);
            builder.AddAttribute(startIndex + 9, "Typo", Typo.caption);
            builder.AddContent(startIndex + 10, $"[Preview: {file.Name}]");
            builder.CloseComponent();
            
            builder.CloseElement();
        }
    }
    
    private static bool IsImageFile(IBrowserFile file)
    {
        var imageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/svg+xml" };
        return imageTypes.Contains(file.ContentType.ToLowerInvariant());
    }
}