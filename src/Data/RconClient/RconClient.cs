using CoreRCON;
using System.Net;

namespace Data.RconClient;

public sealed class RconClient : IRconClient
{
    private readonly RCON _rcon;

    public RconClient()
    {
        _rcon = new RCON(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27015), "1234");
    }

    public async Task KickPlayerAsync(int userId)
    {
        if (!_rcon.Connected)
        {
            await _rcon.ConnectAsync();
        }

        await _rcon.SendCommandAsync($"kickid {userId} Connection closing.");
    }
}
