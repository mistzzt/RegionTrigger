using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace RegionTrigger
{
	internal static class Events
	{
		public static Dictionary<string, string> EventsDescriptions = new Dictionary<string, string>();

		static Events()
		{
			foreach (var enumName in typeof(Event).GetEnumNames())
			{
				var fieldInfo = typeof(Event).GetField(enumName);

				var descattr =
					fieldInfo.GetCustomAttributes(false).FirstOrDefault(o => o is DescriptionAttribute) as DescriptionAttribute;
				var desc = !string.IsNullOrWhiteSpace(descattr?.Description) ? descattr.Description : "None";
				EventsDescriptions.Add(fieldInfo.Name, desc);
			}
		}

		internal static Event ParseEvents(string eventString)
		{
			if (string.IsNullOrWhiteSpace(eventString))
				return Event.None;

			var @event = Event.None;

			var splitedEvents = eventString.Split(new [] {','}, StringSplitOptions.RemoveEmptyEntries);
			foreach (var e in splitedEvents.Select(s => s.Trim()))
			{
				Event val;
				if (!Enum.TryParse(e, true, out val))
				{
					continue;
				}
				@event |= val;
			}
			return @event;
		}

		internal static Event ValidateEventWhenAdd(string eventString, out string invalids)
		{
			invalids = null;

			if (string.IsNullOrWhiteSpace(eventString))
				return Event.None;

			var @event = Event.None;

			var splitedEvents = eventString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var sb = new StringBuilder();
			foreach (var e in splitedEvents.Select(s => s.Trim()))
			{
				Event val;
				if (!Enum.TryParse(e, true, out val))
				{
					sb.Append(e + ", ");
					continue;
				}
				@event |= val;
			}
			if(sb.Length != 0)
				invalids = sb.Remove(sb.Length - 2, 2).ToString();
			return @event;
		}
	}

	[Flags]
	internal enum Event
	{
		[Description("Represents a event that does nothing. It can't be added.")]
		None = 0,

		[Description("Sends player a message when entering regions.")]
		EnterMsg = 1 << 0,

		[Description("Sends player a message when leaving regions.")]
		LeaveMsg = 1 << 1,

		[Description("Sends player in regions a message.")]
		Message = 1 << 2,

		[Description("Alters players' tempgroups when they are in regions.")]
		TempGroup = 1 << 3,

		[Description("Disallows players in regions from using banned items.")]
		Itemban = 1 << 4,

		[Description("Disallows players in regions from using banned projectiles.")]
		Projban = 1 << 5,

		[Description("Disallows players in regions from using banned tiles.")]
		Tileban = 1 << 6,

		[Description("Kills players in regions when they enter.")]
		Kill = 1 << 7,

		[Description("Turns players' godmode on when they are in regions.")]
		Godmode = 1 << 8,

		[Description("Turns players' PvP status on when they are in regions.")]
		Pvp = 1 << 9,

		[Description("Disallows players from enabling their pvp mode.")]
		NoPvp = 1 << 10,

		[Description("Disallows players from changing their pvp mode.")]
		InvariantPvp = 1 << 11,

		[Description("Disallows players from entering regions.")]
		Private = 1 << 12,

		[Description("Temporary permissions for players in region.")]
		TempPermission = 1 << 13,

		[Description("Disallows players from dropping items")]
		NoItem = 1 << 14
	}
}
