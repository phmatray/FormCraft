using FormCraft.DemoBlazorApp.Components.Dialogs;
using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
using FormCraft.DemoBlazorApp.Services;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Pages;

public partial class DialogDemo
{
    private string _simpleDialogResult = string.Empty;
    private string _editDialogResult = string.Empty;

    private readonly DialogOptions _options = new()
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true
    };

    /// <summary>
    /// Structured documentation for this demo page.
    /// </summary>
    public static DemoDocumentation Documentation { get; } = new()
    {
        DemoId = "dialog-demo",
        Title = "Dialog Integration",
        Description = "Learn how to seamlessly integrate FormCraft forms within MudBlazor dialogs for consistent form experiences in modal contexts. This demo showcases creating, editing, and handling form submissions within dialog windows, including proper data passing, result handling, and validation patterns.",
        Icon = Icons.Material.Filled.OpenInNew,
        FeatureHighlights =
        [
            new() { Icon = Icons.Material.Filled.Window, Color = Color.Primary, Text = "Use FormCraft forms seamlessly within MudBlazor dialogs" },
            new() { Icon = Icons.Material.Filled.DataObject, Color = Color.Secondary, Text = "Pass model data to dialogs using DialogParameters" },
            new() { Icon = Icons.Material.Filled.CallReceived, Color = Color.Info, Text = "Handle dialog results with proper null checking" },
            new() { Icon = Icons.Material.Filled.Settings, Color = Color.Tertiary, Text = "Configure dialog options for size and behavior" },
            new() { Icon = Icons.Material.Filled.Cancel, Color = Color.Warning, Text = "Always provide cancel functionality in dialogs" },
            new() { Icon = Icons.Material.Filled.CheckCircle, Color = Color.Success, Text = "Validate forms before closing dialogs" }
        ],
        ApiGuidelines =
        [
            new() { Feature = "DialogParameters", Usage = "Pass model and configuration to dialog", Example = "new DialogParameters { { \"Model\", model } }" },
            new() { Feature = "[CascadingParameter] MudDialog", Usage = "Access dialog instance for closing", Example = "[CascadingParameter] public IMudDialogInstance MudDialog { get; set; }" },
            new() { Feature = "MudDialog.Close(DialogResult.Ok(data))", Usage = "Close dialog with success result", Example = "MudDialog.Close(DialogResult.Ok(Model))" },
            new() { Feature = "MudDialog.Cancel()", Usage = "Close dialog without saving", Example = "MudDialog.Cancel()" },
            new() { Feature = "DialogService.ShowAsync<T>()", Usage = "Display dialog component", Example = "await DialogService.ShowAsync<MyDialog>(\"Title\", parameters)" },
            new() { Feature = "result.Canceled check", Usage = "Verify user didn't cancel", Example = "if (!result.Canceled && result.Data is MyModel data) { ... }" }
        ],
        CodeExamples =
        [
            new() { Title = "Dialog Component Example", Language = "razor", CodeProvider = GetDialogComponentCode },
            new() { Title = "Usage Example", Language = "csharp", CodeProvider = GetDialogUsageCode }
        ],
        WhenToUse = "Use dialogs with FormCraft forms when you need to capture user input in a focused, modal context. Perfect for creating new records, editing existing data, or collecting input without navigating away from the current page. Dialogs work well for short forms (3-8 fields) that require immediate attention. For longer, multi-step forms, consider using a separate page with wizard layout instead.",
        CommonPitfalls =
        [
            "Forgetting to check result.Canceled before accessing result.Data - always validate",
            "Not providing a cancel option - users should always be able to exit without saving",
            "Using dialogs for complex forms with many fields - consider a dedicated page instead",
            "Missing null checks when accessing dialog result data - use pattern matching",
            "Not configuring ShowSubmitButton=\"false\" and adding custom DialogActions instead",
            "Forgetting to validate the form before closing the dialog with success"
        ],
        RelatedDemoIds = ["fluent", "validation", "field-groups"]
    };

    // Legacy properties for backward compatibility with existing razor template
    private List<FormGuidelines.GuidelineItem> _guidelines => Documentation.FeatureHighlights
        .Select(f => new FormGuidelines.GuidelineItem { Icon = f.Icon, Color = f.Color, Text = f.Text })
        .ToList();

    protected override void OnInitialized()
    {
        // Validate documentation in DEBUG mode
        new DemoDocumentationValidator().ValidateOrThrow(Documentation);
    }

    private async Task OpenSimpleDialog()
    {
        var parameters = new DialogParameters
        {
            { "Model", new ContactModel() }
        };

        var dialog = await DialogService.ShowAsync<SimpleFormDialog>("New Contact", parameters, _options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: ContactModel contact })
        {
            _simpleDialogResult = $"Contact created: {contact.FirstName} {contact.LastName} ({contact.Email})";
        }
    }

    private async Task OpenEditDialog()
    {
        var product = new ProductModel
        {
            Name = "Laptop Pro X1",
            Description = "High-performance laptop with cutting-edge features",
            Price = 1299.99m,
            Category = "Electronics",
            IsAvailable = true
        };

        var parameters = new DialogParameters
        {
            { "Model", product }
        };

        var dialog = await DialogService.ShowAsync<EditFormDialog>("Edit Product", parameters, _options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: ProductModel editedProduct })
        {
            _editDialogResult = $"Product updated: {editedProduct.Name} - ${editedProduct.Price:F2}";
        }
    }

    // Code example methods
    private static string GetDialogComponentCode()
    {
        return """
            @using FormCraft
            @using MudBlazor

            <MudDialog>
                <TitleContent>
                    <MudText Typo="Typo.h6">
                        <MudIcon Icon="@Icons.Material.Filled.PersonAdd" Class="mr-2" />
                        New Contact
                    </MudText>
                </TitleContent>
                <DialogContent>
                    <FormCraftComponent
                        TModel="ContactModel"
                        Model="@Model"
                        Configuration="@_formConfig"
                        ShowSubmitButton="false" />
                </DialogContent>
                <DialogActions>
                    <MudButton OnClick="Cancel">Cancel</MudButton>
                    <MudButton Color="Color.Primary" OnClick="Submit" Variant="Variant.Filled">
                        Save
                    </MudButton>
                </DialogActions>
            </MudDialog>

            @code {
                [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = null!;
                [Parameter] public ContactModel Model { get; set; } = new();

                private IFormConfiguration<ContactModel> _formConfig = null!;

                protected override void OnInitialized()
                {
                    _formConfig = FormBuilder<ContactModel>
                        .Create()
                        .AddField(x => x.FirstName, field => field
                            .WithLabel("First Name")
                            .Required())
                        .AddField(x => x.Email, field => field
                            .WithLabel("Email")
                            .Required())
                        .AddField(x => x.Message, field => field
                            .WithLabel("Message")
                            .AsTextArea(lines: 3))
                        .Build();
                }

                private void Submit() => MudDialog.Close(DialogResult.Ok(Model));
                private void Cancel() => MudDialog.Cancel();
            }
            """;
    }

    private static string GetDialogUsageCode()
    {
        return """
            @inject IDialogService DialogService

            private async Task OpenDialog()
            {
                var parameters = new DialogParameters
                {
                    { "Model", new ContactModel() }
                };

                var dialog = await DialogService.ShowAsync<ContactFormDialog>(
                    "New Contact",
                    parameters);

                var result = await dialog.Result;

                if (!result.Canceled && result.Data is ContactModel contact)
                {
                    // Process the submitted data
                    await SaveContact(contact);
                }
            }
            """;
    }
}