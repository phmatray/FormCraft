using FormCraft.DemoBlazorApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Dialogs;

public partial class EditFormDialog
{
    private IFormConfiguration<ProductModel> _formConfig = null!;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;
    
    [Parameter]
    public ProductModel Model { get; set; } = new();
    
    protected override void OnInitialized()
    {
        _formConfig = FormBuilder<ProductModel>
            .Create()
            .AddField(x => x.Name, field => field
                .WithLabel("Product Name")
                .Required())
            .AddField(x => x.Description, field => field
                .WithLabel("Description")
                .AsTextArea(lines: 4)
                .Required())
            .AddField(x => x.Price, field => field
                .WithLabel("Price")
                .WithPlaceholder("0.00")
                .Required())
            .AddField(x => x.Category, field => field
                .WithLabel("Category")
                .WithPlaceholder("Select a category")
                .Required())
            .AddField(x => x.IsAvailable, field => field
                .WithLabel("Available for Sale"))
            .Build();
    }

    private void Submit()
    {
        MudDialog.Close(DialogResult.Ok(Model));
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}