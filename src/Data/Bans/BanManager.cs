using CoreRCON.Parsers.Standard;
using Data.Entities;
using Data.Listener;
using Data.Model;
using Data.PlayerTracker;
using Data.RconClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Data.Bans;

public sealed class BanManager : IBanManager
{
    private readonly ILogger<BanManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRconClient _client;
    private readonly IPlayerTracker _playerTracker;
    private readonly IListenerService _listenerService;

    private readonly HashSet<BannedPlayer> _banCache = new();

    public event Action? OnBansUpdated;

    public BanManager(ILogger<BanManager> logger, IServiceScopeFactory scopeFactory, IRconClient client, IPlayerTracker playerTracker, IListenerService listenerService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _client = client;
        _playerTracker = playerTracker;
        _listenerService = listenerService;

        _listenerService.OnChatMessageReceived += async (sender, message) => await EvaluateChatMessageAsync(message);
        _listenerService.OnPlayerNameChanged += async (sender, nameChange) => await EvaluatePlayerNameChangeAsync(nameChange);
        _listenerService.OnPlayerConnected += async (sender, connectedPlayer) => await OnPlayerConnect(connectedPlayer);
    }

    public async Task AddBanAsync(string steamId, string name = "none available")
    {
        var activePlayer = _playerTracker.GetPlayer(steamId);

        if (activePlayer is not null && IsPlayerImmune(activePlayer))
        {
            _logger.LogWarning("Attempted to ban player with Steam ID {SteamId}, but player is immune.", steamId);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<BanContext>();

        var player = await _context.Players
            .SingleOrDefaultAsync(p => p.SteamId == steamId);

        if (player == null)
        {
            player = new PlayerEntity
            {
                SteamId = steamId,
                Name = name,
                IsBanned = true
            };
            _context.Players.Add(player);
        }
        else
        {
            player.IsBanned = true;
            _context.Players.Update(player);
        }


        await _context.SaveChangesAsync();

        AddToBanCache(steamId, name);
        OnBansUpdated?.Invoke();
        _logger.LogInformation("Player with Steam ID {SteamId} has been banned.", steamId);

        if (activePlayer is not null)
        {
            await _client.KickPlayerAsync(activePlayer.UserId);
        }
    }

    public async Task RemoveBanAsync(string steamId)
    {
        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<BanContext>();

        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.SteamId == steamId);

        if (player is null)
        {
            _logger.LogError("Attempted to remove ban for player with Steam ID {SteamId}, but player was not found.", steamId);
            return;
        }

        player.IsBanned = false;
        await _context.SaveChangesAsync();

        RemoveFromBanCache(steamId);
        OnBansUpdated?.Invoke();
        _logger.LogInformation("Player with Steam ID {SteamId} has been unbanned.", steamId);
    }

    public async Task<IEnumerable<BannedPlayer>> GetBannedPlayersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<BanContext>();

        return await _context.Players
            .Where(x => x.IsBanned == true)
            .Select(x => new BannedPlayer { SteamId = x.SteamId, Name = x.Name })
            .ToListAsync();
    }

    private async Task OnPlayerConnect(PlayerConnected connectedPlayer)
    {
        if (await IsPlayerBannedAsync(connectedPlayer.Player.SteamId))
        {
            await _client.KickPlayerAsync(connectedPlayer.Player.ClientId);
        }
    }

    // Basic check for bots changing their name immediately upon spawn
    private async Task EvaluatePlayerNameChangeAsync(NameChange nameChange)
    {
        if (_playerTracker.IsNamePotentiallyImpersonating(nameChange.NewName))
        {
            await _client.KickPlayerAsync(nameChange.Player.ClientId);
            return;
        }

        var player = _playerTracker.GetPlayer(nameChange.Player.SteamId);
        if (player is not null && (DateTime.UtcNow - player.ConnectTime).TotalMinutes < 2)
        {
            var random = new Random();
            await Task.Delay(random.Next(500, 3000)); // Random delay between 0.5 and 3 seconds
            await AddBanAsync(player.SteamId, nameChange.Player.Name);
        }
    }

    // Basic check for links bots post
    private async Task EvaluateChatMessageAsync(ChatMessage message)
    {
        if (message.Message.Contains("t.me") || message.Message.Contains("tf2publicserver"))
        { 
            var random = new Random();
            await Task.Delay(random.Next(500, 3000)); // Random delay between 0.5 and 3 seconds
            await AddBanAsync(message.Player.SteamId, message.Player.Name);
        }
    }

    private void AddToBanCache(string steamId, string name)
    {
        if (_banCache.Any(p => p.SteamId == steamId))
        {
            return;
        }

        _banCache.Add(new BannedPlayer
        {
            SteamId = steamId,
            Name = name
        });
    }

    private void RemoveFromBanCache(string steamId)
    {
        _banCache.RemoveWhere(p => p.SteamId == steamId);
    }

    private async Task<bool> IsPlayerBannedAsync(string steamId)
    {
        if (_banCache.Any(p => p.SteamId == steamId))
        {
            return true;
        }

        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<BanContext>();

        var player = _context.Players
            .AsNoTracking()
            .SingleOrDefault(p => p.SteamId == steamId);

        if (player is null)
        {
            return false;
        }

        if (player.IsBanned)
        {
            AddToBanCache(steamId, player.Name);
            return true;
        }

        return false;
    }

    private bool IsPlayerImmune(ActivePlayer player)
    {
        return player.IsImmune;
    }
}
