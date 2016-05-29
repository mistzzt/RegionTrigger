using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegionTrigger {
    public class RtRegion {
        public readonly int Id;
        public readonly int RegionId;
        //public 
        public string TempGroup;
        public int[] NpcIds;

        public RtRegion(int id, int rid) {
            Id = id;
            RegionId = rid;
        }
    }
}
