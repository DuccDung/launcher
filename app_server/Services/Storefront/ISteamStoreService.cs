namespace app_server.Services.Storefront;

public interface ISteamStoreService
{
    Task<SteamStoreAppData?> GetAppDetailsAsync(int steamAppId, CancellationToken cancellationToken = default);
}
