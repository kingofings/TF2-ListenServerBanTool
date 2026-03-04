using System.Text.Json.Serialization;

namespace Data.Model.Import;

public sealed class BannedPlayersImport
{
    [JsonPropertyName("bannedAccounts")]
    public required IEnumerable<BannedPlayerJson> BannedPlayers { get; init; }
}
