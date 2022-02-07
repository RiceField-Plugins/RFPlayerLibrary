using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RFPlayerLibrary.Models;
using RFRocketLibrary.Utils;
using Rocket.Unturned.Player;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace RFPlayerLibrary.Utils
{
    internal static class PlayerUtil
    {
        internal static string HWIDToString(byte[] hash)
        {
            return hash == null ? "null" : hash.Aggregate("", (current, t) => current + t.ToString("X"));
        }

        internal static async Task<Tuple<string, Geolocation>> GetGeolocationAsync(CSteamID steamID)
        {
            try
            {
                var player = UnturnedPlayer.FromCSteamID(steamID);
                var ipAddress = player.IP;
                var wc = new WebClient();
                wc.Proxy = null;
                var response = await wc.DownloadStringTaskAsync("http://ip-api.com/json/" + ipAddress);
                var geolocation = JsonConvert.DeserializeObject<Geolocation>(response);
                return new Tuple<string, Geolocation>(ipAddress, geolocation);
            }
            catch (Exception)
            {
                // ignored
                return new Tuple<string, Geolocation>(string.Empty, new Geolocation());
            }
        }

        internal static IEnumerator<WaitForSeconds> UpdatePlaytime(ulong steamId)
        {
            while (Plugin.Inst != null && Plugin.Conf.Enabled)
            {
                yield return new WaitForSeconds(1f);
                Task.Run(async () =>
                {
                    await Plugin.Inst.Database.PlayerManager.AddPlaytimeAsync(steamId);
                }).Forget(exception => Logger.LogError($"[{Plugin.Inst.Name}] [ERROR] UpdatePlaytime: " + exception));
            }
        }
    }
}