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

namespace RegionTrigger
{
    [ApiVersion(1,23)]
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
            if (disposing) {
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

            if(rt.HasEvent(Events.EnterMsg)) {
                if(string.IsNullOrWhiteSpace(rt.EnterMsg))
                    args.Player.SendInfoMessage("You have left region {0}", args.Region.Name);
                else
                    args.Player.SendMessage(rt.EnterMsg, Color.White);
            }
        }

        private void OnRegionEntered(RegionHooks.RegionEnteredEventArgs args) {
            var rt = RtRegions.GetRtRegionByRegionId(args.Region.ID);
            if (rt == null)
                return;

            if (rt.HasEvent(Events.EnterMsg)) {
                if(string.IsNullOrWhiteSpace(rt.EnterMsg))
                    args.Player.SendInfoMessage("You have entered region {0}", args.Region.Name);
                else
                    args.Player.SendMessage(rt.EnterMsg, Color.White);
            }

            if (rt.HasEvent(Events.Message) && !string.IsNullOrWhiteSpace(rt.Message) && rt.MsgInterval == 0) {
                args.Player.SendInfoMessage(rt.Message, args.Region.Name);
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
                    
                    if (dt.MsgCd < rt.MsgInterval) {
                        dt.MsgCd++;
                        
                    }
                    else {
                        ply.SendInfoMessage(rt.Message);
                        dt.MsgCd = 0;
                    }
                }
            }
        }

        private void DefineRtRegion(CommandArgs args) {
            // defrt <Region name> [Flags..]
            if (args.Parameters.Count == 0 || args.Parameters.Count > 2) {
                args.Player.SendErrorMessage("Invaild syntax! Proper syntax: /defrt <Region name> [flags...]");
                args.Player.SendErrorMessage("Use {0} to get available events.",TShock.Utils.ColorTag("/setrt events", Color.Cyan));
                return;
            }

            string regionName = args.Parameters[0];
            string flags = args.Parameters.Count == 1 || string.IsNullOrWhiteSpace(args.Parameters[1])
                ? "none"
                : args.Parameters[1];

            var region = TShock.Regions.GetRegionByName(regionName);
            if (region == null) {
                args.Player.SendErrorMessage("Invaild region name!");
                return;
            }
            if (RtRegions.GetRtRegionByRegionId(region.ID) != null) {
                args.Player.SendErrorMessage("RtRegion already exists!");
                args.Player.SendErrorMessage("Use {0} to set events for regions.", TShock.Utils.ColorTag("/setrt add/set <RtRegion name> <flags>", Color.Cyan));
                return;
            }

            // todo: check flags
            if (RtRegions.AddRtRegion(region.ID, flags)) {
                args.Player.SendSuccessMessage("RtRegion {0} was defined successfully!", TShock.Utils.ColorTag(region.Name, Color.Cyan));
            }
        }
    }
}
