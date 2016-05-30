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

            //output for testing
            for (int b = 1; b < 1 << 5; b <<= 1) {
                Console.WriteLine(b);
            }
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

            // database
            RtRegions = new RtRegionManager(TShock.DB);
        }

        private static void OnJoin(JoinEventArgs args) {
            TShock.Players[args.Who].SetData(Rtdataname, new RtPlayer());
        }

        private static void OnLeave(LeaveEventArgs args) {
            TShock.Players[args.Who].RemoveData(Rtdataname);
        }

        private static void OnUpdate(EventArgs args) {
            
        }

        private void OnRegionDeleted(RegionHooks.RegionDeletedEventArgs args) {
            
        }

        private void OnRegionLeft(RegionHooks.RegionLeftEventArgs args) {
            args.Player.SendInfoMessage("You have left region {0}", args.Region.Name);
        }

        private void OnRegionEntered(RegionHooks.RegionEnteredEventArgs args) {
            args.Player.SendInfoMessage("You have entered region {0}", args.Region.Name);


        }

        private void DefineRtRegion(CommandArgs args) {
            // defrt <Region name> [Flags..]
            if (args.Parameters.Count == 0 || args.Parameters.Count > 2) {
                args.Player.SendErrorMessage("Invaild syntax! Proper syntax: /defrt <Region name> [flags...]");
                args.Player.SendErrorMessage("Use {0} to get available events.",TShock.Utils.ColorTag("/setrt events", Color.Cyan));
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
            }

            // todo: check flags
            if (RtRegions.AddRtRegion(region.ID, flags)) {
                args.Player.SendSuccessMessage("RtRegion {0} was defined successfully!", TShock.Utils.ColorTag(region.Name, Color.Cyan));
            }
        }
    }
}
