using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace RegionTrigger {
    public class RtRegionManager {
        /// <summary>
		/// The list of regions.
		/// </summary>
		public readonly List<RtRegion> Regions = new List<RtRegion>();

        private readonly IDbConnection _database;

        internal RtRegionManager(IDbConnection db) {
            _database = db;
            var table = new SqlTable("RtRegions",
                                     new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                                     new SqlColumn("RegionId", MySqlDbType.Int32) { Unique = true, NotNull = true },
                                     new SqlColumn("Flags", MySqlDbType.String),
                                     new SqlColumn("GroupName", MySqlDbType.String, 32),
                                     new SqlColumn("EnterMsg", MySqlDbType.String),
                                     new SqlColumn("LeaveMsg", MySqlDbType.String),
                                     new SqlColumn("Message", MySqlDbType.String, 20),
                                     new SqlColumn("MessageInterval", MySqlDbType.Int32),
                                     new SqlColumn("TempGroup", MySqlDbType.String, 32),
                                     new SqlColumn("Itembans", MySqlDbType.String),
                                     new SqlColumn("Projbans", MySqlDbType.String),
                                     new SqlColumn("Tilebans", MySqlDbType.String)
                );
            var creator = new SqlTableCreator(db,
                                              db.GetSqlType() == SqlType.Sqlite
                                                  ? (IQueryBuilder)new SqliteQueryCreator()
                                                  : new MysqlQueryCreator());
            creator.EnsureTableStructure(table);
        }

        public void Reload() {
            try {
                using(
                    var reader =
                        _database.QueryReader(
                            "SELECT `rtregions`.* FROM `rtregions`, `regions` WHERE `rtregions`.RegionId = `regions`.Id AND `regions`.WorldID = @0",
                            Main.worldID.ToString())
                    ) {
                    Regions.Clear();
                    while(reader.Read()) {
                        int id = reader.Get<int>("Id");
                        int regionId = reader.Get<int>("RegionId");
                        string flagstr = reader.Get<string>("Flags");
                        string groupstr = reader.Get<string>("TempGroup");

                    }
                }
            } catch {

            }
        }

        public bool AddRtRegion(int regionId, string flags = null) {
            string query = "INSERT INTO RtRegions (RegionId, Flags) VALUES (@0, @1);";
            // todo: check flags here or other place
            try {
                if(_database.Query(query, regionId, string.IsNullOrWhiteSpace(flags) ? "None" : flags) != 0)
                    return true;
                return false;
            } catch(Exception e) {
#if DEBUG
                Debug.WriteLine(e);
                Debugger.Break();
#endif
                TShock.Log.Error(e.ToString());
                return false;
            }
        }

        public bool DeleteRtRegion(int rtregionid) {
            try {
                //_database.Query("DELETE FROM Regions WHERE Id=@0 AND WorldID=@1", id, Main.worldID.ToString());

                //Regions.RemoveAll(r => r.ID == id && r.WorldID == worldid);

                //SELECT `rtregions`.* FROM `rtregions`, `regions` WHERE `rtregions`.RegionId = `regions`.Id


                return true;
            } catch(Exception e) {
#if DEBUG
                Debug.WriteLine(e);
                Debugger.Break();
#endif
                TShock.Log.Error(e.ToString());
            }
            return false;
        }

        public RtRegion GetRtRegionById(int id) {
            if(id < 0 || id >= Regions.Count)
                return null;
            return Regions[id];
        }

        public RtRegion GetRtRegionByRegionId(int regionId)
            => Regions.SingleOrDefault(rt => rt.RegionId == regionId);
    }
}
