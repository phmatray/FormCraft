using FormCraft.DemoBlazorApp.Components.Dialogs;
using FormCraft.DemoBlazorApp.Components.Shared;
using FormCraft.DemoBlazorApp.Models;
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

    private readonly List<FormGuidelines.GuidelineItem> _guidelines = new()
    {
        new() { Icon = Icons.Material.Filled.Window, Text = "Use FormCraft forms seamlessly within MudBlazor dialogs" },
        new() { Icon = Icons.Material.Filled.DataObject, Text = "Pass model data to dialogs using DialogParameters" },
        new() { Icon = Icons.Material.Filled.CallReceived, Text = "Handle dialog results with proper null checking" },
        new() { Icon = Icons.Material.Filled.Settings, Text = "Configure dialog options for size and behavior" },
        new() { Icon = Icons.Material.Filled.Cancel, Text = "Always provide cancel functionality in dialogs" },
        new() { Icon = Icons.Material.Filled.CheckCircle, Text = "Validate forms before closing dialogs" }
    };

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

    private const string DialogExample = @"@using FormCraft
@using MudBlazor

<MudDialog>
    <TitleContent>
        <MudText Typo=""Typo.h6"">
            <MudIcon Icon=""@Icons.Material.Filled.PersonAdd"" Class=""mr-2"" />
            New Contact
        </MudText>
    </TitleContent>
    <DialogContent>
        <FormCraftComponent
            TModel=""ContactModel"" 
            Model=""@Model"" 
            Configuration=""@_formConfig""
            ShowSubmitButton=""false"" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick=""Cancel"">Cancel</MudButton>
        <MudButton Color=""Color.Primary"" OnClick=""Submit"" Variant=""Variant.Filled"">
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
                .WithLabel(""First Name"")
                .Required())
            .AddField(x => x.Email, field => field
                .WithLabel(""Email"")
                .Required())
            .AddField(x => x.Message, field => field
                .WithLabel(""Message"")
                .AsTextArea(lines: 3))
            .Build();
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(Model));
    private void Cancel() => MudDialog.Cancel();
}";

    private const string UsageExample = @"@inject IDialogService DialogService

private async Task OpenDialog()
{
    var parameters = new DialogParameters
    {
        { ""Model"", new ContactModel() }
    };

    var dialog = await DialogService.ShowAsync<ContactFormDialog>(
        ""New Contact"", 
        parameters);
    
    var result = await dialog.Result;

    if (!result.Canceled && result.Data is ContactModel contact)
    {
        // Process the submitted data
        await SaveContact(contact);
    }
}";
}