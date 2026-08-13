using FormCraft.DemoBlazorApp.Models;
using FormCraft.ForMudBlazor;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Dialogs;

public partial class SimpleFormDialog
{
    private IFormConfiguration<ContactModel> _formConfig = null!;
    private FormCraftComponent<ContactModel>? _form;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public ContactModel Model { get; set; } = new();

    protected override void OnInitialized()
    {
        _formConfig = FormBuilder<ContactModel>
            .Create()
            .AddField(x => x.FirstName, field => field
                .WithLabel("First Name")
                .WithPlaceholder("Enter first name")
                .Required())
            .AddField(x => x.LastName, field => field
                .WithLabel("Last Name")
                .WithPlaceholder("Enter last name")
                .Required())
            .AddField(x => x.Email, field => field
                .WithLabel("Email")
                .WithPlaceholder("Enter email address")
                .Required())
            .AddField(x => x.Phone, field => field
                .WithLabel("Phone")
                .WithPlaceholder("Enter phone number (optional)"))
            .AddField(x => x.Message, field => field
                .WithLabel("Message")
                .WithPlaceholder("Any additional notes...")
                .AsTextArea(lines: 3))
            .Build();
    }

    private async Task Submit()
    {
        // Validate (awaiting async validators) before closing with success
        if (_form is null || !await _form.ValidateAsync())
        {
            return;
        }

        MudDialog.Close(DialogResult.Ok(Model));
    }

    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
