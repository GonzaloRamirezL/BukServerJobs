using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class TimeIntervalColombia : TimeInterval
    {
        public SurchargeColombia Surcharge { get; set; }
        public string CompensatedDay { get; set; }
        public string AdditionalTimeBefore { get; set; }
        public string AdditionalTimeAfter { get; set; }

        public string OrdinaryDiurnalOvertime { get; set; }
        public string OrdinaryNocturnalOvertime { get; set; }

        public string SundayDiurnalOvertime { get; set; }
        public string SundayNocturnalOvertime { get; set; }

        public string HolidayDiurnalOvertime { get; set; }
        public string HolidayNocturnalOvertime { get; set; }

        public Shift PlannedShift { get; set; }
    }
}
