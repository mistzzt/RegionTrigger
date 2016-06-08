using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace RegionTrigger {
	public class RtRegion {
		public int Id { get; set; }
		public string EnterMsg { get; set; }
		public string LeaveMsg { get; set; }
		public string Message { get; set; }
		public int MsgInterval { get; set; }
		public Group TempGroup { get; set; }
		public readonly Region Region;

		private readonly List<string> _events = new List<string>();
		public string Events {
			get {
				return _events.Count == 0
					? global::RegionTrigger.Events.None
					: string.Join(",", _events);
			}
			set {
				if(string.IsNullOrWhiteSpace(value))
					return;
				_events.Clear();
				var events = value.Split(',');
				foreach(var @event in events.Where(e => !string.IsNullOrWhiteSpace(e)))
					_events.Add(@event.Trim());
			}
		}

		private readonly List<string> _itembans = new List<string>();
		public string Itembans {
			get { return string.Join(",", _itembans); }
			set {
				if(string.IsNullOrWhiteSpace(value))
					return;
				_itembans.Clear();
				var items = value.Trim().Split(',');
				foreach(var item in items.Where(e => !string.IsNullOrWhiteSpace(e))) {
					_itembans.Add(item);
				}
			}
		}

		private readonly List<short> _projbans = new List<short>();
		public string Projbans {
			get { return string.Join(",", _projbans); }
			set {
				if(string.IsNullOrWhiteSpace(value))
					return;
				_projbans.Clear();
				var projids = value.Trim().ToLower().Split(',');
				foreach(var projid in projids.Where(e => !string.IsNullOrWhiteSpace(e))) {
					short proj;
					if(short.TryParse(projid, out proj) && proj > 0 && proj < Main.maxProjectileTypes)
						_projbans.Add(proj);
				}
			}
		}

		private readonly List<short> _tilebans = new List<short>();
		public string Tilebans {
			get { return string.Join(",", _tilebans); }
			set {
				if(string.IsNullOrWhiteSpace(value))
					return;
				_tilebans.Clear();
				var tileids = value.Trim().ToLower().Split(',');
				foreach(var tileid in tileids.Where(e => !string.IsNullOrWhiteSpace(e))) {
					short tile;
					if(short.TryParse(tileid, out tile) && tile > -1 && tile < Main.maxTileSets)
						_tilebans.Add(tile);
				}
			}
		}

		public RtRegion(int id, int rid) {
			Id = id;
			Region = TShock.Regions.GetRegionByID(rid);
			if(Region == null)
				throw new Exception("Invalid region Id!");
		}

		public bool HasEvent(string @event)
			=> _events.Contains(@event);

		public bool RemoveEvent(string @event)
			=> _events.Remove(@event);

		public bool TileIsBanned(short tileId)
			=> _tilebans.Contains(tileId);

		public bool RemoveBannedTile(short tileId)
			=> _tilebans.Remove(tileId);

		public bool ProjectileIsBanned(short projId)
			=> _projbans.Contains(projId);

		public bool RemoveBannedProjectile(short projId)
			=> _projbans.Remove(projId);

		public bool ItemIsBanned(string itemName)
			=> _itembans.Contains(itemName);

		public bool RemoveBannedItem(string itemName)
			=> _itembans.Remove(itemName);
	}
}
