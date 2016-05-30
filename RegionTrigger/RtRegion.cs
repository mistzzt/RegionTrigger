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
        //public 
        public string EnterMsg;
        public string LeaveMsg;
        public string Message;
        public int Interval;
        public List<int> Itembans;
        public List<int> Projbans;
        public List<int> Tilebans; 

        public Group Group;
        public Group TempGroup;
        
         

        public RtRegion(int id, int rid) {
            Id = id;
            RegionId = rid;
        }
    }
}
