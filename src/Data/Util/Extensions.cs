using Data.Model;
using Data.Model.Export;
using Data.Model.Import;

namespace Data.Util;

public static class Extensions
{
    public static BannedPlayer ToBannedPlayer(this BannedPlayerJson bannedPlayerJson)
    {
        return new BannedPlayer
        {
            SteamId = bannedPlayerJson.SteamId,
            Name = bannedPlayerJson.Name,
        };
    }

    public static BannedPlayerJson ToBannedPlayerJson(this BannedPlayer bannedPlayer)
    {
        return new BannedPlayerJson
        {
            SteamId = bannedPlayer.SteamId,
            Name = bannedPlayer.Name,
        };
    }

    public static BannedPlayersExport ToBannedPlayersExport(this IEnumerable<BannedPlayer> bannedPlayers)
    {
        return new BannedPlayersExport
        {
            BannedPlayers = bannedPlayers.Select(x => x.ToBannedPlayerJson()).ToList()
        };
    }

    public static BannedPlayersImport ToBannedPlayersImport(this IEnumerable<BannedPlayer> bannedPlayers)
    {
        return new BannedPlayersImport
        {
            BannedPlayers = bannedPlayers.Select(x => x.ToBannedPlayerJson()).ToList()
        };
    }

    public static IEnumerable<BannedPlayer> ToBannedPlayers(this BannedPlayersImport bannedPlayersImport)
    {
        return bannedPlayersImport.BannedPlayers.Select(x => x.ToBannedPlayer()).ToList();
    }
}
