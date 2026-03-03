namespace Data.RconClient;

public interface IRconClient
{
    public Task KickPlayerAsync(int userId);
}
