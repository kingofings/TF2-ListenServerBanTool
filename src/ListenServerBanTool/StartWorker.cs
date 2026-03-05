using Data.RconClient;
using Data.Services.BrowserLauncher;
using Data.Services.GameLauncher;
using Data.Services.Listener;

namespace ListenServerBanTool;

public sealed class StartWorker : BackgroundService
{
    private readonly IRconClient _rconClient;
    private readonly IListenerService _listenerService;
    private readonly IGameLauncherService _gameLauncherService;
    private readonly IBrowserLauncherService _browserLauncherService;

    public StartWorker(IRconClient rconClient, IListenerService listenerService, IGameLauncherService gameLauncherService, IBrowserLauncherService browserLauncherService)
    {
        _rconClient = rconClient;
        _listenerService = listenerService;
        _gameLauncherService = gameLauncherService;
        _browserLauncherService = browserLauncherService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_rconClient.CheckIfPasswordWasChanged())
        {
            return;
        }

        await _listenerService.StartServiceAsync();
        await _gameLauncherService.LaunchGameAsync();
        await _browserLauncherService.LaunchBrowserAsync();
    }
}
