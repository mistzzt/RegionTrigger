using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using TShockAPI.Hooks;

namespace RegionTrigger
{
	[ApiVersion(2, 0)]
	[SuppressMessage("ReSharper", "InvertIf")]
	public sealed class RegionTrigger : TerrariaPlugin
	{
		public const string Rtdataname = "rtply";

		internal RtRegionManager RtRegions;

		public override string Name => "RegionTrigger";

		public override string Author => "MistZZT";

		public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

		public override string Description => "Perform actions in regions where players are active.";

		public RegionTrigger(Main game) : base(game) { }

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize, -10);
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -10);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);

			GetDataHandlers.TogglePvp += OnTogglePvp;
			GetDataHandlers.TileEdit += OnTileEdit;
			GetDataHandlers.NewProjectile += OnNewProjectile;
			GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
			RegionHooks.RegionEntered += OnRegionEntered;
			RegionHooks.RegionLeft += OnRegionLeft;
			RegionHooks.RegionDeleted += OnRegionDeleted;
			PlayerHooks.PlayerPermission += OnPlayerPermission;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInit);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);

				GetDataHandlers.TogglePvp -= OnTogglePvp;
				GetDataHandlers.TileEdit -= OnTileEdit;
				GetDataHandlers.NewProjectile -= OnNewProjectile;
				GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
				RegionHooks.RegionEntered -= OnRegionEntered;
				RegionHooks.RegionLeft -= OnRegionLeft;
				RegionHooks.RegionDeleted -= OnRegionDeleted;
				PlayerHooks.PlayerPermission -= OnPlayerPermission;
			}
			base.Dispose(disposing);
		}

		private void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command("regiontrigger.manage", RegionSetProperties, "rt"));

			RtRegions = new RtRegionManager(TShock.DB);
		}

		private void OnPostInit(EventArgs args)
		{
			RtRegions.Reload();
		}

		private static void OnGreetPlayer(GreetPlayerEventArgs args)
		{
			TShock.Players[args.Who]?.SetData(Rtdataname, new RtPlayer());
		}

		private static void OnLeave(LeaveEventArgs args)
		{
			TShock.Players[args.Who]?.RemoveData(Rtdataname);
		}

		private DateTime _lastCheck = DateTime.UtcNow;

		private void OnUpdate(EventArgs args)
		{
			if ((DateTime.UtcNow - _lastCheck).TotalSeconds >= 1)
			{
				OnSecondUpdate();
				_lastCheck = DateTime.UtcNow;
			}
		}

		private static void OnTogglePvp(object sender, GetDataHandlers.TogglePvpEventArgs args)
		{
			var ply = TShock.Players[args.PlayerId];
			var dt = ply.GetData<RtPlayer>(Rtdataname);
			if (dt == null)
				return;

			if (dt.ForcePvP == true && !args.Pvp)
			{
				ply.SendErrorMessage("You can't change your PvP status in this region!");
				ply.SendData(PacketTypes.TogglePvp, "", args.PlayerId);
				args.Handled = true;
				return;
			}

			if (dt.ForcePvP == false && args.Pvp)
			{
				ply.SendErrorMessage("You can't change your PvP status in this region!");
				ply.SendData(PacketTypes.TogglePvp, "", args.PlayerId);
				args.Handled = true;
				// ReSharper disable once RedundantJumpStatement
				return;
			}
		}

		private void OnTileEdit(object sender, GetDataHandlers.TileEditEventArgs args)
		{
			if (args.Action != GetDataHandlers.EditAction.PlaceTile)
				return;
			var rt = RtRegions.GetTopRegion(RtRegions.Regions.Where(r => r.Region.InArea(args.X, args.Y)));
			if (rt?.HasEvent(Events.Tileban) != true)
				return;

			if (rt.TileIsBanned(args.EditData) && !args.Player.HasPermission("regiontrigger.bypass.tileban"))
			{
				args.Player.SendTileSquare(args.X, args.Y, 1);
				args.Player.SendErrorMessage("You do not have permission to place this tile.");
				args.Handled = true;
			}
		}

		private void OnNewProjectile(object sender, GetDataHandlers.NewProjectileEventArgs args)
		{
			var ply = TShock.Players[args.Owner];
			if (ply.CurrentRegion == null)
				return;
			var rt = RtRegions.GetRtRegionByRegionId(ply.CurrentRegion.ID);
			if (rt == null || !rt.HasEvent(Events.Projban))
				return;

			if (rt.ProjectileIsBanned(args.Type) && !ply.HasPermission("regiontrigger.bypass.projban"))
			{
				ply.Disable($"Create banned projectile in region {rt.Region.Name}.", DisableFlags.WriteToLogAndConsole);
				ply.SendErrorMessage("This projectile is banned here.");
				ply.RemoveProjectile(args.Index, args.Owner);
			}
		}

		private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
		{
			var ply = TShock.Players[args.PlayerId];
			if (ply.CurrentRegion == null)
				return;
			var rt = RtRegions.GetRtRegionByRegionId(ply.CurrentRegion.ID);
			if (rt == null || !rt.HasEvent(Events.Itemban))
				return;

			BitsByte control = args.Control;
			if (control[5])
			{
				var itemName = ply.TPlayer.inventory[args.Item].name;
				if (rt.ItemIsBanned(itemName) && !ply.HasPermission("regiontrigger.bypass.itemban"))
				{
					control[5] = false;
					args.Control = control;
					ply.Disable($"using a banned item ({itemName})", DisableFlags.WriteToLogAndConsole);
					ply.SendErrorMessage($"You can't use {itemName} here.");
				}
			}
		}

		private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args)
		{
			try
			{
				RtRegions.DeleteRtRegion(args.Region.Name);
			}
			catch (Exception ex)
			{
				TShock.Log.Error("[RegionTrigger] {0}", ex.Message);
			}
		}

		private void OnPlayerPermission(PlayerPermissionEventArgs args)
		{
			if (args.Player.CurrentRegion == null)
				return;
			var rt = RtRegions.GetRtRegionByRegionId(args.Player.CurrentRegion.ID);
			if (rt?.HasEvent(Events.TempPermission) != true)
				return;

			if (rt.HasPermission(args.Permission) && !args.Player.HasPermission("regiontrigger.bypass.tempperm"))
				args.Handled = true;
		}

		private void OnRegionLeft(RegionHooks.RegionLeftEventArgs args)
		{
			var rt = RtRegions.GetRtRegionByRegionId(args.Region.ID);
			if (rt == null)
				return;
			var dt = args.Player.GetData<RtPlayer>(Rtdataname);
			if (dt == null)
				return;

			if (rt.HasEvent(Events.LeaveMsg))
			{
				if (string.IsNullOrWhiteSpace(rt.LeaveMsg))
					args.Player.SendInfoMessage("You have left region {0}", args.Region.Name);
				else
					args.Player.SendMessage(rt.LeaveMsg, Color.White);
			}

			if (rt.HasEvent(Events.TempGroup) && args.Player.tempGroup == rt.TempGroup)
			{
				args.Player.tempGroup = null;
				args.Player.SendInfoMessage("You are no longer in group {0}.", rt.TempGroup.Name);
			}

			if (rt.HasEvent(Events.Godmode))
			{
				args.Player.GodMode = false;
				args.Player.SendInfoMessage("You are no longer in godmode!");
			}

			if ((rt.HasEvent(Events.Pvp) && dt.ForcePvP == true) || (rt.HasEvent(Events.NoPvp) && dt.ForcePvP == false))
			{
				dt.ForcePvP = null;
				args.Player.SendInfoMessage("You can toggle your PvP status now.");
			}
		}

		private void OnRegionEntered(RegionHooks.RegionEnteredEventArgs args)
		{
			var rt = RtRegions.GetRtRegionByRegionId(args.Region.ID);
			if (rt == null)
				return;
			var dt = args.Player.GetData<RtPlayer>(Rtdataname);
			if (dt == null)
				return;

			if (rt.HasEvent(Events.EnterMsg))
			{
				if (string.IsNullOrWhiteSpace(rt.EnterMsg))
					args.Player.SendInfoMessage("You have entered region {0}", args.Region.Name);
				else
					args.Player.SendMessage(rt.EnterMsg, Color.White);
			}

			if (rt.HasEvent(Events.Message) && !string.IsNullOrWhiteSpace(rt.Message))
			{
				args.Player.SendInfoMessage(rt.Message, args.Region.Name);
			}

			if (rt.HasEvent(Events.TempGroup) && rt.TempGroup != null && !args.Player.HasPermission("regiontrigger.bypass.tempgroup"))
			{
				if (rt.TempGroup == null)
					TShock.Log.ConsoleError("TempGroup in region '{0}' is not valid!", args.Region.Name);
				else
				{
					args.Player.tempGroup = rt.TempGroup;
					args.Player.SendInfoMessage("Your group has been changed to {0} in this region.", rt.TempGroup.Name);
				}
			}

			if (rt.HasEvent(Events.Kill) && !args.Player.HasPermission("regiontrigger.bypass.kill"))
			{
				args.Player.DamagePlayer(9999);
				args.Player.SendInfoMessage("You were killed!");
			}

			if (rt.HasEvent(Events.Godmode))
			{
				args.Player.GodMode = true;
				args.Player.SendInfoMessage("You are now in godmode!");
			}

			if (rt.HasEvent(Events.Pvp) && !args.Player.HasPermission("regiontrigger.bypass.pvp"))
			{
				dt.ForcePvP = true;
				if (!args.Player.TPlayer.hostile)
				{
					args.Player.TPlayer.hostile = true;
					args.Player.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					TSPlayer.All.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					args.Player.SendInfoMessage("Your PvP status is forced enabled in this region!");
				}
			}

			if (rt.HasEvent(Events.NoPvp) && !args.Player.HasPermission("regiontrigger.bypass.nopvp"))
			{
				dt.ForcePvP = false;
				if (args.Player.TPlayer.hostile)
				{
					args.Player.TPlayer.hostile = false;
					args.Player.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					TSPlayer.All.SendData(PacketTypes.TogglePvp, "", args.Player.Index);
					args.Player.SendInfoMessage("You can't enable PvP in this region!");
				}
			}

			if (rt.HasEvent(Events.Private) && !args.Player.HasPermission("regiontrigger.bypass.private"))
			{
				args.Player.Spawn();
				args.Player.SendErrorMessage("You don't have permission to enter that region.");
			}
		}

		private void OnSecondUpdate()
		{
			foreach (var ply in TShock.Players.Where(p => p != null && p.Active))
			{
				if (ply.CurrentRegion == null)
					return;

				var rt = RtRegions.GetRtRegionByRegionId(ply.CurrentRegion.ID);
				var dt = ply.GetData<RtPlayer>(Rtdataname);
				if (rt == null || dt == null)
					return;

				if (rt.HasEvent(Events.Message) && !string.IsNullOrWhiteSpace(rt.Message) && rt.MsgInterval != 0)
				{
					if (dt.MsgCd < rt.MsgInterval)
					{
						dt.MsgCd++;
					}
					else
					{
						ply.SendInfoMessage(rt.Message);
						dt.MsgCd = 0;
					}
				}
			}
		}

		private static readonly string[] DoNotNeedDelValueProps = {
			"em",
			"lm",
			"mi",
			"tg",
			"msg"
		};

		private static readonly string[][] PropStrings = {
			new[] {"e", "event"},
			new[] {"pb", "proj", "projban"},
			new[] {"ib", "item", "itemban"},
			new[] {"tb", "tile", "tileban"},
			new[] {"em", "entermsg"},
			new[] {"lm", "leavemsg"},
			new[] {"msg", "message"},
			new[] {"mi", "msgitv", "msginterval", "messageinterval"},
			new[] {"tg", "tempgroup"},
			new[] {"tp", "perm", "tempperm", "temppermission"}
		};

		[SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
		private void RegionSetProperties(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid syntax! Type /rt --help to get instructions.");
				return;
			}

			var cmd = args.Parameters[0].Trim().ToLower();
			if (cmd.StartsWith("set-"))
			{
				#region set-prop
				if (args.Parameters.Count < 3)
				{
					args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /rt set-<prop> <region> [--del] <value>");
					return;
				}
				var propset = cmd.Substring(4);
				// check the property
				if (!PropStrings.Any(strarray => strarray.Contains(propset)))
				{
					args.Player.SendErrorMessage("Invalid property!");
					return;
				}
				// get the shortest representation of property.
				// e.g. event => e, projban => pb
				propset = PropStrings.Single(props => props.Contains(propset))[0];
				// check existance of region
				var region = TShock.Regions.GetRegionByName(args.Parameters[1]);
				if (region == null)
				{
					args.Player.SendErrorMessage("Invalid region!");
					return;
				}
				// if region hasn't been added into database
				var rt = RtRegions.GetRtRegionByRegionId(region.ID);
				if (rt == null)
				{
					try
					{
						RtRegions.AddRtRegion(region.Name, null);
						rt = RtRegions.GetRtRegionByRegionId(region.ID);
						if (rt == null)
							throw new Exception("Database error: cannot create new region!!");
					}
					catch (Exception ex)
					{
						args.Player.SendErrorMessage(ex.Message);
						return;
					}
				}
				// has parameter --del
				var isDel = args.Parameters[2].ToLower() == "--del";
				// sometimes commands with --del don't need <value> e.g. /rt set-tg <region> --del
				if (isDel && args.Parameters.Count == 3 && !DoNotNeedDelValueProps.Contains(propset))
				{
					args.Player.SendErrorMessage($"Invalid syntax! Proper syntax: /rt set-{propset} <region> [--del] <value>");
					return;
				}
				var propValue = isDel && args.Parameters.Count == 3 ? null : isDel
					? string.Join(" ", args.Parameters.GetRange(3, args.Parameters.Count - 3))
					: string.Join(" ", args.Parameters.GetRange(2, args.Parameters.Count - 2));

				try
				{
					switch (propset)
					{
						case "e":
							var validatedEvents = Events.ValidateEvents(propValue);
							if (validatedEvents.Item1 != null)
							{
								if (!isDel)
									RtRegions.AddEvents(region.Name, validatedEvents.Item1);
								else
									RtRegions.RemoveEvents(region.Name, validatedEvents.Item1);
								args.Player.SendSuccessMessage("Region {0} has been modified successfully!", region.Name);
							}
							if (validatedEvents.Item2 != null)
								args.Player.SendErrorMessage("Invalid events: {0}", validatedEvents.Item2);
							break;
						case "pb":
							short id;
							if (short.TryParse(propValue, out id) && id > 0 && id < Main.maxProjectileTypes)
							{
								if (!isDel)
								{
									RtRegions.AddProjban(region.Name, id);
									args.Player.SendSuccessMessage("Banned projectile {0} in region {1}.", id, region.Name);
								}
								else
								{
									RtRegions.RemoveProjban(region.Name, id);
									args.Player.SendSuccessMessage("Unbanned projectile {0} in region {1}.", id, region.Name);
								}
							}
							else
								args.Player.SendErrorMessage("Invalid projectile ID!");
							break;
						case "ib":
							var items = TShock.Utils.GetItemByIdOrName(propValue);
							if (items.Count == 0)
							{
								args.Player.SendErrorMessage("Invalid item.");
							}
							else if (items.Count > 1)
							{
								TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => i.name));
							}
							else
							{
								if (!isDel)
								{
									RtRegions.AddItemban(region.Name, items[0].name);
									args.Player.SendSuccessMessage("Banned {0} in region {1}.", items[0].name, region.Name);
								}
								else
								{
									RtRegions.RemoveItemban(region.Name, items[0].name);
									args.Player.SendSuccessMessage("Unbanned {0} in region {1}.", items[0].name, region.Name);
								}
							}
							break;
						case "tb":
							short tileid;
							if (short.TryParse(propValue, out tileid) && tileid >= 0 && tileid < Main.maxTileSets)
							{
								if (!isDel)
								{
									RtRegions.AddTileban(region.Name, tileid);
									args.Player.SendSuccessMessage("Banned tile {0} in region {1}.", tileid, region.Name);
								}
								else
								{
									RtRegions.RemoveTileban(region.Name, tileid);
									args.Player.SendSuccessMessage("Unbanned tile {0} in region {1}.", tileid, region.Name);
								}
							}
							else
								args.Player.SendErrorMessage("Invalid tile ID!");
							break;
						case "em":
							RtRegions.SetEnterMessage(region.Name, !isDel ? propValue : null);
							if (!isDel)
							{
								args.Player.SendSuccessMessage("Set enter message of region {0} to '{1}'", region.Name, propValue);
								if (!rt.HasEvent(Events.EnterMsg))
									args.Player.SendWarningMessage("Add event ENTERMESSAGE if you want to make it work.");
							}
							else
								args.Player.SendSuccessMessage("Removed enter message of region {0}.", region.Name);
							break;
						case "lm":
							RtRegions.SetLeaveMessage(region.Name, !isDel ? propValue : null);
							if (!isDel)
							{
								args.Player.SendSuccessMessage("Set leave message of region {0} to '{1}'", region.Name, propValue);
								if (!rt.HasEvent(Events.LeaveMsg))
									args.Player.SendWarningMessage("Add event LEAVEMESSAGE if you want to make it work.");
							}
							else
								args.Player.SendSuccessMessage("Removed leave message of region {0}.", region.Name);
							break;
						case "msg":
							RtRegions.SetMessage(region.Name, !isDel ? propValue : null);
							if (!isDel)
							{
								args.Player.SendSuccessMessage("Set message of region {0} to '{1}'", region.Name, propValue);
								if (!rt.HasEvent(Events.Message))
									args.Player.SendWarningMessage("Add event MESSAGE if you want to make it work.");
							}
							else
								args.Player.SendSuccessMessage("Removed message of region {0}.", region.Name);
							break;
						case "mi":
							if (isDel)
								throw new Exception("Invalid usage! Proper usage: /rt set-mi <region> <interval>");
							int itv;
							if (!int.TryParse(propValue, out itv) || itv < 0)
								throw new Exception("Invalid interval. (Interval must be integer >= 0)");
							RtRegions.SetMsgInterval(region.Name, itv);
							args.Player.SendSuccessMessage("Set message interval of region {0} to {1}.", region.Name, itv);
							if (!rt.HasEvent(Events.Message))
								args.Player.SendWarningMessage("Add event MESSAGE if you want to make it work.");
							break;
						case "tg":
							if (!isDel && propValue != "null")
							{
								RtRegions.SetTempGroup(region.Name, propValue);
								args.Player.SendSuccessMessage("Set tempgroup of region {0} to {1}.", region.Name, propValue);
								if (!rt.HasEvent(Events.TempGroup))
									args.Player.SendWarningMessage("Add event TEMPGROUP if you want to make it work.");
							}
							else
							{
								RtRegions.SetTempGroup(region.Name, null);
								args.Player.SendSuccessMessage("Removed tempgroup of region {0}.", region.Name);
							}
							break;
						case "tp":
							var permissions = propValue.ToLower().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
							if (!isDel)
							{
								RtRegions.AddPermissions(region.Name, permissions);
								args.Player.SendSuccessMessage("Region {0} has been modified successfully.", region.Name);
							}
							else
							{
								RtRegions.DeletePermissions(region.Name, permissions);
								args.Player.SendSuccessMessage("Region {0} has been modified successfully.", region.Name);
							}
							break;
					}
				}
				catch (Exception ex)
				{
					args.Player.SendErrorMessage(ex.Message);
				}
				#endregion
			}
			else
				switch (cmd)
				{
					case "show":
						#region show
						{
							if (args.Parameters.Count != 2)
							{
								args.Player.SendErrorMessage("Invalid syntax! Usage: /rt show <region>");
								return;
							}

							var region = TShock.Regions.GetRegionByName(args.Parameters[1]);
							if (region == null)
							{
								args.Player.SendErrorMessage("Invalid region!");
								return;
							}
							var rt = RtRegions.GetRtRegionByRegionId(region.ID);
							if (rt == null)
							{
								args.Player.SendInfoMessage("{0} has not been set up yet. Use: /rt set-<prop> <name> <value>", region.Name);
								return;
							}

							var infos = new List<string> {
								$"*** Information of region {rt.Region.Name} ***",
								$" * Events: {rt.Events}",
								$" * TempGroup: {rt.TempGroup?.Name ?? "None"}",
								$" * Message & Interval: {rt.Message ?? "None"}({rt.MsgInterval}s)",
								$" * EnterMessage: {rt.EnterMsg ?? "None"}",
								$" * LeaveMessage: {rt.LeaveMsg ?? "None"}",
								$" * Itembans: {(string.IsNullOrWhiteSpace(rt.Itembans) ? "None" : rt.Itembans)}",
								$" * Projbans: {(string.IsNullOrWhiteSpace(rt.Projbans) ? "None" : rt.Projbans)}",
								$" * Tilebans: {(string.IsNullOrWhiteSpace(rt.Tilebans) ? "None" : rt.Tilebans)}"
							};
							infos.ForEach(args.Player.SendInfoMessage);
						}
						#endregion
						break;
					case "reload":
						RtRegions.Reload();
						args.Player.SendSuccessMessage("Reloaded regions from database successfully.");
						break;
					case "--help":
						#region Help
						int pageNumber;
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
							return;

						var lines = new List<string>
						{
							"*** Usage: /rt set-<prop> <region> [--del] <value>",
							"           /rt show <region>",
							"           /rt reload",
							"           /rt --help [page]",
							"*** Avaliable properties:"
						};
						lines.AddRange(PaginationTools.BuildLinesFromTerms(PropStrings, array =>
						{
							var strarray = (string[])array;
							return $"{strarray[0]}({string.Join("/", strarray.Skip(1))})";
						}, ",", 75).Select(s => s.Insert(0, "   * ")));
						lines.Add("*** Available events:");
						lines.AddRange(Events.EventsDescriptions.Select(pair => $"   * {pair.Key} - {pair.Value}"));

						PaginationTools.SendPage(args.Player, pageNumber, lines,
							new PaginationTools.Settings
							{
								HeaderFormat = "RegionTrigger Sub-Commands Instructions ({0}/{1}):",
								FooterFormat = "Type {0}rt --help {{0}} for more instructions.".SFormat(Commands.Specifier)
							}
						);
						#endregion
						break;
					default:
						args.Player.SendErrorMessage("Invalid syntax! Type /rt --help for instructions.");
						return;
				}
		}
	}
}
