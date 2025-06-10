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
            Usage = "Visual color selection for string fields",
            Example = ".AsColorPicker()"
        },
        new()
        {
            Feature = "Rating Control",
            Usage = "Star-based rating for integer fields",
            Example = ".AsRating(maxRating: 5)"
        },
        new()
        {
            Feature = "Field Groups",
            Usage = "Organize fields into responsive columns",
            Example = ".AddFieldGroup(g => g.WithColumns(2))"
        },
        new()
        {
            Feature = "Show In Card",
            Usage = "Display field groups in Material cards",
            Example = ".ShowInCard(elevation: 2)"
        },
        new()
        {
            Feature = "Creating Custom Renderers",
            Usage = "Extend with your own field types",
            Example = "class MyRenderer : CustomFieldRendererBase<T>"
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
                    .AsColorPicker()
                    .WithLabel("Product Color")
                    .WithHelpText("Select the primary color of the product"))
                .AddField(x => x.Rating, field => field
                    .AsRating(maxRating: 5)
                    .WithLabel("Quality Rating")
                    .WithHelpText("Rate the product quality from 1 to 5 stars")))
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
            new() { Label = "In Stock", Value = _model.InStock ? "Yes" : "No" },
            new() { Label = "Release Date", Value = _model.ReleaseDate.ToShortDateString() }
        ];
    }

    private string GetExampleCode()
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
                    .WithColumns(2)
                    .ShowInCard()
                    .AddField(x => x.Color, field => field
                        .AsColorPicker()
                        .WithLabel("Product Color")
                        .WithHelpText("Select the primary color of the product"))
                    .AddField(x => x.Rating, field => field
                        .AsRating(maxRating: 5)
                        .WithLabel("Quality Rating")
                        .WithHelpText("Rate the product quality from 1 to 5 stars")))
                .AddField(x => x.Description, field => field
                    .AsTextArea(lines: 4)
                    .WithLabel("Description")
                    .WithPlaceholder("Enter product description"))
                .AddField(x => x.InStock, field => field
                    .WithLabel("In Stock"))
                .Build();
            """;
    }

    private string GetCustomRendererExample()
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