using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Data.Services.BrowserLauncher;

[SupportedOSPlatform("windows")]
public sealed class WindowsBrowserLauncher : IBrowserLauncherService
{
    private readonly ILogger<WindowsBrowserLauncher> _logger;

    public WindowsBrowserLauncher(ILogger<WindowsBrowserLauncher> logger)
    {
        _logger = logger;
    }

    public Task LaunchBrowserAsync()
    {
        _logger.LogInformation("Attempting to launch browser...");
        // We will just assume so
        var address = "http://localhost:5000";
        try
        {
            Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Failed to launch browser.");
        }
        return Task.CompletedTask;
    }
}
