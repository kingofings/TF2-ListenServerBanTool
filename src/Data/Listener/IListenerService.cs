using CoreRCON.Parsers.Standard;

namespace Data.Listener;

public interface IListenerService
{
    public Task StartServiceAsync();

    public event EventHandler<ChatMessage>? OnChatMessageReceived;
    public event EventHandler<PlayerConnected>? OnPlayerConnected;
    public event EventHandler<PlayerDisconnected>? OnPlayerDisconnected;
    public event EventHandler<NameChange>? OnPlayerNameChanged;
}
