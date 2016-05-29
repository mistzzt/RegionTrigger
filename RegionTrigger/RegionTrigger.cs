using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerrariaApi.Server;
using Terraria;
using TShockAPI;

namespace RegionTrigger
{
    public class RegionTrigger:TerrariaPlugin {
        public override string Name => "RegionEvent";
        public override string Author => "MistZZT";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Description => "Perform actions in regions where players are active.";


        public RegionTrigger(Main game) : base(game) { }
        public override void Initialize() {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
        }
    }
}
