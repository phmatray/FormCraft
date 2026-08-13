namespace FormCraft;

/// <summary>
/// In-memory implementation of rate limiting service.
/// For production use, consider using a distributed cache like Redis.
/// </summary>
/// <remarks>
/// This implementation is thread-safe: all reads and writes of the attempt history are
/// serialized through a single lock, so concurrent checks and recordings never lose attempts
/// or observe a partially mutated state. The service owns a background cleanup timer and
/// implements <see cref="IDisposable"/> so the DI container disposes it with the service provider.
/// </remarks>
public class InMemoryRateLimitService : IRateLimitService, IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, List<DateTime>> _attempts = new();
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryRateLimitService"/> class.
    /// </summary>
    public InMemoryRateLimitService()
    {
        // Cleanup old entries every minute
        _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc />
    public Task<RateLimitResult> CheckRateLimitAsync(string identifier, int maxAttempts, TimeSpan timeWindow)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.Subtract(timeWindow);

        lock (_sync)
        {
            if (_attempts.TryGetValue(identifier, out var attempts))
            {
                // Remove old attempts outside the time window
                attempts.RemoveAll(a => a < windowStart);

                var attemptCount = attempts.Count;
                if (attemptCount >= maxAttempts)
                {
                    var retryAfter = TimeSpan.Zero;
                    if (attemptCount > 0)
                    {
                        var oldestAttempt = attempts.Min();
                        var computed = oldestAttempt.Add(timeWindow).Subtract(now);
                        if (computed > TimeSpan.Zero)
                        {
                            retryAfter = computed;
                        }
                    }

                    return Task.FromResult(new RateLimitResult
                    {
                        IsAllowed = false,
                        RemainingAttempts = 0,
                        RetryAfter = retryAfter
                    });
                }

                return Task.FromResult(new RateLimitResult
                {
                    IsAllowed = true,
                    RemainingAttempts = maxAttempts - attemptCount
                });
            }
        }

        return Task.FromResult(new RateLimitResult
        {
            IsAllowed = true,
            RemainingAttempts = maxAttempts
        });
    }

    /// <inheritdoc />
    public Task RecordAttemptAsync(string identifier)
    {
        var now = DateTime.UtcNow;

        lock (_sync)
        {
            if (!_attempts.TryGetValue(identifier, out var attempts))
            {
                attempts = new List<DateTime>();
                _attempts[identifier] = attempts;
            }

            attempts.Add(now);
        }

        return Task.CompletedTask;
    }

    private void Cleanup(object? state)
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);

        lock (_sync)
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in _attempts)
            {
                kvp.Value.RemoveAll(a => a < cutoff);
                if (kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _attempts.Remove(key);
            }
        }
    }

    /// <summary>
    /// Disposes the background cleanup timer.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cleanupTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}
