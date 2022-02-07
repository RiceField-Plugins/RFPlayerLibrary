using System.Collections.Generic;
using RFPlayerLibrary.DatabaseManagers;
using RFPlayerLibrary.EventListeners;
using RFPlayerLibrary.Utils;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace RFPlayerLibrary
{
    public class Plugin : RocketPlugin<Configuration>
    {
        public static Plugin Inst;
        public static Configuration Conf;
        public DatabaseManager Database;
        internal readonly Dictionary<ulong, Coroutine> PlaytimeCor = new Dictionary<ulong, Coroutine>(); 
        
        protected override void Load()
        {
            Inst = this;
            Conf = Configuration.Instance;
            
            if (Conf.Enabled)
            {
                Database = new DatabaseManager();
                
                if (Level.isLoaded)
                    Reload();
                
                U.Events.OnPlayerConnected += PlayerEvent.OnConnected;
                U.Events.OnPlayerDisconnected += PlayerEvent.OnDisconnected;
            }
            else
                Logger.LogWarning($"[{Name}] Plugin: DISABLED");

            Logger.LogWarning($"[{Name}] Plugin loaded successfully!");
        }

        protected override void Unload()
        {
            if (Conf.Enabled)
            {
                StopAllCoroutines();
                U.Events.OnPlayerConnected -= PlayerEvent.OnConnected;
                U.Events.OnPlayerDisconnected -= PlayerEvent.OnDisconnected;
            }
            
            Inst = null;
            Conf = null;

            Logger.LogWarning($"[{Name}] Plugin unloaded successfully!");
        }
        public override TranslationList DefaultTranslations => new TranslationList
        {
            {"example_translation1", "[TaskTemplate] Example Translation 1"},
        };

        private void Reload()
        {
            foreach (var steamPlayer in Provider.clients)
                PlaytimeCor[steamPlayer.playerID.steamID.m_SteamID] = StartCoroutine(PlayerUtil.UpdatePlaytime(steamPlayer.playerID.steamID.m_SteamID));
        }
    }
}