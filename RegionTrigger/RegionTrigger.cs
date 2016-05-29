using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;
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
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
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
    }
}
