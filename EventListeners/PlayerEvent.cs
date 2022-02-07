using System;
using System.Threading.Tasks;
using RFPlayerLibrary.Models;
using RFPlayerLibrary.Utils;
using RFRocketLibrary.Utils;
using Rocket.Core.Logging;
using Rocket.Unturned.Player;

namespace RFPlayerLibrary.EventListeners
{
    public static class PlayerEvent
    {
        public static void OnConnected(UnturnedPlayer player)
        {
            Task.Run(async () =>
            {
                var steamPlayer = player.SteamPlayer();
                var existingPlayer = Plugin.Inst.Database.PlayerManager.GetInternal(player.CSteamID.m_SteamID);
                var (ip, geolocation) = await PlayerUtil.GetGeolocationAsync(player.CSteamID);
                if (existingPlayer == null)
                {
                    var playerDb = new Player
                    {
                        SteamId = player.CSteamID.m_SteamID,
                        Beard = steamPlayer?.beard ?? 0,
                        Color = $"{player.Color.r},{player.Color.g},{player.Color.b},{player.Color.a}",
                        Face = steamPlayer?.face ?? 0,
                        Gold = player.IsPro,
                        Hair = steamPlayer?.hair ?? 0,
                        Language = steamPlayer?.language ?? string.Empty,
                        Skin =
                            $"{steamPlayer?.skin.r ?? 0},{steamPlayer?.skin.g ?? 0},{steamPlayer?.skin.b ?? 0},{steamPlayer?.skin.a ?? 0}",
                        BackpackSkin = steamPlayer?.backpackItem ?? 0,
                        CharacterName = player.CharacterName ?? string.Empty,
                        ConnectedTime = DateTime.Now,
                        GlassesSkin = steamPlayer?.glassesItem ?? 0,
                        HatSkin = steamPlayer?.hatItem ?? 0,
                        MaskSkin = steamPlayer?.maskItem ?? 0,
                        ShirtSkin = steamPlayer?.shirtItem ?? 0,
                        ItemSkins = string.Join(",", steamPlayer?.skinItems ?? Array.Empty<int>()),
                        IsAdmin = player.IsAdmin,
                        PantsSkin = steamPlayer?.pantsItem ?? 0,
                        PlayTime = 0,
                        SelectedSkillset = steamPlayer?.skillset.ToString() ?? string.Empty,
                        SkinTags = string.Join(",", steamPlayer?.skinTags ?? Array.Empty<string>()),
                        VestSkin = steamPlayer?.vestItem ?? 0,
                        SkinDynamicProps = string.Join(",", steamPlayer?.skinDynamicProps ?? Array.Empty<string>()),
                        SteamGroupId = player.SteamGroupID.m_SteamID,
                        HWID = PlayerUtil.HWIDToString(steamPlayer?.playerID.hwid ?? Array.Empty<byte>())
                    };
                    await Plugin.Inst.Database.PlayerManager.AddAsync(playerDb);

                    var playerGeolocation = new PlayerGeolocation
                    {
                        City = geolocation.City ?? string.Empty,
                        Latitude = geolocation.Lat,
                        Longitude = geolocation.Lon,
                        Organization = geolocation.Org ?? string.Empty,
                        Region = geolocation.Region ?? string.Empty,
                        RegionName = geolocation.RegionName ?? string.Empty,
                        Timezone = geolocation.Timezone ?? string.Empty,
                        Zip = geolocation.Zip ?? string.Empty,
                        Ass = geolocation.As ?? string.Empty,
                        Country = geolocation.Country ?? string.Empty,
                        CountryCode = geolocation.CountryCode ?? string.Empty,
                        IP = ip,
                        SteamId = player.CSteamID.m_SteamID,
                        ISP = geolocation.Isp ?? string.Empty,
                    };
                    await Plugin.Inst.Database.PlayerGeolocationManager.AddAsync(playerGeolocation);
                    return;
                }

                existingPlayer.Beard = steamPlayer?.beard ?? 0;
                existingPlayer.Color = $"{player.Color.r},{player.Color.g},{player.Color.b},{player.Color.a}";
                existingPlayer.Face = steamPlayer?.face ?? 0;
                existingPlayer.Gold = player.IsPro;
                existingPlayer.Hair = steamPlayer?.hair ?? 0;
                existingPlayer.Language = steamPlayer?.language ?? string.Empty;
                existingPlayer.Skin =
                    $"{steamPlayer?.skin.r ?? 0},{steamPlayer?.skin.g ?? 0},{steamPlayer?.skin.b ?? 0},{steamPlayer?.skin.a ?? 0}";
                existingPlayer.BackpackSkin = steamPlayer?.backpackItem ?? 0;
                existingPlayer.CharacterName = player.CharacterName;
                existingPlayer.ConnectedTime = DateTime.Now;
                existingPlayer.GlassesSkin = steamPlayer?.glassesItem ?? 0;
                existingPlayer.HatSkin = steamPlayer?.hatItem ?? 0;
                existingPlayer.MaskSkin = steamPlayer?.maskItem ?? 0;
                existingPlayer.ShirtSkin = steamPlayer?.shirtItem ?? 0;
                existingPlayer.ItemSkins = string.Join(",", steamPlayer?.skinItems ?? Array.Empty<int>());
                existingPlayer.IsAdmin = player.IsAdmin;
                existingPlayer.PantsSkin = steamPlayer?.pantsItem ?? 0;
                existingPlayer.SelectedSkillset = steamPlayer?.skillset.ToString() ?? string.Empty;
                existingPlayer.SkinTags = string.Join(",", steamPlayer?.skinTags ?? Array.Empty<string>());
                existingPlayer.SteamId = player.CSteamID.m_SteamID;
                existingPlayer.VestSkin = steamPlayer?.vestItem ?? 0;
                existingPlayer.SkinDynamicProps =
                    string.Join(",", steamPlayer?.skinDynamicProps ?? Array.Empty<string>());
                existingPlayer.SteamGroupId = player.SteamGroupID.m_SteamID;
                existingPlayer.HWID = PlayerUtil.HWIDToString(steamPlayer?.playerID.hwid ?? Array.Empty<byte>());
                await Plugin.Inst.Database.PlayerManager.UpdateAsync(existingPlayer);

                
                var existingGeolocation =
                    Plugin.Inst.Database.PlayerGeolocationManager.GetInternal(player.CSteamID.m_SteamID);
                if (existingGeolocation == null)
                {
                    var playerGeolocation = new PlayerGeolocation
                    {
                        City = geolocation.City ?? string.Empty,
                        Latitude = geolocation.Lat,
                        Longitude = geolocation.Lon,
                        Organization = geolocation.Org ?? string.Empty,
                        Region = geolocation.Region ?? string.Empty,
                        RegionName = geolocation.RegionName ?? string.Empty,
                        Timezone = geolocation.Timezone ?? string.Empty,
                        Zip = geolocation.Zip ?? string.Empty,
                        Ass = geolocation.As ?? string.Empty,
                        Country = geolocation.Country ?? string.Empty,
                        CountryCode = geolocation.CountryCode ?? string.Empty,
                        IP = ip,
                        SteamId = player.CSteamID.m_SteamID,
                        ISP = geolocation.Isp ?? string.Empty,
                    };
                    await Plugin.Inst.Database.PlayerGeolocationManager.AddAsync(playerGeolocation);
                    return;
                }
                
                existingGeolocation.City = geolocation.City;
                existingGeolocation.Latitude = geolocation.Lat;
                existingGeolocation.Longitude = geolocation.Lon;
                existingGeolocation.Organization = geolocation.Org;
                existingGeolocation.Region = geolocation.Region;
                existingGeolocation.RegionName = geolocation.RegionName;
                existingGeolocation.Timezone = geolocation.Timezone;
                existingGeolocation.Zip = geolocation.Zip;
                existingGeolocation.Ass = geolocation.As;
                existingGeolocation.Country = geolocation.Country;
                existingGeolocation.CountryCode = geolocation.CountryCode;
                existingGeolocation.IP = ip;
                existingGeolocation.SteamId = player.CSteamID.m_SteamID;
                existingGeolocation.ISP = geolocation.Isp;
                await Plugin.Inst.Database.PlayerGeolocationManager.UpdateAsync(existingGeolocation);
            }).Forget(exception => Logger.LogError($"[{Plugin.Inst.Name}] [ERROR] OnPlayerConnected: " + exception));
            Plugin.Inst.PlaytimeCor[player.CSteamID.m_SteamID] = Plugin.Inst.StartCoroutine(PlayerUtil.UpdatePlaytime(player.CSteamID.m_SteamID));
        }

        public static void OnDisconnected(UnturnedPlayer player)
        {
            Plugin.Inst.StopCoroutine(Plugin.Inst.PlaytimeCor[player.CSteamID.m_SteamID]);
            Task.Run(async () =>
                await Plugin.Inst.Database.PlayerManager.UpdateConnectedDisconnectedAsync(player.CSteamID.m_SteamID,
                    true));
        }
    }
}