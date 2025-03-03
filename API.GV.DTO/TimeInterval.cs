using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class TimeInterval
    {
        public string Date { get; set; }
        
        public string CalculatedIntervalDate { get; set; }

        public List<Punch> Punches { get; set; }
        public List<Shift> Shifts { get; set; }


        public string Delay { get; set; }
        public string BreakDelay { get; set; }
        public string EarlyLeave { get; set; }

        public string DelayTimeAfterCompensation { get; set; }
        public string EarlyLeaveTimeAfterCompensation { get; set; }

        public List<TimeOff> TimeOffs { get; set; }

        public string WorkedHours { get; set; }

        public string Absent { get; set; }
        public string Holiday { get; set; }
        public string Worked { get; set; }

        
        public string WorkedBHours { get; set; }
        
        public string NonWorkedBHours { get; set; }
        
        public string NonWorkedHours { get; set; }
        
        public string RealWorkedHours { get; set; }
        
        public string NocturnalWorkedHours { get; set; }
        
        public string DiurnalWorkedHours { get; set; }
        
        public string ConsidersExtraTimeBefore { get; set; }
        
        public string ConsidersExtraTimeAfter { get; set; }
        
        public string CoveredNonWorkingHours { get; set; }
        
        public string AuthorizedExtraTimeBefore { get; set; }
        
        public string AuthorizedExtraTimeAfter { get; set; }
        
        public string TotalAuthorizedExtraTime { get; set; }
       
        public Dictionary<string, string> AccomplishedExtraTimeBefore { get; set; }
        
        public Dictionary<string, string> AccomplishedExtraTimeAfter { get; set; }
        
        public Dictionary<string, string> NocturnalAccomplishedExtraTimeBefore { get; set; }
        
        public Dictionary<string, string> NocturnalAccomplishedExtraTimeAfter { get; set; }
        
        public Dictionary<string, string> AccomplishedExtraTime { get; set; }
    }
}
