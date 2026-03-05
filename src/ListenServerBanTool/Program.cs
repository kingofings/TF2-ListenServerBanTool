using Data;
using Data.Bans;
using Data.RconClient;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using ListenServerBanTool;
using ListenServerBanTool.Components;
using Data.PlayerTracker;
using Data.Model.Settings;
using Data.Services.GameLauncher;
using Data.Services.BrowserLauncher;
using Data.Services.Listener;

var builder = WebApplication.CreateBuilder(args);

var baseDir = AppDomain.CurrentDomain.BaseDirectory;
var dbPath = Path.Combine(baseDir, "data", "bans.db");

builder.Services.AddDbContext<BanContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.Configure<RconSettings>(
    builder.Configuration.GetSection("RconSettings"));

builder.Services.AddSingleton<IPlayerTracker, PlayerTracker>();
builder.Services.AddSingleton<IRconClient, RconClient>();
builder.Services.AddSingleton<IListenerService, ListenerService>();
builder.Services.AddSingleton<IBanManager, BanManager>();

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddSingleton<IGameLauncherService, WindowsGameLauncher>();
}
else
{
    builder.Services.AddSingleton<IGameLauncherService, UnsuppotedPlatformGameLauncher>();
}

if (!builder.Environment.IsDevelopment())
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        builder.Services.AddSingleton<IBrowserLauncherService, WindowsBrowserLauncher>();
    }
    else
    {
        builder.Services.AddSingleton<IBrowserLauncherService, UnsupportedPlatformBrowserLauncher>();
    }
}
else
{
    builder.Services.AddSingleton<IBrowserLauncherService, UnsupportedPlatformBrowserLauncher>();
}

builder.Services.AddHostedService<StartWorker>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    if (!Directory.Exists(Path.Combine(baseDir, "data")))
    {
        Directory.CreateDirectory(Path.Combine(baseDir, "data"));
    }
    var context = scope.ServiceProvider.GetRequiredService<BanContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
