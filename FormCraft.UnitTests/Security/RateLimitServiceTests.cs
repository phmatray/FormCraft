namespace FormCraft.UnitTests.Security;

public class RateLimitServiceTests
{
    private readonly IRateLimitService _rateLimitService;

    public RateLimitServiceTests()
    {
        _rateLimitService = new InMemoryRateLimitService();
    }

    [Fact]
    public async Task Should_Allow_Requests_Within_Limit()
    {
        // Arrange
        const string identifier = "test-user";
        const int maxAttempts = 3;
        var timeWindow = TimeSpan.FromMinutes(1);

        // Act & Assert
        for (int i = 0; i < maxAttempts; i++)
        {
            var result = await _rateLimitService.CheckRateLimitAsync(identifier, maxAttempts, timeWindow);
            result.IsAllowed.ShouldBeTrue();
            result.RemainingAttempts.ShouldBe(maxAttempts - i);
            
            await _rateLimitService.RecordAttemptAsync(identifier);
        }
    }

    [Fact]
    public async Task Should_Block_Requests_Exceeding_Limit()
    {
        // Arrange
        const string identifier = "test-user";
        const int maxAttempts = 2;
        var timeWindow = TimeSpan.FromMinutes(1);

        // Record attempts up to the limit
        for (int i = 0; i < maxAttempts; i++)
        {
            await _rateLimitService.RecordAttemptAsync(identifier);
        }

        // Act
        var result = await _rateLimitService.CheckRateLimitAsync(identifier, maxAttempts, timeWindow);

        // Assert
        result.IsAllowed.ShouldBeFalse();
        result.RemainingAttempts.ShouldBe(0);
        result.RetryAfter.ShouldNotBeNull();
        result.RetryAfter.Value.TotalSeconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Reset_After_Time_Window()
    {
        // Arrange
        const string identifier = "test-user";
        const int maxAttempts = 1;
        var timeWindow = TimeSpan.FromMilliseconds(100); // Short window for testing

        // Record an attempt
        await _rateLimitService.RecordAttemptAsync(identifier);

        // Verify blocked
        var blockedResult = await _rateLimitService.CheckRateLimitAsync(identifier, maxAttempts, timeWindow);
        blockedResult.IsAllowed.ShouldBeFalse();

        // Wait for time window to pass
        await Task.Delay(150, Xunit.TestContext.Current.CancellationToken);

        // Act
        var allowedResult = await _rateLimitService.CheckRateLimitAsync(identifier, maxAttempts, timeWindow);

        // Assert
        allowedResult.IsAllowed.ShouldBeTrue();
        allowedResult.RemainingAttempts.ShouldBe(maxAttempts);
    }

    [Fact]
    public async Task Should_Track_Different_Identifiers_Separately()
    {
        // Arrange
        const string identifier1 = "user1";
        const string identifier2 = "user2";
        const int maxAttempts = 1;
        var timeWindow = TimeSpan.FromMinutes(1);

        // Record attempt for identifier1
        await _rateLimitService.RecordAttemptAsync(identifier1);

        // Act
        var result1 = await _rateLimitService.CheckRateLimitAsync(identifier1, maxAttempts, timeWindow);
        var result2 = await _rateLimitService.CheckRateLimitAsync(identifier2, maxAttempts, timeWindow);

        // Assert
        result1.IsAllowed.ShouldBeFalse(); // Should be blocked
        result2.IsAllowed.ShouldBeTrue();  // Should be allowed
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Requests()
    {
        // Arrange
        const string identifier = "concurrent-test";
        const int maxAttempts = 10;
        var timeWindow = TimeSpan.FromMinutes(1);
        var tasks = new List<Task>();

        // Act - Record 10 concurrent attempts
        for (int i = 0; i < maxAttempts; i++)
        {
            tasks.Add(_rateLimitService.RecordAttemptAsync(identifier));
        }
        await Task.WhenAll(tasks);

        var result = await _rateLimitService.CheckRateLimitAsync(identifier, maxAttempts, timeWindow);

        // Assert
        result.IsAllowed.ShouldBeFalse();
        result.RemainingAttempts.ShouldBe(0);
    }
}