
using Data.Listener;

namespace ListenServerBanTool;

public sealed class ListenWorker : BackgroundService
{
    private readonly IListenerService _listenerService;

    public ListenWorker(IListenerService listenerService)
    {
        _listenerService = listenerService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _listenerService.StartServiceAsync();
    }
}
