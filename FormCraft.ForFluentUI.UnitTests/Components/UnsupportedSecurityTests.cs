namespace FormCraft.ForFluentUI.UnitTests.Components;

/// <summary>
/// A form configured with <c>.WithSecurity(...)</c> must be refused rather than rendered without
/// enforcement.
/// </summary>
/// <remarks>
/// The MudBlazor container enforces those settings in its submit path - rate limiting, CSRF
/// generation and validation, audit logging. This adapter implements none of it yet. Rendering the
/// form anyway would accept unlimited submissions with no CSRF check and no audit trail, with no
/// exception and no warning, on a form the configuration says is protected. Failing closed is the
/// only safe behaviour until enforcement exists.
/// </remarks>
public class UnsupportedSecurityTests : FluentUITestBase
{
    [Fact]
    public void Form_With_Security_Configured_Should_Refuse_To_Render()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .WithSecurity(s => s.WithRateLimit(5, TimeSpan.FromMinutes(1)))
            .Build();

        // Act & Assert - fails loudly instead of silently dropping the protections
        var ex = Should.Throw<NotSupportedException>(() =>
            Render<FormCraftComponent<TestModel>>(p => p
                .Add(c => c.Model, new TestModel())
                .Add(c => c.Configuration, config)));

        ex.Message.ShouldContain("WithSecurity");
    }

    [Fact]
    public void Form_With_Csrf_Configured_Should_Refuse_To_Render()
    {
        // Arrange
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .WithSecurity(s => s.EnableCsrfProtection())
            .Build();

        // Act & Assert
        Should.Throw<NotSupportedException>(() =>
            Render<FormCraftComponent<TestModel>>(p => p
                .Add(c => c.Model, new TestModel())
                .Add(c => c.Configuration, config)));
    }

    [Fact]
    public void Form_Without_Security_Should_Render_Normally()
    {
        // Arrange - the guard must not fire on the ordinary case
        var config = FormBuilder<TestModel>.Create()
            .AddField(x => x.Name, f => f.WithLabel("Name"))
            .Build();

        // Act
        var component = Render<FormCraftComponent<TestModel>>(p => p
            .Add(c => c.Model, new TestModel())
            .Add(c => c.Configuration, config));

        // Assert
        component.Find("form").ShouldNotBeNull();
    }
}
