using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
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
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
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
            // commands
            Commands.ChatCommands.Add(new Command("regiontrigger.manage.define", DefineRtRegion, "defrt"));
            Commands.ChatCommands.Add(new Command("regiontrigger.manage.set", SetRt, "setrt"));
            // database
            RtRegions = new RtRegionManager(TShock.DB);
        }

        private static void OnJoin(JoinEventArgs args) {
            TShock.Players[args.Who].SetData(Rtdataname, new RtPlayer());
        }

        private static void OnLeave(LeaveEventArgs args) {
            TShock.Players[args.Who].RemoveData(Rtdataname);
        }

        /// <summary>LastCheck - Used to keep track of the last check for basically all time based checks.</summary>
		private DateTime _lastCheck = DateTime.UtcNow;

        private void OnUpdate(EventArgs args) {
            //call these every second, not every update
            if((DateTime.UtcNow - _lastCheck).TotalSeconds >= 1) {
                OnSecondUpdate();
                _lastCheck = DateTime.UtcNow;
            }
        }

        private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args) {

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

            if(rt.HasEvent(Events.EnterMsg)) {
                if(string.IsNullOrWhiteSpace(rt.EnterMsg))
                    args.Player.SendInfoMessage("You have left region {0}", args.Region.Name);
                else
                    args.Player.SendMessage(rt.EnterMsg, Color.White);
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

            if(rt.HasEvent(Events.Message) && !string.IsNullOrWhiteSpace(rt.Message) && rt.MsgInterval == 0) {
                args.Player.SendInfoMessage(rt.Message, args.Region.Name);
            }

            if(rt.HasEvent(Events.TempGroup) && rt.TempGroup != null && !args.Player.HasPermission("regiontrigger.bypass.tempgroup")) {
                if(rt.TempGroup == null)
                    ; // todo: send warning msg to the console
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

            if(rt.HasEvent(Events.Pvp) && !args.Player.TPlayer.hostile && !args.Player.HasPermission("regiontrigger.bypass.pvp")) {
                dt.Pvp = true;
                args.Player.TPlayer.hostile = true;
                NetMessage.SendData(30, -1, args.Player.Index, "", args.Player.Index); // todo: validate that
                args.Player.SendInfoMessage("Your PVP status is turned on by system!");
            }

            if(rt.HasEvent(Events.NoPvp)) {

            }
        }

        /// <summary>OnSecondUpdate - Called effectively every second for all time based checks.</summary>
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
                    NetMessage.SendData(30, -1, ply.Index, "", ply.Index); // todo: validate that
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
                ? "none"
                : args.Parameters[1];

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

            // todo: check flags
            if(RtRegions.AddRtRegion(region.ID, flags)) {
                args.Player.SendSuccessMessage("RtRegion {0} was defined successfully!", TShock.Utils.ColorTag(region.Name, Color.Cyan));
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
                        case "listevents":
                            #region ListEvents
                            int pageNumber;
                            if(!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pageNumber))
                                return;

                            PaginationTools.SendPage(args.Player, pageNumber, Events.EventsDescriptions.Select(kvp => $"{kvp.Key} -- {kvp.Value}").ToList(),
                                new PaginationTools.Settings {
                                    HeaderFormat = "Available events in RtRegions ({0}/{1}):",
                                    FooterFormat = "Type {0}setrt event listevents {{0}} for more events.".SFormat(Commands.Specifier)
                                }
                            );
                            #endregion
                            return;
                        case "add":
                            #region Add
                            #endregion
                            return;
                        case "del":
                            #region Delete
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
                case "help":
                    #region Help
                    {
                        int pageNumber;
                        if(!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;

                        var lines = new List<string>
                        {
                            "event <Region> - Lists all events of a RtRegion.",
                            "event add <Region> <Events> - Adds specific events to a RtRegion.",
                            "event del <Region> <Events> - Deletes events of a RtRegion.",
                            "projban add <Region> <projectile ID> - Disallows players in RtRegions from using a projectile.",
                            "projban del <Region> <projectile ID> - Allows players in RtRegions to use a projectile.",
                            "itemban add <Region> <Item ID/name> - Disallows players in RtRegions from using a item.",
                            "itemban del <Region> <Item ID/name> - Allows players in RtRegions to use a item.",
                            "help [page] - Lists all available helps of this command.",
                            "list [page] - Lists all RtRegions."
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
            }
        }
    }
}
