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

		private void OnPostInit(EventArgs args) {
			RtRegions.Reload();
		}

		private static void OnJoin(JoinEventArgs args) {
			TShock.Players[args.Who].SetData(Rtdataname, new RtPlayer());
		}

		private static void OnLeave(LeaveEventArgs args) {
			TShock.Players[args.Who].RemoveData(Rtdataname);
		}

		private DateTime _lastCheck = DateTime.UtcNow;

		private void OnUpdate(EventArgs args) {
			if((DateTime.UtcNow - _lastCheck).TotalSeconds >= 1) {
				OnSecondUpdate();
				_lastCheck = DateTime.UtcNow;
			}
		}

		private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args) {
			if(!RtRegions.DeleteRtRegion(args.Region.ID))
				TShock.Log.ConsoleError("Failed to remove region '{0}'!", args.Region.Name);
		}

		private void OnRegionLeft(RegionHooks.RegionLeftEventArgs args) {
			var rt = RtRegions.GetRtRegionByRegionId(args.Region.ID);
			if(rt == null)
				return;
			var dt = args.Player.GetData<RtPlayer>(Rtdataname);
			if(dt == null) {
#if DEBUG
				TShock.Log.ConsoleError("RTPDATA of Player {0} is null!", args.Player.Name);
#endif
				return;
			}

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

			if(rt.HasEvent(Events.Pvp) && !args.Player.HasPermission("regiontrigger.bypass.pvp")) {
				dt.Pvp = false;
				args.Player.SendInfoMessage("You can toggle your PVP status now.");
			}
		}

		private void OnRegionEntered(RegionHooks.RegionEnteredEventArgs args) {
			var rt = RtRegions.GetRtRegionByRegionId(args.Region.ID);
			if(rt == null)
				return;
			var dt = args.Player.GetData<RtPlayer>(Rtdataname);
			if(dt == null) {
#if DEBUG
				TShock.Log.ConsoleError("RTPDATA of Player {0} is null!", args.Player.Name);
#endif
				return;
			}

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
				if(rt.TempGroup == null) {
					TShock.Log.ConsoleError("TempGroup in region '{0}' is not valid", args.Region.Name);
					return;
				}
				args.Player.tempGroup = rt.TempGroup;
				args.Player.SendInfoMessage("Your group has been changed to {0} in this region.", rt.TempGroup.Name);
			}

			if(rt.HasEvent(Events.Kill) && !args.Player.HasPermission("regiontrigger.bypass.kill")) {
				args.Player.DamagePlayer(9999);
				args.Player.SendInfoMessage("You were killed by this region!");
			}

			if(rt.HasEvent(Events.Godmode)) {
				args.Player.GodMode = true;
				args.Player.SendInfoMessage("You are now in godmode!");
			}

			if(rt.HasEvent(Events.Pvp) && !args.Player.HasPermission("regiontrigger.bypass.pvp")) {
				dt.Pvp = true;
				if(!args.Player.TPlayer.hostile) {
					args.Player.TPlayer.hostile = true;
					NetMessage.SendData(30, -1, args.Player.Index, "", args.Player.Index); // todo: validate that
					args.Player.SendInfoMessage("Your PVP status is enabled in this region!");
				}
			}

			if(rt.HasEvent(Events.NoPvp) && !args.Player.HasPermission("regiontrigger.bypass.nopvp")) {
				dt.Pvp = false;
				dt.NoPvp = true;
				if(args.Player.TPlayer.hostile) {
					args.Player.TPlayer.hostile = false;
					args.Player.SendInfoMessage("You can't enable PVP in this region!");
				}
			}

			if(rt.HasEvent(Events.Private) && !args.Player.HasPermission("regiontrigger.bypass.private")) {
				if(rt.Group == null) {
					args.Player.Teleport(args.Player.LastNetPosition.X, args.Player.LastNetPosition.Y);
				} else if(args.Player.Group != rt.Group) {
					args.Player.Teleport(args.Player.LastNetPosition.X, args.Player.LastNetPosition.Y);
				} else {
					return;
				}
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

				if(dt.Pvp && !ply.TPlayer.hostile) {
					ply.TPlayer.hostile = true;
					ply.SendErrorMessage("Forced PVP mode is enabled in this region! You can't change your PVP status.");
					NetMessage.SendData(30, -1, ply.Index, "", ply.Index); // todo: validate that
				}

				if(dt.NoPvp && ply.TPlayer.hostile) {
					ply.TPlayer.hostile = false;
					ply.SendErrorMessage("You can't pvp in this region!");
				}
			}
		}

		private void DefineRtRegion(CommandArgs args) {
			if(args.Parameters.Count == 0 || args.Parameters.Count > 2) {
				args.Player.SendErrorMessage("Invaild syntax! Proper syntax: /defrt <Region name> [flags...]");
				args.Player.SendErrorMessage("Use {0} to get available events.", TShock.Utils.ColorTag("/setrt events", Color.Cyan));
				return;
			}

			string regionName = args.Parameters[0];
			string flags = args.Parameters.Count == 1 || string.IsNullOrWhiteSpace(args.Parameters[1])
				? Events.None
				: args.Parameters[1].ToLower();

			var region = TShock.Regions.GetRegionByName(regionName);
			if(region == null) {
				args.Player.SendErrorMessage("Invaild region name!");
				return;
			}
			if(RtRegions.GetRtRegionByRegionId(region.ID) != null) {
				args.Player.SendErrorMessage("RtRegion already exists!");
				args.Player.SendErrorMessage("Use {0} to set events for regions.", TShock.Utils.ColorTag("/setrt add/set <RtRegion name> <flags>", Color.Cyan));
				return;
			}

			var events = flags.Split(',');
			var valid = new List<string>();
			var invalid = new List<string>();

			events.ForEach(txt => {
				var e = txt.Trim();
				if(!Events.Contains(e))
					invalid.Add(e);
				else
					valid.Add(e);
			});

			string validEvents = string.Join(",", valid);
			// todo: check flags
			if(RtRegions.AddRtRegion(region.ID, validEvents)) {
				args.Player.SendSuccessMessage("RtRegion {0} was defined successfully!", TShock.Utils.ColorTag(region.Name, Color.Cyan));
				if(invalid.Count > 0)
					args.Player.SendErrorMessage("These invalid events wern't added to this region: {0}", string.Join(", ", invalid));
			}
		}

		private void SetRt(CommandArgs args) {
			string subCmd = args.Parameters.Count == 0 ? "help" : args.Parameters[0].ToLower();
			switch(subCmd) {
				case "event":
				case "events":
					#region Event
					string cmd = args.Parameters.Count == 1 ? "listevents" : args.Parameters[1].ToLower(); // add del <region name> listevents
					switch(cmd) {
						case "add":
							#region Add
							if(args.Parameters.Count != 4) {
								args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setrt add <region name> <event(s)>");
								return;
							}
							if(string.IsNullOrWhiteSpace(args.Parameters[2]) || string.IsNullOrWhiteSpace(args.Parameters[3])) {
								args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setrt add <region name> <event(s)>");
								return;
							}

							string rtname = args.Parameters[2].Trim();
							string[] estr = args.Parameters[3].ToLower().Trim().Split(',');

							var region = TShock.Regions.GetRegionByName(rtname);
							if(region == null) {
								args.Player.SendErrorMessage("Cannot find region named \"{0}\"!", rtname);
								return;
							}
							var rtregion = RtRegions.GetRtRegionByRegionId(region.ID);
							if(rtregion == null) {
								args.Player.SendErrorMessage("This region has not defined! Use {0} to define that.", TShock.Utils.ColorTag($"/defrt {region.Name}", Color.Cyan));
								return;
							}

							var valid = new List<string>();
							var invalid = new List<string>();

							estr.ForEach(str => {
								var s = str.Trim();
								if(Events.Contains(s))
									valid.Add(s);
								else
									invalid.Add(s);
							});

							if(valid.Count > 0) {
								if(!RtRegions.AddFlags(rtregion.Id, string.Join(",", valid))) {
									args.Player.SendErrorMessage("Add failed: database error");
									return;
								}
								args.Player.SendSuccessMessage("Events have been added to region successfully!");
							}
							if(invalid.Count > 0)
								args.Player.SendErrorMessage("These invalid events wern't added to this region: {0}", string.Join(", ", invalid));
							#endregion
							return;
						case "del":
							#region Delete

							#endregion
							return;
						case "listevents":
							#region ListEvents

							PaginationTools.SendPage(args.Player, 1,
								Events.EventsDescriptions.Select(kvp => $"{kvp.Key} -- {kvp.Value}").ToList(),
								new PaginationTools.Settings {
									HeaderFormat = "Available events of RegionTrigger ({0}/{1}):",
									FooterFormat = "Type {0}setrt event {{0}} for more events.".SFormat(Commands.Specifier)
								}
								);

							#endregion
							return;
						default:
							#region ListEvent [page]
							if(cmd.All(char.IsDigit)) {
								int num;
								if(!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out num))
									return;

								PaginationTools.SendPage(args.Player, num,
									Events.EventsDescriptions.Select(kvp => $"{kvp.Key} -- {kvp.Value}").ToList(),
									new PaginationTools.Settings {
										HeaderFormat = "Available events of RegionTrigger ({0}/{1}):",
										FooterFormat = "Type {0}setrt event {{0}} for more events.".SFormat(Commands.Specifier)
									}
									);
								return;
							}
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setrt event <add/del>/[page]");
							#endregion
							return;
					}
					#endregion
					return;
				case "projban":
					#region ProjectileBan
					if(args.Parameters.Count != 4) {
						args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setrt projban <add/del> <Region> <projectile ID>");
						return;
					}
					switch(args.Parameters[1].ToLower()) {
						case "add":

							break;
						case "del":

							break;
						default:
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setrt projban <add/del> <Region> <projectile ID>");
							return;
					}
					#endregion
					return;
				case "itemban":
					#region ItemBan
					if(args.Parameters.Count != 4) {
						args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setrt itemban <add/del> <Region> <Item ID/name>");
						return;
					}
					switch(args.Parameters[1].ToLower()) {
						case "add":

							break;
						case "del":

							break;
						default:
							args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /setrt itemban <add/del> <Region> <Item ID/name>");
							return;
					}
					#endregion
					return;
				case "reload":
					#region Reload
					RtRegions.Reload();
					#endregion
					return;
				case "help":
					#region Help
					{
						int pageNumber;
						if(!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
							return;

						var lines = new List<string>
						{
							"event [page] - Lists all available events.",
							"event add <Region> <Events> - Adds specific events to a RtRegion.",
							"event del <Region> <Events> - Deletes events of a RtRegion.",
							"event show <Region> - Lists all events of a RtRegion.",
							"projban add <Region> <proj ID> - Disallows players from using a projectile.",
							"projban del <Region> <proj ID> - Allows players to use a projectile.",
							"itemban add <Region> <item ID/name> - Disallows players from using a item.",
							"itemban del <Region> <item ID/name> - Allows players to use a item.",
							"help [page] - Lists all available helps of this command.",
							"list [page] - Lists all RtRegions.",
							"reload -- Reloads data in database.",
						};

						PaginationTools.SendPage(args.Player, pageNumber, lines,
							new PaginationTools.Settings {
								HeaderFormat = "RtRegion Setting Sub-Commands ({0}/{1}):",
								FooterFormat = "Type {0}setrt help {{0}} for more sub-commands.".SFormat(Commands.Specifier)
							}
						);
					}
					#endregion
					return;
				default:
					args.Player.SendErrorMessage("Invalid syntax! Type /setrt help to get a list of sub-commands.");
					return;
			}
		}
	}
}
