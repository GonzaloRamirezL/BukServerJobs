using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class Shift
    {
        public string StartTime { get; set; }
        public string ExitTime { get; set; }

        public string Type { get; set; }
        public string FixedShiftHours { get; set; }

        public string Ends { get; set; }


        public string Begins { get; set; }
        public string ShiftDisplay { get; set; }
    }
}
