using Data;
using Data.Bans;
using Data.Listener;
using Data.Players.PlayerTracker;
using Data.RconClient;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ListenServerBanTool;
using ListenServerBanTool.Components;

var builder = WebApplication.CreateBuilder(args);

var baseDir = AppDomain.CurrentDomain.BaseDirectory;
var dbPath = Path.Combine(baseDir, "data", "bans.db");

builder.Services.AddDbContext<BanContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<IPlayerTracker, PlayerTracker>();
builder.Services.AddSingleton<IRconClient, RconClient>();
builder.Services.AddSingleton<IListenerService, ListenerService>();
builder.Services.AddSingleton<IBanManager, BanManager>();
builder.Services.AddHostedService<ListenWorker>();


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
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (!app.Environment.IsDevelopment())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var address = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
            }
        }
        catch
        {

        }
    });
}

await app.RunAsync();
