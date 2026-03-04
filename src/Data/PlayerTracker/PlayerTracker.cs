using CoreRCON.Parsers.Standard;
using Data.Listener;
using Data.Model;

namespace Data.PlayerTracker;

public sealed class PlayerTracker : IPlayerTracker
{
    private readonly HashSet<ActivePlayer> _activePlayers = new();

    private readonly IListenerService _listenerService;

    public event Action? OnTrackedPlayersUpdate;

    public PlayerTracker(IListenerService listenerService)
    {
        _listenerService = listenerService;

        _listenerService.OnPlayerConnected += (sender, connectedPlayer) => OnPlayerConnect(connectedPlayer);
        _listenerService.OnPlayerDisconnected += (sender, disconnectedPlayer) => OnPlayerDisconnect(disconnectedPlayer);
    }

    public bool AddPlayer(ActivePlayer player)
    {
        if (IsPlayerTracked(player.SteamId))
        {
            return false;
        }

        _activePlayers.Add(player);
        OnTrackedPlayersUpdate?.Invoke();
        return true;
    }

    public bool RemovePlayer(string steamId)
    {
        if (!IsPlayerTracked(steamId))
        {
            return false;
        }

        _activePlayers.RemoveWhere(p => p.SteamId == steamId);
        OnTrackedPlayersUpdate?.Invoke();
        return true;
    }

    public async Task SetPlayerImmunityAsync(string steamId, bool isImmune)
    {
        var player = _activePlayers.SingleOrDefault(p => p.SteamId == steamId);
        if (player is not null)
        {
            player.IsImmune = isImmune;
            OnTrackedPlayersUpdate?.Invoke();
        }
    }

    public ActivePlayer? GetPlayer(string steamId)
    {
        return _activePlayers.SingleOrDefault(x => x.SteamId == steamId);
    }

    public IEnumerable<ActivePlayer> GetAllActivePlayers()
    {
        return _activePlayers;
    }

    public bool IsNamePotentiallyImpersonating(string name)
    {
        return _activePlayers.Any(p => p.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private bool IsPlayerTracked(string steamId)
    {
        return _activePlayers.Any(p => p.SteamId == steamId);
    }

    private void OnPlayerConnect(PlayerConnected connectedPlayer)
    {
        var player = new ActivePlayer
        {
            Name = connectedPlayer.Player.Name,
            SteamId = connectedPlayer.Player.SteamId,
            UserId = connectedPlayer.Player.ClientId,
            ConnectTime = DateTime.UtcNow
        };
        AddPlayer(player);
    }

    private void OnPlayerDisconnect(PlayerDisconnected disconnectedPlayer)
    {
        RemovePlayer(disconnectedPlayer.Player.SteamId);
    }
}
