
using CoreRCON;
using CoreRCON.Parsers.Standard;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Data.Listener;

public sealed class ListenerService : IListenerService
{
    private readonly ILogger<ListenerService> _logger;
    private LogReceiver? _logReceiver;

    public event EventHandler<ChatMessage>? OnChatMessageReceived;
    public event EventHandler<PlayerConnected>? OnPlayerConnected;
    public event EventHandler<PlayerDisconnected>? OnPlayerDisconnected;
    public event EventHandler<NameChange>? OnPlayerNameChanged;

    public ListenerService(ILogger<ListenerService> logger)
    {
        _logger = logger;
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
    private void StartTF2(string listenIp, int listenPort)
    {



        if (!OperatingSystem.IsWindows())
        {
            _logger.LogError($"Platfrom does not support automatic launch of tf2. please launch tf2 with the following launch options:\n" +
                $"-usercon +log on +logaddress_add {listenIp}:{listenPort} +rcon_password 1234 +ip 0.0.0.0 +hostport 27015");
            return;
        }

        var process = Process.GetProcessesByName("tf_win64");

        if (process.Length > 0)
        {
            _logger.LogWarning($"TF2 Already Launched. Make sure you have the following launch options set:\n" +
                $"-usercon +log on +logaddress_add {listenIp}:{listenPort} +rcon_password 1234 +ip 0.0.0.0 +hostport 27015");
            return;
        }


        var rawArgs = $"-applaunch 440 -usercon +log on +logaddress_add {listenIp}:{listenPort} +rcon_password 1234 +ip 0.0.0.0 +hostport 27015";

        Process.Start(new ProcessStartInfo
        {
            FileName = @"C:\Program Files (x86)\Steam\steam.exe",
            Arguments = rawArgs,
            UseShellExecute = false,
        });
    }
}
