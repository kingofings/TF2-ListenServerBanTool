using System.Text.Json.Serialization;

namespace Data.Model;

public sealed class BannedPlayerJson
{
    [JsonPropertyName("steamId")]
    public required string SteamId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
