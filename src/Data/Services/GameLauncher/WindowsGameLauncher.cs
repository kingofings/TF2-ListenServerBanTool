using Data.RconClient;
using Data.Service.Listener;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Data.Services.GameLauncher;

[SupportedOSPlatform("windows")]
public sealed class WindowsGameLauncher : IGameLauncherService
{
    private readonly ILogger<WindowsGameLauncher> _logger;
    private readonly IRconClient _rconClient;
    private readonly IListenerService _listenerService;

    public WindowsGameLauncher(ILogger<WindowsGameLauncher> logger, IRconClient rconClient, IListenerService listenerService)
    {
        _logger = logger;
        _rconClient = rconClient;
        _listenerService = listenerService;
    }

    public async Task LaunchGameAsync()
    {
        var endpoint = _listenerService.GetListenerEndpoint();
        if (endpoint is null)
        {
            _logger.LogError("Failed to get listener endpoint. The app will not work. Check your network configuration!");
            return;
        }

        await StartTF2(endpoint.Value.ip, endpoint.Value.port);
    }

    private async Task StartTF2(string listenIp, int listenPort)
    {
        _logger.LogInformation("Attempting to launch TF2...");
        var process = Process.GetProcessesByName("tf_win64");

        if (process.Length > 0)
        {
            _logger.LogWarning($"TF2 Already Launched. Make sure you have the following launch options set:\n" +
                $"-usercon +log on +rcon_password {_rconClient.GetPassword()} +ip 0.0.0.0 +hostport 27015");

            // Attempt to set log address via RCON in case TF2 has the minimum required launch options but is missing the log address
            await _rconClient.AddLogAddressAsync(listenIp, listenPort);
            return;
        }


        var rawArgs = $"-applaunch 440 -usercon +log on +logaddress_add {listenIp}:{listenPort} +rcon_password {_rconClient.GetPassword()} +ip 0.0.0.0 +hostport 27015";

        var steamInstallDir = GetSteamInstallDir();

        if (steamInstallDir is null)
        {
            _logger.LogCritical("FAILED TO FIND STEAM INSTALL DIR!");
            return;
        }


        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(steamInstallDir, "steam.exe"),
            Arguments = rawArgs,
            UseShellExecute = false,
        });
    }

    private string? GetSteamInstallDir()
    {
        var path = @"SOFTWARE\Valve\Steam";

        using var key = Registry.LocalMachine.OpenSubKey(path) ??
            Registry.CurrentUser.OpenSubKey(path);

        if (key is null)
        {
            return null;
        }

        return key.GetValue("InstallPath") as string;
    }
}
