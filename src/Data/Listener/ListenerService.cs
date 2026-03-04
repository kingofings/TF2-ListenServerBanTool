
using CoreRCON;
using CoreRCON.Parsers.Standard;
using Data.RconClient;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Data.Listener;

public sealed class ListenerService : IListenerService
{
    private readonly ILogger<ListenerService> _logger;
    private readonly IRconClient _rconClient;
    private LogReceiver? _logReceiver;

    public event EventHandler<ChatMessage>? OnChatMessageReceived;
    public event EventHandler<PlayerConnected>? OnPlayerConnected;
    public event EventHandler<PlayerDisconnected>? OnPlayerDisconnected;
    public event EventHandler<NameChange>? OnPlayerNameChanged;

    public ListenerService(ILogger<ListenerService> logger, IRconClient rconClient)
    {
        _logger = logger;
        _rconClient = rconClient;
    }

    public async Task StartServiceAsync()
    {
        _logger.LogInformation("Starting ConnectListenerService...");

        var primaryIP = GetPrimaryIP();

        if (primaryIP == "FAILED")
        {
            _logger.LogError("Failed to determine primary IP address. ConnectListenerService will not start.");
            Environment.Exit(1);
            return;
        }

        try
        {
            _logReceiver = new LogReceiver(0, new IPEndPoint(IPAddress.Parse(primaryIP), 27015));
            _logReceiver.Listen<PlayerConnected>(OnPlayerConnect);
            _logReceiver.Listen<PlayerDisconnected>(OnPlayerDisconnect);
            _logReceiver.Listen<NameChange>(OnPlayerNameChange);
            _logReceiver.Listen<ChatMessage>(OnChatMessage);

            StartTF2(primaryIP, _logReceiver.ResolvedPort);

            await Task.Delay(Timeout.Infinite);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ConnectListenerService is stopping...");
        }
        finally
        {
            _logReceiver?.Dispose();
        }
    }

    private void OnPlayerConnect(PlayerConnected connectedPlayer)
    {
        OnPlayerConnected?.Invoke(this, connectedPlayer);
    }

    private void OnPlayerDisconnect(PlayerDisconnected disconnectedPlayer)
    {
        OnPlayerDisconnected?.Invoke(this, disconnectedPlayer);
    }

    private void OnPlayerNameChange(NameChange nameChange)
    {
        OnPlayerNameChanged?.Invoke(this, nameChange);
    }

    private void OnChatMessage(ChatMessage message)
    {
        OnChatMessageReceived?.Invoke(this, message);
    }

    private string GetPrimaryIP()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        try
        {
            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address.ToString() ?? "FAILED";
        }
        catch
        {
            return "FAILED";
        }
    }

    // This should really not be here but whatever
    private async void StartTF2(string listenIp, int listenPort)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogError($"Automatic launch of tf2 not supported on this platform at the moment. Please launch tf2 with the following launch options:\n" +
                $"-usercon +log on +logaddress_add {listenIp}:{listenPort} +rcon_password 1234 +ip 0.0.0.0 +hostport 27015");
            return;
        }

        var process = Process.GetProcessesByName("tf_win64");

        if (process.Length > 0)
        {
            _logger.LogWarning($"TF2 Already Launched. Make sure you have the following launch options set:\n" +
                $"-usercon +log on +rcon_password 1234 +ip 0.0.0.0 +hostport 27015");

            // Attempt to set log address via RCON in case TF2 has the minimum required launch options but is missing the log address
            await _rconClient.AddLogAddressAsync(listenIp, listenPort);
            return;
        }


        var rawArgs = $"-applaunch 440 -usercon +log on +logaddress_add {listenIp}:{listenPort} +rcon_password 1234 +ip 0.0.0.0 +hostport 27015";

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

    // This should really not be here but whatever
    private string? GetSteamInstallDir()
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogError("Automatic detection of Steam install directory is only supported on Windows at the moment.");
            return null;
        }
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
