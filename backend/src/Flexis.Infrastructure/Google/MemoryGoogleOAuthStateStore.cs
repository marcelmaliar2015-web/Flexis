using Flexis.Application.Google;
using Microsoft.Extensions.Caching.Memory;

namespace Flexis.Infrastructure.Google;

internal sealed class MemoryGoogleOAuthStateStore : IGoogleOAuthStateStore
{
    private readonly IMemoryCache _cache;

    public MemoryGoogleOAuthStateStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Save(string state, GoogleOAuthPending pending, TimeSpan lifetime)
    {
        _cache.Set(CacheKey(state), pending, lifetime);
    }

    public bool TryTake(string state, out GoogleOAuthPending? pending)
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
        return $"google-oauth:{state}";
    }
}
