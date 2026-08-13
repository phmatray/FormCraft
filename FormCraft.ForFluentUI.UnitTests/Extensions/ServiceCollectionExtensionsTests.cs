using FormCraft.ForFluentUI.Extensions;
using FormCraft.ForMudBlazor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace FormCraft.ForFluentUI.UnitTests.Extensions;

/// <summary>
/// Covers what <c>AddFormCraftFluentUI()</c> puts in the container, what it takes out, and the one
/// configuration it refuses outright.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFormCraftFluentUI_Should_Register_The_Text_Renderer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();

        // Act
        services.AddFormCraftFluentUI();

        // Assert
        services.Any(s => s.ImplementationType == typeof(FluentUITextFieldRenderer)).ShouldBeTrue();
    }

    [Fact]
    public void AddFormCraftFluentUI_Should_Remove_The_Core_Default_Renderers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();

        // Act
        services.AddFormCraftFluentUI();

        // Assert - nothing from the core assembly still claims IFieldRenderer
        var coreAssembly = typeof(IFieldRenderer).Assembly;
        services.Any(s => s.ServiceType == typeof(IFieldRenderer)
                          && s.ImplementationType?.Assembly == coreAssembly).ShouldBeFalse();
    }

    [Fact]
    public void AddFormCraftFluentUI_Should_Keep_A_Custom_Renderer_Registered_By_The_Application()
    {
        // Arrange - an application-supplied renderer must survive and keep precedence
        var services = new ServiceCollection();
        services.AddFormCraft();
        services.AddScoped<IFieldRenderer, CustomTestRenderer>();

        // Act
        services.AddFormCraftFluentUI();

        // Assert
        services.Any(s => s.ImplementationType == typeof(CustomTestRenderer)).ShouldBeTrue();
    }

    [Fact]
    public void AddFormCraftFluentUI_Should_Register_Select_Before_Text()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();

        // Act
        services.AddFormCraftFluentUI();

        // Assert - renderer selection is first-match-wins, so a configuration-driven renderer that
        // sorts after the type-based text renderer would never be reached by a string field.
        var registered = services
            .Where(s => s.ServiceType == typeof(IFieldRenderer))
            .Select(s => s.ImplementationType)
            .ToList();

        registered.IndexOf(typeof(FluentUISelectFieldRenderer))
            .ShouldBeLessThan(registered.IndexOf(typeof(FluentUITextFieldRenderer)));
    }

    [Fact]
    public void AddFormCraftFluentUI_Should_Throw_When_MudBlazor_Is_Already_Registered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();
        services.AddFormCraftMudBlazor();

        // Act & Assert - two adapters in one container render a half-Material, half-Fluent form
        // with no error, so registration is where this has to fail.
        var ex = Should.Throw<InvalidOperationException>(() => services.AddFormCraftFluentUI());

        ex.Message.ShouldContain("mutually exclusive");
        // Both adapters named, so the message is actionable from either direction.
        ex.Message.ShouldContain("FormCraft.ForMudBlazor");
        ex.Message.ShouldContain("FormCraft.ForFluentUI");
    }

    [Fact]
    public void AddFormCraftFluentUI_Should_Throw_In_Either_Registration_Order()
    {
        // Arrange - the mirror of the MudBlazor suite's reverse-order test (#279). Pinned from both
        // sides on purpose: the defect being fixed was a guard that existed in exactly one of the
        // two packages, and a single-sided test is what let that ship.
        var mudFirst = new ServiceCollection();
        mudFirst.AddFormCraft();
        mudFirst.AddFormCraftMudBlazor();

        var fluentFirst = new ServiceCollection();
        fluentFirst.AddFormCraft();
        fluentFirst.AddFormCraftFluentUI();

        // Act
        var mudFirstEx = Should.Throw<InvalidOperationException>(() => mudFirst.AddFormCraftFluentUI());
        var fluentFirstEx = Should.Throw<InvalidOperationException>(() => fluentFirst.AddFormCraftMudBlazor());

        // Assert - both orders fail, and each names the adapter that was already there first.
        mudFirstEx.Message.ShouldStartWith("FormCraft.ForMudBlazor is already registered");
        fluentFirstEx.Message.ShouldStartWith("FormCraft.ForFluentUI is already registered");
    }

    [Fact]
    public void Both_Registration_Orders_Should_Leave_The_Same_Renderers()
    {
        // #279 changed this for the Fluent adapter, deliberately. The old test in AddFormCraft() was
        // "is an IUIFrameworkAdapter registered?", and only AddFormCraftMudBlazor() ever registered
        // one - so calling AddFormCraftFluentUI() FIRST left core's built-in renderers in place as a
        // silent fallback, while the documented order stripped them. The adapter marker is
        // adapter-neutral, so the two orders now agree.
        //
        // The visible consequence: Fluent registers no file-upload renderer, so an IBrowserFile
        // field renders "Unsupported field type" in BOTH orders now, rather than only in the
        // documented one. That is the behaviour the documented order always had.
        var coreAssembly = typeof(IFieldRenderer).Assembly;

        var adapterFirst = new ServiceCollection();
        adapterFirst.AddFormCraftFluentUI();
        adapterFirst.AddFormCraft();

        var coreFirst = new ServiceCollection();
        coreFirst.AddFormCraft();
        coreFirst.AddFormCraftFluentUI();

        static string[] Renderers(IServiceCollection services) => services
            .Where(s => s.ServiceType == typeof(IFieldRenderer))
            .Select(s => s.ImplementationType!.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Renderers(adapterFirst).ShouldBe(Renderers(coreFirst));
        adapterFirst.ShouldNotContain(s =>
            s.ServiceType == typeof(IFieldRenderer) && s.ImplementationType!.Assembly == coreAssembly);
    }

    [Fact]
    public void AddFormCraftFluentUI_Should_Throw_When_An_Adapter_Registered_Renderers_Without_The_Marker()
    {
        // Arrange - simulates a MudBlazor package published BEFORE #279, which registers its
        // renderers but never calls EnsureSingleAdapter. Both adapters ship in lockstep, but a
        // consumer can pin one and upgrade the other, and a guard that only fires when both sides
        // call in is the same "it works if everyone agrees" failure #279 exists to remove — arriving
        // as version skew rather than as package placement.
        var services = new ServiceCollection();
        services.AddFormCraft();
        services.AddScoped<IFieldRenderer, FormCraft.ForMudBlazor.MudBlazorTextFieldRenderer>();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => services.AddFormCraftFluentUI());

        ex.Message.ShouldStartWith("FormCraft.ForMudBlazor is already registered");
    }

    [Fact]
    public void AddFormCraftFluentUI_Should_Not_Treat_An_Application_Renderer_As_A_Rival_Adapter()
    {
        // Arrange - the false positive an assembly-NAME rule invites. This test assembly is called
        // "FormCraft.ForFluentUI.UnitTests", so a `FormCraft.For*` prefix match reads a renderer
        // declared here as another adapter and blocks a legitimate registration. Matching the known
        // adapter names in full is what keeps a consumer's own assembly out of it.
        var services = new ServiceCollection();
        services.AddFormCraft();
        services.AddScoped<IFieldRenderer, CustomTestRenderer>();

        // Act & Assert - Should not throw
        services.AddFormCraftFluentUI();

        services.ShouldContain(s =>
            s.ServiceType == typeof(IFieldRenderer) && s.ImplementationType == typeof(CustomTestRenderer));
    }

    [Fact]
    public void AddFormCraftFluentUI_Can_Be_Called_Twice_Without_Tripping_The_Adapter_Guard()
    {
        // Arrange - the guard must exclude the registering assembly, or re-registering the SAME
        // adapter reads as a conflict with itself.
        var services = new ServiceCollection();
        services.AddFormCraft();

        // Act & Assert - Should not throw
        services.AddFormCraftFluentUI();
        services.AddFormCraftFluentUI();

        services.BuildServiceProvider().GetService<IFieldRendererService>().ShouldNotBeNull();
    }

    private sealed class CustomTestRenderer : FieldRendererBase
    {
        protected override Type ComponentType => typeof(FluentUITextFieldComponent<>);

        public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field) => false;
    }
}
