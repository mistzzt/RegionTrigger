using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
									 new SqlColumn("Flags", MySqlDbType.Text),
									 new SqlColumn("EnterMsg", MySqlDbType.Text),
									 new SqlColumn("LeaveMsg", MySqlDbType.Text),
									 new SqlColumn("Message", MySqlDbType.String, 20),
									 new SqlColumn("MessageInterval", MySqlDbType.Int32),
									 new SqlColumn("TempGroup", MySqlDbType.String, 32),
									 new SqlColumn("Itembans", MySqlDbType.Text),
									 new SqlColumn("Projbans", MySqlDbType.Text),
									 new SqlColumn("Tilebans", MySqlDbType.Text)
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
						var entermsg = reader.Get<string>("EnterMsg");
						var leavemsg = reader.Get<string>("LeaveMsg");
						var msg = reader.Get<string>("Message");
						var msgitv = reader.Get<int?>("MessageInterval");
						var tempgroupstr = reader.Get<string>("TempGroup");
						var itemb = reader.Get<string>("Itembans");
						var projb = reader.Get<string>("Projbans");
						var tileb = reader.Get<string>("Tilebans");

						Group temp = TShock.Utils.GetGroup(tempgroupstr);
						var region = new RtRegion(id, regionId) {
							Events = flagstr ?? Events.None,
							EnterMsg = entermsg,
							LeaveMsg = leavemsg,
							Message = msg,
							MsgInterval = msgitv ?? 0,
							TempGroup = temp,
							Itembans = itemb,
							Projbans = projb,
							Tilebans = tileb
						};

						if(region.HasEvent(Events.TempGroup) && region.TempGroup == null)
							TShock.Log.Error("[RegionTrigger] TempGroup '{0}' of region '{1}' is invalid!", tempgroupstr, region.Region.Name);

						Regions.Add(region);
					}
				}
			} catch(Exception e) {
#if DEBUG
				Debug.WriteLine(e);
				Debugger.Break();
#endif
				TShock.Log.ConsoleError("[RegionTrigger] Load regions failed. Check log for more information.");
				TShock.Log.Error(e.ToString());
			}
		}

		public void AddRtRegion(string regionName, string flags) {
			if(Regions.Any(r => r.Region.Name == regionName))
				throw new Exception("Region is already defined!");

			var region = TShock.Regions.GetRegionByName(regionName);
			if(region == null)
				throw new Exception($"Couldn't find region named '{regionName}'!");
			
			string query = "INSERT INTO RtRegions (RegionId, Flags) VALUES (@0, @1);";
			try {
				if(_database.Query(query, region.ID, string.IsNullOrWhiteSpace(flags) ? Events.None : flags) != 0)
					return;
				throw new Exception("Database error: No affected rows.");
			} catch(Exception e) {
				TShock.Log.Error(e.ToString());
				throw new Exception("Database error! Check logs for more information.", e);
			}
		}

		public void DeleteRtRegion(string regionName) {
			var region = TShock.Regions.GetRegionByName(regionName);
			if(region == null)
				throw new Exception($"Couldn't find region named '{regionName}'!");

			if(Regions.All(r => r.Region.ID != region.ID))
				throw new Exception("Region has not been defined!");

			try {
				if(_database.Query("DELETE FROM RtRegions WHERE RegionId=@0", region.ID) != 0 &&
					Regions.RemoveAll(r => r.Region.ID == region.ID) != 0)
					return;
				throw new Exception("Database error: No affected rows.");
			} catch(Exception e) {
				TShock.Log.Error(e.ToString());
				throw new Exception("Database error! Check logs for more information.", e);
			}
		}

		public void AddFlags(string regionName, string flags) {
			var region = TShock.Regions.GetRegionByName(regionName);
			if(region == null)
				throw new Exception($"Couldn't find region named '{regionName}'!");

			RtRegion rt = GetRtRegionById(region.ID);
			if(rt == null)
				throw new Exception("Region has not been defined!");
			if(string.IsNullOrWhiteSpace(flags) || flags.ToLower() == Events.None)
				throw new ArgumentException("Invalid events!");

			var oldstr = rt.Events;
			var newevt = flags.ToLower().Split(',');
			if(oldstr.Length != 0)
				oldstr += ',';

			oldstr = newevt
				.Where(en => !string.IsNullOrWhiteSpace(en) && Events.Contains(en) && !rt.HasEvent(en))
				.Aggregate(oldstr, (current, en) => current + $"{en},");
			oldstr = oldstr.Substring(0, oldstr.Length - 1);

			if(_database.Query("UPDATE RtRegions SET Flags=@0 WHERE Id=@1", oldstr, rt.Id) == 0)
				throw new Exception("Database error: No affected rows.");
			rt.Events = oldstr;
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

		public RtRegion GetRtRegionById(int id)
			=> Regions.SingleOrDefault(rt => rt.Id == id);

		public RtRegion GetRtRegionByRegionId(int regionId)
			=> Regions.SingleOrDefault(rt => regionId == rt.Region.ID);

		public RtRegion GetTopRegion(IEnumerable<RtRegion> regions) {
			RtRegion ret = null;
			foreach(RtRegion r in regions) {
				if(ret == null)
					ret = r;
				else {
					if(r.Region.Z > ret.Region.Z)
						ret = r;
				}
			}
			return ret;
		}
	}
}
