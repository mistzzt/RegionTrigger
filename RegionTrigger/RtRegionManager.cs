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
									 new SqlColumn("GroupName", MySqlDbType.String, 32),
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
						var groupstr = reader.Get<string>("GroupName");
						var entermsg = reader.Get<string>("EnterMsg");
						var leavemsg = reader.Get<string>("LeaveMsg");
						var msg = reader.Get<string>("Message");
						var msgitv = reader.Get<int?>("MessageInterval");
						var tempgroupstr = reader.Get<string>("TempGroup");
						var itemb = reader.Get<string>("Itembans");
						var projb = reader.Get<string>("Projbans");
						var tileb = reader.Get<string>("Tilebans");

						Group group = TShock.Utils.GetGroup(groupstr),
							temp = TShock.Utils.GetGroup(tempgroupstr);
						List<int> itemblist = new List<int>(),
							projblist = new List<int>(),
							tileblist = new List<int>();

						Regions.Add(new RtRegion(id, regionId) {
							Events = flagstr,
							Group = group,
							EnterMsg = entermsg,
							LeaveMsg = leavemsg,
							Message = msg,
							MsgInterval = msgitv ?? 0,
							TempGroup = temp,
							Itembans = itemblist,
							Projbans = projblist,
							Tilebans = tileblist
						});
					}
				}
#if DEBUG
				Console.WriteLine("[RegionTrigger] Successfully loaded {0}/{1} region(s).", Regions.Count, TShock.Regions.Regions.Count);
#endif
			} catch(Exception e) {
				Debug.WriteLine(e);
				Debugger.Break();
			}
		}

		public bool AddRtRegion(int regionId, string flags) {
			if(Regions.Any(r => r.RegionId == regionId))
				return false;

			string query = "INSERT INTO RtRegions (RegionId, Flags) VALUES (@0, @1);";
			try {
				if(_database.Query(query, regionId, string.IsNullOrWhiteSpace(flags) ? Events.None : flags) != 0)
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

		public bool DeleteRtRegion(int regionId) {
			try {
				if (_database.Query("DELETE FROM RtRegions WHERE RegionId=@0", regionId) != 0 &&
				    Regions.RemoveAll(r => r.RegionId == regionId) != 0)
					return true;
			}
			catch (Exception e) {
#if DEBUG
				Debug.WriteLine(e);
				Debugger.Break();
#endif
				TShock.Log.Error(e.ToString());
			}
			return false;
		}

		public bool AddFlags(int rtregionId, string flags) {
			RtRegion rt = GetRtRegionById(rtregionId);
			if(rt == null)
				return false;
			if(string.IsNullOrWhiteSpace(flags) || flags.ToLower() == Events.None)
				return false;
			// todo: it may be removed because in RtRegion.Events.Set the function will also validate events
			var oldstr = rt.Events;
			var newevt = flags.ToLower().Split(',');
			if(oldstr.Length != 0)
				oldstr += ',';

			oldstr = newevt
				.Where(en => !string.IsNullOrWhiteSpace(en) && Events.Contains(en) && !rt.EventsList.Contains(en))
				.Aggregate(oldstr, (current, en) => current + $"{en},");
			oldstr = oldstr.Substring(0, oldstr.Length - 1);
			//update db and rtregion
			if(_database.Query("UPDATE RtRegions SET Flags=@0 WHERE Id=@1", oldstr, rtregionId) == 0)
				return false;
			rt.Events = oldstr;
			return true;
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
			=> Regions.SingleOrDefault(rt => rt.RegionId == regionId);
	}
}
