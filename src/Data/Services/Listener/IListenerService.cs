using CoreRCON.Parsers.Standard;

namespace Data.Services.Listener;

public interface IListenerService
{
    public Task StartServiceAsync();
    public (string ip, int port)? GetListenerEndpoint();

    public event EventHandler<ChatMessage>? OnChatMessageReceived;
    public event EventHandler<PlayerConnected>? OnPlayerConnected;
    public event EventHandler<PlayerDisconnected>? OnPlayerDisconnected;
    public event EventHandler<NameChange>? OnPlayerNameChanged;
}
