using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RegionTrigger {
    class Events {
		[Description("Sends player a specific message when entering regions.")]
        public static readonly string EnterMsg = "entermsg"; // ok

		[Description("Sends player a specific message when leaving regions.")]
		public static readonly string LeaveMsg = "leavemsg"; // ok

		[Description("Sends player in regions a specific message.")]
		public static readonly string Message = "message"; // ok

		[Description("Alters players' tempgroups when they are in regions.")]
		public static readonly string TempGroup = "tempgroup"; // ok

		[Description("Disallows players in specific regions from using banned items.")]
		public static readonly string Itemban = "itemban";

		[Description("Disallows players in specific regions from using banned projectiles.")]
		public static readonly string Projban = "projban";

		[Description("Disallows players in specific regions from using banned tiles.")]
		public static readonly string Tileban = "tileban";
		
		[Description("Kills players in regions when they enter.")]
		public static readonly string Kill = "kill"; // ok

		[Description("Turns players' godmode on when they are in regions.")]
		public static readonly string Godmode = "godmode"; // ok

		[Description("Turns players' PVP status on when they are in regions.")]
		public static readonly string Pvp = "pvp"; // ok

		[Description("Disallows players from enabling their pvp mode.")]
		public static readonly string NoPvp = "nopvp"; // ok

		[Description("Disallows players without permissions from entering specific regions.")]
        public static readonly string Private = "private";

		[Description("Changes players' prefix when they are in regions.")]
        public static readonly string ThirdView = "thirdview";

        public static List<string> EventsList = new List<string>();
        public static Dictionary<string, string> EventsDescriptions = new Dictionary<string, string>();

        static Events() {
            Type t = typeof(Events);

            foreach(var fieldInfo in t.GetFields()
                .Where(f => f.IsPublic && f.FieldType == typeof(string))
                .OrderBy(f => f.Name)) {

                EventsList.Add((string)fieldInfo.GetValue(null));

                var descattr =
                    fieldInfo.GetCustomAttributes(false).FirstOrDefault(o => o is DescriptionAttribute) as DescriptionAttribute;
                var desc = !string.IsNullOrWhiteSpace(descattr?.Description) ? descattr.Description : "None";
                EventsDescriptions.Add(fieldInfo.Name, desc);
            }
        }

        internal static bool Contains(string @event)
            => EventsList.Contains(@event);
    }
}
