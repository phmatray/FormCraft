using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class CustomRenderers
{
    private ProductModel _model = new();
    private IFormConfiguration<ProductModel> _formConfiguration = null!;
    private bool _isSubmitting;
    private bool _isSubmitted;

    private readonly List<GuidelineItem> _apiGuidelineTableItems =
    [
        new()
        {
            Feature = "Color Picker",
            Usage = "Visual color selection control",
            Example = ".WithCustomRenderer<TModel, string, ColorPickerRenderer>()"
        },
        new()
        {
            Feature = "Rating Control",
            Usage = "Star-based rating input",
            Example = ".WithCustomRenderer<TModel, int, RatingRenderer>()"
        },
        new()
        {
            Feature = "Custom Attributes",
            Usage = "Pass parameters to renderers",
            Example = ".WithAttribute(\"MaxRating\", 5)"
        },
        new()
        {
            Feature = "Help Text",
            Usage = "Provide user guidance",
            Example = ".WithHelpText(\"Select the primary color\")"
        },
        new()
        {
            Feature = "Creating Renderers",
            Usage = "Implement ICustomFieldRenderer",
            Example = "public class MyRenderer : ICustomFieldRenderer"
        }
    ];

    private readonly List<FormGuidelines.GuidelineItem> _sidebarFeatures =
    [
        new() { Text = "Color picker provides visual color selection", Icon = Icons.Material.Filled.ColorLens },
        new() { Text = "Rating control offers intuitive star-based input", Icon = Icons.Material.Filled.Star },
        new() { Text = "Custom renderers integrate seamlessly with validation", Icon = Icons.Material.Filled.CheckCircle },
        new() { Text = "Easily create your own renderers for specific needs", Icon = Icons.Material.Filled.Extension }
    ];

    protected override void OnInitialized()
    {
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
                .WithColumns(2)
                .ShowInCard()
                .AddField(x => x.Color, field => field
                    .WithLabel("Product Color")
                    .WithCustomRenderer<ProductModel, string, ColorPickerRenderer>()
                    .WithHelpText("Select the primary color of the product"))
                .AddField(x => x.Rating, field => field
                    .WithLabel("Quality Rating")
                    .WithCustomRenderer<ProductModel, int, RatingRenderer>()
                    .WithAttribute("MaxRating", 5)
                    .WithHelpText("Rate the product quality from 1 to 5 stars")))
            .AddField(x => x.Description)
            .WithLabel("Description")
            .AsTextArea(lines: 4)
            .WithPlaceholder("Enter product description")
            .AddField(x => x.InStock)
            .WithLabel("In Stock")
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
            new() { Label = "In Stock", Value = _model.InStock ? "Yes" : "No" },
            new() { Label = "Release Date", Value = _model.ReleaseDate.ToShortDateString() }
        ];
    }

    private string GetGeneratedCode()
    {
        // Generate code from the actual form configuration
        return CodeGenerator.GenerateFormBuilderCode(_formConfiguration);
    }
}