using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TShockAPI;

namespace RegionTrigger {
	class Events {
		[Description("Represents a event that does nothing. It can't be added to a region.")]
		public static readonly string None = "none"; // ok

		[Description("Sends player a specific message when entering regions.")]
		public static readonly string EnterMsg = "entermsg"; // ok

		[Description("Sends player a specific message when leaving regions.")]
		public static readonly string LeaveMsg = "leavemsg"; // ok

		[Description("Sends player in regions a specific message.")]
		public static readonly string Message = "message"; // ok

		[Description("Alters players' tempgroups when they are in regions.")]
		public static readonly string TempGroup = "tempgroup"; // ok

		[Description("Disallows players in specific regions from using banned items.")]
		public static readonly string Itemban = "itemban"; // ok

		[Description("Disallows players in specific regions from using banned projectiles.")]
		public static readonly string Projban = "projban"; // ok

		[Description("Disallows players in specific regions from using banned tiles.")]
		public static readonly string Tileban = "tileban"; // ok

		[Description("Kills players in regions when they enter.")]
		public static readonly string Kill = "kill"; // ok

		[Description("Turns players' godmode on when they are in regions.")]
		public static readonly string Godmode = "godmode"; // ok

		[Description("Turns players' PvP status on when they are in regions.")]
		public static readonly string Pvp = "pvp"; // ok

		[Description("Disallows players from enabling their pvp mode.")]
		public static readonly string NoPvp = "nopvp"; // ok

		[Description("(DONT WORK!)Disallows players from entering specific regions.")]
		public static readonly string Private = "private";

		[Description("(DONT WORK!)Only players in the same regions can chat with each other.")]
		public static readonly string RegionChat = "regionchat";

		[Description("(DONT WORK!)Changes players' prefix when they are in regions.")]
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
			=> !string.IsNullOrWhiteSpace(@event) && @event != None && EventsList.Contains(@event);

		/// <summary>
		/// Checks given events
		/// </summary>
		/// <param name="events">Events splited by ','</param>
		/// <returns>T1: Valid events & T2: Invalid events</returns>
		internal static Tuple<string, string> ValidateEvents(string events) {
			if(string.IsNullOrWhiteSpace(events))
				return new Tuple<string, string>(None, null);

			List<string> valid = new List<string>(),
				invalid = new List<string>();
			var splitedEvents = events.Trim().ToLower().Split(',');

			splitedEvents
				.Where(e => !string.IsNullOrWhiteSpace(e))
				.ForEach(e => {
					if(Contains(e))
						valid.Add(e);
					else
						invalid.Add(e);
				});

			var item1 = valid.Count != 0 ? string.Join(",", valid) : null;
			var item2 = invalid.Count != 0 ? string.Join(", ", invalid) : null;
			return new Tuple<string, string>(item1, item2);
		}

		/// <summary>
		/// Checks given events
		/// </summary>
		/// <param name="events">Events splited by ','</param>
		/// <returns>T1: Valid events & T2: Invalid events</returns>
		internal static Tuple<List<string>, List<string>> ValidateEventsList(string events) {
			if(string.IsNullOrWhiteSpace(events))
				return new Tuple<List<string>, List<string>>(new List<string> { None }, null);

			List<string> valid = new List<string>(),
				invalid = new List<string>();
			var splitedEvents = events.Trim().ToLower().Split(',');

			splitedEvents
				.Where(e => !string.IsNullOrWhiteSpace(e))
				.ForEach(e => {
					if(Contains(e))
						valid.Add(e);
					else
						invalid.Add(e);
				});

			var item1 = valid.Count != 0 ? valid : null;
			var item2 = invalid.Count != 0 ? invalid : null;
			return new Tuple<List<string>, List<string>>(item1, item2);
		}
	}
}
