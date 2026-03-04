namespace Data.Model;

public sealed class ActivePlayer
{
    public required string SteamId { get; init; }
    public required int UserId { get; init; }
    public string? Name { get; init; }
    public bool IsImmune { get; set; }
    public required DateTime ConnectTime { get; init; }
}
