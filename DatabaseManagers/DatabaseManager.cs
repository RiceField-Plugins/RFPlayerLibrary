using System.IO;

namespace RFPlayerLibrary.DatabaseManagers
{
    public class DatabaseManager
    {
        private static readonly string LiteDB_FileName = "playerlibrary.db";
        internal static readonly string LiteDB_FilePath = Path.Combine(Plugin.Inst.Directory, LiteDB_FileName);
        internal static readonly string LiteDB_ConnectionString = $"Filename={LiteDB_FilePath};Connection=shared;";
        
        internal static string MySql_ConnectionString => Plugin.Conf.MySqlConnectionString;

        public PlayerManager PlayerManager;
        public PlayerGeolocationManager PlayerGeolocationManager;
        
        internal DatabaseManager()
        {
            PlayerManager = new PlayerManager();
            PlayerGeolocationManager = new PlayerGeolocationManager();
        }
    }
}