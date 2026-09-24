namespace FormCraft.UnitTests.Rendering;

/// <summary>
/// Base class for core built-in renderer tests that render through
/// <see cref="IFieldRendererService"/> with NO UI adapter registered — the standalone-consumer path
/// <c>ServiceCollectionExtensions.AddFormCraft()</c> conditionally registers these renderers for
/// (#460). Deliberately does NOT call <c>AddFormCraftMudBlazor()</c>/<c>AddFormCraftFluentUI()</c>:
/// registering either would make <see cref="AdapterRegistration.IsAdapterRegistered"/> true and
/// remove the very renderers under test.
/// </summary>
public abstract class CoreRendererTestBase : BunitContext
{
    protected CoreRendererTestBase()
    {
        Services.AddFormCraft();
    }

    /// <summary>The real render pipeline a standalone consumer resolves — never a fake.</summary>
    protected IFieldRendererService RendererService => Services.GetRequiredService<IFieldRendererService>();
}
