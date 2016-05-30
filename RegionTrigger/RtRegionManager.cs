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
                        var id = reader.Get<int>("Id");
                        var regionId = reader.Get<int>("RegionId");
                        var flagstr = reader.Get<string>("Flags");
                        var groupstr = reader.Get<string>("GroupName");
                        var entermsg = reader.Get<string>("EnterMsg");
                        var leavemsg = reader.Get<string>("LeaveMsg");
                        var msg = reader.Get<string>("Message");
                        var msgitv = reader.Get<int>("MessageInterval");
                        var tempgroupstr = reader.Get<string>("TempGroup");
                        var itemb = reader.Get<string>("Itembans");
                        var projb = reader.Get<string>("Projbans");
                        var tileb = reader.Get<string>("Tilebans");

                    }
                }
            } catch {

            }
        }

        public bool AddRtRegion(int regionId, string flags = "None") {
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
                if (_database.Query("DELETE FROM RtRegions WHERE Id=@0", rtregionid) != 0 && Regions.RemoveAll(r => r.Id == rtregionid) != 0)
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

        public bool AddFlags(int rtregionId, string flags = "None") {
            // if have - return
            // if noexist - return
            // update db
            return false;
        }

        public bool SetGroup(int rtregionId, string group) {
            // if have - return
            // if noexist - return
            // update db
            return false;
        }

        public bool SetTempGroup(int rtregionId, string tempGroup) {
            // if have - return
            // if noexist - return
            // update db
            return false;
        }

        public bool AddItembans(int rtregionId, string item) {
            // if have - return
            // if noexist - return
            // update db
            return false;
        }

        public bool RmItembans(int rtregionId, string item) {
            // if dont have - return
            // if noexist - return
            // update db
            return false;
        }

        public bool AddProjbans(int rtregionId, int proj) {
            // if have - return
            // if noexist - return
            // update db
            return false;
        }

        public bool RmProjbans(int rtregionId, int proj) {
            // if dont have - return
            // if noexist - return
            // update db
            return false;
        }
        public bool AddTilebans(int rtregionId, int tile) {
            // if have - return
            // if noexist - return
            // update db
            return false;
        }

        public bool RmTilebans(int rtregionId, int tile) {
            // if dont have - return
            // if noexist - return
            // update db
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
