using Data.Model;

namespace Data.Bans;

public interface IBanManager
{
    /// <summary>
    /// Adds a ban to the database and updates the cache.
    /// </summary>
    /// <param name="steamId"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public Task AddBanAsync(string steamId, string name = "none available");

    /// <summary>
    /// Removes a ban from the database and updates the cache.
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    public Task RemoveBanAsync(string steamId);

    /// <summary>
    /// Gets the list of banned players from the database.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<BannedPlayer>> GetBannedPlayersAsync();

    public event Action? OnBansUpdated;
}
