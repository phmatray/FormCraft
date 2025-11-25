namespace FormCraft.ForMudBlazor.UnitTests;

/// <summary>
/// Base class for MudBlazor component tests that configures all required services.
/// </summary>
public abstract class MudBlazorTestBase : BunitContext
{
    protected MudBlazorTestBase()
    {
        // Add FormCraft services
        Services.AddFormCraft();

        // Add MudBlazor services
        Services.AddMudServices();

        // Configure JSInterop for MudBlazor
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
