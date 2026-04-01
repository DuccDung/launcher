using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace app_server.Services.Storefront;

public sealed class SteamStoreService(HttpClient httpClient, IMemoryCache memoryCache) : ISteamStoreService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(4);

    public async Task<SteamStoreAppData?> GetAppDetailsAsync(int steamAppId, CancellationToken cancellationToken = default)
    {
        if (steamAppId <= 0)
        {
            return null;
        }

        var cacheKey = $"steam:appdetails:{steamAppId}";
        if (memoryCache.TryGetValue(cacheKey, out SteamStoreAppData? cached))
        {
            return cached;
        }

        var payload = await httpClient.GetFromJsonAsync<Dictionary<string, SteamStoreAppLookupResponse>>(
            $"api/appdetails?appids={steamAppId}",
            cancellationToken);

        if (payload is null ||
            !payload.TryGetValue(steamAppId.ToString(), out var appLookup) ||
            appLookup is null ||
            !appLookup.Success ||
            appLookup.Data is null)
        {
            return null;
        }

        memoryCache.Set(cacheKey, appLookup.Data, CacheDuration);
        return appLookup.Data;
    }
}
