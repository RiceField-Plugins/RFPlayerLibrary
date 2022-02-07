using RFPlayerLibrary.Enums;
using Rocket.API;

namespace RFPlayerLibrary
{
    public class Configuration : IRocketPluginConfiguration
    {
        public bool Enabled;
        public EDatabase Database;
        public string MySqlConnectionString;
        
        public void LoadDefaults()
        {
            Enabled = true;
            Database = EDatabase.LITEDB;
            MySqlConnectionString = "SERVER=127.0.0.1;DATABASE=unturned;UID=root;PASSWORD=123456;PORT=3306;";
        }
    }
}