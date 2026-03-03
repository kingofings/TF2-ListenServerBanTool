namespace Data.Model;

public sealed class BannedPlayer
{
    public required string SteamId { get; init; }
    public string? Name { get; init; }
}
