using Microsoft.Extensions.Logging;

namespace Data.Services.BrowserLauncher;

public sealed class UnsupportedPlatformBrowserLauncher : IBrowserLauncherService
{
    private readonly ILogger<UnsupportedPlatformBrowserLauncher> _logger;

    public UnsupportedPlatformBrowserLauncher(ILogger<UnsupportedPlatformBrowserLauncher> logger)
    {  
        _logger = logger; 
    }

    public Task LaunchBrowserAsync()
    {
        _logger.LogWarning("Browser launch is not supported on this platform.\nPlease enter this in your Web Browser of choice: 'http://localhost:5000'");
        return Task.CompletedTask;
    }
}
