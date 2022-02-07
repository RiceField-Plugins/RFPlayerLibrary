using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RFPlayerLibrary.Enums;
using RFPlayerLibrary.Models;
using RFRocketLibrary.Storages;

namespace RFPlayerLibrary.DatabaseManagers
{
    public class PlayerManager
    {
        internal static bool Ready { get; set; }
        internal List<Player> Collection { get; set; } = new List<Player>();
        internal List<Player> MigrateCollection { get; set; } = new List<Player>();

        private const string LiteDB_TableName = "player";

        private const string Json_FileName = "_player.json";
        private DataStore<List<Player>> Json_DataStore { get; set; }

        private const string MySql_TableName = "rfplayerlibrary";

        private const string MySql_CreateTableQuery =
            "`Id` BIGINT NOT NULL AUTO_INCREMENT, " +
            "`SteamId` BIGINT UNSIGNED NOT NULL DEFAULT 0, " +
            "`SteamGroupId` BIGINT UNSIGNED NOT NULL DEFAULT 0, " +
            "`CharacterName` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`Language` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`IsAdmin` BOOLEAN NOT NULL DEFAULT false, " +
            "`Gold` BOOLEAN NOT NULL DEFAULT false, " +
            "`Beard` TINYINT UNSIGNED NOT NULL DEFAULT 0, " +
            "`Face` TINYINT UNSIGNED NOT NULL DEFAULT 0, " +
            "`Hair` TINYINT UNSIGNED NOT NULL DEFAULT 0, " +
            "`Skin` TEXT NOT NULL, " + "`Color` TEXT NOT NULL, " +
            "`BackpackSkin` INT NOT NULL DEFAULT 0, " +
            "`GlassesSkin` INT NOT NULL DEFAULT 0, " +
            "`HatSkin` INT NOT NULL DEFAULT 0, " +
            "`MaskSkin` INT NOT NULL DEFAULT 0, " +
            "`PantsSkin` INT NOT NULL DEFAULT 0, " +
            "`ShirtSkin` INT NOT NULL DEFAULT 0, " +
            "`VestSkin` INT NOT NULL DEFAULT 0, " +
            "`ItemSkins` TEXT NOT NULL, " + "`SkinTags` TEXT NOT NULL, " +
            "`SkinDynamicProps` TEXT NOT NULL, " +
            "`SelectedSkillset` VARCHAR(255) NOT NULL, " +
            "`ConnectedTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
            "`DisconnectedTime` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
            "`PlayTime` BIGINT UNSIGNED NOT NULL DEFAULT 0, " +
            "`HWID` TEXT NOT NULL, " + "PRIMARY KEY (Id)";

        internal PlayerManager()
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    LiteDB_Init();
                    break;
                case EDatabase.JSON:
                    Json_DataStore = new DataStore<List<Player>>(Plugin.Inst.Directory, Json_FileName);
                    JSON_Reload();
                    break;
                case EDatabase.MYSQL:
                    // new CP1250();
                    MySQL_CreateTable(MySql_TableName, MySql_CreateTableQuery);
                    break;
            }

            Ready = true;
        }

        private void LiteDB_Init()
        {
            using (var db = new LiteDB.LiteDatabase(DatabaseManager.LiteDB_ConnectionString))
            {
                if (db.UserVersion == 0)
                {
                    var col = db.GetCollection<Player>(LiteDB_TableName);
                    col.EnsureIndex(x => x.SteamId);
                    col.EnsureIndex(x => x.CharacterName);
                    db.UserVersion = 1;
                }
            }
        }

        private long Json_NewId()
        {
            try
            {
                var last = Collection.Max(x => x.Id);
                return last + 1;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private async Task<List<Player>> LiteDB_LoadAllAsync()
        {
            var result = new List<Player>();
            using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
            {
                var col = db.GetCollection<Player>(LiteDB_TableName);
                var all = await col.FindAllAsync();
                result.AddRange(all);
            }

            return result;
        }

        private void JSON_Reload(bool migrate = false)
        {
            if (migrate)
            {
                MigrateCollection = Json_DataStore.Load();
                if (MigrateCollection != null)
                    return;
                MigrateCollection = new List<Player>();
                return;
            }

            Collection = Json_DataStore.Load();
            if (Collection != null)
                return;
            Collection = new List<Player>();
            Json_DataStore.Save(Collection);
        }

        private void MySQL_CreateTable(string tableName, string createTableQuery)
        {
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
            {
                Dapper.SqlMapper.Execute(connection, $"CREATE TABLE IF NOT EXISTS `{tableName}` ({createTableQuery});");
            }
        }

        private async Task<List<Player>> MySQL_LoadAllAsync()
        {
            var result = new List<Player>();
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
            {
                var loadQuery = $"SELECT * FROM `{MySql_TableName}`;";
                var databases = await Dapper.SqlMapper.QueryAsync<Player>(connection, loadQuery);
                result.AddRange(databases);
            }

            return result;
        }

        internal async Task AddAsync(Player player)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<Player>(LiteDB_TableName);
                        var exists = await col.ExistsAsync(x => x.SteamId == player.SteamId);
                        if (exists)
                            return;
                        await col.InsertAsync(player);
                    }

                    return;
                case EDatabase.JSON:
                    var existingPlayer = Collection.Find(x => x.SteamId == player.SteamId);
                    if (existingPlayer != null)
                        return;
                    player.Id = Json_NewId();
                    Collection.Add(player);
                    await Json_DataStore.SaveAsync(Collection);
                    return;
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var query =
                            $"SELECT 1 WHERE EXISTS (SELECT 1 FROM {MySql_TableName} WHERE SteamId = @SteamId)";
                        var exists = await Dapper.SqlMapper.QueryAsync<object>(connection,
                            query, new {SteamId = player.SteamId.ToString()});
                        if (exists.Any())
                            return;

                        query =
                            $"INSERT INTO {MySql_TableName} (SteamId, SteamGroupId, CharacterName, Language, IsAdmin, Gold, Beard, " +
                            $"Face, Hair, Skin, Color, BackpackSkin, GlassesSkin, HatSkin, MaskSkin, PantsSkin, ShirtSkin, VestSkin, " +
                            $"ItemSkins, SkinTags, SkinDynamicProps, SelectedSkillset, ConnectedTime, DisconnectedTime, PlayTime, HWID) " +
                            $"VALUES (@SteamId, @SteamGroupId, @CharacterName, @Language, @IsAdmin, @Gold, @Beard, @" +
                            $"Face, @Hair, @Skin, @Color, @BackpackSkin, @GlassesSkin, @HatSkin, @MaskSkin, @PantsSkin, @ShirtSkin, @VestSkin, @" +
                            $"ItemSkins, @SkinTags, @SkinDynamicProps, @SelectedSkillset, @ConnectedTime, @DisconnectedTime, @PlayTime, @HWID);";
                        var parameter = new Dapper.DynamicParameters();
                        parameter.Add("@SteamId", player.SteamId, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@SteamGroupId", player.SteamGroupId, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@CharacterName", player.CharacterName, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Language", player.Language, DbType.String, ParameterDirection.Input);
                        parameter.Add("@IsAdmin", player.IsAdmin, DbType.Boolean, ParameterDirection.Input);
                        parameter.Add("@Gold", player.Gold, DbType.Boolean, ParameterDirection.Input);
                        parameter.Add("@Beard", player.Beard, DbType.Byte, ParameterDirection.Input);
                        parameter.Add("@Face", player.Face, DbType.Byte, ParameterDirection.Input);
                        parameter.Add("@Hair", player.Hair, DbType.Byte, ParameterDirection.Input);
                        parameter.Add("@Skin", player.Skin, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Color", player.Color, DbType.String, ParameterDirection.Input);
                        parameter.Add("@BackpackSkin", player.BackpackSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@GlassesSkin", player.GlassesSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@HatSkin", player.HatSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@MaskSkin", player.MaskSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@PantsSkin", player.PantsSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@ShirtSkin", player.ShirtSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@VestSkin", player.VestSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@ItemSkins", player.ItemSkins, DbType.String, ParameterDirection.Input);
                        parameter.Add("@SkinTags", player.SkinTags, DbType.String, ParameterDirection.Input);
                        parameter.Add("@SkinDynamicProps", player.SkinDynamicProps, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@SelectedSkillset", player.SelectedSkillset, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@ConnectedTime", player.ConnectedTime, DbType.DateTime,
                            ParameterDirection.Input);
                        parameter.Add("@DisconnectedTime", player.DisconnectedTime, DbType.DateTime,
                            ParameterDirection.Input);
                        parameter.Add("@PlayTime", player.PlayTime, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@HWID", player.HWID, DbType.String, ParameterDirection.Input);
                        await Dapper.SqlMapper.ExecuteAsync(connection, query, parameter);
                    }

                    return;
            }
        }

        internal Player GetInternal(ulong steamId)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.LiteDatabase(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<Player>(LiteDB_TableName);
                        return col.Query().Where(x => x.SteamId == steamId).FirstOrDefault();
                    }
                case EDatabase.JSON:
                    return Collection.Find(x => x.SteamId == steamId);
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var query = $"SELECT * FROM `{MySql_TableName}` WHERE SteamId = @SteamId;";
                        return Dapper.SqlMapper.QueryFirstOrDefault<Player>(connection, query, new {SteamId = steamId});
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal List<Player> GetInternal(string name)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.LiteDatabase(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<Player>(LiteDB_TableName);
                        return col.Find(x => x.CharacterName.ToLower().Contains(name.ToLower())).ToList();
                    }
                case EDatabase.JSON:
                    return Collection.FindAll(x => x.CharacterName.ToLower().Contains(name.ToLower()));
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var query = $"SELECT * FROM `{MySql_TableName}` WHERE LOCATE('{name}', CharacterName) > 0;";
                        return Dapper.SqlMapper.Query<Player>(connection, query) as List<Player> ?? new List<Player>();
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IDictionary<string, object> GetInfoBySteamId(ulong steamId)
        {
            var player = GetInternal(steamId);
            return player?.GetType().GetProperties()
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(player, null));
        }

        public List<IDictionary<string, object>> GetInfoByName(string name)
        {
            var players = GetInternal(name);
            return players.Select(player => player?.GetType()
                    .GetProperties()
                    .ToDictionary(prop => prop.Name, prop => prop.GetValue(player, null)))
                .Cast<IDictionary<string, object>>()
                .ToList();
        }

        public Player Get(ulong steamId)
        {
            return GetInternal(steamId)?.Copy();
        }

        public IReadOnlyList<Player> Get(string name)
        {
            return GetInternal(name);
        }

        internal async Task UpdateAsync(Player player)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<Player>(LiteDB_TableName);
                        await col.UpdateAsync(player);
                        return;
                    }
                case EDatabase.JSON:
                    await Json_DataStore.SaveAsync(Collection);
                    return;
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var query1 = string.Empty;
                        var propertyInfos =
                            typeof(Player).GetProperties(BindingFlags.Public |
                                                         BindingFlags.Instance);
                        foreach (var propertyInfo in propertyInfos)
                        {
                            if (propertyInfo.Name == "SteamId")
                                continue;
                            if (propertyInfo.Name == propertyInfos.Last().Name)
                                query1 += $"`{propertyInfo.Name}` = @{propertyInfo.Name}";
                            else
                                query1 += $"`{propertyInfo.Name}` = @{propertyInfo.Name}, ";
                        }

                        var query =
                            $"UPDATE {MySql_TableName} SET {query1} WHERE SteamId = @SteamId;";
                        var parameter = new Dapper.DynamicParameters();
                        parameter.Add("@Id", player.Id, DbType.Int64, ParameterDirection.Input);
                        parameter.Add("@SteamId", player.SteamId, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@SteamGroupId", player.SteamGroupId, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@CharacterName", player.CharacterName, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Language", player.Language, DbType.String, ParameterDirection.Input);
                        parameter.Add("@IsAdmin", player.IsAdmin, DbType.Boolean, ParameterDirection.Input);
                        parameter.Add("@Gold", player.Gold, DbType.Boolean, ParameterDirection.Input);
                        parameter.Add("@Beard", player.Beard, DbType.Byte, ParameterDirection.Input);
                        parameter.Add("@Face", player.Face, DbType.Byte, ParameterDirection.Input);
                        parameter.Add("@Hair", player.Hair, DbType.Byte, ParameterDirection.Input);
                        parameter.Add("@Skin", player.Skin, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Color", player.Color, DbType.String, ParameterDirection.Input);
                        parameter.Add("@BackpackSkin", player.BackpackSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@GlassesSkin", player.GlassesSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@HatSkin", player.HatSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@MaskSkin", player.MaskSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@PantsSkin", player.PantsSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@ShirtSkin", player.ShirtSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@VestSkin", player.VestSkin, DbType.Int32, ParameterDirection.Input);
                        parameter.Add("@ItemSkins", player.ItemSkins, DbType.String, ParameterDirection.Input);
                        parameter.Add("@SkinTags", player.SkinTags, DbType.String, ParameterDirection.Input);
                        parameter.Add("@SkinDynamicProps", player.SkinDynamicProps, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@SelectedSkillset", player.SelectedSkillset, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@ConnectedTime", player.ConnectedTime, DbType.DateTime,
                            ParameterDirection.Input);
                        parameter.Add("@DisconnectedTime", player.DisconnectedTime, DbType.DateTime,
                            ParameterDirection.Input);
                        parameter.Add("@PlayTime", player.PlayTime, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@HWID", player.HWID, DbType.String, ParameterDirection.Input);
                        await Dapper.SqlMapper.ExecuteAsync(connection, query, parameter);
                        return;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal async Task AddPlaytimeAsync(ulong steamId)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<Player>(LiteDB_TableName);
                        var player = await col.Query().Where(x => x.SteamId == steamId).FirstOrDefaultAsync();
                        player.PlayTime++;
                        await col.UpdateAsync(player);
                        return;
                    }
                case EDatabase.JSON:
                    var playerr = Collection.Find(x => x.SteamId == steamId);
                    playerr.PlayTime++;
                    await Json_DataStore.SaveAsync(Collection);
                    return;
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var query =
                            $"UPDATE {MySql_TableName} SET PlayTime = PlayTime + 1 WHERE SteamId = @SteamId;";
                        await Dapper.SqlMapper.ExecuteAsync(connection, query, new {SteamId = steamId});
                        return;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal async Task UpdateConnectedDisconnectedAsync(ulong steamId, bool disconnected = false)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<Player>(LiteDB_TableName);
                        var player = await col.Query().Where(x => x.SteamId == steamId).FirstOrDefaultAsync();
                        if (disconnected)
                            player.DisconnectedTime = DateTime.Now;
                        else
                            player.ConnectedTime = DateTime.Now;
                        await col.UpdateAsync(player);
                        return;
                    }
                case EDatabase.JSON:
                    var playerr = Collection.Find(x => x.SteamId == steamId);
                    if (disconnected)
                        playerr.DisconnectedTime = DateTime.Now;
                    else
                        playerr.ConnectedTime = DateTime.Now;
                    await Json_DataStore.SaveAsync(Collection);
                    return;
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var parameter = new Dapper.DynamicParameters();
                        parameter.Add(disconnected ? "@DisconnectedTime" : "@ConnectedTime", DateTime.Now,
                            DbType.DateTime, ParameterDirection.Input);
                        var query =
                            $"UPDATE {MySql_TableName} SET {(disconnected ? "DisconnectedTime = @DisconnectedTime" : "ConnectedTime = @ConnectedTime")} WHERE SteamId = @SteamId;";
                        parameter.Add("@SteamId", steamId, DbType.UInt64, ParameterDirection.Input);
                        await Dapper.SqlMapper.ExecuteAsync(connection, query, parameter);
                        return;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal async Task MigrateAsync(EDatabase from, EDatabase to)
        {
            switch (from)
            {
                case EDatabase.LITEDB:
                    MigrateCollection = await LiteDB_LoadAllAsync();
                    switch (to)
                    {
                        case EDatabase.JSON:
                            Json_DataStore = new DataStore<List<Player>>(Plugin.Inst.Directory, Json_FileName);
                            await Json_DataStore.SaveAsync(MigrateCollection);
                            break;
                        case EDatabase.MYSQL:
                            MySQL_CreateTable(MySql_TableName, MySql_CreateTableQuery);
                            using (var connection =
                                new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                            {
                                var deleteQuery = $"DELETE FROM {MySql_TableName};";
                                await Dapper.SqlMapper.ExecuteAsync(connection, deleteQuery);

                                foreach (var player in MigrateCollection)
                                {
                                    var query =
                                        $"INSERT INTO {MySql_TableName} (Id, SteamId, SteamGroupId, CharacterName, Language, IsAdmin, Gold, Beard, " +
                                        $"Face, Hair, Skin, Color, BackpackSkin, GlassesSkin, HatSkin, MaskSkin, PantsSkin, ShirtSkin, VestSkin, " +
                                        $"ItemSkins, SkinTags, SkinDynamicProps, SelectedSkillset, ConnectedTime, DisconnectedTime, PlayTime, HWID) " +
                                        $"VALUES (@Id, @SteamId, @SteamGroupId, @CharacterName, @Language, @IsAdmin, @Gold, @Beard, @" +
                                        $"Face, @Hair, @Skin, @Color, @BackpackSkin, @GlassesSkin, @HatSkin, @MaskSkin, @PantsSkin, @ShirtSkin, @VestSkin, @" +
                                        $"ItemSkins, @SkinTags, @SkinDynamicProps, @SelectedSkillset, @ConnectedTime, @DisconnectedTime, @PlayTime, @HWID);";
                                    var parameter = new Dapper.DynamicParameters();
                                    parameter.Add("@Id", player.Id, DbType.Int64, ParameterDirection.Input);
                                    parameter.Add("@SteamId", player.SteamId, DbType.UInt64, ParameterDirection.Input);
                                    parameter.Add("@SteamGroupId", player.SteamGroupId, DbType.UInt64,
                                        ParameterDirection.Input);
                                    parameter.Add("@CharacterName", player.CharacterName, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Language", player.Language, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@IsAdmin", player.IsAdmin, DbType.Boolean, ParameterDirection.Input);
                                    parameter.Add("@Gold", player.Gold, DbType.Boolean, ParameterDirection.Input);
                                    parameter.Add("@Beard", player.Beard, DbType.Byte, ParameterDirection.Input);
                                    parameter.Add("@Face", player.Face, DbType.Byte, ParameterDirection.Input);
                                    parameter.Add("@Hair", player.Hair, DbType.Byte, ParameterDirection.Input);
                                    parameter.Add("@Skin", player.Skin, DbType.String, ParameterDirection.Input);
                                    parameter.Add("@Color", player.Color, DbType.String, ParameterDirection.Input);
                                    parameter.Add("@BackpackSkin", player.BackpackSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@GlassesSkin", player.GlassesSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@HatSkin", player.HatSkin, DbType.Int32, ParameterDirection.Input);
                                    parameter.Add("@MaskSkin", player.MaskSkin, DbType.Int32, ParameterDirection.Input);
                                    parameter.Add("@PantsSkin", player.PantsSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@ShirtSkin", player.ShirtSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@VestSkin", player.VestSkin, DbType.Int32, ParameterDirection.Input);
                                    parameter.Add("@ItemSkins", player.ItemSkins, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@SkinTags", player.SkinTags, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@SkinDynamicProps", player.SkinDynamicProps, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@SelectedSkillset", player.SelectedSkillset, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@ConnectedTime", player.ConnectedTime, DbType.DateTime,
                                        ParameterDirection.Input);
                                    parameter.Add("@DisconnectedTime", player.DisconnectedTime, DbType.DateTime,
                                        ParameterDirection.Input);
                                    parameter.Add("@PlayTime", player.PlayTime, DbType.UInt64,
                                        ParameterDirection.Input);
                                    parameter.Add("@HWID", player.HWID, DbType.String, ParameterDirection.Input);
                                    await Dapper.SqlMapper.ExecuteAsync(connection, query, parameter);
                                }
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(to), to, null);
                    }

                    break;
                case EDatabase.JSON:
                    Json_DataStore = new DataStore<List<Player>>(Plugin.Inst.Directory, Json_FileName);
                    JSON_Reload(true);
                    switch (to)
                    {
                        case EDatabase.LITEDB:
                            using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                            {
                                var col = db.GetCollection<Player>(LiteDB_TableName);
                                await col.DeleteAllAsync();
                                await col.InsertBulkAsync(MigrateCollection);
                            }

                            break;
                        case EDatabase.MYSQL:
                            MySQL_CreateTable(MySql_TableName, MySql_CreateTableQuery);
                            using (var connection =
                                new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                            {
                                var deleteQuery = $"DELETE FROM {MySql_TableName};";
                                await Dapper.SqlMapper.ExecuteAsync(connection, deleteQuery);

                                foreach (var player in MigrateCollection)
                                {
                                    var query =
                                        $"INSERT INTO {MySql_TableName} (Id, SteamId, SteamGroupId, CharacterName, Language, IsAdmin, Gold, Beard, " +
                                        $"Face, Hair, Skin, Color, BackpackSkin, GlassesSkin, HatSkin, MaskSkin, PantsSkin, ShirtSkin, VestSkin, " +
                                        $"ItemSkins, SkinTags, SkinDynamicProps, SelectedSkillset, ConnectedTime, DisconnectedTime, PlayTime, HWID) " +
                                        $"VALUES (@Id, @SteamId, @SteamGroupId, @CharacterName, @Language, @IsAdmin, @Gold, @Beard, @" +
                                        $"Face, @Hair, @Skin, @Color, @BackpackSkin, @GlassesSkin, @HatSkin, @MaskSkin, @PantsSkin, @ShirtSkin, @VestSkin, @" +
                                        $"ItemSkins, @SkinTags, @SkinDynamicProps, @SelectedSkillset, @ConnectedTime, @DisconnectedTime, @PlayTime, @HWID);";
                                    var parameter = new Dapper.DynamicParameters();
                                    parameter.Add("@Id", player.Id, DbType.Int64, ParameterDirection.Input);
                                    parameter.Add("@SteamId", player.SteamId, DbType.UInt64, ParameterDirection.Input);
                                    parameter.Add("@SteamGroupId", player.SteamGroupId, DbType.UInt64,
                                        ParameterDirection.Input);
                                    parameter.Add("@CharacterName", player.CharacterName, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Language", player.Language, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@IsAdmin", player.IsAdmin, DbType.Boolean, ParameterDirection.Input);
                                    parameter.Add("@Gold", player.Gold, DbType.Boolean, ParameterDirection.Input);
                                    parameter.Add("@Beard", player.Beard, DbType.Byte, ParameterDirection.Input);
                                    parameter.Add("@Face", player.Face, DbType.Byte, ParameterDirection.Input);
                                    parameter.Add("@Hair", player.Hair, DbType.Byte, ParameterDirection.Input);
                                    parameter.Add("@Skin", player.Skin, DbType.String, ParameterDirection.Input);
                                    parameter.Add("@Color", player.Color, DbType.String, ParameterDirection.Input);
                                    parameter.Add("@BackpackSkin", player.BackpackSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@GlassesSkin", player.GlassesSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@HatSkin", player.HatSkin, DbType.Int32, ParameterDirection.Input);
                                    parameter.Add("@MaskSkin", player.MaskSkin, DbType.Int32, ParameterDirection.Input);
                                    parameter.Add("@PantsSkin", player.PantsSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@ShirtSkin", player.ShirtSkin, DbType.Int32,
                                        ParameterDirection.Input);
                                    parameter.Add("@VestSkin", player.VestSkin, DbType.Int32, ParameterDirection.Input);
                                    parameter.Add("@ItemSkins", player.ItemSkins, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@SkinTags", player.SkinTags, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@SkinDynamicProps", player.SkinDynamicProps, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@SelectedSkillset", player.SelectedSkillset, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@ConnectedTime", player.ConnectedTime, DbType.DateTime,
                                        ParameterDirection.Input);
                                    parameter.Add("@DisconnectedTime", player.DisconnectedTime, DbType.DateTime,
                                        ParameterDirection.Input);
                                    parameter.Add("@PlayTime", player.PlayTime, DbType.UInt64,
                                        ParameterDirection.Input);
                                    parameter.Add("@HWID", player.HWID, DbType.String, ParameterDirection.Input);
                                    await Dapper.SqlMapper.ExecuteAsync(connection, query, parameter);
                                }
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(to), to, null);
                    }

                    break;
                case EDatabase.MYSQL:
                    MySQL_CreateTable(MySql_TableName, MySql_CreateTableQuery);
                    MigrateCollection = await MySQL_LoadAllAsync();
                    switch (to)
                    {
                        case EDatabase.LITEDB:
                            using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                            {
                                var col = db.GetCollection<Player>(LiteDB_TableName);
                                await col.DeleteAllAsync();
                                await col.InsertBulkAsync(MigrateCollection);
                            }

                            break;
                        case EDatabase.JSON:
                            Json_DataStore = new DataStore<List<Player>>(Plugin.Inst.Directory, Json_FileName);
                            await Json_DataStore.SaveAsync(MigrateCollection);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(to), to, null);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(from), from, null);
            }

            MigrateCollection.Clear();
            MigrateCollection.TrimExcess();
        }
    }
}