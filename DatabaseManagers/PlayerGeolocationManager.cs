using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RFPlayerLibrary.Enums;
using RFPlayerLibrary.Models;
using RFRocketLibrary.Storages;
using Rocket.Core.Logging;

namespace RFPlayerLibrary.DatabaseManagers
{
    public class PlayerGeolocationManager
    {
        internal static bool Ready { get; set; }
        internal List<PlayerGeolocation> Collection { get; set; } = new List<PlayerGeolocation>();
        internal List<PlayerGeolocation> MigrateCollection { get; set; } = new List<PlayerGeolocation>();

        private const string LiteDB_TableName = "geolocation";

        private const string Json_FileName = "_geolocation.json";
        private DataStore<List<PlayerGeolocation>> Json_DataStore { get; set; }

        private const string MySql_TableName = "rfplayerlibrary_geolocation";

        private const string MySql_CreateTableQuery =
            "`Id` BIGINT NOT NULL AUTO_INCREMENT, " +
            "`SteamId` BIGINT UNSIGNED NOT NULL DEFAULT 0, " +
            "`IP` VARCHAR(50) NOT NULL DEFAULT 0, " +
            "`Country` VARCHAR(10) NOT NULL DEFAULT 'N/A', " +
            "`CountryCode` VARCHAR(10) NOT NULL DEFAULT 'N/A', " +
            "`Region` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`RegionName` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`City` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`Zip` VARCHAR(10) NOT NULL DEFAULT 'N/A', " +
            "`Latitude` DECIMAL NOT NULL DEFAULT 0, " +
            "`Longitude` DECIMAL NOT NULL DEFAULT 0, " +
            "`Timezone` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`ISP` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`Organization` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "`Ass` VARCHAR(255) NOT NULL DEFAULT 'N/A', " +
            "PRIMARY KEY (Id)";

        internal PlayerGeolocationManager()
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    LiteDB_Init();
                    break;
                case EDatabase.JSON:
                    Json_DataStore = new DataStore<List<PlayerGeolocation>>(Plugin.Inst.Directory, Json_FileName);
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
                    var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
                    col.EnsureIndex(x => x.SteamId);
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

        private async Task<List<PlayerGeolocation>> LiteDB_LoadAllAsync()
        {
            var result = new List<PlayerGeolocation>();
            using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
            {
                var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
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
                MigrateCollection = new List<PlayerGeolocation>();
                return;
            }
            Collection = Json_DataStore.Load();
            if (Collection != null)
                return;
            Collection = new List<PlayerGeolocation>();
            Json_DataStore.Save(Collection);
        }

        private void MySQL_CreateTable(string tableName, string createTableQuery)
        {
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
            {
                Dapper.SqlMapper.Execute(connection, $"CREATE TABLE IF NOT EXISTS `{tableName}` ({createTableQuery});");
            }
        }

        private async Task<List<PlayerGeolocation>> MySQL_LoadAllAsync()
        {
            var result = new List<PlayerGeolocation>();
            using (var connection = new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
            {
                var loadQuery = $"SELECT * FROM `{MySql_TableName}`;";
                var databases = await Dapper.SqlMapper.QueryAsync<PlayerGeolocation>(connection, loadQuery);
                result.AddRange(databases);
            }

            return result;
        }

        internal async Task AddAsync(PlayerGeolocation playerGeolocation)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
                        var exists = await col.ExistsAsync(x => x.SteamId == playerGeolocation.SteamId);
                        if (exists)
                            return;
                        await col.InsertAsync(playerGeolocation);
                    }

                    break;
                case EDatabase.JSON:
                    var existingPlayer = Collection.Find(x => x.SteamId == playerGeolocation.SteamId);
                    if (existingPlayer != null)
                        return;
                    playerGeolocation.Id = Json_NewId();
                    Collection.Add(playerGeolocation);
                    await Json_DataStore.SaveAsync(Collection);
                    break;
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var query =
                            $"SELECT 1 WHERE EXISTS (SELECT 1 FROM {MySql_TableName} WHERE SteamId = @SteamId)";
                        var exists = await Dapper.SqlMapper.QueryAsync<object>(connection,
                            query, new {playerGeolocation.SteamId});
                        if (exists.Any())
                            return;
                        
                        query =
                            $"INSERT INTO {MySql_TableName} (SteamId, `IP`, `Country`, CountryCode, Region, RegionName, City, Zip, " +
                            $"Latitude, Longitude, Timezone, `ISP`, `Organization`, `Ass`) VALUES (@SteamId, @IP, @Country, @CountryCode, " +
                            $"@Region, @RegionName, @City, @Zip, @Latitude, @Longitude, @Timezone, @ISP, @Organization, @Ass);";
                        var parameter = new Dapper.DynamicParameters();
                        parameter.Add("@SteamId", playerGeolocation.SteamId, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@IP", playerGeolocation.IP, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Country", playerGeolocation.Country, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@CountryCode", playerGeolocation.CountryCode, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@Region", playerGeolocation.Region, DbType.String, ParameterDirection.Input);
                        parameter.Add("@RegionName", playerGeolocation.RegionName, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@City", playerGeolocation.City, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Zip", playerGeolocation.Zip, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Latitude", playerGeolocation.Latitude, DbType.Decimal,
                            ParameterDirection.Input);
                        parameter.Add("@Longitude", playerGeolocation.Longitude, DbType.Decimal,
                            ParameterDirection.Input);
                        parameter.Add("@Timezone", playerGeolocation.Timezone, DbType.String, ParameterDirection.Input);
                        parameter.Add("@ISP", playerGeolocation.ISP, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Organization", playerGeolocation.Organization, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@Ass", playerGeolocation.Ass, DbType.String, ParameterDirection.Input);
                        await Dapper.SqlMapper.ExecuteAsync(connection, query, parameter);
                    }

                    break;
            }
        }

        internal PlayerGeolocation GetInternal(ulong steamId)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.LiteDatabase(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
                        return col.Query().Where(x => x.SteamId == steamId).FirstOrDefault();
                    }
                case EDatabase.JSON:
                    return Collection.Find(x => x.SteamId == steamId);
                case EDatabase.MYSQL:
                    using (var connection =
                        new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                    {
                        var query = $"SELECT * FROM `{MySql_TableName}` WHERE SteamId = @SteamId;";
                        return Dapper.SqlMapper.QueryFirstOrDefault<PlayerGeolocation>(connection, query, new {SteamId = steamId});
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // internal List<PlayerGeolocation> GetInternal(string name)
        // {
        //     switch (Plugin.Conf.Database)
        //     {
        //         case EDatabase.LITEDB:
        //             using (var db = new LiteDB.LiteDatabase(DatabaseManager.LiteDB_ConnectionString))
        //             {
        //                 var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
        //                 return col.Query().Where(x => x.SteamId == steamId).FirstOrDefault();
        //             }
        //         case EDatabase.JSON:
        //             return Collection.Find(x => x.SteamId == steamId);
        //         case EDatabase.MYSQL:
        //             using (var connection =
        //                 new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
        //             {
        //                 var query = $"SELECT * FROM `{MySql_TableName}` WHERE SteamId = @SteamId;";
        //                 return Dapper.SqlMapper.QueryFirstOrDefault<PlayerGeolocation>(connection, query, new {SteamId = steamId});
        //             }
        //         default:
        //             throw new ArgumentOutOfRangeException();
        //     }
        // }

        public IDictionary<string, object> GetInfoBySteamId(ulong steamId)
        {
            var playerGeolocation = GetInternal(steamId);
            return playerGeolocation?.GetType().GetProperties()
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(playerGeolocation, null));
        }

        // public List<IDictionary<string, object>> GetPlayerGeolocationInfoByName(string name)
        // {
        //     var playerGeolocations = GetInternal(name);
        //     return playerGeolocations.Select(player => player?.GetType()
        //             .GetProperties()
        //             .ToDictionary(prop => prop.Name, prop => prop.GetValue(player, null)))
        //         .Cast<IDictionary<string, object>>()
        //         .ToList();
        // }

        public PlayerGeolocation Get(ulong steamId)
        {
            return GetInternal(steamId)?.Copy();
        }

        // public IReadOnlyList<PlayerGeolocation> Get(string name)
        // {
        //     return Collection.FindAll(x =>
        //         x.CharacterName.ToLower().Contains(name.ToLower()));
        // }

        internal async Task UpdateAsync(PlayerGeolocation playerGeolocation)
        {
            switch (Plugin.Conf.Database)
            {
                case EDatabase.LITEDB:
                    using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                    {
                        var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
                        await col.UpdateAsync(playerGeolocation);
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
                            typeof(PlayerGeolocation).GetProperties(BindingFlags.Public |
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
                        parameter.Add("@Id", playerGeolocation.Id, DbType.Int64, ParameterDirection.Input);
                        parameter.Add("@SteamId", playerGeolocation.SteamId, DbType.UInt64, ParameterDirection.Input);
                        parameter.Add("@IP", playerGeolocation.IP, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Country", playerGeolocation.Country, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@CountryCode", playerGeolocation.CountryCode, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@Region", playerGeolocation.Region, DbType.String, ParameterDirection.Input);
                        parameter.Add("@RegionName", playerGeolocation.RegionName, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@City", playerGeolocation.City, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Zip", playerGeolocation.Zip, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Latitude", playerGeolocation.Latitude, DbType.Decimal,
                            ParameterDirection.Input);
                        parameter.Add("@Longitude", playerGeolocation.Longitude, DbType.Decimal,
                            ParameterDirection.Input);
                        parameter.Add("@Timezone", playerGeolocation.Timezone, DbType.String, ParameterDirection.Input);
                        parameter.Add("@ISP", playerGeolocation.ISP, DbType.String, ParameterDirection.Input);
                        parameter.Add("@Organization", playerGeolocation.Organization, DbType.String,
                            ParameterDirection.Input);
                        parameter.Add("@Ass", playerGeolocation.Ass, DbType.String, ParameterDirection.Input);
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
                            Json_DataStore = new DataStore<List<PlayerGeolocation>>(Plugin.Inst.Directory, Json_FileName);
                            await Json_DataStore.SaveAsync(MigrateCollection);
                            break;
                        case EDatabase.MYSQL:
                            MySQL_CreateTable(MySql_TableName, MySql_CreateTableQuery);
                            using (var connection =
                                new MySql.Data.MySqlClient.MySqlConnection(DatabaseManager.MySql_ConnectionString))
                            {
                                var deleteQuery = $"DELETE FROM {MySql_TableName};";
                                await Dapper.SqlMapper.ExecuteAsync(connection, deleteQuery);

                                foreach (var playerGeolocation in MigrateCollection)
                                {
                                    var query =
                                        $"INSERT INTO {MySql_TableName} (Id, SteamId, `IP`, Country, CountryCode, Region, RegionName, City, Zip, " +
                                        $"Latitude, Longitude, Timezone, ISP, `Organization`, `Ass`) VALUES (@Id, @SteamId, @IP, @Country, @CountryCode, " +
                                        $"@Region, @RegionName, @City, @Zip, @Latitude, @Longitude, @Timezone, @ISP, @Organization, @Ass);";
                                    var parameter = new Dapper.DynamicParameters();
                                    parameter.Add("@Id", playerGeolocation.Id, DbType.Int64, ParameterDirection.Input);
                                    parameter.Add("@SteamId", playerGeolocation.SteamId, DbType.UInt64,
                                        ParameterDirection.Input);
                                    parameter.Add("@IP", playerGeolocation.IP, DbType.String, ParameterDirection.Input);
                                    parameter.Add("@Country", playerGeolocation.Country, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@CountryCode", playerGeolocation.CountryCode, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Region", playerGeolocation.Region, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@RegionName", playerGeolocation.RegionName, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@City", playerGeolocation.City, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Zip", playerGeolocation.Zip, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Latitude", playerGeolocation.Latitude, DbType.Decimal,
                                        ParameterDirection.Input);
                                    parameter.Add("@Longitude", playerGeolocation.Longitude, DbType.Decimal,
                                        ParameterDirection.Input);
                                    parameter.Add("@Timezone", playerGeolocation.Timezone, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@ISP", playerGeolocation.ISP, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Organization", playerGeolocation.Organization, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Ass", playerGeolocation.Ass, DbType.String,
                                        ParameterDirection.Input);
                                    await Dapper.SqlMapper.ExecuteAsync(connection, query, parameter);
                                }
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(to), to, null);
                    }

                    break;
                case EDatabase.JSON:
                    Json_DataStore = new DataStore<List<PlayerGeolocation>>(Plugin.Inst.Directory, Json_FileName);
                    JSON_Reload(true);
                    switch (to)
                    {
                        case EDatabase.LITEDB:
                            using (var db = new LiteDB.Async.LiteDatabaseAsync(DatabaseManager.LiteDB_ConnectionString))
                            {
                                var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
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

                                foreach (var playerGeolocation in MigrateCollection)
                                {
                                    var query =
                                        $"INSERT INTO {MySql_TableName} (Id, SteamId, `IP`, Country, CountryCode, Region, RegionName, City, Zip, " +
                                        $"Latitude, `Longitude`, `Timezone`, `ISP`, `Organization`, `Ass`) VALUES (@Id, @SteamId, @IP, @Country, @CountryCode, " +
                                        $"@Region, @RegionName, @City, @Zip, @Latitude, @Longitude, @Timezone, @ISP, @Organization, @Ass);";
                                    var parameter = new Dapper.DynamicParameters();
                                    parameter.Add("@Id", playerGeolocation.Id, DbType.Int64, ParameterDirection.Input);
                                    parameter.Add("@SteamId", playerGeolocation.SteamId, DbType.UInt64,
                                        ParameterDirection.Input);
                                    parameter.Add("@IP", playerGeolocation.IP, DbType.String, ParameterDirection.Input);
                                    parameter.Add("@Country", playerGeolocation.Country, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@CountryCode", playerGeolocation.CountryCode, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Region", playerGeolocation.Region, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@RegionName", playerGeolocation.RegionName, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@City", playerGeolocation.City, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Zip", playerGeolocation.Zip, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Latitude", playerGeolocation.Latitude, DbType.Decimal,
                                        ParameterDirection.Input);
                                    parameter.Add("@Longitude", playerGeolocation.Longitude, DbType.Decimal,
                                        ParameterDirection.Input);
                                    parameter.Add("@Timezone", playerGeolocation.Timezone, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@ISP", playerGeolocation.ISP, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Organization", playerGeolocation.Organization, DbType.String,
                                        ParameterDirection.Input);
                                    parameter.Add("@Ass", playerGeolocation.Ass, DbType.String,
                                        ParameterDirection.Input);
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
                                var col = db.GetCollection<PlayerGeolocation>(LiteDB_TableName);
                                await col.DeleteAllAsync();
                                await col.InsertBulkAsync(MigrateCollection);
                            }

                            break;
                        case EDatabase.JSON:
                            Json_DataStore = new DataStore<List<PlayerGeolocation>>(Plugin.Inst.Directory, Json_FileName);
                            await Json_DataStore.SaveAsync(MigrateCollection);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(to), to, null);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(from), from, null);
            }
        }
    }
}