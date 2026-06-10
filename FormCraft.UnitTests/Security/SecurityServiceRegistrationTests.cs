using Microsoft.Extensions.Logging;

namespace FormCraft.UnitTests.Security;

public class SecurityServiceRegistrationTests
{
    [Fact]
    public void AddFormCraft_Should_Register_Aes_Based_Encryption_Service_By_Default()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        // Assert - the XOR-based BlazorEncryptionService must not be the default outside the browser
        encryptionService.ShouldBeOfType<DefaultEncryptionService>();
    }

    [Fact]
    public void AddFormCraft_Should_Not_Override_A_Previously_Registered_Encryption_Service()
    {
        // Arrange
        var services = new ServiceCollection();
        var customService = A.Fake<IEncryptionService>();
        services.AddScoped<IEncryptionService>(_ => customService);

        // Act
        services.AddFormCraft();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Assert
        scope.ServiceProvider.GetRequiredService<IEncryptionService>().ShouldBeSameAs(customService);
    }

    [Fact]
    public void AddFormCraft_Should_Register_Rate_Limit_Service_That_Is_Disposed_With_The_Container()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFormCraft();

        var provider = services.BuildServiceProvider();
        var rateLimitService = provider.GetRequiredService<IRateLimitService>();

        // Act & Assert - the singleton implements IDisposable, so disposing the provider disposes it
        rateLimitService.ShouldBeOfType<InMemoryRateLimitService>();
        rateLimitService.ShouldBeAssignableTo<IDisposable>();
        Should.NotThrow(() => provider.Dispose());
    }

    [Fact]
    public void AddFormCraft_Should_Register_Audit_Log_Service()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        services.AddFormCraft();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

        // Assert
        auditLogService.ShouldBeOfType<ConsoleAuditLogService>();
    }
}
