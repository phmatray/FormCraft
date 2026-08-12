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
    }

    private sealed class CustomTestRenderer : FieldRendererBase
    {
        protected override Type ComponentType => typeof(FluentUITextFieldComponent<>);

        public override bool CanRender(Type fieldType, IFieldConfiguration<object, object> field) => false;
    }
}
