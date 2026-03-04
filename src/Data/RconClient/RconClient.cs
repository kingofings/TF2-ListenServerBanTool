using CoreRCON;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Data.RconClient;

public sealed class RconClient : IRconClient
{
    private readonly RCON _rcon;
    private readonly ILogger<RconClient> _logger;

    public RconClient(ILogger<RconClient> logger)
    {
        _rcon = new RCON(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27015), "1234");
        _logger = logger;
    }

    public async Task KickPlayerAsync(int userId)
    {
        try
        {
            if (!_rcon.Connected)
            {
                await _rcon.ConnectAsync();
            }

            await _rcon.SendCommandAsync($"kickid {userId} Connection closing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to kick player with user ID {userId}.");
        }
    }

    public async Task AddLogAddressAsync(string address, int port)
    {
        try
        {
            if (!_rcon.Connected)
            {
                await _rcon.ConnectAsync();
            }

            await _rcon.SendCommandAsync($"logaddress_add {address}:{port}");
        }
        catch (Exception)
        {
            _logger.LogError($"Failed to set log address via RCON. Please make sure TF2 has this logaddress:\n" +
                    $"{address}:{port}\n" +
                    $"If TF2 is missing the log address, please add it via the console with the following command:\n" +
                    $"logaddress_add {address}:{port}\n" +
                    "You can check if it worked with 'logaddress_list'\n" +
                    "Also make sure you run the command 'log on'\n" +
                    "Alternatively close tf2 and restart this program and every launch option required should be set for you automatically.");
        }

    }
}
