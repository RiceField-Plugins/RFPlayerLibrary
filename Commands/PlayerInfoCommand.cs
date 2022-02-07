using System.Linq;
using System.Threading.Tasks;
using RFRocketLibrary.Plugins;
using SDG.Unturned;

namespace RFPlayerLibrary.Commands
{
    [CommandInfo("Fetch player information.", "/playerinfo <id|playerName>")]
    [AllowedCaller(Rocket.API.AllowedCaller.Both)]
    [Permissions("playerinfo")]
    public class PlayerInfoCommand : RocketCommand
    {
        public override async Task ExecuteAsync(CommandContext context)
        {
            if (context.CommandRawArguments.Length != 1)
            {
                await context.ReplyAsync("Invalid parameter! Usage: {0}");
                return;
            }

            if (ulong.TryParse(context.CommandRawArguments[0], out var steamId))
            {
                var player = Plugin.Inst.Database.PlayerManager.Get(steamId);
                if (player == null)
                {
                    await context.ReplyAsync("Player not found!");
                    return;
                }

                var exists = Provider.clients.FirstOrDefault(x => x.playerID.steamID.m_SteamID == steamId);
                await context.ReplyAsync(
                    $"Player Information: [Status] {(exists == null ? "Offline" : "Online" )} [Steam Id] {steamId} [Character Name] {player.CharacterName} " +
                    $"[Steam Group Id] {player.SteamGroupId} [Last Visit] {player.ConnectedTime:MM/dd/yyyy HH:mm:ss} " +
                    $"[HWID] {player.HWID} [Total Play Time] {player.PlayTime}");
                return;
            }
            
            var players = Plugin.Inst.Database.PlayerManager.Get(context.CommandRawArguments[0]);
            if (players.Count == 0)
            {
                await context.ReplyAsync("Player not found!");
                return;
            }

            var count = 0;
            foreach (var player in players)
            {
                var exists = Provider.clients.FirstOrDefault(x => x.playerID.steamID.m_SteamID == player.SteamId);
                await context.ReplyAsync(
                    $"#{++count} Player Information: [Status] {(exists == null ? "Offline" : "Online" )} [Steam Id] {player.SteamId} [Character Name] {player.CharacterName} " +
                    $"[Steam Group Id] {player.SteamGroupId} [Last Visit] {player.ConnectedTime:MM/dd/yyyy HH:mm:ss} " +
                    $"[HWID] {player.HWID} [Total Play Time] {player.PlayTime}");
            }
        }
    }
}