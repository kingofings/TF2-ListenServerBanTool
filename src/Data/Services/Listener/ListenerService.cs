
using CoreRCON;
using CoreRCON.Parsers.Standard;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Data.Services.Listener;

public sealed class ListenerService : IListenerService, IDisposable
{
    private readonly ILogger<ListenerService> _logger;

    private LogReceiver? _logReceiver;
    private string? _primaryIP;
    private bool disposedValue;

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
        _logger.LogInformation("Starting ListenerService...");

        _primaryIP = GetPrimaryIP();

        if (_primaryIP == "FAILED")
        {
            _logger.LogError("Failed to determine primary IP address. ConnectListenerService will not start.");
            Environment.Exit(1);
            return;
        }

        try
        {
            _logReceiver = new LogReceiver(0, new IPEndPoint(IPAddress.Parse(_primaryIP), 27015));
            _logReceiver.Listen<PlayerConnected>(OnPlayerConnect);
            _logReceiver.Listen<PlayerDisconnected>(OnPlayerDisconnect);
            _logReceiver.Listen<NameChange>(OnPlayerNameChange);
            _logReceiver.Listen<ChatMessage>(OnChatMessage);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("ListenerService is stopping...");
        }
    }

    public (string ip, int port)? GetListenerEndpoint()
    {
        if (_primaryIP is null || _logReceiver is null)
        {
            return null;
        }

        return (_primaryIP, _logReceiver.ResolvedPort);
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

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _logReceiver?.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
