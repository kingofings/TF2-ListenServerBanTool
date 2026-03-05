namespace Data.RconClient;

public interface IRconClient
{
    public bool CheckIfPasswordWasChanged();
    public Task KickPlayerAsync(int userId);
    public Task AddLogAddressAsync(string address, int port);
    public string GetPassword();
}
