using FormCraft.ForFluentUI.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace FormCraft.ForFluentUI.UnitTests;

/// <summary>
/// Base class for Fluent UI component tests that configures all required services, mirroring the
/// Program.cs setup of a real application.
/// </summary>
public abstract class FluentUITestBase : BunitContext
{
    protected FluentUITestBase()
    {
        Services.AddFormCraft();
        ((IServiceCollection)Services).AddFormCraftFluentUI();
        Services.AddFluentUIComponents();

        // Fluent UI v5 renders web components that call into JS on first render.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
