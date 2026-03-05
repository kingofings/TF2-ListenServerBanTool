using Data.RconClient;
using Data.Services.Listener;
using Microsoft.Extensions.Logging;

namespace Data.Services.GameLauncher;

public sealed class UnsuppotedPlatformGameLauncher : IGameLauncherService
{
    private readonly ILogger<UnsuppotedPlatformGameLauncher> _logger;
    private readonly IListenerService _listenerService;
    private readonly IRconClient _rconClient;

    public UnsuppotedPlatformGameLauncher(ILogger<UnsuppotedPlatformGameLauncher> logger, IListenerService listenerService, IRconClient rconClient)
    {
        _logger = logger;
        _listenerService = listenerService;
        _rconClient = rconClient;
    }

    public async Task LaunchGameAsync()
    {
        var endpoint = _listenerService.GetListenerEndpoint();

        if (endpoint is null)
        {
            _logger.LogError("Failed to get listener endpoint, the app will not work. Check your network configuration!");
            return;
        }

        var rawArgs = $"-usercon +log on +logaddress_add {endpoint.Value.ip}:{endpoint.Value.port} +rcon_password {_rconClient.GetPassword()} +ip 0.0.0.0 +hostport 27015";

        _logger.LogError("Unsupported platform. Game launch is not implemented at the moment.");
        _logger.LogInformation("Please launch TF2 manually with the following launch options:\n" +
            rawArgs + "\n" +
            "Make sure you have the minimum required launch options set, otherwise the app will not work!");

        // Try to set the log address still!
        await _rconClient.AddLogAddressAsync(endpoint.Value.ip, endpoint.Value.port);
    }
}
