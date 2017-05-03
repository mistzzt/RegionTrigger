using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Streams;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using TShockAPI.Hooks;

namespace RegionTrigger
{
	[ApiVersion(2, 1)]
	public sealed class RegionTrigger : TerrariaPlugin
	{
		internal RtRegionManager RtRegions;

		public override string Name => "RegionTrigger";

		public override string Author => "MistZZT";

		public override Version Version => GetType().Assembly.GetName().Version;

		public override string Description => "区域内执行特定事件";

		public RegionTrigger(Main game) : base(game) { }

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize, -10);
			ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInit, -10);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
			ServerApi.Hooks.NetGetData.Register(this, OnGetData, 2000);

			GetDataHandlers.TogglePvp += OnTogglePvp;
			GetDataHandlers.TileEdit += OnTileEdit;
			GetDataHandlers.NewProjectile += OnNewProjectile;
			GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
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
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);

				GetDataHandlers.TogglePvp -= OnTogglePvp;
				GetDataHandlers.TileEdit -= OnTileEdit;
				GetDataHandlers.NewProjectile -= OnNewProjectile;
				GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
				RegionHooks.RegionDeleted -= OnRegionDeleted;
				PlayerHooks.PlayerPermission -= OnPlayerPermission;
			}
			base.Dispose(disposing);
		}

		private void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command("regiontrigger.manage", RegionSetProperties, "rt", "区域事件"));

			RtRegions = new RtRegionManager(TShock.DB);
		}

		private void OnPostInit(EventArgs args)
		{
			RtRegions.Reload();
		}

		private static void OnGreetPlayer(GreetPlayerEventArgs args)
		{
			RtPlayer.GetPlayerInfo(TShock.Players[args.Who]);
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
			var dt = RtPlayer.GetPlayerInfo(ply);

			if (dt.ForcePvP == true && !args.Pvp ||
				dt.ForcePvP == false && args.Pvp ||
				!dt.CanTogglePvP)
			{
				ply.SendErrorMessage("你在此区域内无法改变PvP状态!");
				ply.SendData(PacketTypes.TogglePvp, "", args.PlayerId);
				args.Handled = true;
			}
		}

		private void OnTileEdit(object sender, GetDataHandlers.TileEditEventArgs args)
		{
			if (args.Action != GetDataHandlers.EditAction.PlaceTile)
				return;

			var rt = RtRegions.GetTopRegion(RtRegions.Regions.Where(r => r.Region.InArea(args.X, args.Y)));

			if (rt?.HasEvent(Event.Tileban) != true)
				return;

			if (rt.TileIsBanned(args.EditData) && !args.Player.HasPermission("regiontrigger.bypass.tileban"))
			{
				args.Player.SendTileSquare(args.X, args.Y, 1);
				args.Player.SendErrorMessage("你在此区域内无法放置该物块!");
				args.Handled = true;
			}
		}

		private void OnGetData(GetDataEventArgs args)
		{
			if (args.Handled)
				return;

			if (args.MsgID != PacketTypes.ItemDrop && args.MsgID != PacketTypes.UpdateItemDrop)
				return;

			using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1))
			{
				var id = data.ReadInt16();
				var pos = new Vector2(data.ReadSingle(), data.ReadSingle());

				if (id < 400)
					return;

				var rt = RtRegions.GetTopRegion(RtRegions.Regions.Where(r => r.Region.InArea((int)(pos.X / 16), (int)(pos.Y / 16))));

				if (rt?.HasEvent(Event.NoItem) != true)
					return;

				var player = TShock.Players[args.Msg.whoAmI];
				if (player == null || player.HasPermission("regiontrigger.bypass.itemdrop"))
					return;

				player.SendErrorMessage("你在此区域内无法丢弃东西!");
				player.Disable("非法丢弃东西");
				args.Handled = true;
			}
		}

		private static void OnNewProjectile(object sender, GetDataHandlers.NewProjectileEventArgs args)
		{
			var ply = TShock.Players[args.Owner];
			var rt = RtPlayer.GetPlayerInfo(ply).CurrentRegion;

			if (rt?.HasEvent(Event.Projban) != true)
				return;

			if (rt.ProjectileIsBanned(args.Type) && !ply.HasPermission("regiontrigger.bypass.projban"))
			{
				ply.Disable("非法用抛射体", DisableFlags.WriteToLogAndConsole);
				ply.SendErrorMessage("你在此区域内无法发射该抛射体!");
				ply.RemoveProjectile(args.Index, args.Owner);
			}
		}

		private static void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
		{
			var ply = TShock.Players[args.PlayerId];
			var rt = RtPlayer.GetPlayerInfo(ply).CurrentRegion;

			if (rt?.HasEvent(Event.Itemban) != true)
				return;

			BitsByte control = args.Control;
			if (control[5])
			{
				var itemName = ply.TPlayer.inventory[args.Item].Name;
				if (rt.ItemIsBanned(itemName) && !ply.HasPermission("regiontrigger.bypass.itemban"))
				{
					control[5] = false;
					args.Control = control;
					ply.Disable($"使用封禁物品 ({itemName})", DisableFlags.WriteToLogAndConsole);
					ply.SendErrorMessage($"你不能在这里用{itemName} 。");
				}
			}
		}

		private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args)
		{
			RtRegions.DeleteRtRegion(args.Region.ID);
		}

		private static void OnPlayerPermission(PlayerPermissionEventArgs args)
		{
			var rt = RtPlayer.GetPlayerInfo(args.Player).CurrentRegion;

			if (rt?.HasEvent(Event.TempPermission) != true)
				return;

			if (rt.HasPermission(args.Permission) && !args.Player.HasPermission("regiontrigger.bypass.tempperm"))
				args.Handled = true;
		}

		private static void OnRegionLeft(TSPlayer player, RtRegion region, RtPlayer data)
		{
			if (region.HasEvent(Event.LeaveMsg))
			{
				if (string.IsNullOrWhiteSpace(region.LeaveMsg))
					player.SendInfoMessage("你离开了区域{0}。", region.Region.Name);
				else
					player.SendMessage(region.LeaveMsg, Color.White);
			}

			if (region.HasEvent(Event.TempGroup) && player.tempGroup == region.TempGroup)
			{
				player.tempGroup = null;
				player.SendInfoMessage("区域内临时组{0}已经失效。", region.TempGroup.Name);
			}

			if (region.HasEvent(Event.Godmode))
			{
				player.GodMode = false;
				player.SendInfoMessage("区域内的无敌模式失效。");
			}

			if (region.HasEvent(Event.Pvp) || region.HasEvent(Event.NoPvp) || region.HasEvent(Event.InvariantPvp))
			{
				data.ForcePvP = null;
				data.CanTogglePvP = true;
				player.SendInfoMessage("现在你可以切换PvP模式了。");
			}
		}

		private static void OnRegionEntered(TSPlayer player, RtPlayer data)
		{
			var rt = data.CurrentRegion;

			if (rt.HasEvent(Event.EnterMsg))
			{
				if (string.IsNullOrWhiteSpace(rt.EnterMsg))
					player.SendInfoMessage("你进入了区域{0}。", rt.Region.Name);
				else
					player.SendMessage(rt.EnterMsg, Color.White);
			}

			if (rt.HasEvent(Event.Message) && !string.IsNullOrWhiteSpace(rt.Message))
			{
				player.SendInfoMessage(rt.Message);
			}

			if (rt.HasEvent(Event.TempGroup) && rt.TempGroup != null && !player.HasPermission("regiontrigger.bypass.tempgroup"))
			{
				if (rt.TempGroup == null)
					TShock.Log.ConsoleError("区域{0}的临时组无效！", rt.Region.Name);
				else
				{
					player.tempGroup = rt.TempGroup;
					player.SendInfoMessage("区域内用户组已切换为{0}。", rt.TempGroup.Name);
				}
			}

			if (rt.HasEvent(Event.Kill) && !player.HasPermission("regiontrigger.bypass.kill"))
			{
				player.KillPlayer();
				player.SendInfoMessage("你踏入了不该踏入之地…");
			}

			if (rt.HasEvent(Event.Godmode))
			{
				player.GodMode = true;
				player.SendInfoMessage("你在区域内会受到服务器的庇护。");
			}

			if (rt.HasEvent(Event.Pvp) && !player.HasPermission("regiontrigger.bypass.pvp"))
			{
				data.ForcePvP = true;
				if (!player.TPlayer.hostile)
				{
					player.TPlayer.hostile = true;
					player.SendData(PacketTypes.TogglePvp, "", player.Index);
					TSPlayer.All.SendData(PacketTypes.TogglePvp, "", player.Index);
					player.SendInfoMessage("区域内强制开启PvP模式。");
				}
			}

			if (rt.HasEvent(Event.NoPvp) && !player.HasPermission("regiontrigger.bypass.nopvp"))
			{
				data.ForcePvP = false;
				if (player.TPlayer.hostile)
				{
					player.TPlayer.hostile = false;
					player.SendData(PacketTypes.TogglePvp, "", player.Index);
					TSPlayer.All.SendData(PacketTypes.TogglePvp, "", player.Index);
					player.SendInfoMessage("区域内禁止PvP模式。");
				}
			}

			if (rt.HasEvent(Event.InvariantPvp) && !player.HasPermission("regiontrigger.bypass.inpvp"))
			{
				data.CanTogglePvP = false;
			}

			if (rt.HasEvent(Event.Private) && !player.HasPermission("regiontrigger.bypass.private"))
			{
				player.Spawn();
				player.SendErrorMessage("你踏入了不该踏入之地。");
			}
		}

		private void OnSecondUpdate()
		{
			foreach (var player in TShock.Players.Where(p => p?.Active == true))
			{
				var dt = RtPlayer.GetPlayerInfo(player);
				var oldRegion = dt.CurrentRegion;
				dt.CurrentRegion = RtRegions.GetCurrentRegion(player);

				if (dt.CurrentRegion != oldRegion)
				{
					if (oldRegion != null)
					{
						OnRegionLeft(player, oldRegion, dt);
					}

					if (dt.CurrentRegion != null)
					{
						OnRegionEntered(player, dt);
					}
				}

				if (dt.CurrentRegion == null)
					continue;

				if (dt.CurrentRegion.HasEvent(Event.Message) && !string.IsNullOrWhiteSpace(dt.CurrentRegion.Message) && dt.CurrentRegion.MsgInterval != 0)
				{
					if (dt.MsgCd < dt.CurrentRegion.MsgInterval)
					{
						dt.MsgCd++;
					}
					else
					{
						player.SendInfoMessage(dt.CurrentRegion.Message);
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
			new[] {"e", "event", "事件"},
			new[] {"pb", "proj", "projban", "禁proj"},
			new[] {"ib", "item", "itemban", "禁物品"},
			new[] {"tb", "tile", "tileban", "禁物块"},
			new[] {"em", "entermsg", "进入消息"},
			new[] {"lm", "leavemsg", "离去消息"},
			new[] {"msg", "message", "消息"},
			new[] {"mi", "msgitv", "msginterval", "messageinterval", "消息间隔"},
			new[] {"tg", "tempgroup", "组"},
			new[] {"tp", "perm", "tempperm", "temppermission", "权限"}
		};

		[SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
		private void RegionSetProperties(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage("语法无效！键入 /rt --help 获取使用说明。");
				return;
			}

			var cmd = args.Parameters[0].Trim().ToLowerInvariant();
			if (cmd.StartsWith("set-"))
			{
				#region set-prop
				if (args.Parameters.Count < 3)
				{
					args.Player.SendErrorMessage("语法无效！正确语法： /rt set-<属性> <区域名> [--del] <值>");
					return;
				}
				var propset = cmd.Substring(4);
				// check the property
				if (!PropStrings.Any(strarray => strarray.Contains(propset)))
				{
					args.Player.SendErrorMessage("设置属性无效！");
					return;
				}
				// get the shortest representation of property.
				// e.g. event => e, projban => pb
				propset = PropStrings.Single(props => props.Contains(propset))[0];
				// check existance of region
				var region = TShock.Regions.GetRegionByName(args.Parameters[1]);
				if (region == null)
				{
					args.Player.SendErrorMessage("区域名无效！");
					return;
				}
				// if region hasn't been added into database
				var rt = RtRegions.GetRtRegionByRegionId(region.ID);
				if (rt == null)
				{
					RtRegions.AddRtRegion(region.ID);
					rt = RtRegions.GetRtRegionByRegionId(region.ID);
				}
				// has parameter --del
				var isDel = string.Equals(args.Parameters[2], "--del", StringComparison.OrdinalIgnoreCase);
				// sometimes commands with --del don't need <value> e.g. /rt set-tg <region> --del
				if (isDel && args.Parameters.Count == 3 && !DoNotNeedDelValueProps.Contains(propset))
				{
					args.Player.SendErrorMessage($"语法无效！正确语法： /rt set-{propset} <区域名> [--del] <值>");
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
							var validatedEvents = Events.ValidateEventWhenAdd(propValue, out var invalids);
							if (!isDel)
								RtRegions.AddEvents(rt, validatedEvents);
							else
								RtRegions.RemoveEvents(rt, validatedEvents);
							args.Player.SendSuccessMessage("区域{0}的事件设定完毕！", region.Name);
							if (!string.IsNullOrWhiteSpace(invalids))
								args.Player.SendErrorMessage("无效事件名：{0}", invalids);
							break;
						case "pb":
							if (short.TryParse(propValue, out var id) && id > 0 && id < Main.maxProjectileTypes)
							{
								if (!isDel)
								{
									RtRegions.AddProjban(rt, id);
									args.Player.SendSuccessMessage("Banned projectile {0} in region {1}.", id, region.Name);
								}
								else
								{
									RtRegions.RemoveProjban(rt, id);
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
								TShock.Utils.SendMultipleMatchError(args.Player, items.Select(i => i.Name));
							}
							else
							{
								if (!isDel)
								{
									RtRegions.AddItemban(rt, items[0].Name);
									args.Player.SendSuccessMessage("Banned {0} in region {1}.", items[0].Name, region.Name);
								}
								else
								{
									RtRegions.RemoveItemban(rt, items[0].Name);
									args.Player.SendSuccessMessage("Unbanned {0} in region {1}.", items[0].Name, region.Name);
								}
							}
							break;
						case "tb":
							if (short.TryParse(propValue, out var tileid) && tileid >= 0 && tileid < Main.maxTileSets)
							{
								if (!isDel)
								{
									RtRegions.AddTileban(rt, tileid);
									args.Player.SendSuccessMessage("Banned tile {0} in region {1}.", tileid, region.Name);
								}
								else
								{
									RtRegions.RemoveTileban(rt, tileid);
									args.Player.SendSuccessMessage("Unbanned tile {0} in region {1}.", tileid, region.Name);
								}
							}
							else
								args.Player.SendErrorMessage("Invalid tile ID!");
							break;
						case "em":
							RtRegions.SetEnterMessage(rt, !isDel ? propValue : null);
							if (!isDel)
							{
								args.Player.SendSuccessMessage("Set enter message of region {0} to '{1}'", region.Name, propValue);
								if (!rt.HasEvent(Event.EnterMsg))
									args.Player.SendWarningMessage("Add event ENTERMESSAGE if you want to make it work.");
							}
							else
								args.Player.SendSuccessMessage("Removed enter message of region {0}.", region.Name);
							break;
						case "lm":
							RtRegions.SetLeaveMessage(rt, !isDel ? propValue : null);
							if (!isDel)
							{
								args.Player.SendSuccessMessage("Set leave message of region {0} to '{1}'", region.Name, propValue);
								if (!rt.HasEvent(Event.LeaveMsg))
									args.Player.SendWarningMessage("Add event LEAVEMESSAGE if you want to make it work.");
							}
							else
								args.Player.SendSuccessMessage("Removed leave message of region {0}.", region.Name);
							break;
						case "msg":
							RtRegions.SetMessage(rt, !isDel ? propValue : null);
							if (!isDel)
							{
								args.Player.SendSuccessMessage("Set message of region {0} to '{1}'", region.Name, propValue);
								if (!rt.HasEvent(Event.Message))
									args.Player.SendWarningMessage("Add event MESSAGE if you want to make it work.");
							}
							else
								args.Player.SendSuccessMessage("Removed message of region {0}.", region.Name);
							break;
						case "mi":
							if (isDel)
								throw new Exception("Invalid usage! Proper usage: /rt set-mi <region> <interval>");
							if (!int.TryParse(propValue, out var itv) || itv < 0)
								throw new Exception("Invalid interval. (Interval must be integer >= 0)");
							RtRegions.SetMsgInterval(rt, itv);
							args.Player.SendSuccessMessage("Set message interval of region {0} to {1}.", region.Name, itv);
							if (!rt.HasEvent(Event.Message))
								args.Player.SendWarningMessage("Add event MESSAGE if you want to make it work.");
							break;
						case "tg":
							if (!isDel && propValue != "null")
							{
								RtRegions.SetTempGroup(rt, propValue);
								args.Player.SendSuccessMessage("Set tempgroup of region {0} to {1}.", region.Name, propValue);
								if (!rt.HasEvent(Event.TempGroup))
									args.Player.SendWarningMessage("Add event TEMPGROUP if you want to make it work.");
							}
							else
							{
								RtRegions.SetTempGroup(rt, null);
								args.Player.SendSuccessMessage("Removed tempgroup of region {0}.", region.Name);
							}
							break;
						case "tp":
							// ReSharper disable once PossibleNullReferenceException
							var permissions = propValue.ToLower().Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
							if (!isDel)
							{
								RtRegions.AddPermissions(rt, permissions);
								args.Player.SendSuccessMessage("Region {0} has been modified successfully.", region.Name);
							}
							else
							{
								RtRegions.DeletePermissions(rt, permissions);
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
								args.Player.SendErrorMessage("语法无效！正确语法： /rt show <区域名>");
								return;
							}

							var region = TShock.Regions.GetRegionByName(args.Parameters[1]);
							if (region == null)
							{
								args.Player.SendErrorMessage("区域名无效！");
								return;
							}
							var rt = RtRegions.GetRtRegionByRegionId(region.ID);
							if (rt == null)
							{
								args.Player.SendInfoMessage("区域{0}还未被设置事件。使用 /rt set-<属性> <区域名> <值> 来设置区域后再查看。", region.Name);
								return;
							}

							var infos = new List<string> {
								$"*** 区域{rt.Region.Name}事件状态 ***",
								$" *     事件: {rt.CnEvents}",
								$" *   临时组: {rt.TempGroup?.Name ?? "无"}",
								$" * 消息发送: {rt.Message ?? "无"} ({rt.MsgInterval}秒)",
								$" * 进入消息: {rt.EnterMsg ?? "无"}",
								$" * 离去消息: {rt.LeaveMsg ?? "无"}",
								$" * 物品封禁: {(string.IsNullOrWhiteSpace(rt.Itembans) ? "无" : rt.Itembans)}",
								$" * 禁抛射体: {(string.IsNullOrWhiteSpace(rt.Projbans) ? "无" : rt.Projbans)}",
								$" * 物块封禁: {(string.IsNullOrWhiteSpace(rt.Tilebans) ? "无" : rt.Tilebans)}"
							};
							infos.ForEach(args.Player.SendInfoMessage);
						}
						#endregion
						break;
					case "reload":
						RtRegions.Reload();
						args.Player.SendSuccessMessage("重新加载了事件区域数据。");
						break;
					case "--help":
						#region Help
						if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out var pageNumber))
							return;

						var lines = new List<string>
						{
							"*** 用法： /rt set-<属性> <区域名> [--del] <值>",
							"           /rt show <区域名>",
							"           /rt reload",
							"           /rt --help [页码]",
							"*** 可以设置的属性:"
						};
						lines.AddRange(PaginationTools.BuildLinesFromTerms(PropStrings, array =>
						{
							var strarray = (string[])array;
							return $"{strarray[0]}({string.Join("/", strarray.Skip(1))})";
						}, ",", 75).Select(s => s.Insert(0, "   * ")));
						lines.Add("*** 可用事件:");
						lines.AddRange(Events.EventsDescriptions.Select(pair => $"   * {Events.InternalToCnName(pair.Key)} - {pair.Value}"));

						PaginationTools.SendPage(args.Player, pageNumber, lines,
							new PaginationTools.Settings
							{
								HeaderFormat = "区域事件帮助 ({0}/{1}):",
								FooterFormat = "键入 {0}rt --help {{0}} 以获取更多帮助。".SFormat(Commands.Specifier)
							}
						);
						#endregion
						break;
					default:
						args.Player.SendErrorMessage("语法无效！键入 /rt --help 获取使用说明。");
						return;
				}
		}
	}
}
