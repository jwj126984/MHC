using System;
using System.Collections.Generic;

namespace MHC.Model
{
    public class FaultLogEntry
    {
        public DateTime Time { get; set; }
        
        public Dictionary<string, string> DisplayValues { get; set; }
        
        public Dictionary<string, bool> IsFaultColumn { get; set; }

        public FaultLogEntry()
        {
            Time = DateTime.Now;
            DisplayValues = new Dictionary<string, string>();
            IsFaultColumn = new Dictionary<string, bool>();
        }
    }
}