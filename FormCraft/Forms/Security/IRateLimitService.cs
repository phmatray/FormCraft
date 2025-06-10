namespace FormCraft;

/// <summary>
/// Service for managing rate limiting.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if a request is allowed based on rate limiting rules.
    /// </summary>
    /// <param name="identifier">The identifier for rate limiting (e.g., IP address, user ID).</param>
    /// <param name="maxAttempts">Maximum number of attempts allowed.</param>
    /// <param name="timeWindow">Time window for rate limiting.</param>
    /// <returns>True if the request is allowed, false if rate limited.</returns>
    Task<RateLimitResult> CheckRateLimitAsync(string identifier, int maxAttempts, TimeSpan timeWindow);
    
    /// <summary>
    /// Records an attempt for rate limiting.
    /// </summary>
    /// <param name="identifier">The identifier for rate limiting.</param>
    Task RecordAttemptAsync(string identifier);
}

/// <summary>
/// Result of a rate limit check.
/// </summary>
public class RateLimitResult
{
    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; set; }
    
    /// <summary>
    /// Number of remaining attempts.
    /// </summary>
    public int RemainingAttempts { get; set; }
    
    /// <summary>
    /// Time until the rate limit resets.
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }
}