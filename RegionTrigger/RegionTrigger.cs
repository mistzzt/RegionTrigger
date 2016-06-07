using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using TShockAPI.Hooks;

namespace RegionTrigger {
	[ApiVersion(1, 23)]
	public class RegionTrigger:TerrariaPlugin {
		public const string Rtdataname = "rtply";
		public RtRegionManager RtRegions;

		public override string Name => "RegionTrigger";
		public override string Author => "MistZZT";
		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
		public override string Description => "Perform actions in regions where players are active.";
		public RegionTrigger(Main game) : base(game) { }

		public override void Initialize() {
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize, -10);
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -10);
			ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);

			GetDataHandlers.TogglePvp += OnTogglePvp;
			GetDataHandlers.TileEdit += OnTileEdit;
			GetDataHandlers.NewProjectile += OnNewProjectile;
			GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
			RegionHooks.RegionEntered += OnRegionEntered;
			RegionHooks.RegionLeft += OnRegionLeft;
			RegionHooks.RegionDeleted += OnRegionDeleted;
		}

		protected override void Dispose(bool disposing) {
			if(disposing) {
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
				ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);

				GetDataHandlers.TogglePvp -= OnTogglePvp;
				GetDataHandlers.TileEdit -= OnTileEdit;
				GetDataHandlers.NewProjectile -= OnNewProjectile;
				GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
				RegionHooks.RegionEntered -= OnRegionEntered;
				RegionHooks.RegionLeft -= OnRegionLeft;
				RegionHooks.RegionDeleted -= OnRegionDeleted;
			}
			base.Dispose(disposing);
		}

		private void OnInitialize(EventArgs args) {
			Commands.ChatCommands.Add(new Command("regiontrigger.manage.define", DefineRtRegion, "defrt"));
			Commands.ChatCommands.Add(new Command("regiontrigger.manage.set", SetRt, "setrt"));

			RtRegions = new RtRegionManager(TShock.DB);
		}

		private void OnPostInit(EventArgs args)
			=> RtRegions.Reload();

		private static void OnJoin(JoinEventArgs args)
			=> TShock.Players[args.Who].SetData(Rtdataname, new RtPlayer());

		private static void OnLeave(LeaveEventArgs args)
			=> TShock.Players[args.Who].RemoveData(Rtdataname);

		private DateTime _lastCheck = DateTime.UtcNow;

		private void OnUpdate(EventArgs args) {
			if((DateTime.UtcNow - _lastCheck).TotalSeconds >= 1) {
				OnSecondUpdate();
				_lastCheck = DateTime.UtcNow;
			}
		}

		private void OnTogglePvp(object sender, GetDataHandlers.TogglePvpEventArgs args) {
			var ply = TShock.Players[args.PlayerId];
			var dt = ply.GetData<RtPlayer>(Rtdataname);
			if(dt == null)
				return;

			if(dt.Pvp && !args.Pvp) {
				ply.SendErrorMessage("You can't change your PvP status in this region!");
				ply.SendData(PacketTypes.TogglePvp, "", args.PlayerId);
				args.Handled = true;
				return;
			}

			if(dt.NoPvp && args.Pvp) {
				ply.SendErrorMessage("You can't change your PvP status in this region!");
				ply.SendData(PacketTypes.TogglePvp, "", args.PlayerId);
				args.Handled = true;
				return;
			}
		}

		private void OnTileEdit(object sender, GetDataHandlers.TileEditEventArgs args) {
			if(args.Action != GetDataHandlers.EditAction.PlaceTile)
				return;
			var rt = RtRegions.GetTopRegion(RtRegions.Regions.Where(r => r.Region.InArea(args.X, args.Y)));
			if(rt == null || !rt.HasEvent(Events.Tileban))
				return;

			if(rt.TileIsBanned(args.EditData) && !args.Player.HasPermission("regiontrigger.bypass.tileban")) {
				args.Player.SendTileSquare(args.X, args.Y, 1);
				args.Player.SendErrorMessage("You do not have permission to place this tile.");
				args.Handled = true;
			}
		}

		private void OnNewProjectile(object sender, GetDataHandlers.NewProjectileEventArgs args) {
			var ply = TShock.Players[args.Index];
			if(ply.CurrentRegion == null)
				return;
			var rt = RtRegions.GetRtRegionByRegionId(ply.CurrentRegion.ID);
			if(rt == null || !rt.HasEvent(Events.Projban))
				return;

			if(rt.ProjectileIsBanned(args.Type) && !ply.HasPermission("regiontrigger.bypass.projban")) {
				ply.Disable($"Create banned projectile in region {rt.Region.Name}.", DisableFlags.WriteToLogAndConsole);
				ply.SendErrorMessage("This projectile is banned here.");
				ply.RemoveProjectile(args.Index, args.Owner);
			}
		}

		private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args) {
			var ply = TShock.Players[args.PlayerId];
			if(ply.CurrentRegion == null)
				return;
			var rt = RtRegions.GetRtRegionByRegionId(ply.CurrentRegion.ID);
			if(rt == null || !rt.HasEvent(Events.Itemban))
				return;

			BitsByte control = args.Control;
			if(control[5]) {
				var itemId = ply.TPlayer.inventory[args.Item].netID;
				var itemName = ply.TPlayer.inventory[args.Item].name;
				if(rt.ItemIsBanned(itemId) && !ply.HasPermission("regiontrigger.bypass.itemban")) {
					control[5] = false;
					args.Control = control;
					ply.Disable($"using a banned item ({itemName})", DisableFlags.WriteToLogAndConsole);
					ply.SendErrorMessage($"You can't use {itemName} here.");
				}
			}
		}

		private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args) {
			try {
				RtRegions.DeleteRtRegion(args.Region.Name);
			} catch(Exception ex) {
				TShock.Log.ConsoleError("[RegionTrigger] {0}", ex.Message);
			}
		}

		private void OnRegionLeft(RegionHooks.RegionLeftEventArgs args) {
			var rt = RtRegions.GetRtRegionByRegionId(args.Region.ID);
			if(rt == null)
				return;
			var dt = args.Player.GetData<RtPlayer>(Rtdataname);
			if(dt == null)
				return;

			if(rt.HasEvent(Events.LeaveMsg)) {
				if(string.IsNullOrWhiteSpace(rt.LeaveMsg))
					args.Player.SendInfoMessage("You have left region {0}", args.Region.Name);
				else
					args.Player.SendMessage(rt.LeaveMsg, Color.White);
			}

			if(rt.HasEvent(Events.TempGroup) && args.Player.tempGroup != null && args.Player.tempGroup == rt.TempGroup) {
				args.Player.tempGroup = null;
				args.Player.SendInfoMessage("You are no longer in group {0}.", rt.TempGroup.Name);
			}

			if(rt.HasEvent(Events.Godmode)) {
				args.Player.GodMode = false;
				args.Player.SendInfoMessage("You are no longer in godmode!");
			}

			if(rt.HasEvent(Events.Pvp) && dt.Pvp) {
				dt.Pvp = false;
				args.Player.SendInfoMessage("You can toggle your PvP status now.");
			}

			if(rt.HasEvent(Events.NoPvp) && dt.NoPvp) {
				dt.NoPvp = false;
				args.Player.SendInfoMessage("You can toggle your PvP status now.");
			}
		}

		private void OnRegionEntered(RegionHooks.RegionEnteredEventArgs args) {
			var rt = RtRegions.GetRtRegionByRegionId(args.Region.ID);
			if(rt == null)
				return;
			var dt = args.Player.GetData<RtPlayer>(Rtdataname);
			if(dt == null)
				return;

			if(rt.HasEvent(Events.EnterMsg)) {
				if(string.IsNullOrWhiteSpace(rt.EnterMsg))
					args.Player.SendInfoMessage("You have entered region {0}", args.Region.Name);
				else
					args.Player.SendMessage(rt.EnterMsg, Color.White);
			}

			if(rt.HasEvent(Events.Message) && !string.IsNullOrWhiteSpace(rt.Message)) {
				args.Player.SendInfoMessage(rt.Message, args.Region.Name);
			}

			if(rt.HasEvent(Events.TempGroup) && rt.TempGroup != null && !args.Player.HasPermission("regiontrigger.bypass.tempgroup")) {
				if(rt.TempGroup == null)
					TShock.Log.ConsoleError("TempGroup in region '{0}' is not valid!", args.Region.Name);
				else {
					args.Player.tempGroup = rt.TempGroup;
					args.Player.SendInfoMessage("Your group has been changed to {0} in this region.", rt.TempGroup.Name);
				}
			}

			if(rt.HasEvent(Events.Kill) && !args.Player.HasPermission("regiontrigger.bypass.kill")) {
				args.Player.DamagePlayer(9999);
				args.Player.SendInfoMessage("You were killed!");
			}

			if(rt.HasEvent(Events.Godmode)) {
				args.Player.GodMode = true;
				args.Player.SendInfoMessage("You are now in godmode!");
			}

			if(rt.HasEvent(Events.Pvp) && !args.Player.HasPermission("regiontrigger.bypass.pvp")) {
				dt.Pvp = true;
				if(!args.Player.TPlayer.hostile) {
					args.Player.TPlayer.hostile = true;
					args.Player.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					TSPlayer.All.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					args.Player.SendInfoMessage("Your PvP status is forced enabled in this region!");
				}
			}

			if(rt.HasEvent(Events.NoPvp) && !args.Player.HasPermission("regiontrigger.bypass.nopvp")) {
				dt.Pvp = false;
				dt.NoPvp = true;
				if(args.Player.TPlayer.hostile) {
					args.Player.TPlayer.hostile = false;
					args.Player.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					TSPlayer.All.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					args.Player.SendInfoMessage("You can't enable PvP in this region!");
				}
			}

			if(rt.HasEvent(Events.Private) && !args.Player.HasPermission("regiontrigger.bypass.private")) {
				args.Player.Teleport(args.Player.LastNetPosition.X, args.Player.LastNetPosition.Y); // todo: validate that
				args.Player.SendErrorMessage("You don't have permission to enter that region.");
			}
		}

		private void OnSecondUpdate() {
			foreach(var ply in TShock.Players.Where(p => p != null && p.Active)) {
				if(ply.CurrentRegion == null)
					return;

				var rt = RtRegions.GetRtRegionByRegionId(ply.CurrentRegion.ID);
				var dt = ply.GetData<RtPlayer>(Rtdataname);
				if(rt == null || dt == null)
					return;

				if(rt.HasEvent(Events.Message) && !string.IsNullOrWhiteSpace(rt.Message) && rt.MsgInterval != 0) {
					if(dt.MsgCd < rt.MsgInterval) {
						dt.MsgCd++;
					} else {
						ply.SendInfoMessage(rt.Message);
						dt.MsgCd = 0;
					}
				}
			}
		}

		private void DefineRtRegion(CommandArgs args) {
			if(args.Parameters.Count == 0 || args.Parameters.Count > 2) {
				args.Player.SendErrorMessage("Invaild syntax! Proper syntax: /defrt <Region name> [events...]");
				args.Player.SendErrorMessage("Use {0} to get available events.", TShock.Utils.ColorTag("/setrt events", Color.Cyan));
				return;
			}
			string regionName = args.Parameters[0];
			string events = args.Parameters.Count == 1 || string.IsNullOrWhiteSpace(args.Parameters[1])
				? Events.None
				: args.Parameters[1].ToLower();
			try {
				var validatedEvents = Events.ValidateEvents(events);
				RtRegions.AddRtRegion(regionName, validatedEvents.Item1);
				args.Player.SendSuccessMessage("Region {0} has been defined successfully!", TShock.Utils.ColorTag(regionName, Color.Cyan));
				if(validatedEvents.Item2 != null)
					args.Player.SendErrorMessage("Invalid events: {0}", validatedEvents.Item2);
			}
			catch(RtRegionManager.RegionDefinedException ex) {
				args.Player.SendErrorMessage(ex.Message);
				args.Player.SendErrorMessage("Use {0} to set events for regions.", TShock.Utils.ColorTag("/setrt", Color.Cyan));
			}
			catch(Exception ex) {
				args.Player.SendErrorMessage(ex.Message);
			}
		}
	}
}
