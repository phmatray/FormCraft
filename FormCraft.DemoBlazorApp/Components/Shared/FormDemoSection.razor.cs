using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FormCraft.DemoBlazorApp.Components.Shared;

public partial class FormDemoSection<TModel>
{
    [Parameter, EditorRequired]
    public TModel Model { get; set; } = new();
    
    [Parameter, EditorRequired]
    public IFormConfiguration<TModel> Configuration { get; set; } = null!;
    
    [Parameter, EditorRequired]
    public string FormTitle { get; set; } = "";
    
    [Parameter]
    public string FormIcon { get; set; } = Icons.Material.Filled.ContactPage;
    
    [Parameter]
    public bool IsSubmitted { get; set; }
    
    [Parameter]
    public bool IsSubmitting { get; set; }
    
    [Parameter]
    public bool ShowSubmitButton { get; set; } = true;
    
    [Parameter]
    public string SubmitButtonText { get; set; } = "Submit";
    
    [Parameter]
    public string SubmittingText { get; set; } = "Submitting...";
    
    [Parameter]
    public string? SubmitButtonClass { get; set; }
    
    [Parameter, EditorRequired]
    public EventCallback<TModel> OnValidSubmit { get; set; }
    
    [Parameter]
    public EventCallback<(string fieldName, object? value)> OnFieldChanged { get; set; }
    
    [Parameter, EditorRequired]
    public EventCallback OnReset { get; set; }
    
    [Parameter]
    public List<FormSuccessDisplay.DataDisplayItem> DataDisplayItems { get; set; } = [];
    
    [Parameter]
    public bool ShowSidebar { get; set; } = true;
    
    [Parameter]
    public RenderFragment? SidebarContent { get; set; }
    
    [Parameter]
    public RenderFragment? AdditionalContent { get; set; }
}