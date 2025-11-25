using System.Collections.Concurrent;

namespace FormCraft;

/// <summary>
/// In-memory implementation of rate limiting service.
/// For production use, consider using a distributed cache like Redis.
/// </summary>
public class InMemoryRateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _attempts = new();
    private readonly Timer _cleanupTimer;

    public InMemoryRateLimitService()
    {
        // Cleanup old entries every minute
        _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<RateLimitResult> CheckRateLimitAsync(string identifier, int maxAttempts, TimeSpan timeWindow)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.Subtract(timeWindow);

        if (_attempts.TryGetValue(identifier, out var entry))
        {
            // Remove old attempts outside the time window
            entry.Attempts.RemoveAll(a => a < windowStart);

            var attemptCount = entry.Attempts.Count;
            if (attemptCount >= maxAttempts)
            {
                var oldestAttempt = entry.Attempts.Min();
                var retryAfter = oldestAttempt.Add(timeWindow).Subtract(now);

                return Task.FromResult(new RateLimitResult
                {
                    IsAllowed = false,
                    RemainingAttempts = 0,
                    RetryAfter = retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero
                });
            }

            return Task.FromResult(new RateLimitResult
            {
                IsAllowed = true,
                RemainingAttempts = maxAttempts - attemptCount
            });
        }

        return Task.FromResult(new RateLimitResult
        {
            IsAllowed = true,
            RemainingAttempts = maxAttempts
        });
    }

    public Task RecordAttemptAsync(string identifier)
    {
        var now = DateTime.UtcNow;

        _attempts.AddOrUpdate(identifier,
            new RateLimitEntry { Attempts = new List<DateTime> { now } },
            (_, entry) =>
            {
                entry.Attempts.Add(now);
                return entry;
            });

        return Task.CompletedTask;
    }

    private void Cleanup(object? state)
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var keysToRemove = new List<string>();

        foreach (var kvp in _attempts)
        {
            kvp.Value.Attempts.RemoveAll(a => a < cutoff);
            if (kvp.Value.Attempts.Count == 0)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _attempts.TryRemove(key, out _);
        }
    }

    private class RateLimitEntry
    {
        public List<DateTime> Attempts { get; set; } = new();
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}