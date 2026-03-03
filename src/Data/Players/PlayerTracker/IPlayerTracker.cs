using Data.Model;

namespace Data.Players.PlayerTracker;

public interface IPlayerTracker
{
    /// <summary>
    /// Adds a player to the tracker. Returns true if the player was added, false if the player was already being tracked.
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public bool AddPlayer(ActivePlayer player);

    /// <summary>
    /// Removes a player from the tracker. Returns true if the player was removed, false if the player was not being tracked.
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    public bool RemovePlayer(string steamId);

    /// <summary>
    /// Checks if a player may be being impersonated, super basic
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool IsNamePotentiallyImpersonating(string name);

    /// <summary>
    /// Gets a player from the tracker. Returns null if the player is not being tracked.
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    public ActivePlayer? GetPlayer(string steamId);

    public IEnumerable<ActivePlayer> GetAllActivePlayers();

    public event Action? OnTrackedPlayersUpdate;
}
