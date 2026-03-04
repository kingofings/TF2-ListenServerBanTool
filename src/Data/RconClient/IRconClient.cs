namespace Data.RconClient;

public interface IRconClient
{
    public Task KickPlayerAsync(int userId);
    public Task AddLogAddressAsync(string address, int port);
}
