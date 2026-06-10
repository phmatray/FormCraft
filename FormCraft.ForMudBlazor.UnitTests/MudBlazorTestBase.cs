using FormCraft.ForMudBlazor.Extensions;

namespace FormCraft.ForMudBlazor.UnitTests;

/// <summary>
/// Base class for MudBlazor component tests that configures all required services.
/// </summary>
public abstract class MudBlazorTestBase : BunitContext
{
    protected MudBlazorTestBase()
    {
        // Add FormCraft services and the MudBlazor renderers, mirroring the
        // Program.cs setup of a real application. Since the render pipeline was
        // consolidated (#148), every field flows through IFieldRendererService,
        // so the MudBlazor renderers must be registered for components to render.
        Services.AddFormCraft();
        ((IServiceCollection)Services).AddFormCraftMudBlazor();

        // Add MudBlazor services
        Services.AddMudServices();

        // Configure JSInterop for MudBlazor
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
