using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public string Events {
            get {
                return string.Join(",", _events);
            }
            set {
                _events.Clear();

                var es = value.Trim().ToLower().Split(',');
                for(int i = 0;i < es.Length;++i) {
                    var en = es[i].Trim();
                    if(global::RegionTrigger.Events.Contains(en))
                        _events.Add(en);
                    else
                        TShock.Log.ConsoleError("[RTrigger] Invaild event in region {0}({1}): event \"{2}\" does not exist.", TShock.Regions.GetRegionByID(RegionId).Name, RegionId, en);
                }
            }
        }

        public RtRegion(int id, int rid, List<string> events) {
            Id = id;
            RegionId = rid;
            _events.AddRange(events);
        }

        public bool HasEvent(string @event)
            => _events.Contains(@event);
    }
}
