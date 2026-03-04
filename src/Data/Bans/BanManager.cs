using CoreRCON.Parsers.Standard;
using Data.Entities;
using Data.Listener;
using Data.Model;
using Data.Model.Export;
using Data.Model.Import;
using Data.PlayerTracker;
using Data.RconClient;
using Data.Util;
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

        var player = await _context.BannedPlayers
            .SingleOrDefaultAsync(p => p.SteamId == steamId);

        if (player is null)
        {
            player = new PlayerEntity
            {
                SteamId = steamId,
                Name = name
            };
            _context.BannedPlayers.Add(player);
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

        var player = await _context.BannedPlayers
            .FirstOrDefaultAsync(p => p.SteamId == steamId);

        if (player is null)
        {
            _logger.LogError("Attempted to remove ban for player with Steam ID {SteamId}, but player was not found.", steamId);
            return;
        }

        _context.BannedPlayers.Remove(player);
        await _context.SaveChangesAsync();

        RemoveFromBanCache(steamId);
        OnBansUpdated?.Invoke();
        _logger.LogInformation("Player with Steam ID {SteamId} has been unbanned.", steamId);
    }

    public async Task<bool> ImportBansAsync(BannedPlayersImport bannedPlayersImport)
    {
        var importedBans = bannedPlayersImport.ToBannedPlayers();

        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<BanContext>();

        var existingBanIds = await _context
            .BannedPlayers
            .Select(x => x.SteamId)
            .ToListAsync();

        var newBans = importedBans
            .Where(x => !existingBanIds.Contains(x.SteamId, StringComparer.OrdinalIgnoreCase))
            .Select(x => new PlayerEntity
            {
                SteamId = x.SteamId,
                Name = x.Name ?? "none provided"
            })
            .ToList();

        if (newBans.Any())
        {
            await _context.BannedPlayers.AddRangeAsync(newBans);
            await _context.SaveChangesAsync();

            OnBansUpdated?.Invoke();

            return true;
        }

        return false;
    }

    public async Task<BannedPlayersExport> ExportBansAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<BanContext>();

        var bans = await _context.BannedPlayers
            .Select(x => new BannedPlayer { SteamId = x.SteamId, Name = x.Name })
            .ToListAsync();

        return bans.ToBannedPlayersExport();
    }

    public async Task<IEnumerable<BannedPlayer>> GetBannedPlayersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<BanContext>();

        return await _context.BannedPlayers
            .Select(x => new BannedPlayer { SteamId = x.SteamId, Name = x.Name })
            .ToListAsync();
    }

    private async Task OnPlayerConnect(PlayerConnected connectedPlayer)
    {
        if (await IsPlayerBannedAsync(connectedPlayer.Player.SteamId))
        {
            // We kick on purpose to waste the bots time and resources, as they will likely just try to reconnect.
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
            await Task.Delay(random.Next(500, 3000));
            await AddBanAsync(player.SteamId, nameChange.Player.Name);
        }
    }

    // Basic check for links bots post
    private async Task EvaluateChatMessageAsync(ChatMessage message)
    {
        if (message.Message.Contains("t.me") || message.Message.Contains("tf2publicserver"))
        { 
            var random = new Random();
            await Task.Delay(random.Next(500, 3000));
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

        var player = _context.BannedPlayers
            .AsNoTracking()
            .SingleOrDefault(p => p.SteamId == steamId);

        if (player is null)
        {
            return false;
        }

        AddToBanCache(steamId, player.Name);
        return true;
    }

    private bool IsPlayerImmune(ActivePlayer player)
    {
        return player.IsImmune;
    }
}
