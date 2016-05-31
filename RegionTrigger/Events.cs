using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace RegionTrigger {
    class Events {
        public static readonly string EnterMsg = "entermsg"; // ok
        public static readonly string LeaveMsg = "leavemsg"; // ok
        public static readonly string Message = "message"; // ok
        public static readonly string TempGroup = "tempgroup"; // ok
        public static readonly string Itemban = "itemban";
        public static readonly string Projban = "projban";
        public static readonly string Tileban = "tileban";
        public static readonly string Kill = "kill"; // ok
        public static readonly string Godmode = "godmode"; // ok
        public static readonly string Pvp = "pvp"; // ok
        public static readonly string NoPvp = "nopvp"; // ok
        public static readonly string Private = "private";
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
