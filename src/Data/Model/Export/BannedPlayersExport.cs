using System.Text.Json.Serialization;

namespace Data.Model.Export;

public sealed class BannedPlayersExport
{
    [JsonPropertyName("bannedAccounts")]
    public required IEnumerable<BannedPlayerJson> BannedPlayers { get; init; }
}
