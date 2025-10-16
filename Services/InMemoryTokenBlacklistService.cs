using System.Collections.Concurrent;

namespace SafeScribe.Api.Services;

public class InMemoryTokenBlacklistService : ITokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _store = new();

    public Task AddAsync(string jti, DateTimeOffset expiresAt)
    {
        if (!string.IsNullOrWhiteSpace(jti))
            _store[jti] = expiresAt;
        Cleanup();
        return Task.CompletedTask;
    }

    public bool IsBlacklisted(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti)) return false;
        Cleanup();
        return _store.ContainsKey(jti);
    }

    private void Cleanup()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _store)
        {
            if (kv.Value <= now)
            {
                _store.TryRemove(kv.Key, out _);
            }
        }
    }
}
