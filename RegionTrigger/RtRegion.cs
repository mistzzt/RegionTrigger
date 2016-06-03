using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace RegionTrigger {
	public class RtRegion {
		public readonly int Id;
		public readonly int RegionId;
		public string EnterMsg;
		public string LeaveMsg;
		public string Message;
		public int MsgInterval;
		public List<int> Itembans;
		public List<int> Projbans;
		public List<int> Tilebans;

		public Group Group;
		public Group TempGroup;
		private readonly List<string> _events = new List<string>();
		public IReadOnlyList<string> EventsList => _events.AsReadOnly();

		public string Events {
			get {
				return string.Join(",", _events);
			}
			set {
				_events.Clear();
				var es = value.Trim().ToLower().Split(',');
				foreach(var e in es.Where(e => !string.IsNullOrWhiteSpace(e)))
					_events.Add(e.Trim());
			}
		}

		public RtRegion(int id, int rid) {
			Id = id;
			RegionId = rid;
		}

		public bool HasEvent(string @event)
			=> _events.Contains(@event);
	}
}
