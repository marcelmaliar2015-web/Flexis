using Flexis.Application.Microsoft;
using Microsoft.Extensions.Caching.Memory;

namespace Flexis.Infrastructure.Microsoft;

internal sealed class MemoryMicrosoftOAuthStateStore : IMicrosoftOAuthStateStore
{
    private readonly IMemoryCache _cache;

    public MemoryMicrosoftOAuthStateStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Save(string state, MicrosoftOAuthPending pending, TimeSpan lifetime)
    {
        _cache.Set(CacheKey(state), pending, lifetime);
    }

    public bool TryTake(string state, out MicrosoftOAuthPending? pending)
    {
        if (_cache.TryGetValue(CacheKey(state), out pending) && pending is not null)
        {
            _cache.Remove(CacheKey(state));
            return true;
        }

        pending = null;
        return false;
    }

    private static string CacheKey(string state)
    {
        return $"microsoft-oauth:{state}";
    }
}
