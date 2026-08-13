using System.Reflection;

namespace FormCraft.UnitTests.Extensions;

/// <summary>
/// Tests the typed native-required builder method now that it lives in core (#279).
/// </summary>
/// <remarks>
/// <para>
/// #204 introduced <c>.WithNativeRequired(...)</c> in <c>FormCraft.ForMudBlazor</c>. The rule it
/// writes is UI-agnostic — it is a key in the field's attribute bag, read by every adapter's
/// component base — so Fluent UI consumers were left typing the raw
/// <c>.WithAttribute("Required", …)</c> form the typed method exists to replace. #279 moves it to
/// core, where both adapters reach it.
/// </para>
/// <para>
/// The declaring-assembly assertion below is load-bearing and is why these tests are not a vacuous
/// green: this project references <c>FormCraft.ForMudBlazor</c> and globally imports its namespace,
/// so every *behavioural* assertion here would have passed unchanged against the old MudBlazor
/// method. Only the reflection assertion can tell the two apart.
/// </para>
/// </remarks>
public class NativeRequiredBuilderTests
{
    [Fact]
    public void WithNativeRequired_Should_Be_Declared_By_The_Core_Assembly()
    {
        // The point of the move. Both packages declare extension methods on FieldBuilder<,> in
        // namespace FormCraft, so a caller cannot see which assembly answered — but an adapter that
        // does not reference MudBlazor very much can.
        var method = typeof(FieldBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(m => m.Name == nameof(FieldBuilderExtensions.WithNativeRequired));

        method.ShouldNotBeNull();
        method.DeclaringType!.Assembly.ShouldBe(typeof(FormBuilder<>).Assembly);
    }

    [Fact]
    public void WithNativeRequired_Should_Write_The_Required_Attribute()
    {
        // Arrange & Act
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithNativeRequired())
            .Build();

        // Assert
        Attributes(config)[NativeRequired.AttributeName].ShouldBe(true);
    }

    [Fact]
    public void WithNativeRequired_False_Should_Write_False()
    {
        // Arrange & Act - an explicit opt-out has to be expressible, or the method is a one-way door.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithNativeRequired(false))
            .Build();

        // Assert
        Attributes(config)[NativeRequired.AttributeName].ShouldBe(false);
    }

    [Fact]
    public void WithNativeRequired_Should_Write_The_Key_NativeRequired_Resolves()
    {
        // The builder and the resolver have to agree on the key, which is the whole reason
        // NativeRequired.AttributeName is public rather than a literal repeated in both.
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f => f.WithNativeRequired(false))
            .Build();

        NativeRequired.Resolve(Attributes(config), isRequired: true).ShouldBeFalse();
    }

    [Fact]
    public void WithNativeRequired_Should_Return_The_Same_Builder_For_Chaining()
    {
        // Arrange
        object? returned = null;
        object? original = null;

        // Act
        FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Name, f =>
            {
                original = f;
                returned = f.WithNativeRequired();
            })
            .Build();

        // Assert
        returned.ShouldBeSameAs(original);
    }

    [Fact]
    public void WithNativeRequired_Should_Be_Available_On_A_Numeric_Field()
    {
        // Arrange & Act - declared for every TValue rather than strings only (#204).
        var config = FormBuilder<TestModel>
            .Create()
            .AddField(x => x.Quantity, f => f.WithNativeRequired())
            .Build();

        // Assert
        Attributes(config)[NativeRequired.AttributeName].ShouldBe(true);
    }

    private static IReadOnlyDictionary<string, object> Attributes(IFormConfiguration<TestModel> config)
        => config.Fields[0].AdditionalAttributes;

    private class TestModel
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
