using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Extensions;

namespace RegionTrigger
{
	internal static class Events
	{
		public static Dictionary<Event, string> EventsDescriptions = new Dictionary<Event, string>();

		public static KeyValuePair<string, Event>[] CnEvents;

		static Events()
		{
			var values = typeof(Event).GetEnumValues();
			var cnList = new List<KeyValuePair<string, Event>>();

			for (var index = 0; index < values.Length; index++)
			{
				var val = (Event)values.GetValue(index);
				var enumName = val.ToString();
				var fieldInfo = typeof(Event).GetField(enumName);

				var descattr =
					fieldInfo.GetCustomAttributes(false).FirstOrDefault(o => o is DescriptionAttribute) as DescriptionAttribute;
				var desc = !string.IsNullOrWhiteSpace(descattr?.Description) ? descattr.Description : "None";
				EventsDescriptions.Add(val, desc);

				var cnattr =
					fieldInfo.GetCustomAttributes(false).FirstOrDefault(o => o is CnNameAttribute) as CnNameAttribute;
				var cn = !string.IsNullOrWhiteSpace(cnattr?.Name) ? cnattr.Name : val.ToString();
				cnList.Add(new KeyValuePair<string, Event>(cn, val));
			}

			CnEvents = cnList.ToArray();
		}

		internal static Event ParseEvents(string eventString)
		{
			if (string.IsNullOrWhiteSpace(eventString))
				return Event.None;

			var @event = Event.None;

			var splitedEvents = eventString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var e in splitedEvents.Select(s => s.Trim()))
			{
				Event val;
				if (!Enum.TryParse(e, true, out val) && !TryParseCn(e, true, out val))
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
				if (!Enum.TryParse(e, true, out val) && !TryParseCn(e, true, out val))
				{
					sb.Append(e + ", ");
					continue;
				}
				@event |= val;
			}
			if (sb.Length != 0)
				invalids = sb.Remove(sb.Length - 2, 2).ToString();
			return @event;
		}

		internal static string InternalToCnName(Event value)
		{
			return CnEvents.SingleOrDefault(kvp => kvp.Value == value).Key;
		}

		internal static string InternalFlagsFormat(Event value)
		{
			var stringBuilder = new StringBuilder();

			foreach (var kvp in CnEvents)
			{
				if (kvp.Value == Event.None || !value.Has(kvp.Value))
					continue;

				stringBuilder.Append(kvp.Key + ", ");
			}

			if (stringBuilder.Length != 0)
				stringBuilder.Remove(stringBuilder.Length - 2, 2);
			else
				stringBuilder.Append("无");

			return stringBuilder.ToString();
		}

		private static bool TryParseCn(string value, bool ignoreCase, out Event result)
		{
			var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

			foreach (var kvp in CnEvents)
			{
				if (kvp.Key.Equals(value, comparison))
				{
					result = kvp.Value;
					return true;
				}
			}

			result = Event.None;
			return false;
		}
	}

	[Flags]
	internal enum Event
	{
		[CnName("无"), Description("Represents a event that does nothing. It can't be added.")]
		None = 0,

		[CnName("进入消息"), Description("Sends player a message when entering regions.")]
		EnterMsg = 1 << 0,

		[CnName("离去消息"), Description("Sends player a message when leaving regions.")]
		LeaveMsg = 1 << 1,

		[CnName("消息"), Description("Sends player in regions a message.")]
		Message = 1 << 2,

		[CnName("临时组"), Description("Alters players' tempgroups when they are in regions.")]
		TempGroup = 1 << 3,

		[CnName("禁物品"), Description("Disallows players in regions from using banned items.")]
		Itemban = 1 << 4,

		[CnName("禁抛射体"), Description("Disallows players in regions from using banned projectiles.")]
		Projban = 1 << 5,

		[CnName("禁物块"), Description("Disallows players in regions from using banned tiles.")]
		Tileban = 1 << 6,

		[CnName("杀"), Description("Kills players in regions when they enter.")]
		Kill = 1 << 7,

		[CnName("无敌"), Description("Turns players' godmode on when they are in regions.")]
		Godmode = 1 << 8,

		[Description("Turns players' PvP status on when they are in regions.")]
		Pvp = 1 << 9,

		[CnName("禁PvP"), Description("Disallows players from enabling their pvp mode.")]
		NoPvp = 1 << 10,

		[CnName("不变PvP"), Description("Disallows players from changing their pvp mode.")]
		InvariantPvp = 1 << 11,

		[CnName("私"), Description("Disallows players from entering regions.")]
		Private = 1 << 12,

		[CnName("权限"), Description("Temporary permissions for players in region.")]
		TempPermission = 1 << 13,

		[CnName("禁丢物品"), Description("Disallows players from dropping items")]
		NoItem = 1 << 14
	}
}
