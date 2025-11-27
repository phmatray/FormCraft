using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class CustomRenderers
{
    private ProductModel _model = new();
    private IFormConfiguration<ProductModel> _formConfiguration = null!;
    private bool _isSubmitting;
    private bool _isSubmitted;

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "custom-renderers",
        Title = "Custom Field Renderers",
        Description = "Create specialized input controls for specific data types using custom renderers. FormCraft provides built-in renderers for color pickers, ratings, and sliders, and makes it easy to create your own custom renderers for any input type.",
        Icon = Icons.Material.Filled.Palette,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.ColorLens, Color = Color.Primary, Text = "Color picker provides visual color selection" },
            new() { Icon = Icons.Material.Filled.Star, Color = Color.Secondary, Text = "Rating control offers intuitive star-based input" },
            new() { Icon = Icons.Material.Filled.Tune, Color = Color.Tertiary, Text = "Slider control enables precise range selection" },
            new() { Icon = Icons.Material.Filled.CheckCircle, Color = Color.Success, Text = "Custom renderers integrate seamlessly with validation" },
            new() { Icon = Icons.Material.Filled.Extension, Color = Color.Info, Text = "Easily create your own renderers for specific needs" },
            new() { Icon = Icons.Material.Filled.ViewQuilt, Color = Color.Warning, Text = "Field groups organize controls into responsive layouts" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "AsColorPicker()", Usage = "Visual color selection for string fields", Example = "field.AsColorPicker().WithLabel(\"Product Color\")" },
            new() { Feature = "AsRating()", Usage = "Star-based rating for integer fields", Example = "field.AsRating(maxRating: 5).WithLabel(\"Quality\")" },
            new() { Feature = "AsSlider()", Usage = "Range selection for numeric fields", Example = "field.AsSlider(min: 0, max: 100, step: 5)" },
            new() { Feature = "CustomFieldRendererBase<T>", Usage = "Base class for creating custom renderers", Example = "class MyRenderer : CustomFieldRendererBase<string>" },
            new() { Feature = "WithCustomRenderer()", Usage = "Apply a custom renderer to a field", Example = "field.WithCustomRenderer<TModel, TValue, TRenderer>()" },
            new() { Feature = "ShowInCard()", Usage = "Display field groups in Material cards", Example = "group.ShowInCard(elevation: 2)" }
        ],
        CodeExamples =
        [
            new() { Title = "Form Configuration with Custom Renderers", Language = "csharp", CodeProvider = GetFormConfigCodeStatic },
            new() { Title = "Creating a Custom Renderer", Language = "csharp", CodeProvider = GetCustomRendererCodeStatic }
        ],
        WhenToUse = "Use custom renderers when you need specialized input controls beyond standard text fields, selects, and checkboxes. Examples include color pickers for design tools, star ratings for reviews, sliders for volume or range settings, rich text editors, file uploaders with preview, or any domain-specific input widget. Custom renderers maintain full integration with FormCraft's validation, field dependencies, and state management while providing a tailored user experience.",
        CommonPitfalls =
        [
            "Not implementing proper two-way binding - custom renderers must update model values correctly",
            "Ignoring field configuration properties like IsReadOnly, IsDisabled, or validation state",
            "Creating overly complex renderers - consider if a standard field with styling would suffice",
            "Not handling null values or default states properly in custom renderers",
            "Forgetting to pass through label, placeholder, and help text to the custom component"
        ],
        RelatedDemoIds = ["field-groups", "validation", "conditional-fields", "field-dependencies"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<FormGuidelines.GuidelineItem> _sidebarFeatures => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    private List<GuidelineItem> _apiGuidelineTableItems => Documentation.ApiGuidelines
        .Select(g => new GuidelineItem { Feature = g.Feature, Usage = g.Usage, Example = g.Example })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);
        _formConfiguration = FormBuilder<ProductModel>
            .Create()
            .AddFieldGroup(group => group
                .WithGroupName("Basic Information")
                .WithColumns(2)
                .AddField(x => x.Name, field => field
                    .WithLabel("Product Name")
                    .Required()
                    .WithPlaceholder("Enter product name"))
                .AddField(x => x.Category, field => field
                    .WithLabel("Category")
                    .Required()
                    .WithOptions(
                        ("electronics", "Electronics"),
                        ("clothing", "Clothing"),
                        ("books", "Books"),
                        ("home", "Home & Garden")
                    )))
            .AddFieldGroup(group => group
                .WithGroupName("Details")
                .WithColumns(2)
                .AddField(x => x.Price, field => field
                    .WithLabel("Price")
                    .Required()
                    .WithPlaceholder("0.00"))
                .AddField(x => x.ReleaseDate, field => field
                    .WithLabel("Release Date")))
            .AddFieldGroup(group => group
                .WithGroupName("Appearance & Rating")
                .WithColumns(3)
                .ShowInCard()
                .AddField(x => x.Color, field => field
                    .AsColorPicker()
                    .WithLabel("Product Color")
                    .WithHelpText("Select the primary color of the product"))
                .AddField(x => x.Rating, field => field
                    .AsRating(maxRating: 5)
                    .WithLabel("Quality Rating")
                    .WithHelpText("Rate the product quality from 1 to 5 stars"))
                .AddField(x => x.Volume, field => field
                    .AsSlider(min: 0, max: 100, step: 5, showTickMarks: true)
                    .WithLabel("Volume Level")
                    .WithHelpText("Adjust the volume level from 0 to 100")))
            .AddField(x => x.Description, field => field
                .AsTextArea(lines: 4)
                .WithLabel("Description")
                .WithPlaceholder("Enter product description"))
            .AddField(x => x.InStock, field => field
                .WithLabel("In Stock"))
            .Build();
    }

    private async Task HandleValidSubmit()
    {
        _isSubmitting = true;
        StateHasChanged();

        // Simulate API call
        await Task.Delay(2000);

        _isSubmitted = true;
        _isSubmitting = false;
        StateHasChanged();
    }

    private void ResetForm()
    {
        _model = new ProductModel();
        _isSubmitted = false;
        StateHasChanged();
    }

    private List<FormSuccessDisplay.DataDisplayItem> GetDataDisplayItems()
    {
        return
        [
            new() { Label = "Product Name", Value = _model.Name },
            new() { Label = "Category", Value = _model.Category },
            new() { Label = "Price", Value = $"${_model.Price:F2}" },
            new() { Label = "Color", Value = _model.Color },
            new() { Label = "Rating", Value = $"{_model.Rating} / 5 stars" },
            new() { Label = "Volume", Value = $"{_model.Volume}%" },
            new() { Label = "In Stock", Value = _model.InStock ? "Yes" : "No" },
            new() { Label = "Release Date", Value = _model.ReleaseDate.ToShortDateString() }
        ];
    }

    // Code Examples
    private string GetExampleCode() => GetFormConfigCodeStatic();

    private static string GetFormConfigCodeStatic()
    {
        return """
            _formConfiguration = FormBuilder<ProductModel>
                .Create()
                .AddFieldGroup(group => group
                    .WithGroupName("Basic Information")
                    .WithColumns(2)
                    .AddField(x => x.Name, field => field
                        .WithLabel("Product Name")
                        .Required()
                        .WithPlaceholder("Enter product name"))
                    .AddField(x => x.Category, field => field
                        .WithLabel("Category")
                        .Required()
                        .WithOptions(
                            ("electronics", "Electronics"),
                            ("clothing", "Clothing"),
                            ("books", "Books"),
                            ("home", "Home & Garden")
                        )))
                .AddFieldGroup(group => group
                    .WithGroupName("Details")
                    .WithColumns(2)
                    .AddField(x => x.Price, field => field
                        .WithLabel("Price")
                        .Required()
                        .WithPlaceholder("0.00"))
                    .AddField(x => x.ReleaseDate, field => field
                        .WithLabel("Release Date")))
                .AddFieldGroup(group => group
                    .WithGroupName("Appearance & Rating")
                    .WithColumns(3)
                    .ShowInCard()
                    .AddField(x => x.Color, field => field
                        .AsColorPicker()
                        .WithLabel("Product Color")
                        .WithHelpText("Select the primary color of the product"))
                    .AddField(x => x.Rating, field => field
                        .AsRating(maxRating: 5)
                        .WithLabel("Quality Rating")
                        .WithHelpText("Rate the product quality from 1 to 5 stars"))
                    .AddField(x => x.Volume, field => field
                        .AsSlider(min: 0, max: 100, step: 5, showTickMarks: true)
                        .WithLabel("Volume Level")
                        .WithHelpText("Adjust the volume level from 0 to 100")))
                .AddField(x => x.Description, field => field
                    .AsTextArea(lines: 4)
                    .WithLabel("Description")
                    .WithPlaceholder("Enter product description"))
                .AddField(x => x.InStock, field => field
                    .WithLabel("In Stock"))
                .Build();
            """;
    }

    private string GetCustomRendererExample() => GetCustomRendererCodeStatic();

    private static string GetCustomRendererCodeStatic()
    {
        return """
            // Step 1: Create the custom renderer class
            public class MudBlazorColorPickerRenderer : CustomFieldRendererBase<string>
            {
                public override RenderFragment Render(IFieldRenderContext context)
                {
                    return builder =>
                    {
                        builder.OpenComponent(0, typeof(MudBlazorColorPickerComponent<>)
                            .MakeGenericType(context.Model.GetType()));
                        builder.AddAttribute(1, "Context", context);
                        builder.CloseComponent();
                    };
                }
            }

            // Step 2: Create the Blazor component
            @namespace FormCraft.ForMudBlazor
            @typeparam TModel
            @inherits FieldComponentBase<TModel, string>

            <MudColorPicker
                Label="@Label"
                Text="@(CurrentValue ?? "#FFFFFF")"
                TextChanged="@HandleColorChanged"
                Placeholder="@Placeholder"
                HelperText="@HelpText"
                ReadOnly="@IsReadOnly"
                Disabled="@IsDisabled"
                Variant="Variant.Outlined"
                PickerVariant="PickerVariant.Dialog" />

            @code {
                private void HandleColorChanged(string? color)
                {
                    CurrentValue = color ?? "#FFFFFF";
                }
            }

            // Step 3: Create extension method for easy usage
            public static FieldBuilder<TModel, string> AsColorPicker<TModel>(
                this FieldBuilder<TModel, string> builder)
                where TModel : new()
            {
                return builder.WithCustomRenderer<TModel, string, MudBlazorColorPickerRenderer>();
            }
            """;
    }
}